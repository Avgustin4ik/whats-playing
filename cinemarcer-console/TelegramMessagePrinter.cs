namespace cinemarcer_console;

using System.Text;
using Movies.Abstract;

public readonly record struct TelegramMoviesMessage(string Text, IReadOnlyList<string> PosterUrls);

public static class TelegramMessagePrinter
{
    // No ticket-booking integration yet — placeholder link so the design's "buy ticket" row has something to show.
    private const string PlaceholderTicketUrl = "https://example.com/tickets";

    public static TelegramMoviesMessage Build(IEnumerable<Movie> movies, string? locationName = null)
    {
        //// todo сейчас в список попадают вьетнамские названия фильмов.
        /// регист учитывается, поэтому 2 и 3 элементы считаются как разные
        /// один и тот же фильмо может быть записан с вьетнамским и международным (английским) названием.
        /// например 2-3-8
        /// todo вероятно стоит сразу привести фильмы к международным названиям.
        /// todo нужно оптимизировать. работает очень долго
        var today = DateTime.Today;
        var sections = movies
            .Where(m => m.ShowDateTime.Date == today)
            .GroupBy(m => m.Title)
            .Distinct()
            .OrderBy(g => g.Key, StringComparer.InvariantCulture)
            .Select(g => new
            {
                Title = g.Key,
                PosterUrl = g.Select(m => m.PosterUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)),
                Rows = g.GroupBy(m => (m.CinemaName, m.Description))
                    .Select(rowGroup => (
                        rowGroup.Key.CinemaName,
                        rowGroup.Key.Description,
                        Times: (IEnumerable<DateTime>)rowGroup.Select(m => m.ShowDateTime).Distinct().OrderBy(t => t)))
                    .OrderBy(r => r.CinemaName, StringComparer.InvariantCulture)
                    .ThenBy(r => r.Description, StringComparer.InvariantCulture)
                    .ToList()
            })
            .ToList();
        if (sections.Count == 0) return new TelegramMoviesMessage("No movies nearby today.", Array.Empty<string>());
        var sb = new StringBuilder();
        sb.Append('*').Append(sections.Count).Append(" movies nearby");
        if (!string.IsNullOrWhiteSpace(locationName)) sb.Append(" — ").Append(EscapeMarkdownV2(locationName));
        sb.Append('*').Append('\n').Append('\n');

        for (var i = 0; i < sections.Count; i++)
        {
            AppendMovieBlock(sb, i + 1, sections[i].Title, sections[i].Rows, isOpen: i == 0);
            if (i < sections.Count - 1) sb.Append('\n');
        }

        var posterUrls = sections
            .Select(s => s.PosterUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!)
            .ToList();

        return new TelegramMoviesMessage(sb.ToString().TrimEnd('\n'), posterUrls);
    }

    private static void AppendMovieBlock(StringBuilder sb, int index, string title,
        IEnumerable<(string CinemaName, string Description, IEnumerable<DateTime> Times)> rows, bool isOpen)
    {
        var lines = new List<string>
        {
            $"*{index}\\. {EscapeMarkdownV2(title)}*"
        };

        foreach (var row in rows)
        {
            lines.Add($"{EscapeMarkdownV2(row.CinemaName)} · {EscapeMarkdownV2(row.Description)} · {FormatDistance(row.CinemaName)}");
            lines.Add($"`{string.Join(" · ", row.Times.Select(t => t.ToString("HH:mm")))}`");
        }

        // Only the open (top) block gets a buy-ticket row, matching the approved design.
        if (isOpen) lines.Add($"[Купить билет →]({PlaceholderTicketUrl})");

        for (var i = 0; i < lines.Count; i++)
        {
            var prefix = i == 0 && !isOpen ? "**>" : ">";
            sb.Append(prefix).Append(lines[i]);
            if (i == lines.Count - 1 && !isOpen) sb.Append("||");
            sb.Append('\n');
        }
    }

    // No real geo-distance data source yet (SerpAPI result isn't wired through to the printer) —
    // stand-in so the design's distance badge has something to render. Uses a manual char-sum hash
    // instead of string.GetHashCode(), which is randomized per process and wouldn't stay stable across restarts.
    private static string FormatDistance(string cinemaName)
    {
        var hash = cinemaName.Aggregate(0, (acc, c) => acc + c);
        var km = 0.3 + hash % 40 / 10.0;
        return EscapeMarkdownV2($"~{km:0.0} km");
    }

    private static string EscapeMarkdownV2(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if ("_*[]()~`>#+-=|{}.!\\".IndexOf(c) >= 0) sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
