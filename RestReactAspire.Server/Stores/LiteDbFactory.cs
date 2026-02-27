using System.Globalization;
using LiteDB;
using RestReactAspire.Server.Models;

namespace RestReactAspire.Server.Stores;

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

            // Pre-warm entity mapper cache to avoid concurrent lazy-init race conditions
            // when multiple requests trigger deserialization simultaneously.
            BsonMapper.Global.Entity<Patient>();
            BsonMapper.Global.Entity<Doctor>();
            BsonMapper.Global.Entity<Exam>();

            _configured = true;
        }
    }
}
