using System.Globalization;
using LiteDB;

namespace RestReactAspire.StatisticsService.Stores;

public static class LiteDbFactory
{
    private static bool _configured;
    private static readonly object _lock = new();

    public static void ConfigureMapper()
    {
        lock (_lock)
        {
            if (_configured) return;

            BsonMapper.Global.RegisterType(
                serialize: (DateOnly d) => new BsonValue(d.ToString("O", CultureInfo.InvariantCulture)),
                deserialize: (BsonValue bson) => DateOnly.ParseExact(bson.AsString, "O", CultureInfo.InvariantCulture)
            );

            BsonMapper.Global.RegisterType(
                serialize: (TimeOnly t) => new BsonValue(t.ToString("O", CultureInfo.InvariantCulture)),
                deserialize: (BsonValue bson) => TimeOnly.ParseExact(bson.AsString, "O", CultureInfo.InvariantCulture)
            );

            _configured = true;
        }
    }
}
