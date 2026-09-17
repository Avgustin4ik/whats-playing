namespace cinemarcer_console.Movies.Countries.vn;

using System.Globalization;
using Abstract;
using Newtonsoft.Json.Linq;

public class LotteCinemaConvertor : IConvertor
{
    public string Key { get; set; } = "lottecinemavn.com";
    private static readonly HttpClient Http = new();
    private const string BaseUrl = "https://www.lottecinemavn.com/LCWS/Ticketing/TicketingData.aspx";
    private const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36";
    private static JObject? _ticketingPage;

    private static async Task<JObject> GetTicketingPage()
    {
        if (_ticketingPage != null) return _ticketingPage;

        var paramList = new JObject
        {
            ["MethodName"] = "GetTicketingPage",
            ["channelType"] = "HO",
            ["osType"] = "WEB",
            ["osVersion"] = "Chrome",
            ["memberOnNo"] = ""
        };
        _ticketingPage = await Call(paramList);
        return _ticketingPage;
    }

    private static async Task<JObject> Call(JObject paramList)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "?nocashe=" + Random.Shared.NextDouble());
        req.Headers.Add("User-Agent", UserAgent);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["paramList"] = paramList.ToString()
        });
        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JObject.Parse(json);
    }

    public Movie[] Convert(IGrouping<string?, JToken> json)
    {
        var cinemaTitles = json.Select(x => x.Value<string>("title") ?? "").ToArray();
        var ticketingPage = GetTicketingPage().Result;
        var cinemas = ((JArray)ticketingPage["Cinemas"]!["Cinemas"]!["Items"]!)
            .Where(c => cinemaTitles.Any(t => t.Contains(c.Value<string>("CinemaName") ?? "\0", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var moviesByCode = ((JArray)ticketingPage["Movies"]!["Movies"]!["Items"]!)
            .ToDictionary(m => m.Value<string>("RepresentationMovieCode")!);

        var today = DateTime.Now.ToString("yyyyMMdd");
        var movies = new List<Movie>();
        foreach (var cinema in cinemas)
        {
            var divisionCode = cinema.Value<string>("DivisionCode");
            var detailDivisionCode = cinema.Value<string>("DetailDivisionCode") ?? "";
            var cinemaId = cinema.Value<string>("CinemaID");
            var cinemaIdParam = $"{divisionCode}|{detailDivisionCode[^1..]}|{cinemaId}";

            var paramList = new JObject
            {
                ["MethodName"] = "GetPlaySequence",
                ["channelType"] = "HO",
                ["osType"] = "WEB",
                ["osVersion"] = "Chrome",
                ["playDate"] = today,
                ["cinemaID"] = cinemaIdParam,
                ["representationMovieCode"] = ""
            };
            var response = Call(paramList).Result;
            var sessions = (JArray?)response["PlaySeqs"]?["Items"] ?? new JArray();

            foreach (var session in sessions)
            {
                var movieCode = session.Value<string>("RepresentationMovieCode") ?? "";
                if (!moviesByCode.TryGetValue(movieCode, out var movie)) continue;

                var showDateTime = DateTime.ParseExact(
                    session.Value<string>("PlayDt") + session.Value<string>("StartTime"),
                    "yyyyMMddHH:mm",
                    CultureInfo.InvariantCulture);
                var releaseDate = movie.Value<string>("ReleaseDate");

                movies.Add(new Movie(
                    movie.Value<string>("MovieName") ?? "",
                    session.Value<string>("CinemaName") ?? "",
                    startDate: string.IsNullOrEmpty(releaseDate) ? "" : DateTime.ParseExact(releaseDate, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("O"),
                    posterUrl: movie.Value<string>("PosterURL") ?? "",
                    description: session.Value<string>("ScreenNameUS") ?? session.Value<string>("ScreenName") ?? "",
                    city: cinema.Value<string>("Address") ?? "")
                {
                    ShowDateTime = showDateTime,
                    Rate = movie.Value<double?>("Evaluation") ?? 0
                });
            }
        }

        return movies.ToArray();
    }
}
