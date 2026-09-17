namespace cinemarcer_console.Movies.Countries.vn;

using System.Net;
using System.Text.RegularExpressions;
using Abstract;
using Newtonsoft.Json.Linq;

public class BetaCinemaConvertor : IConvertor
{
    public string Key { get; set; } = "betacinemas.vn";
    private static readonly HttpClient Http = new();
    private const string BaseUrl = "https://lichchieu.betacorp.vn";
    private const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36";
    private static readonly Regex CinemaOptionPattern = new(@"<option value=""([0-9a-fA-F-]{36})"">([^<]+)</option>", RegexOptions.Compiled);
    private static readonly Regex FilmBlockPattern = new(@"class='filmName[^']*'\s*>(?<title>[^<]+)</span></td><td class='[^']*wview2[^']*'\s*>(?<body>.*?)</td>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex SessionTimePattern = new(@"class='sessionTime'\s*>\s*(?<time>\d{1,2}:\d{2})", RegexOptions.Compiled);
    private static readonly Regex TitleFormatPattern = new(@"^(?<title>.*?)\s*\((?<format>[^)]*)\)\s*$", RegexOptions.Compiled);
    private static Dictionary<string, string>? _cinemas;

    private static async Task<Dictionary<string, string>> GetCinemas()
    {
        if (_cinemas != null) return _cinemas;

        using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/");
        req.Headers.Add("User-Agent", UserAgent);
        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        _cinemas = CinemaOptionPattern.Matches(html)
            .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));
        return _cinemas;
    }

    private static async Task<string> GetScheduleHtml(string cinemaId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + $"/Home/GetByCinemaID?CinemaID={cinemaId}");
        req.Headers.Add("User-Agent", UserAgent);
        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        return JToken.Parse(body).Value<string>() ?? "";
    }

    public Movie[] Convert(IGrouping<string?, JToken> json)
    {
        var cinemaTitles = json.ToDictionary(x => x.Value<string>("title") ?? "", x => x.Value<string>("address") ?? "");
        var matches = GetCinemas().Result
            .Select(c => (Id: c.Key, Location: c.Value.Replace("Beta ", "", StringComparison.OrdinalIgnoreCase).Trim()))
            .SelectMany(c => cinemaTitles.Keys
                .Where(t => t.Contains(c.Location, StringComparison.OrdinalIgnoreCase))
                .Select(t => (c.Id, Title: t)))
            .ToArray();

        var today = DateTime.Today;
        var movies = new List<Movie>();
        foreach (var (cinemaId, title) in matches)
        {
            var city = cinemaTitles[title];
            var html = GetScheduleHtml(cinemaId).Result;

            foreach (Match block in FilmBlockPattern.Matches(html))
            {
                var rawTitle = WebUtility.HtmlDecode(block.Groups["title"].Value).Trim();
                var formatMatch = TitleFormatPattern.Match(rawTitle);
                var movieTitle = formatMatch.Success ? formatMatch.Groups["title"].Value : rawTitle;
                var description = formatMatch.Success ? formatMatch.Groups["format"].Value : "";

                foreach (Match sessionTime in SessionTimePattern.Matches(block.Groups["body"].Value))
                {
                    var showDateTime = today.Add(TimeSpan.Parse(sessionTime.Groups["time"].Value));
                    movies.Add(new Movie(movieTitle, title, description: description, city: city)
                    {
                        ShowDateTime = showDateTime
                    });
                }
            }
        }

        return movies.ToArray();
    }
}
