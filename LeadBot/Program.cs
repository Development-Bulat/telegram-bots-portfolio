using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? throw new InvalidOperationException("Задай BOT_TOKEN в терминале");

var adminChatIdStr = Environment.GetEnvironmentVariable("ADMIN_CHAT_ID")
    ?? throw new InvalidOperationException("Задай ADMIN_CHAT_ID в терминале");

var adminChatId = long.Parse(adminChatIdStr);

var userStep = new Dictionary<long, string>();

var draftName = new Dictionary<long, string>();
var draftPhone = new Dictionary<long, string>();

var bot = new TelegramBotClient(token);

using var cts = new CancellationTokenSource();

bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions {AllowedUpdates = []},
cts.Token);

var me = await bot.GetMe(cts.Token);
Console.WriteLine($"LeadBot @{me.Username} запущен");
Console.WriteLine($"Нажми Enter для остановки.");

Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(
    ITelegramBotClient botClient,
    Update update,
    CancellationToken cancellationToken)
{
    if(update.CallbackQuery is { } callback)
    {
        var chatId = callback.Message!.Chat.Id;
        if(callback.Data == "new_lead")
        {
            userStep[chatId] = "name";
            await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);
            await botClient.SendMessage(
                chatId,
                "📝 Оставить заявку\n\nКак вас зовут?",
                cancellationToken: cancellationToken
            );
        }

        return;
    }

    if(update.Message is not {Text: { } text} message)
        return;
    
    var userChatId = message.Chat.Id;

    if(text == "/start")
    {
        ClearDraft(userChatId);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] {InlineKeyboardButton.WithCallbackData("📝 Оставить заявку", "new_lead")},
        });

        await botClient.SendMessage(
            userChatId,
            "Привет! Я бот для приёма заявок.\n\n" +
            "Нажми кнопку ниже или /cancel чтобы отменить.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
        return;
    }

    if(text == "/cancel")
    {
        ClearDraft(userChatId);
        await botClient.SendMessage(
            userChatId,
             "❌ Заявка отменена.\n/start — начать снова.",
             cancellationToken: cancellationToken
        );
        return;
    }

    if(!userStep.TryGetValue(userChatId, out var step))
        return;

    switch (step)
    {
        case "name":
            draftName[userChatId] = text.Trim();
            userStep[userChatId] = "phone";
            await botClient.SendMessage(userChatId, "📞 Укажите телефон или @username:", cancellationToken: cancellationToken);
            break;
        case "phone":
        draftPhone[userChatId] = text.Trim();
        userStep[userChatId] = "message";
        await botClient.SendMessage(userChatId, "💬 Опишите, что вам нужно:", cancellationToken: cancellationToken);
        break;
        case "message":
        var name = draftName.GetValueOrDefault(userChatId, "-");
        var phone = draftPhone.GetValueOrDefault(userChatId, "-");
        var requestText = text.Trim();

        var adminMessage = 
                "🆕 Новая заявка!\n\n" +
                $"👤 Имя: {name}\n" +
                $"📞 Контакт: {phone}\n" +
                $"💬 Запрос: {requestText}\n\n" +
                $"🆔 User ID: {userChatId}";

        await botClient.SendMessage(adminChatId, adminMessage, cancellationToken: cancellationToken);

        await botClient.SendMessage(userChatId, "✅ Заявка принята! Мы свяжемся с вами в ближайшее время.\n\n" + "/start — оставить новую заявку", cancellationToken: cancellationToken);

        ClearDraft(userChatId);
        break;
    }
}

Task HandleErrorAsync(
    ITelegramBotClient botClient,
    Exception exception,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
    return Task.CompletedTask;
}

void ClearDraft(long chatId)
{
    userStep.Remove(chatId);
    draftName.Remove(chatId);
    draftPhone.Remove(chatId);
}