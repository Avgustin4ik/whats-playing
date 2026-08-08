namespace cinemarcer_console.parsers.countries.vn;

using @abstract;
using Newtonsoft.Json.Linq;

public class GalaxyMovie : IMovie
{
    public string Title { get; }
    public string PosterUrl { get; }
    public string Description { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public string CinemaName { get; }
    public string City { get; }
    public string Country => "VN";

    public GalaxyMovie(JToken session)
    {
        var movie = session["movie"]!;
        var cinema = session["cinema"]!;

        Title = movie["name"]!.Value<string>()!;
        PosterUrl = movie["imagePortrait"]!.Value<string>()!;
        Description = session["movieFormat"]?.Value<string>() ?? string.Empty;
        StartDate = DateTime.Parse(movie["startDate"]!.Value<string>()!);
        EndDate = DateTime.Parse(movie["endDate"]!.Value<string>()!);
        CinemaName = cinema["name"]!.Value<string>()!;
        City = cinema["address"]!.Value<string>()!;
    }
}
