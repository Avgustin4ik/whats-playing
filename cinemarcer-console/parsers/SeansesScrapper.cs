namespace cinemarcer_console.parsers;

using @abstract;
using Newtonsoft.Json.Linq;

public static class SeansesScrapper
{
    public static IEnumerable<IMovie> Scrap(IGrouping<string?, JToken> cinemaAndData)
    {
        return ScrapperStrategy.GetProvider(cinemaAndData.Key).Convert(cinemaAndData);
    }
}