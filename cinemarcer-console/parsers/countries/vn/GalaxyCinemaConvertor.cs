namespace cinemarcer_console.parsers.countries.vn;

using System.Net;
using @abstract;
using Newtonsoft.Json.Linq;

public class GalaxyCinemaConvertor : IConvertor
{
    public string KeyName { get; set; } = "https://www.galaxycine.vn/";

    private static readonly CookieContainer Cookies = new();
    private static readonly HttpClient Http = new(new HttpClientHandler { CookieContainer = Cookies });
    private const string BaseUrl = "https://www.galaxycine.vn";
    private const string ApiUrl = BaseUrl + "/api/v2/mobile/sessions?includeCinema=true&includeMovie=true";
    private const string ClientId = "3da4eba4-94dd-4e74-bc25-38e89a01fe07";
    private const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36";
    private static JArray? _sessions;

    private static async Task<JArray> GetSessions()
    {
        if (_sessions != null) return _sessions;

        // Bootstrap: get x-session-id cookie from homepage
        using var bootstrapReq = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        bootstrapReq.Headers.Add("User-Agent", UserAgent);
        await Http.SendAsync(bootstrapReq);

        var sessionId = Cookies.GetCookies(new Uri(BaseUrl))["x-session-id"]?.Value ?? "";

        // Fetch sessions API with required headers
        using var req = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
        req.Headers.Add("clientid", ClientId);
        req.Headers.Add("platform", "website");
        req.Headers.Add("session-id", sessionId);
        req.Headers.Add("User-Agent", UserAgent);
        req.Headers.Add("Referer", BaseUrl + "/");
        req.Headers.Add("Accept", "application/json");

        var response = await Http.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var root = JObject.Parse(json);
        _sessions = (JArray)root["data"]!["result"]!;
        return _sessions;
    }

    public IMovie[] Convert(IGrouping<string?, JToken> json)
    {
        var cinemaName = json.Key ?? string.Empty;
        var sessions = GetSessions().Result;
        return sessions
            .Where(s => s["cinema"]!["name"]!.Value<string>()!
                .Contains(cinemaName, StringComparison.OrdinalIgnoreCase))
            .Select(s => (IMovie)new GalaxyMovie(s))
            .ToArray();
    }
}
