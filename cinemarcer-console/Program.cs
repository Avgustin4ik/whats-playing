using cinemarcer_console.Movies;
using cinemarcer_console.Movies.Countries.vn;
using cinemarcer_console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;

//todo нужен тг-бот. Это факт.
//как пользователь будет взаимодействовать с ТГ и сервисом?
//бот должен собрать данные о местоположении пользователя. 
//todo узнать как tg передает данные о местоположении пользователя. 
//вероятно просто попросить "скинуть геолокацию"
//todo [фича] кнопка "Поделиться локацией" чтобы чтобы не скидывать ее в ручную
//получив локацию мы должны запарсить ее как location и отправить ShowtimeAggregator. далее работает как обычно
//ответ мы должны вернуть как сообщение в телеграм.
//Это приложение должно использоваться как апишка?
//или просто консольное приложение?
//но мне нужен будет кеш. Redis для небольшого хранения информации в течении суток
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddUserSecrets<Program>(optional: true))
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("telegram_bot_client")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramBotClientOptions options = new(context.Configuration["BotToken"] ?? "no_token_detected");
                var botClient = new TelegramBotClient(options, httpClient);
                botClient.SetMyCommands([
                    new BotCommand(Commands.MOVIES.TrimStart('/'), "Show me what you got"), 
                    new BotCommand(Commands.LOCATION.TrimStart('/'),"Give me your location")]);
                return botClient;
            });
        services.AddScoped<ShowtimeAggregator>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();
ScrapperStrategy.Register(new GalaxyCinemaConvertor());
ScrapperStrategy.Register(new CgvCinemaConvertor());
ScrapperStrategy.Register(new LotteCinemaConvertor());
ScrapperStrategy.Register(new BetaCinemaConvertor());
await host.RunAsync();