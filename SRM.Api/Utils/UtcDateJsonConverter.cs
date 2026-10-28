using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace SRM.Api.Utils
{
    public class UtcDateJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String ||
                !DateTimeOffset.TryParse(
                    reader.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var date))
            {
                throw new JsonException("La fecha debe estar en formato ISO 8601.");
            }

            return date.UtcDateTime.ToUtcDate();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }
}
