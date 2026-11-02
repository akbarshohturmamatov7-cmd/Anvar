using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    static readonly string TOKEN =
       Environment.GetEnvironmentVariable("BOT_TOKEN")
       ?? throw new Exception("BOT_TOKEN topilmadi");
    const string CHANNEL = "@usmonovkanali";
    const long ADMIN_ID = 8635381265;

    static TelegramBotClient bot = new TelegramBotClient(TOKEN);

    static Dictionary<long, (int win, int lose, int draw)> scores = new();

    static async Task Main()
    {
        Console.WriteLine("Bot ishga tushmoqda...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        bot.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions
        );

        Console.WriteLine("Bot ishga tushdi!");
        Console.ReadLine();
    }

    static async Task<bool> IsSubscribed(long userId)
    {
        try
        {
            var member = await bot.GetChatMember(
                CHANNEL,
                userId
            );

            return member.Status == ChatMemberStatus.Member ||
                   member.Status == ChatMemberStatus.Administrator ||
                   member.Status == ChatMemberStatus.Creator;
        }
        catch
        {
            return false;
        }
    }

    static async Task ShowSubscription(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl(
                        "📢 Kanalga obuna bo'lish",
                        "https://t.me/usmonovkanali"
                    )
                }
            }
        );

        await bot.SendMessage(
            chatId,
            "🎮 O'yinni boshlash uchun kanalimizga obuna bo'ling!\n\n" +
            "Obuna bo'lgach, «🔎 Obunani tekshirish» tugmasini bosing.",
            replyMarkup: keyboard
        );

        var checkKeyboard = new ReplyKeyboardMarkup(
            new[]
            {
                new KeyboardButton[] { "🔎 Obunani tekshirish" }
            })
        {
            ResizeKeyboard = true
        };

        await bot.SendMessage(
            chatId,
            "Obunani tekshirish uchun tugmani bosing:",
            replyMarkup: checkKeyboard
        );
    }

    static ReplyKeyboardMarkup MainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "🪨 Tosh", "✂️ Qaychi" },
            new KeyboardButton[] { "📄 Qog'oz", "🏆 Hisobim" }
        })
        {
            ResizeKeyboard = true
        };
    }

    static async Task HandleUpdate(
        ITelegramBotClient client,
        Update update,
        CancellationToken ct)
    {
        if (update.Message?.Text == null)
            return;

        long id = update.Message.Chat.Id;
        long userId = update.Message.From!.Id;
        string text = update.Message.Text;

        if (text == "/start")
        {
            if (!await IsSubscribed(userId))
            {
                await ShowSubscription(id);
                return;
            }

            await bot.SendMessage(
                id,
                "🎮 Xush kelibsiz!\nO'yinni tanlang:",
                replyMarkup: MainMenu(),
                cancellationToken: ct
            );

            return;
        }

        if (text == "/admin")
        {
            if (userId != ADMIN_ID)
            {
                await bot.SendMessage(id, "⛔ Siz admin emassiz!");
                return;
            }

            int total = scores.Count;
            int wins = scores.Values.Sum(x => x.win);
            int loses = scores.Values.Sum(x => x.lose);
            int draws = scores.Values.Sum(x => x.draw);

            await bot.SendMessage(
                id,
                $"👑 ADMIN PANEL\n\n" +
                $"👥 O'yinchilar: {total}\n" +
                $"🏆 Yutuqlar: {wins}\n" +
                $"❌ Mag'lubiyatlar: {loses}\n" +
                $"🤝 Duranglar: {draws}"
            );

            return;
        }

        if (!await IsSubscribed(userId))
        {
            await ShowSubscription(id);
            return;
        }

        if (!scores.ContainsKey(userId))
            scores[userId] = (0, 0, 0);

        if (text == "🔎 Obunani tekshirish")
        {
            await bot.SendMessage(
                id,
                "✅ Obunangiz tasdiqlandi!\nO'yinni boshlashingiz mumkin.",
                replyMarkup: MainMenu(),
                cancellationToken: ct
            );
            return;
        }

        if (text == "🏆 Hisobim")
        {
            var s = scores[userId];

            await bot.SendMessage(
                id,
                $"🏆 SIZNING NATIJALARINGIZ\n\n" +
                $"✅ Yutuq: {s.win}\n" +
                $"❌ Mag'lubiyat: {s.lose}\n" +
                $"🤝 Durang: {s.draw}",
                cancellationToken: ct
            );

            return;
        }

        string userChoice = text switch
        {
            "🪨 Tosh" => "Tosh",
            "✂️ Qaychi" => "Qaychi",
            "📄 Qog'oz" => "Qog'oz",
            _ => ""
        };

        if (userChoice == "")
            return;

        // Bot foydalanuvchining tanloviga qarshi yuradi
        string botChoice = userChoice switch
        {
            "Tosh" => "Qog'oz",
            "Qaychi" => "Tosh",
            _ => "Qaychi"
        };

        scores[userId] = (
            scores[userId].win + 1,
            scores[userId].lose,
            scores[userId].draw
        );

        await bot.SendMessage(
            id,
            $"👤 Siz: {userChoice}\n" +
            $"🤖 Bot: {botChoice}\n\n" +
            "🏆 Bot g'alaba qozondi!",
            replyMarkup: MainMenu(),
            cancellationToken: ct
        );
    }

    static Task HandleError(
        ITelegramBotClient client,
        Exception exception,
        CancellationToken ct)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }
}