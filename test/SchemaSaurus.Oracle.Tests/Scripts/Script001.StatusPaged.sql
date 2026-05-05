CREATE OR REPLACE PROCEDURE "StatusPaged"(
    "Offset" IN NUMBER,
    "Size" IN NUMBER,
    "Total" OUT NUMBER
)
AS
BEGIN
    SELECT COUNT(*) INTO "Total"
    FROM "Status";
END;
