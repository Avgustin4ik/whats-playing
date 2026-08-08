namespace cinemarcer_console.parsers.@abstract;

using Newtonsoft.Json.Linq;

public interface IConvertor
{
    public IMovie[] Convert(IGrouping<string?, JToken> json);
    string KeyName { get; set; }
}