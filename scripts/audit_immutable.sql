-- Run after the first migration. Makes agent_audit_logs append-only at the database level (compliance evidence).
CREATE OR REPLACE FUNCTION forbid_audit_change() RETURNS trigger AS $$
BEGIN RAISE EXCEPTION 'agent_audit_logs is append-only'; END; $$ LANGUAGE plpgsql;
CREATE TRIGGER agent_audit_logs_immutable BEFORE UPDATE OR DELETE ON agent_audit_logs
FOR EACH ROW EXECUTE FUNCTION forbid_audit_change();
