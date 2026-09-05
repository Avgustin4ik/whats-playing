using System.Collections;
using System.Reflection;
using cinemarcer_console;
using cinemarcer_console.parsers;
using cinemarcer_console.parsers.countries.vn;
using Microsoft.Extensions.Configuration;
using SerpApi;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(true)
    .Build();

ScrapperStrategy.Register(new GalaxyCinemaConvertor());
ScrapperStrategy.Register(new CgvCinemaConvertor());
ScrapperStrategy.Register(new LotteCinemaConvertor());
ScrapperStrategy.Register(new BetaCinemaConvertor());

const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
const string location = "@12.240994,109.196397,15.1z";
var serpApiKey = config["SerpApiKey"];
Hashtable ht = new Hashtable();
ht.Add("engine", "google_maps");
ht.Add("q", "cinema");
ht.Add("ll", location);
// ht.Add("location", locationName);
ht.Add("type", "search"); 
//todo add distance in request
const string MOVIE_THEATER = "Movie theater";
const string CINEMA_NAME = "Galaxy";//for test
try
{
    var search = new GoogleSearch(ht, serpApiKey);
    var data = search.GetJson();
    var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == MOVIE_THEATER);
    //todo Movie Title, Theatre Name, Location, Website, Show Time, Show Date, Movie Poster
    foreach (var theater in localTheaters.Select(x => (x.Value<string>("title"), x.Value<string>("website"))))
    {
        Console.WriteLine($"{theater.Item1} --- {theater.Item2}");
    }
    var result = ShowtimeAggregator.Aggregate(localTheaters);
    foreach (var theater in result)
    {
        Console.WriteLine(theater.ToString());
    }
    //здесь мы должны переконвертировать json в красивый MD
    var filename =  "./example_data/result.md";
    MdPrinter.Print(result, filename);
    Console.ReadLine();
}

catch (SerpApiSearchException ex)
{
    Console.WriteLine("Exception:");
    Console.WriteLine(ex.ToString());
}