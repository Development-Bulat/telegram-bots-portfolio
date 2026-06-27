using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? throw new InvalidOperationException("Задай переменную BOT_TOKEN в терминале");

var bot = new TelegramBotClient(token);

using var cts = new CancellationTokenSource();

bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions{AllowedUpdates = []}, cts.Token);

var me = await bot.GetMe(cts.Token);
Console.WriteLine($"Бот @{me.Username} запущен. Нажми Enter для остановки.");
Console.ReadLine();
cts.Cancel();

static async Task HandleUpdateAsync(
    ITelegramBotClient bot,
    Update update,
    CancellationToken ct)
{
    if(update.CallbackQuery is { } callback)
    {
        var answer = callback.Data switch
        {
            "prices" => "💰 Цены:\n• Telegram-бот — от 5000 ₽\n• Лендинг — от 8000 ₽\n• Консультация — 2000 ₽",
            "hours" => "🕐 Режим работы:\nПн–Пт: 10:00–19:00\nСб: 11:00–16:00\nВс: выходной",
            "contact" => "📞 Контакты:\nTelegram: @твой_ник\nEmail: example@mail.com",
            "services" => "🛠 Услуги:\n• Telegram-боты\n• Сайты и лендинги\n• Автоматизация таблиц",
            _ => "Неизвестная команда"
        };
        await bot.AnswerCallbackQuery(callback.Id,cancellationToken: ct);
        await bot.SendMessage(callback.Message!.Chat.Id, answer, cancellationToken: ct);
        return;
    }
    if(update.Message is not {Text: { } text} message)
        return;
    
    var chatId = message.Chat.Id;
    if(text == "/start")
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] {InlineKeyboardButton.WithCallbackData("💰 Цены", "prices")},
            new[] {InlineKeyboardButton.WithCallbackData("🕐 Часы работы", "hours")},
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📞 Контакты", "contact"),
                InlineKeyboardButton.WithCallbackData("🛠 Услуги", "services"),
            },
        });
        await bot.SendMessage(chatId, "Привет! Выбери раздел:", replyMarkup: keyboard, cancellationToken: ct);
            return;
    }

    if(text == "/help")
    {
        await bot.SendMessage(chatId, "/start - меню\n/help - справка", cancellationToken: ct);
    }
}

static Task HandleErrorAsync(
    ITelegramBotClient bot,
    Exception ex,
    CancellationToken ct)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return Task.CompletedTask;
}

