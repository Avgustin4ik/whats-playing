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
            if (string.IsNullOrEmpty(startDate)) StartDate = DateTime.Parse(startDate);
            if (string.IsNullOrEmpty(endDate)) EndDate = DateTime.Parse(endDate);
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
        }

        public override string ToString()
        {
            return $"title : {this.Title}, CinemaName {this.CinemaName},  posterUrl : {this.PosterUrl}, description : {this.Description}";
        }
    }
}