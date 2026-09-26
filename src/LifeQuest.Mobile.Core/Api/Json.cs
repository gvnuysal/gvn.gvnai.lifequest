using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeQuest.Mobile.Core.Api;

public static class Json
{
    /// <summary>API ile aynı sözleşme: camelCase alanlar, metin enum'lar, boş alanlar gönderilmez.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new UtcDateTimeConverter() }
    };

    /// <summary>Saat dilimi bilgisi olmayan zaman damgaları UTC kabul edilir (API her zaman UTC döndürür).</summary>
    private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(value.Kind == DateTimeKind.Unspecified ? "yyyy-MM-ddTHH:mm:ss" : "O", CultureInfo.InvariantCulture));
    }
}
