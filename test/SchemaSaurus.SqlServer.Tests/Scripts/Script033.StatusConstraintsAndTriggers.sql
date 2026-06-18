IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'CK_Status_DisplayOrder_NonNegative'
      AND [parent_object_id] = OBJECT_ID(N'[dbo].[Status]')
)
BEGIN
    ALTER TABLE [dbo].[Status]
    ADD CONSTRAINT [CK_Status_DisplayOrder_NonNegative]
    CHECK ([DisplayOrder] >= 0);
END;

EXEC(N'
CREATE OR ALTER TRIGGER [dbo].[TR_Status_Audit]
ON [dbo].[Status]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    RETURN;
END;
');
