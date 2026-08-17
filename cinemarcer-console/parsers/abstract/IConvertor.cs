namespace cinemarcer_console.parsers.@abstract
{
    using Newtonsoft.Json.Linq;

    public interface IConvertor
    {
        public Movie[] Convert(IGrouping<string?, JToken> json);
        string Key { get; set; } //website
    }
}