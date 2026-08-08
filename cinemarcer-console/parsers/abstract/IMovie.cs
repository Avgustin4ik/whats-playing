namespace cinemarcer_console.parsers.@abstract;

public interface IMovie
{
    public string Title { get; }
    public string PosterUrl { get; }
    public string Description { get; }
    public DateTime EndDate { get; }
    public DateTime StartDate { get; }
    public string CinemaName { get; }
    public string City { get; }
    public string Country { get; }
}