namespace cinemarcer_console.Services;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Movies;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

public class UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, ShowtimeAggregator showtimeAggregator) : IUpdateHandler
{
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message }                        => OnMessage(message),
            { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        if (msg.Type == MessageType.Location)
        {
            await OnLocation(msg);
            return;
        }
        if (msg.Text is not { } messageText)
            return;

        var sentMessage = await (messageText.Split(' ')[0] switch
        {
            Commands.MOVIES => GetMoviesData(msg),
            Commands.LOCATION => GetLocation(msg),
            _ => Usage(msg)
        });
        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
    }

    private async Task<Message> GetLocation(Message msg)
    {
        //todo ask user to send location
        const string message = """
                               Сейчас я умею искать кино только в Нячанге! Другие города в процессе добавления!
                               """;
        await bot.SendMessage(msg.Chat, message, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        throw new NotImplementedException();
    }

    private async Task<Message> OnLocation(Message msg)
    {
        //todo save user location
        const string message = """
                               Локация получена! Ищу кинчики...
                               """;
        await bot.SendMessage(msg.Chat, message, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
        return await GetMoviesData(msg);
    }

    private async Task<Message> GetMoviesData(Message msg)
    {
        //мы знаем локацию юзера?
        //если нет - спрашиваем, как только мы получаем его локацию - продолжаем этот метод.
        if (!UserLocationIsKnown(out string location))
        {
            throw new ShowtimeAggregator.UnknownLocationException();
        }
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        // todo достаем запрос из кеша по геолокации
        var moviesInLocation = showtimeAggregator.GetMoviesInLocation(location);
        logger.Log(LogLevel.Information, $"[StopWatch] showtimeAggregator.GetMoviesInLocation --- {stopwatch.ElapsedMilliseconds}");
        stopwatch.Restart();
        var enumerable = showtimeAggregator.Aggregate(moviesInLocation); //todo ускорить надо здесь
        logger.Log(LogLevel.Information, $"[StopWatch] showtimeAggregator.Aggregate --- {stopwatch.ElapsedMilliseconds}");
        stopwatch.Restart();
        var result = TelegramMessagePrinter.Build(enumerable, location);
        logger.Log(LogLevel.Information, $"[StopWatch] TelegramMessagePrinter.Build --- {stopwatch.ElapsedMilliseconds}");
        stopwatch.Restart();
        await SendPosters(msg.Chat, result.PosterUrls);
        logger.Log(LogLevel.Information, $"[StopWatch] SendPosters --- {stopwatch.ElapsedMilliseconds}");
        stopwatch.Restart();
        Task<Message> messageTask = bot.SendMessage(msg.Chat, result.Text, parseMode: ParseMode.MarkdownV2,
            replyMarkup: new ReplyKeyboardRemove());
        var message = await messageTask;
        logger.Log(LogLevel.Information, $"[StopWatch] bot.SendMessage --- {stopwatch.ElapsedMilliseconds}");
        stopwatch.Stop();
        /// присылаем пользователю результат.
        return message;
    }

    private async Task SendPosters(Chat chat, IReadOnlyList<string> posterUrls)
    {
        if (posterUrls.Count == 0) return;

        try
        {
            // sendMediaGroup caps out at 10 photos per call, so split into multiple albums when there are more.
            foreach (var chunk in posterUrls.Chunk(10))
            {
                if (chunk.Length == 1)
                    await bot.SendPhoto(chat, InputFile.FromUri(chunk[0]));
                else
                    await bot.SendMediaGroup(chat, chunk.Select(url => new InputMediaPhoto(InputFile.FromUri(url))));
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Problem with sending posters");
        }
    }

    private bool UserLocationIsKnown(out string s)
    {
        s = "@12.240994,109.196397,15.1z"; //todo refactor
        return true;
    }

    async Task<Message> Usage(Message msg)
    {
        const string usage = """
                <b><u>Bot menu</u></b>:
                /movies      - aggregate info about movies nearby
                /location    - tell this bot where to look for cinemas
            """;
        return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow()
                .AddButton("WithCallbackData", "CallbackData")
                .AddButton(new InlineKeyboardButton("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        return await bot.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow().AddButton("2.1").AddButton("2.2");
        return await bot.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
    }

    async Task<Message> RemoveKeyboard(Message msg)
    {
        return await bot.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    
    async Task<Message> RequestLocation(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddButton(new("Location", requestLocation: true));
        return await bot.SendMessage(msg.Chat, "Where are you?" +
                                               "I need your location to find what movies in cinemas right now near you"
            , replyMarkup: replyMarkup);
    }

    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = new InlineKeyboardButton("Inline Mode", InlineButtonType.SwitchInlineQueryCurrentChat);
        return await bot.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    static Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("FailingHandler");
    }

    // Process Inline Keyboard callback data
    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        await bot.AnswerCallbackQuery(callbackQuery.Id, $"Received {callbackQuery.Data}");
        await bot.SendMessage(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
    }

    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [ // displayed result
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await bot.AnswerInlineQuery(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await bot.SendMessage(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion


    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
