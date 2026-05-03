using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests;

public class DatabaseModelJsonTests
{
    [Fact]
    public void WhenModelSerializedAndDeserializedThenJsonIsStable()
    {
        var original = DatabaseModelFixtures.CreateFullModel();

        var json1 = original.ToJson();
        var deserialized = DatabaseModelExtensions.FromJson(json1);
        var json2 = deserialized.ToJson();

        json2.Should().Be(json1);
    }

    [Fact]
    public void WhenDeserializedThenBackReferencesAreWired()
    {
        var json = DatabaseModelFixtures.CreateFullModel().ToJson();
        var model = DatabaseModelExtensions.FromJson(json);

        // Table.Database back-reference
        var orders = model.FindTable("dbo", "Orders");
        orders.Should().NotBeNull();
        orders!.Database.Should().BeSameAs(model);

        // Column.Parent back-reference
        var idColumn = orders.Columns.First(c => c.Name == "Id");
        idColumn.Parent.Should().BeSameAs(orders);

        // ForeignKey.DependentTable and PrincipalTable
        var fk = orders.ForeignKeys.First();
        fk.DependentTable.Should().BeSameAs(orders);
        fk.PrincipalTable.Should().NotBeNull();
        fk.PrincipalTable.SchemaQualifiedName.Name.Should().Be("Customers");

        // ForeignKeyColumnMapping resolved columns
        var mapping = fk.ColumnMappings.First();
        mapping.DependentColumn.Should().NotBeNull();
        mapping.DependentColumn.Name.Should().Be("CustomerId");
        mapping.PrincipalColumn.Should().NotBeNull();
        mapping.PrincipalColumn.Name.Should().Be("Id");

        // PrimaryKey column resolution
        orders.PrimaryKey.Should().NotBeNull();
        orders.PrimaryKey!.Columns.First().Column.Should().NotBeNull();
        orders.PrimaryKey.Columns.First().Column.Name.Should().Be("Id");

        // View.Database back-reference
        var view = model.FindView("dbo", "vw_ActiveCustomers");
        view.Should().NotBeNull();
        view!.Database.Should().BeSameAs(model);
    }

    [Fact]
    public void WhenMinimalModelSerializedThenEmptyCollectionsAreOmitted()
    {
        var model = DatabaseModelFixtures.CreateMinimalModel();

        var json = model.ToJson();

        // SkipEmptyCollections should omit empty lists and dictionaries
        json.Should().NotContain("\"tables\"");
        json.Should().NotContain("\"views\"");
        json.Should().NotContain("\"sequences\"");
        json.Should().NotContain("\"storedProcedures\"");
        json.Should().NotContain("\"scalarFunctions\"");
        json.Should().NotContain("\"tableValuedFunctions\"");
        json.Should().NotContain("\"userDefinedTypes\"");
        json.Should().NotContain("\"annotations\"");
    }

    [Fact]
    public void WhenMinimalModelDeserializedThenResolveReferencesSucceeds()
    {
        var model = DatabaseModelFixtures.CreateMinimalModel();

        var json = model.ToJson();
        var deserialized = DatabaseModelExtensions.FromJson(json);

        deserialized.DatabaseName.Should().Be("EmptyDb");
        deserialized.Provider.Should().Be("Sqlite");
    }

    [Fact]
    public void WhenFullModelSerializedThenPopulatedCollectionsArePresent()
    {
        var model = DatabaseModelFixtures.CreateFullModel();

        var json = model.ToJson();

        json.Should().Contain("\"tables\"");
        json.Should().Contain("\"views\"");
        json.Should().Contain("\"sequences\"");
        json.Should().Contain("\"storedProcedures\"");
        json.Should().Contain("\"scalarFunctions\"");
        json.Should().Contain("\"tableValuedFunctions\"");
        json.Should().Contain("\"userDefinedTypes\"");
        json.Should().Contain("\"edition\"");
        json.Should().Contain("\"compatibilityLevel\"");
    }

    [Fact]
    public void WhenTableHasNoTriggersOrIndexesThenEmptyCollectionsAreOmitted()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("TestDb")
            .WithProvider("SqlServer")
            .AddTable(t => t
                .WithSchemaQualifiedName("dbo", "Simple")
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))))
            .Build();

        var json = model.ToJson();

        // The "Simple" table has no indexes, triggers, uniqueConstraints, etc.
        json.Should().NotContain("\"indexes\"");
        json.Should().NotContain("\"triggers\"");
        json.Should().NotContain("\"uniqueConstraints\"");
        json.Should().NotContain("\"checkConstraints\"");
        json.Should().NotContain("\"foreignKeys\"");
    }

    [Fact]
    public void WhenAnnotationsSerializedThenAnnotationsAppearInJson()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("AnnotatedDb")
            .WithProvider("SqlServer")
            .WithAnnotation("custom_key", "custom_value")
            .AddTable(t => t
                .WithSchemaQualifiedName("dbo", "Annotated")
                .WithAnnotation("table_flag", "true")
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))
                    .WithAnnotation("col_note", "pk")))
            .Build();

        var json = model.ToJson();

        json.Should().Contain("\"custom_key\"");
        json.Should().Contain("\"custom_value\"");
        json.Should().Contain("\"table_flag\"");
        json.Should().Contain("\"col_note\"");
    }

    [Fact]
    public void WhenAnnotationsDeserializedThenAnnotationsArePreserved()
    {
        var original = new DatabaseModelBuilder()
            .WithDatabaseName("AnnotatedDb")
            .WithProvider("SqlServer")
            .WithAnnotation("db_level", "value1")
            .AddTable(t => t
                .WithSchemaQualifiedName("dbo", "Annotated")
                .WithAnnotation("table_level", "value2")
                .AddColumn(c => c
                    .WithName("Id")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.Int32)
                    .WithNativeTypeName("int")
                    .WithSystemType(typeof(int))))
            .Build();

        var json = original.ToJson();
        var deserialized = DatabaseModelExtensions.FromJson(json);

        deserialized.Annotations.Should().ContainKey("db_level");
        deserialized.Annotations["db_level"]!.ToString().Should().Be("value1");

        var table = deserialized.FindTable("dbo", "Annotated");
        table.Should().NotBeNull();
        table!.Annotations.Should().ContainKey("table_level");
        table.Annotations["table_level"]!.ToString().Should().Be("value2");
    }

    [Fact]
    public void WhenColumnHasDefaultValueThenDefaultValueSqlIsSerialized()
    {
        var model = new DatabaseModelBuilder()
            .WithDatabaseName("DefaultsDb")
            .WithProvider("SqlServer")
            .AddTable(t => t
                .WithSchemaQualifiedName("dbo", "WithDefaults")
                .AddColumn(c => c
                    .WithName("CreatedDate")
                    .WithOrdinalPosition(1)
                    .WithIsNullable(false)
                    .WithDbType(DbType.DateTime)
                    .WithNativeTypeName("datetime2")
                    .WithSystemType(typeof(DateTime))
                    .WithDefaultValueSql("(getdate())")))
            .Build();

        var json = model.ToJson();

        json.Should().Contain("\"defaultValueSql\"");
        json.Should().Contain("(getdate())");
    }

    [Fact]
    public void WhenEnumValuesSerializedThenStringRepresentationIsUsed()
    {
        var model = DatabaseModelFixtures.CreateFullModel();

        var json = model.ToJson();

        // Enums should be serialized as strings, not integers
        json.Should().Contain("\"Cascade\"");
        json.Should().Contain("\"After\"");
    }

    }
