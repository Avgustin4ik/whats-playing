namespace cinemarcer_console.Movies.Countries.vn;

using System.Net.Security;
using Abstract;
using Newtonsoft.Json.Linq;

public class CgvCinemaConvertor : IConvertor
{
    public string Key { get; set; } = "cgv.vn";

    // cgv.vn's edge only negotiates legacy TLS_RSA_WITH_AES_128_CBC_SHA, which .NET's
    // default cipher suite policy on Linux excludes; opt this HttpClient back into it.
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            CipherSuitesPolicy = new CipherSuitesPolicy(new[] { TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA })
        }
    });
    private const string BaseUrl = "https://www.cgv.vn/en";
    private const string UserAgent = "CGV Cinema/2.9.4 (iPhone; iOS 18.3.1; Scale/3.00)";
    private static JArray? _cinemas;

    private static async Task<JArray> GetCinemas()
    {
        if (_cinemas != null) return _cinemas;

        using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/api/cinema/list");
        req.Headers.Add("User-Agent", UserAgent);
        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var root = JObject.Parse(json);
        var cinemas = new JArray();
        foreach (var city in (JArray)root["data"]!)
        {
            foreach (var cinema in (JArray)city["cinemas"]!)
            {
                cinemas.Add(cinema);
            }
        }
        _cinemas = cinemas;
        return _cinemas;
    }

    private static async Task<JArray> GetSchedule(string cinemaId, string date)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + $"/cinemas/catalog_mobile/siteschedules/id/{cinemaId}/date/{date}");
        req.Headers.Add("User-Agent", UserAgent);
        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var root = JObject.Parse(json);
        var movies = new JArray();
        foreach (var day in (JArray?)root["data"] ?? new JArray())
        {
            foreach (var movie in (JArray)day["movies"]!)
            {
                movies.Add(movie);
            }
        }
        return movies;
    }

    public Movie[] Convert(IGrouping<string?, JToken> json)
    {
        var cinemaNames = json.Select(x => x.Value<string>("title") ?? "").ToArray();
        var addresses = json.ToDictionary(x => x.Value<string>("title") ?? "", x => x.Value<string>("address") ?? "");
        var cinemas = GetCinemas().Result
            .Where(c => cinemaNames.Contains(c.Value<string>("name")))
            .ToArray();

        var today = DateTime.Now.ToString("ddMMyyyy");
        var movies = new List<Movie>();
        foreach (var cinema in cinemas)
        {
            var cinemaId = cinema.Value<string>("id")!;
            var cinemaName = cinema.Value<string>("name")!;
            var city = addresses.GetValueOrDefault(cinemaName, "");

            foreach (var movie in GetSchedule(cinemaId, today).Result)
            {
                var title = movie.Value<string>("name") ?? "";
                var posterUrl = movie.Value<string>("thumbnail") ?? "";
                var duration = movie.Value<int?>("movie_endtime") ?? 0;

                foreach (var language in (JArray)movie["languages"]!)
                {
                    var description = language.Value<string>("name") ?? "";
                    foreach (var session in (JArray)language["sessions"]!)
                    {
                        var showDateTime = DateTime.ParseExact(
                            session.Value<string>("showing_date_time")!,
                            "yyyyMMddHHmm",
                            System.Globalization.CultureInfo.InvariantCulture);

                        movies.Add(new Movie(title, cinemaName, city: city, posterUrl: posterUrl, description: description)
                        {
                            ShowDateTime = showDateTime,
                            Duration = duration
                        });
                    }
                }
            }
        }

        return movies.ToArray();
    }
}
