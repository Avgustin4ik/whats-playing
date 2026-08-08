namespace cinemarcer_console.parsers;

using @abstract;
using countries.vn;

public static class ScrapperStrategy
{
    public static Dictionary<string, IConvertor> Convertors = new();
    public static void Register(IConvertor convertor)
    {
        Convertors.Add(convertor.KeyName, convertor);
    }
    public static void Unregister(IConvertor convertor)
    {
        Convertors.Remove(convertor.GetType().Name);
    }
    public static IConvertor GetProvider(string? cinemaWebsite)
    {
        foreach (var (key, convertor) in Convertors)
        {
            if (cinemaWebsite != null && cinemaWebsite.Contains(key, StringComparison.OrdinalIgnoreCase))
                return convertor;
        }
        throw new NotSupportedException($"No convertor registered for cinema: {cinemaWebsite}");
    }
}