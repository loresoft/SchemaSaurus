namespace SchemaSaurus.Metadata;

/// <summary>
/// Base class for walking an entire <see cref="DatabaseModel"/> object graph.
/// </summary>
/// <remarks>
/// The default implementation performs a depth-first traversal of every metadata object,
/// calling the appropriate <c>Visit</c> method for each node. Subclasses override only the
/// methods they care about — for example, a code generator might override
/// <see cref="VisitTable"/> and <see cref="VisitColumn"/> while ignoring sequences and
/// stored procedures.
/// <para>
/// Traversal order within the <see cref="DatabaseModel"/>:
/// tables → views → sequences → stored procedures → scalar functions →
/// table-valued functions → user-defined types.
/// </para>
/// </remarks>
public class DatabaseVisitor
{
    /// <summary>
    /// Visits the root <see cref="DatabaseModel"/> and traverses all child collections.
    /// </summary>
    /// <param name="model">The database model to visit.</param>
    public virtual void VisitDatabase(DatabaseModel model)
    {
        foreach (var table in model.Tables)
            VisitTable(table);

        foreach (var view in model.Views)
            VisitView(view);

        foreach (var sequence in model.Sequences)
            VisitSequence(sequence);

        foreach (var storedProcedure in model.StoredProcedures)
            VisitStoredProcedure(storedProcedure);

        foreach (var scalarFunction in model.ScalarFunctions)
            VisitScalarFunction(scalarFunction);

        foreach (var tableValuedFunction in model.TableValuedFunctions)
            VisitTableValuedFunction(tableValuedFunction);

        foreach (var userDefinedType in model.UserDefinedTypes)
            VisitUserDefinedType(userDefinedType);
    }

    /// <summary>
    /// Visits a <see cref="RelationBase"/> (table or view) and traverses its columns,
    /// indexes, and triggers. Called by both <see cref="VisitTable"/> and
    /// <see cref="VisitView"/> before visiting type-specific children.
    /// </summary>
    /// <param name="relation">The relation to visit.</param>
    public virtual void VisitRelation(RelationBase relation)
    {
        foreach (var column in relation.Columns)
            VisitColumn(column);

        foreach (var index in relation.Indexes)
            VisitIndex(index);

        foreach (var trigger in relation.Triggers)
            VisitTrigger(trigger);
    }

    /// <summary>
    /// Visits a <see cref="Table"/> and traverses its shared relation members
    /// (via <see cref="VisitRelation"/>), then its constraints: primary key,
    /// unique constraints, check constraints, and foreign keys.
    /// </summary>
    /// <param name="table">The table to visit.</param>
    public virtual void VisitTable(Table table)
    {
        VisitRelation(table);

        if (table.PrimaryKey is { } primaryKey)
            VisitPrimaryKey(primaryKey);

        foreach (var uniqueConstraint in table.UniqueConstraints)
            VisitUniqueConstraint(uniqueConstraint);

        foreach (var checkConstraint in table.CheckConstraints)
            VisitCheckConstraint(checkConstraint);

        foreach (var foreignKey in table.ForeignKeys)
            VisitForeignKey(foreignKey);
    }

    /// <summary>
    /// Visits a <see cref="View"/> and traverses its shared relation members
    /// via <see cref="VisitRelation"/>.
    /// </summary>
    /// <param name="view">The view to visit.</param>
    public virtual void VisitView(View view)
    {
        VisitRelation(view);
    }

    /// <summary>
    /// Visits a single <see cref="Column"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="column">The column to visit.</param>
    public virtual void VisitColumn(Column column) { }

    /// <summary>
    /// Visits a <see cref="PrimaryKey"/> constraint. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="primaryKey">The primary key to visit.</param>
    public virtual void VisitPrimaryKey(PrimaryKey primaryKey) { }

    /// <summary>
    /// Visits a <see cref="UniqueConstraint"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="uniqueConstraint">The unique constraint to visit.</param>
    public virtual void VisitUniqueConstraint(UniqueConstraint uniqueConstraint) { }

    /// <summary>
    /// Visits a <see cref="CheckConstraint"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="checkConstraint">The check constraint to visit.</param>
    public virtual void VisitCheckConstraint(CheckConstraint checkConstraint) { }

    /// <summary>
    /// Visits a <see cref="ForeignKey"/> constraint. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="foreignKey">The foreign key to visit.</param>
    public virtual void VisitForeignKey(ForeignKey foreignKey) { }

    /// <summary>
    /// Visits an <see cref="Index"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="index">The index to visit.</param>
    public virtual void VisitIndex(Index index) { }

    /// <summary>
    /// Visits a <see cref="Trigger"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="trigger">The trigger to visit.</param>
    public virtual void VisitTrigger(Trigger trigger) { }

    /// <summary>
    /// Visits a <see cref="Sequence"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="sequence">The sequence to visit.</param>
    public virtual void VisitSequence(Sequence sequence) { }

    /// <summary>
    /// Visits a <see cref="StoredProcedure"/> and traverses its parameters.
    /// </summary>
    /// <param name="storedProcedure">The stored procedure to visit.</param>
    public virtual void VisitStoredProcedure(StoredProcedure storedProcedure)
    {
        foreach (var parameter in storedProcedure.Parameters)
            VisitParameter(parameter);
    }

    /// <summary>
    /// Visits a <see cref="ScalarFunction"/> and traverses its parameters.
    /// </summary>
    /// <param name="scalarFunction">The scalar function to visit.</param>
    public virtual void VisitScalarFunction(ScalarFunction scalarFunction)
    {
        foreach (var parameter in scalarFunction.Parameters)
            VisitParameter(parameter);
    }

    /// <summary>
    /// Visits a <see cref="TableValuedFunction"/> and traverses its parameters.
    /// </summary>
    /// <param name="tableValuedFunction">The table-valued function to visit.</param>
    public virtual void VisitTableValuedFunction(TableValuedFunction tableValuedFunction)
    {
        foreach (var parameter in tableValuedFunction.Parameters)
            VisitParameter(parameter);
    }

    /// <summary>
    /// Visits a <see cref="Parameter"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="parameter">The parameter to visit.</param>
    public virtual void VisitParameter(Parameter parameter) { }

    /// <summary>
    /// Visits a <see cref="UserDefinedType"/>. This is a leaf node — no further
    /// traversal occurs by default.
    /// </summary>
    /// <param name="userDefinedType">The user-defined type to visit.</param>
    public virtual void VisitUserDefinedType(UserDefinedType userDefinedType) { }
}
