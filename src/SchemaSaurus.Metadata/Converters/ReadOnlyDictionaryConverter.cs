using System.Text.Json;

namespace SchemaSaurus.Metadata.Converters;

/// <summary>
/// Converts <see cref="IReadOnlyDictionary{String, Object}"/> to and from JSON by
/// deserializing into a <see cref="Dictionary{String, Object}"/> concrete instance.
/// Required because STJ cannot construct an interface type during deserialization.
/// </summary>
public sealed class ReadOnlyDictionaryConverter<TKey, TValue>
    : JsonConverter<IReadOnlyDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<TKey, TValue>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options) ?? [];
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<TKey, TValue>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}

