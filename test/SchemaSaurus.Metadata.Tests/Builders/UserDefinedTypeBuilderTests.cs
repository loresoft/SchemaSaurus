using System.Data;

using SchemaSaurus.Metadata.Builders;

namespace SchemaSaurus.Metadata.Tests.Builders;

public class UserDefinedTypeBuilderTests
{
    [Fact]
    public void WhenAliasTypeBuiltThenBuildSucceeds()
    {
        var udt = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "PhoneNumber")
            .WithKind(UserDefinedTypeKind.Alias)
            .WithDbType(DbType.String)
            .WithNativeTypeName("nvarchar(20)")
            .WithSystemType(typeof(string))
            .WithMaxLength(20)
            .WithIsUnicode(true)
            .Build();

        udt.QualifiedName.Name.Should().Be("PhoneNumber");
        udt.Kind.Should().Be(UserDefinedTypeKind.Alias);
        udt.DbType.Should().Be(DbType.String);
        udt.MaxLength.Should().Be(20);
        udt.Columns.Should().BeNull();
        udt.EnumLabels.Should().BeNull();
    }

    [Fact]
    public void WhenTableTypeBuiltWithColumnsViaBuilderActionThenColumnsArePopulated()
    {
        var udt = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "OrderTableType")
            .WithKind(UserDefinedTypeKind.TableType)
            .WithDbType(DbType.Object)
            .WithNativeTypeName("table")
            .WithSystemType(typeof(object))
            .AddColumn(c => c
                .WithName("Id")
                .WithOrdinalPosition(1)
                .WithIsNullable(false)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .AddColumn(c => c
                .WithName("Quantity")
                .WithOrdinalPosition(2)
                .WithIsNullable(false)
                .WithDbType(DbType.Int32)
                .WithNativeTypeName("int")
                .WithSystemType(typeof(int)))
            .Build();

        udt.Kind.Should().Be(UserDefinedTypeKind.TableType);
        udt.Columns.Should().HaveCount(2);
        udt.Columns![0].Name.Should().Be("Id");
        udt.Columns[1].Name.Should().Be("Quantity");
    }

    [Fact]
    public void WhenEnumTypeBuiltWithLabelsThenLabelsArePopulated()
    {
        var udt = new UserDefinedTypeBuilder()
            .WithQualifiedName("public", "status_enum")
            .WithKind(UserDefinedTypeKind.Enum)
            .WithDbType(DbType.String)
            .WithNativeTypeName("status_enum")
            .WithSystemType(typeof(string))
            .AddEnumLabel("active")
            .AddEnumLabel("inactive")
            .AddEnumLabel("archived")
            .Build();

        udt.Kind.Should().Be(UserDefinedTypeKind.Enum);
        udt.EnumLabels.Should().HaveCount(3);
        udt.EnumLabels.Should().ContainInOrder("active", "inactive", "archived");
    }

    [Fact]
    public void WhenAnnotationAddedThenAnnotationIsPresent()
    {
        var udt = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "MyType")
            .WithKind(UserDefinedTypeKind.Alias)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int))
            .WithAnnotation("source", "migration")
            .Build();

        udt.Annotations.Should().ContainKey("source").WhoseValue.Should().Be("migration");
    }

    [Fact]
    public void WhenSchemaQualifiedNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new UserDefinedTypeBuilder()
            .WithKind(UserDefinedTypeKind.Alias)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithQualifiedName*");
    }

    [Fact]
    public void WhenKindMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "MyType")
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithKind*");
    }

    [Fact]
    public void WhenDbTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "MyType")
            .WithKind(UserDefinedTypeKind.Alias)
            .WithNativeTypeName("int")
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithDbType*");
    }

    [Fact]
    public void WhenNativeTypeNameMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "MyType")
            .WithKind(UserDefinedTypeKind.Alias)
            .WithDbType(DbType.Int32)
            .WithSystemType(typeof(int));

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithNativeTypeName*");
    }

    [Fact]
    public void WhenSystemTypeMissingThenBuildThrowsInvalidOperationException()
    {
        var builder = new UserDefinedTypeBuilder()
            .WithQualifiedName("dbo", "MyType")
            .WithKind(UserDefinedTypeKind.Alias)
            .WithDbType(DbType.Int32)
            .WithNativeTypeName("int");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WithSystemType*");
    }
}
