namespace cinemarcer_console;

using System.Text;
using Movies.Abstract;

public static class MdPrinter
{
    public static void Print(IEnumerable<Movie> movies, string filename, string? locationName = null)
    {
        var sections = movies
            .GroupBy(m => m.Title)
            .OrderBy(g => g.Key, StringComparer.InvariantCulture)
            .Select(g => new
            {
                Title = g.Key,
                StartDate = g.First().StartDate,
                EndDate = g.First().EndDate,
                PosterUrl = g.Select(m => m.PosterUrl).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? string.Empty,
                Rows = g.Select(m => (m.CinemaName, m.City, m.Description))
                    .Distinct()
                    .OrderBy(r => r.CinemaName, StringComparer.InvariantCulture)
                    .ThenBy(r => r.Description, StringComparer.InvariantCulture)
                    .ToList()
            });

        var sb = new StringBuilder();
        sb.Append("# Movies");
        if (!string.IsNullOrWhiteSpace(locationName)) sb.Append(" — ").Append(EscapeMd(locationName));
        sb.Append('\n');
        sb.Append($"_Generated: {DateTime.Now:yyyy-MM-dd}_\n\n");

        var index = 1;
        foreach (var movie in sections)
        {
            AppendMovieSection(sb, index++, movie.Title, movie.StartDate, movie.EndDate, movie.PosterUrl, movie.Rows);
        }

        var fullPath = Path.GetFullPath(filename);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        Console.WriteLine($"{nameof(GetType)}: Generate and saved: {fullPath}");
        File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
    }

    private static void AppendMovieSection(StringBuilder sb, int index, string title,
        DateTime startDate, DateTime endDate, string posterUrl,
        IEnumerable<(string CinemaName, string City, string Description)> rows)
    {
        var escapedTitle = EscapeMd(title);
        sb.Append("## ").Append(index).Append(". ").Append(escapedTitle).Append('\n').Append('\n');

        if (startDate != default && endDate != default)
        {
            sb.Append('*').Append(startDate.ToString("dd.MM.yyyy")).Append(" – ")
                .Append(endDate.ToString("dd.MM.yyyy")).Append('*').Append('\n').Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(posterUrl))
        {
            sb.Append('!').Append('[').Append(escapedTitle).Append(']').Append('(').Append(posterUrl).Append(')').Append('\n').Append('\n');
        }

        sb.Append("| Cinema | Address | Format |\n");
        sb.Append("| --- | --- | --- |\n");
        foreach (var row in rows)
        {
            sb.Append("| ").Append(EscapeMd(row.CinemaName))
                .Append(" | ").Append(EscapeMd(row.City))
                .Append(" | ").Append(EscapeMd(row.Description))
                .Append(" |\n");
        }

        sb.Append('\n');
    }

    private static string EscapeMd(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace("|", "\\|")
            .Replace("[", "\\[")
            .Replace("]", "\\]");
    }
}
