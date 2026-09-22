using cinemarcer_console.Movies;
using cinemarcer_console.Movies.Countries.vn;
using cinemarcer_console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(true)
    .Build();

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
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient("telegram_bot_client")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramBotClientOptions options = new(config["BotToken"] ?? "no_token_detected");
                var botClient = new TelegramBotClient(options, httpClient);
                botClient.SetMyCommands([
                    new BotCommand(Commands.MOVIES.TrimStart('/'), "Show me what you got"), 
                    new BotCommand(Commands.LOCATION.TrimStart('/'),"Give me your location")]);
                return botClient;
            });

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

// const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
// const string location = "@12.240994,109.196397,15.1z";
// var serpApiKey = config["SerpApiKey"];
// Hashtable ht = new Hashtable();
// ht.Add("engine", "google_maps");
// ht.Add("q", "cinema");
// ht.Add("ll", location);
// ht.Add("type", "search"); 
// //todo add distance in request
// const string MOVIE_THEATER = "Movie theater";
// const string CINEMA_NAME = "Galaxy";//for test
// try
// {
//     var search = new GoogleSearch(ht, serpApiKey);
//     var data = search.GetJson();
//     var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == MOVIE_THEATER);
//     foreach (var theater in localTheaters.Select(x => (x.Value<string>("title"), x.Value<string>("website"))))
//     {
//         Console.WriteLine($"{theater.Item1} --- {theater.Item2}");
//     }
//     var result = ShowtimeAggregator.Aggregate(localTheaters);
//     foreach (var theater in result)
//     {
//         Console.WriteLine(theater.ToString());
//     }
//     var filename =  "./example_data/result.md";
//     MdPrinter.Print(result, filename);
//     //todo Принтер сообщения для Telegram Бота
//     Console.ReadLine();
// }
//
// catch (SerpApiSearchException ex)
// {
//     Console.WriteLine("Exception:");
//     Console.WriteLine(ex.ToString());
// }