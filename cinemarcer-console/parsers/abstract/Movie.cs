namespace cinemarcer_console.parsers.@abstract
{
    using Newtonsoft.Json.Linq;

    public struct Movie
    {
        public string Title { get; init; }
        public string PosterUrl { get;   init; }
        public string Description { get;   init; }
        public DateTime EndDate { get;    init; }
        public DateTime StartDate { get;     init; }
        public string CinemaName { get;    init; }
        public string City { get;      init;  }
        public string Country { get;      init;  }
        public DateTime ShowDateTime { get; init; }
        public int Duration { get; init; }
        public double Rate { get; init; }

        public Movie(string title, string cinemaName, string startDate = "",
            string endDate = "",
            string posterUrl = "",
            string description = "",
            string city = "",
            string country = "")
        {
            Title = title;
            PosterUrl = posterUrl;
            Description = description;
            CinemaName = cinemaName;
            City = city;
            Country = country;
            if (!string.IsNullOrEmpty(startDate)) StartDate = DateTime.Parse(startDate);
            if (!string.IsNullOrEmpty(endDate)) EndDate = DateTime.Parse(endDate);
        }

        public Movie(JToken movieSession)
        {
            var movie = movieSession["movie"]!;
            var cinema = movieSession["cinema"]!;
            
            Title = movie["name"]!.Value<string>()!;
            PosterUrl = movie["imagePortrait"]!.Value<string>()!;
            Description = movieSession["movieFormat"]?.Value<string>() ?? string.Empty;
            StartDate = DateTime.Parse(movie["startDate"]!.Value<string>()!);
            EndDate = DateTime.Parse(movie["endDate"]!.Value<string>()!);
            CinemaName = cinema["name"]!.Value<string>()!;
            City = cinema["address"]!.Value<string>()!;
            ShowDateTime = DateTime.Parse($"{movieSession["showDate"]!.Value<string>()} {movieSession["showTime"]!.Value<string>()}");
            Duration = movie["duration"]?.Value<int>() ?? 0;
            Rate = movie["rate"]?.Value<double>() ?? 0;
        }

        public override string ToString()
        {
            return $"title : {this.Title}, CinemaName {this.CinemaName},  posterUrl : {this.PosterUrl}, description : {this.Description}";
        }
    }
}