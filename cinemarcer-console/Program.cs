using cinemarcer_console.Movies;
using cinemarcer_console.Movies.Countries.vn;
using cinemarcer_console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;

//todo [фича] кнопка "Поделиться локацией" чтобы чтобы не скидывать ее в ручную
//получив локацию мы должны запарсить ее как location и отправить ShowtimeAggregator. далее работает как обычно
//ответ мы должны вернуть как сообщение в телеграм.
//мне нужен будет кеш. Redis для небольшого хранения информации в течении суток
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddUserSecrets<Program>(true);
        
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("telegram_bot_client")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var config = context.Configuration;
                TelegramBotClientOptions options = new(config["BotToken"] ?? "no_token_detected");
                var botClient = new TelegramBotClient(options, httpClient);
                botClient.SetMyCommands([
                    new BotCommand(Commands.MOVIES.TrimStart('/'), "Show me what you got"), 
                    new BotCommand(Commands.LOCATION.TrimStart('/'),"Give me your location")]);
                return botClient;
            });
        services.AddScoped<ShowtimeAggregator>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>(); //todo read why this is "deprecated" approach
    })
    .Build();

//todo inject
ScrapperStrategy.Register(new GalaxyCinemaConvertor());
ScrapperStrategy.Register(new CgvCinemaConvertor());
ScrapperStrategy.Register(new LotteCinemaConvertor());
ScrapperStrategy.Register(new BetaCinemaConvertor());

await host.RunAsync();