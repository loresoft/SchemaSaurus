CREATE OR REPLACE FUNCTION public."AuditStatusChange"()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER "TR_Status_Audit"
AFTER INSERT OR UPDATE ON public."Status"
FOR EACH ROW
EXECUTE FUNCTION public."AuditStatusChange"();

CREATE TRIGGER "TR_Status_PreventDelete"
BEFORE DELETE ON public."Status"
FOR EACH ROW
EXECUTE FUNCTION public."AuditStatusChange"();

ALTER TABLE public."Status" DISABLE TRIGGER "TR_Status_PreventDelete";
