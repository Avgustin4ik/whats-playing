namespace cinemarcer_console.Movies;

using System.Collections;
using Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SerpApi;
using Telegram.Bot.Types;

public class ShowtimeAggregator
{
    private readonly Message _locationMessage;
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    public ShowtimeAggregator(Message locationMessage, IConfiguration config, ILogger logger)
    {
        _locationMessage = locationMessage;
        _config = config;
        _logger = logger;
    }

    // public IEnumerable<Movie> GetMovies()
    // {
    //     const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
    //     const string location = "@12.240994,109.196397,15.1z";
    //     var serpApiKey = _config["SerpApiKey"];
    //     Hashtable ht = new Hashtable();
    //     ht.Add("engine", "google_maps");
    //     ht.Add("q", "cinema");
    //     ht.Add("ll", location);
    //     ht.Add("type", "search"); 
    //     //todo add distance in request
    //     const string MOVIE_THEATER = "Movie theater";
    //     const string CINEMA_NAME = "Galaxy";//for test
    //     try
    //     {
    //         var search = new GoogleSearch(ht, serpApiKey);
    //         var data = search.GetJson();
    //         var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == MOVIE_THEATER);
    //         foreach (var theater in localTheaters.Select(x => (x.Value<string>("title"), x.Value<string>("website"))))
    //         {
    //             Console.WriteLine($"{theater.Item1} --- {theater.Item2}");
    //         }
    //         return Aggregate(localTheaters);
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError(e, e.Message);
    //     }
    // }
    public class UnknownLocationException : Exception
    {
        public UnknownLocationException() : base("Unknown location")
        {
        }

        public UnknownLocationException(string message) : base(message)
        {
        }
    }
    public IEnumerable<JToken> GetMoviesInLocation(string location)
    {
        // const string locationName = "Nha Trang"; //todo (позже) перевод локации по названию в координаты https://developers.google.com/maps/documentation/geocoding/guides-v3/overview?hl=ru
        // const string location = "@12.240994,109.196397,15.1z";
        var serpApiKey = _config["WebSearch/SerpApiKey"];
        Hashtable ht = new Hashtable();
        ht.Add("engine", _config["WebSearch/Engine"]);
        ht.Add("q", _config["WebSearch/Query"]);
        if (location.StartsWith('@'))
        {
            ht.Add("ll", location);
        }
        else
        {
            //todo try to find location by Name
            throw new UnknownLocationException($"Unknown location {location}");
        }
        ht.Add("type", "search"); 
        //todo add distance in request
        var search = new GoogleSearch(ht, serpApiKey);
        var data = search.GetJson();
        
        var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == _config["WebSearch/Type"]);
        foreach (var theater in localTheaters.Select(x => (x.Value<string>("title"), x.Value<string>("website"))))
        {
            Console.WriteLine($"{theater.Item1} --- {theater.Item2}");
        }

        return localTheaters;
    }
    
    public IEnumerable<Movie> Aggregate(IEnumerable<JToken> moviesDataByCinema)
    {
        //split all by different cinemas
        var list = new List<Movie>(10); //todo 
        var cinemasByGroup = moviesDataByCinema.GroupBy(x => x.Value<string>("website")?.ToString());
        //todo do it in parallel
        foreach (var record in cinemasByGroup)
        {
            //Call convertor for each cinema
            //choose the best strategy
            try
            {
                list.AddRange(SeansesScrapper.Scrap(record));
            }   
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        return list;
    }
}