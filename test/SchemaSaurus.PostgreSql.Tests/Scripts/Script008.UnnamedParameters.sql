-- unnamed input combined with a named OUT parameter => proargnames = {'', 'result'}
CREATE OR REPLACE FUNCTION public."UnnamedInputWithOutput"(
    integer,
    OUT "Result" text
)
LANGUAGE sql
AS $$
    SELECT 'x'::text;
$$;

-- unnamed input on a RETURNS TABLE function => proargnames = {''}
CREATE OR REPLACE FUNCTION public."UnnamedInputReturnsTable"(
    integer
)
RETURNS TABLE ("Id" integer)
LANGUAGE sql
AS $$
    SELECT 1;
$$;

-- no named parameters at all => proargnames IS NULL
CREATE OR REPLACE FUNCTION public."UnnamedInputScalar"(
    integer
)
RETURNS integer
LANGUAGE sql
AS $$
    SELECT $1;
$$;
