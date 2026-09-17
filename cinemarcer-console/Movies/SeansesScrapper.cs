namespace cinemarcer_console.Movies;

using Abstract;
using Newtonsoft.Json.Linq;

public static class SeansesScrapper
{
    public static IEnumerable<Movie> Scrap(IGrouping<string?, JToken> cinemaAndData)
    {
        return ScrapperStrategy.GetProvider(cinemaAndData.Key).Convert(cinemaAndData);
    }
}