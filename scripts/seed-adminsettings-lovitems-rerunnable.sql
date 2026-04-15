-- Rerunnable PostgreSQL seed script template
-- Source: convert from raw SQL using scripts/normalize-seed-sql.ps1
-- Rules enforced by normalizer:
-- 1) Uses TRUNCATE ... RESTART IDENTITY CASCADE for reruns
-- 2) Converts all hard-coded TIMESTAMP literals to CURRENT_TIMESTAMP

BEGIN;

TRUNCATE TABLE "LovItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE "AdminSystemSettings" RESTART IDENTITY CASCADE;

-- Paste generated INSERT statements below this line.
-- Example:
-- INSERT INTO "AdminSystemSettings" ("Id","Key","Value","Description","Category","Type","IsEditable","UpdatedAt","UpdatedBy","IsDeleted")
-- VALUES ('calc-001','gsmFactor','1550','GSM Factor for paper calculation','Calculations','decimal',TRUE,CURRENT_TIMESTAMP,'system',FALSE);

COMMIT;

