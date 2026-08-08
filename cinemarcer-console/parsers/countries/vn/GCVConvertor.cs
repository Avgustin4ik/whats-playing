namespace cinemarcer_console.parsers.countries.vn;

using @abstract;
using Newtonsoft.Json.Linq;

public class GCVConvertor : IConvertor
{
    public IMovie[] Convert(IGrouping<string?, JToken> json)
    {
        throw new NotImplementedException();
    }
    public string KeyName { get; set; }
}