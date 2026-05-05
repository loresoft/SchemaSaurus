CREATE OR REPLACE FUNCTION "FormatAddress"(
    "AddressLine1" IN NVARCHAR2,
    "City" IN NVARCHAR2,
    "StateProvince" IN NVARCHAR2,
    "PostalCode" IN NVARCHAR2
)
RETURN NVARCHAR2
AS
BEGIN
    RETURN "AddressLine1" || ', ' || "City" || ', ' || "StateProvince" || ' ' || "PostalCode";
END;
