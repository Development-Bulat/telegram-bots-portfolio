# telegram-bots-portfolio

Portfolio of Telegram bots built with **C#** and **.NET**.  
Two demo bots for small business: FAQ helper and lead capture with admin notifications.

## Live demo

| Bot | Purpose | Link |
|-----|---------|------|
| **BusinessFaqBot** | FAQ menu with inline buttons (prices, hours, contacts) | [@faq007_bot](https://t.me/faq007_bot) |
| **LeadBot** | Step-by-step lead form → notification to business owner | [@lead07_bot](https://t.me/lead07_bot) |

> Bots are online when the server is running. Open a link and send `/start`.

## Projects

### BusinessFaqBot
- Commands: `/start`, `/help`
- Inline keyboard: prices, working hours, contacts, services
- Good for: salons, freelancers, small shops

### LeadBot
- Commands: `/start`, `/cancel`
- Flow: name → phone/contact → request text
- Sends a formatted lead to the admin in Telegram
- Good for: capturing customer requests without a website

## Stack

- C# / .NET
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot)
- Polling (local run)

## Project structure

```
tg_bots/
├── BusinessFaqBot/
├── LeadBot/
├── TgBots.slnx
└── README.md
```

## Run locally

### Requirements

- [.NET SDK 8+](https://dotnet.microsoft.com/download)
- Bot token from [@BotFather](https://t.me/BotFather)
- Your Telegram user ID from [@userinfobot](https://t.me/userinfobot) (for LeadBot admin)

### BusinessFaqBot

```bash
cd BusinessFaqBot
export BOT_TOKEN="your_token_from_BotFather"
dotnet run
```

### LeadBot

```bash
cd LeadBot
export BOT_TOKEN="your_token_from_BotFather"
export ADMIN_CHAT_ID="your_telegram_user_id"
dotnet run
```

**Do not commit tokens.** Use environment variables only.

## Author

GitHub: [Development-Bulat](https://github.com/Development-Bulat)

Telegram bots for business — FAQ, lead forms, custom automation.
