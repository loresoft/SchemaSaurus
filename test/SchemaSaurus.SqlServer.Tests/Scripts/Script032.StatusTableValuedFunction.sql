CREATE OR ALTER FUNCTION dbo.GetStatusDropdown()
RETURNS TABLE
AS
RETURN
(
    SELECT [Id], [Name], [DisplayOrder]
    FROM [dbo].[Status]
);
