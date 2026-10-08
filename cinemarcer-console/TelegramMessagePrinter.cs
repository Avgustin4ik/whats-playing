namespace cinemarcer_console;

using System.Text;
using Movies.Abstract;

public static class TelegramMessagePrinter
{
    public static string Format(IEnumerable<Movie> movies, string? locationName = null)
    {
        var today = DateTime.Today;
        var sections = movies
            .Where(m => m.ShowDateTime.Date == today)
            .GroupBy(m => m.Title)
            .OrderBy(g => g.Key, StringComparer.InvariantCulture)
            .Select(g => new
            {
                Title = g.Key,
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
        Console.WriteLine($"Number of movies {movies.Count()} ");
        if (sections.Count == 0) return "No movies nearby today.";

        var sb = new StringBuilder();
        sb.Append('*').Append(sections.Count).Append(" movies nearby");
        if (!string.IsNullOrWhiteSpace(locationName)) sb.Append(" — ").Append(EscapeMarkdownV2(locationName));
        sb.Append('*').Append('\n').Append('\n');

        for (var i = 0; i < sections.Count; i++)
        {
            AppendMovieBlock(sb, i + 1, sections[i].Title, sections[i].Rows, isOpen: i == 0);
            if (i < sections.Count - 1) sb.Append('\n');
        }

        return sb.ToString().TrimEnd('\n');
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
            lines.Add($"{EscapeMarkdownV2(row.CinemaName)} · {EscapeMarkdownV2(row.Description)}");
            lines.Add($"`{string.Join(" · ", row.Times.Select(t => t.ToString("HH:mm")))}`");
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var prefix = i == 0 && !isOpen ? "**>" : ">";
            sb.Append(prefix).Append(lines[i]);
            if (i == lines.Count - 1 && !isOpen) sb.Append("||");
            sb.Append('\n');
        }
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
