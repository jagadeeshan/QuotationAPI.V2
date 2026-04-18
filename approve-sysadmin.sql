-- Approve sysadmin user and assign Admin role
BEGIN TRANSACTION;

-- Update user access status to Approved
UPDATE "Users" 
SET "AccessStatus" = 'Approved',
    "AccessReviewedAt" = NOW(),
    "AccessReviewNotes" = 'Approved for system administration'
WHERE "Username" = 'sysadmin' AND "IsActive" = true;

-- Get or create Admin role
INSERT INTO "Roles" ("Id", "Name", "Description")
VALUES ('admin-role-001', 'Admin', 'Administrator role')
ON CONFLICT ("Name") DO NOTHING;

-- Assign Admin role to sysadmin user
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u, "Roles" r
WHERE u."Username" = 'sysadmin' 
  AND r."Name" = 'Admin'
  AND NOT EXISTS (
    SELECT 1 FROM "UserRoles" ur 
    WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
  );

COMMIT;
