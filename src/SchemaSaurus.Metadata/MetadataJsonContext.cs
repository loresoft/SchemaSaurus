using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SchemaSaurus.Metadata;

[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true
)]
[JsonSerializable(typeof(DatabaseModel))]
[JsonSerializable(typeof(Table))]
[JsonSerializable(typeof(View))]
[JsonSerializable(typeof(Column))]
[JsonSerializable(typeof(PrimaryKey))]
[JsonSerializable(typeof(ForeignKey))]
[JsonSerializable(typeof(ForeignKeyColumnMapping))]
[JsonSerializable(typeof(Index))]
[JsonSerializable(typeof(IndexColumn))]
[JsonSerializable(typeof(ColumnReference))]
[JsonSerializable(typeof(UniqueConstraint))]
[JsonSerializable(typeof(CheckConstraint))]
[JsonSerializable(typeof(Trigger))]
[JsonSerializable(typeof(Sequence))]
[JsonSerializable(typeof(StoredProcedure))]
[JsonSerializable(typeof(ScalarFunction))]
[JsonSerializable(typeof(TableValuedFunction))]
[JsonSerializable(typeof(Parameter))]
[JsonSerializable(typeof(ReturnColumn))]
[JsonSerializable(typeof(TableOptions))]
[JsonSerializable(typeof(TypeMapping))]
[JsonSerializable(typeof(UserDefinedType))]
[JsonSerializable(typeof(SchemaQualifiedName))]
/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for serializing and deserializing
/// the <see cref="DatabaseModel"/> object graph.
/// </summary>
/// <remarks>
/// Registers all metadata types for ahead-of-time JSON code generation via
/// <see cref="JsonSerializableAttribute"/>. The generated serializers use camel-case
/// property names, indented output, and skip empty collections
/// (<see cref="IReadOnlyList{T}"/> / <see cref="IReadOnlyDictionary{TKey, TValue}"/>)
/// to keep the JSON compact.
/// </remarks>
public partial class MetadataJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Lazily-initialized <see cref="System.Text.Json.JsonSerializerOptions"/> configured
    /// with the source-generated <see cref="Default"/> resolver and the
    /// empty-collection skipping modifier.
    /// </summary>
    public static Lazy<JsonSerializerOptions> JsonSerializerOptions => new(CreateOptions);

    /// <summary>
    /// Creates a new <see cref="System.Text.Json.JsonSerializerOptions"/> instance
    /// using the source-generated resolver with the <see cref="SkipEmptyCollections"/>
    /// modifier applied.
    /// </summary>
    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            TypeInfoResolver = Default.WithAddedModifier(SkipEmptyCollections),
        };
    }

    /// <summary>
    /// <see cref="JsonTypeInfo"/> modifier that suppresses serialization of empty
    /// <see cref="IReadOnlyList{T}"/> and <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// properties, keeping the JSON output compact.
    /// </summary>
    /// <param name="typeInfo">The type info to modify.</param>
    private static void SkipEmptyCollections(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var property in typeInfo.Properties)
        {
            if (!IsSkippableCollectionType(property.PropertyType))
                continue;

            property.ShouldSerialize = (_, value) => value is ICollection { Count: > 0 };
        }
    }

    /// <summary>
    /// Determines whether a property type is an <see cref="IReadOnlyList{T}"/> or
    /// <see cref="IReadOnlyDictionary{TKey, TValue}"/> eligible for empty-collection
    /// skipping.
    /// </summary>
    /// <param name="type">The property type to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the type is a skippable collection;
    /// <see langword="false"/> otherwise.
    /// </returns>
    private static bool IsSkippableCollectionType(Type type)
    {
        return type.IsGenericType
            && type.GetGenericTypeDefinition() is var definition
            && (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IReadOnlyList<>));
    }
}
