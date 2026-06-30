using System.Globalization;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? throw new InvalidOperationException("Задай BOT_TOKEN в терминале");
var bot = new TelegramBotClient(token);
using var cts = new CancellationTokenSource();

var remindersFile = Path.Combine(AppContext.BaseDirectory, "reminders.json");
var reminders = LoadReminders(remindersFile);

var nextId = reminders.Count > 0 ? reminders.Max(r => r.Id) + 1 : 1;

_ = Task.Run(() => CheckRemindersLoop(bot, reminders, remindersFile, cts.Token));

bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions{AllowedUpdates = []},
cts.Token);

var me = await bot.GetMe(cts.Token);
Console.WriteLine($"ReminderBot @{me.Username} запущен.");
Console.WriteLine($"Напоминании в базе: {reminders.Count(r => !r.Sent)}");
Console.WriteLine("Нажми Enter для остановки");

Console.ReadLine();
SaveReminders(remindersFile, reminders);
cts.Cancel();

async Task HandleUpdateAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken)
{
    if(update.Message is not {Text : { } text} message)
    return;

    var chatId = message.Chat.Id;

    if(text == "/start")
    {
        await botClient.SendMessage(
            chatId,
            "⏰ ReminderBot — бот-напоминания\n\n" +
            "Команды:\n" +
            "/remind 28.06 15:00 Текст — создать напоминание\n" +
            "/list — мои напоминания\n" +
            "/cancel 1 — отменить №1 из списка\n" +
            "/help — справка",
            cancellationToken: cancellationToken
        );
        return;
    }

    if(text == "/help")
    {
        await botClient.SendMessage(
            chatId,
            "Формат:\n" +
            "/remind DD.MM HH:mm Текст\n\n" +
            "Пример:\n" +
            "/remind 28.06 15:00 Стрижка",
            cancellationToken: cancellationToken
        );
        return;
    }

    if(text.StartsWith("/remind", StringComparison.OrdinalIgnoreCase))
    {
        var args = text["/remind".Length..].Trim();

        var parts = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        
        if(parts.Length < 3)
        {
            await botClient.SendMessage(
                chatId,
                "❌ Неверный формат.\nПример: /remind 28.06 15:00 Стрижка",
                cancellationToken: cancellationToken            
            );
            return;
        }

        var datePart = parts[0];
        var timePart = parts[1];
        var reminderText = parts[2];

        if(!TryParseDateTime(datePart, timePart,out var when))
        {
            await botClient.SendMessage(
                chatId,
                "❌ Не понял дату или время.\nПример: /remind 28.06 15:00 Стрижка",
                cancellationToken: cancellationToken
            );
            return;
        }

        if(when <= DateTime.Now)
        {
            await botClient.SendMessage(
                chatId,
                "❌ Время должно быть в будущем.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var reminder = new Reminder
        {
            Id = nextId++,
            ChatId = chatId,
            When = when,
            Text = reminderText,
            Sent = false
        };

        reminders.Add(reminder);
        SaveReminders(remindersFile, reminders);

        await botClient.SendMessage(
            chatId,
            $"✅ Напоминание #{reminder.Id} создано\n" +
            $"📅 {when:dd.MM.yyyy HH:mm}\n" +
            $"💬 {reminderText}",
            cancellationToken: cancellationToken
        );
        return;
    }

    if(text == "/list")
    {
        var myReminders = reminders
        .Where(r => r.ChatId == chatId && !r.Sent)
        .OrderBy(r => r.When)
        .ToList();

        if(myReminders.Count == 0)
        {
            await botClient.SendMessage(chatId, "Список пуст.", cancellationToken:cancellationToken);
            return;
        }

        var lines = myReminders.Select(r =>
        $"#{r.Id} — {r.When:dd.MM HH:mm} — {r.Text}");

        await botClient.SendMessage(
            chatId,
            "📋 Ваши напоминания:\n" + string.Join("\n", lines),
            cancellationToken: cancellationToken
        );
        return;
    }

    if(text.StartsWith("/cancel", StringComparison.OrdinalIgnoreCase))
    {
        var idPart = text["/cancel".Length..].Trim();
        if(!int.TryParse(idPart, out var id))
        {
            await botClient.SendMessage(
                chatId,
                "❌ Укажите номер. Пример: /cancel 1",
                cancellationToken: cancellationToken
            );
            return;
        }

        var reminder = reminders.FirstOrDefault(r => r.Id == id && r.ChatId == chatId && !r.Sent);
        if(reminder is null)
        {
            await botClient.SendMessage(
                chatId,
                $"❌ Напоминание #{id} не найдено.",
                cancellationToken: cancellationToken
            );
            return;
        }
        reminders.Remove(reminder);
        SaveReminders(remindersFile, reminders);

        await botClient.SendMessage(
            chatId,
            $"🗑 Напоминание #{id} отменено.",
            cancellationToken: cancellationToken
        );
    }
}

async Task CheckRemindersLoop(
    ITelegramBotClient botClient,
    List<Reminder> list,
    string filePath,
    CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            var now = DateTime.Now;
            var due = list.Where(r => !r.Sent && r.When <= now).ToList();

            foreach(var r in due)
            {
                await botClient.SendMessage(
                    r.ChatId,
                    $"⏰ Напоминание #{r.Id}:\n{r.Text}",
                    cancellationToken: cancellationToken
                );
                r.Sent = true;
            }

            if(due.Count > 0)
                SaveReminders(filePath, list);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Ошибка в цикле напоминаний: {ex.Message}");
        }
        await Task.Delay(30_000, cancellationToken);
    }
}

bool TryParseDateTime(string datePart, string timePart, out DateTime result)
{
    result = default;
    var withYear = $"{datePart}.{DateTime.Now.Year} {timePart}";
    
    if(DateTime.TryParseExact(withYear, "dd.MM.yyyy HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
    {
        if(result < DateTime.Now)
            result = result.AddYears(1);
        return true;
    }

    withYear = $"{datePart} {timePart}";
    return DateTime.TryParseExact(withYear, "dd.MM.yyyy HH:mm",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out result); 
}

List<Reminder> LoadReminders(string path)
{
    if(!File.Exists(path))
        return new List<Reminder>();

    try
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Reminder>>(json) ?? new List<Reminder>();
    }
    catch
    {
        return new List<Reminder>();
    }
}

void SaveReminders(string path, List<Reminder> list)
{
    var json = JsonSerializer.Serialize(list, new JsonSerializerOptions{WriteIndented = true});
    File.WriteAllText(path, json);
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return Task.CompletedTask;
}

class Reminder
{
    public int Id {get; set;}
    public long ChatId{get; set;}
    public DateTime When {get; set;}
    public string Text {get; set;} = "";
    public bool Sent {get; set;}
}