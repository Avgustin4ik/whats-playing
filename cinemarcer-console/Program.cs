using System.Collections;
using Microsoft.Extensions.Configuration;
using SerpApi;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>(true)
    .Build();


const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
const string location = "@12.240994,109.196397,15.1z";
var serpApiKey = config["SerpApiKey"];
Hashtable ht = new Hashtable();
ht.Add("engine", "google_maps");
ht.Add("q", "cinema");
ht.Add("ll", location);
ht.Add("type", "search"); 

try
{
    var search = new GoogleSearch(ht, serpApiKey);
    var data = search.GetJson();
    var localResultsRaw = data["local_results"].Where(x => x["type_id"]?.ToString() == "movie_theater");
    //todo titles, location (distance from user?), website
    
    //todo start with galaxy-cinema. we will got one big JSON. Find actual movies. Build a list of output data. 
    //todo Movie Title, Theatre Name, Location, Website, Show Time, Show Date, Movie Poster
    var localResults = localResultsRaw.Select(x => x["title"]).ToList();
    Console.WriteLine(string.Join(", ", localResults));
}
catch (SerpApiSearchException ex)
{
    Console.WriteLine("Exception:");
    Console.WriteLine(ex.ToString());
}