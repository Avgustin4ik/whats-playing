namespace cinemarcer_console.parsers;

using System.Text.RegularExpressions;
using @abstract;
using Newtonsoft.Json.Linq;

public static class ShowtimeAggregator
{
    static string pattern = @"^(?:https?:\/\/)?(?:www\.)?([^\.]+)\.";
    public static IEnumerable<IMovie> Aggregate(IEnumerable<JToken> moviesDataByCinema)
    {
        //split all by different cinemas
        var list = new List<IMovie>(10); //todo 
        var cinemasByGroup = moviesDataByCinema.GroupBy(x => x.Value<string>("website")?.ToString());
        //todo do it in parallel
        foreach (var record in cinemasByGroup)
        {
            //Call convertor for each cinema
            //choose the best strategy
            list.AddRange(SeansesScrapper.Scrap(record));
        }
        return list;
    }
}