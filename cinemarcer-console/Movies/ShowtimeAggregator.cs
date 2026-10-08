namespace cinemarcer_console.Movies;

using System.Collections;
using Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SerpApi;

public class ShowtimeAggregator(IConfiguration config)
{

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
        var serpApiKey = config["SerpApiKey"];
        Hashtable ht = new Hashtable();
        ht.Add("engine", "google_maps");
        ht.Add("q", "cinema");
        ht.Add("type", "search"); 
        if (location.StartsWith('@'))
        {
            ht.Add("ll", location);
        }
        else
        {
            //todo try to find location by Name
            throw new UnknownLocationException($"Unknown location {location}");
        }
        //todo add distance in request
        foreach (var item in ht) Console.WriteLine($"request ht = " + item);
        var search = new GoogleSearch(ht, serpApiKey);
        var data = search.GetJson();
        
        var localTheaters = data["local_results"].Where(x => x["type"]?.ToString() == "Movie theater");
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