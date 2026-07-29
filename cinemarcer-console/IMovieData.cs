namespace cinemarcer_console;


public interface IMovieData
{
    public string Title { get; }
    public string PosterUrl { get; }
    public string Description { get; }
    public DateTime EndDate { get; }
    public DateTime StartDate { get; }
    public string CinemaName { get; }
}