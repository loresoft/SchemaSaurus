namespace SchemaSaurus.Metadata;

/// <summary>
/// Direction of a stored procedure or function parameter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParameterDirection>))]
public enum ParameterDirection
{
    /// <summary>Parameter supplies a value to the routine.</summary>
    Input,

    /// <summary>Parameter returns a value from the routine.</summary>
    Output,

    /// <summary>Parameter both supplies and returns a value.</summary>
    InputOutput,

    /// <summary>Represents the scalar return value of a function.</summary>
    ReturnValue,
}
