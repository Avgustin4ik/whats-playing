using System.Collections;
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

const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
const string location = "@12.240994,109.196397,15.1z";
var serpApiKey = config["SerpApiKey"];
Hashtable ht = new Hashtable();
ht.Add("engine", "google_maps");
ht.Add("q", "cinema");
ht.Add("ll", location);
ht.Add("type", "search"); 
const string MOVIE_THEATER = "Movie theater";
const string CINEMA_NAME = "Galaxy";//for test
try
{
    var search = new GoogleSearch(ht, serpApiKey);
    var data = search.GetJson();
    var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == MOVIE_THEATER);
    //todo titles, location (distance from user?), website
    //todo start with galaxy-cinema. we will got one big JSON. Find actual movies. Build a list of output data. 
    //todo Movie Title, Theatre Name, Location, Website, Show Time, Show Date, Movie Poster
    foreach (var theater in localTheaters.Select(x => x.Value<string>("title")))
    {
        Console.WriteLine(theater);
    }
    localTheaters = localTheaters.Where(x => x["title"]?.ToString().StartsWith(CINEMA_NAME) == true);
    var result = ShowtimeAggregator.Aggregate(localTheaters);
    Console.ReadLine();
}

catch (SerpApiSearchException ex)
{
    Console.WriteLine("Exception:");
    Console.WriteLine(ex.ToString());
}