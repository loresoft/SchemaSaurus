using System.Text.Json;

namespace SchemaSaurus.Metadata.Converters;

/// <summary>
/// Converts <see cref="Type"/> to and from its <see cref="Type.FullName"/> string.
/// On deserialization, resolution is performed via <see cref="Type.GetType(string)"/>.
/// Only BCL / runtime types are expected; assembly-qualified names are not required.
/// </summary>
public sealed class TypeJsonConverter : JsonConverter<Type>
{
    /// <inheritdoc />
    public override Type? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var typeName = reader.GetString();
        if (typeName is null)
            return null;

        return Type.GetType(typeName)
            ?? throw new JsonException(
                $"Could not resolve CLR type '{typeName}'. " +
                "Ensure the type is available in the current runtime.");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        Type value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName ?? value.Name);
    }
}

