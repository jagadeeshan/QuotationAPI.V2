-- Rerunnable PostgreSQL role, feature-toggle, and permission seed
-- Creates three baseline users: admin, owner, and user
-- Applies restrictions using AdminFeatures + AdminPermissions + Auth roles

BEGIN;

-- -----------------------------------------------------------------------------
-- 1) Auth roles (used by JWT/authz)
-- -----------------------------------------------------------------------------
INSERT INTO "Roles" ("Id", "Name", "Description")
SELECT 'seed-role-admin', 'Admin', 'Application administrator with full access'
WHERE NOT EXISTS (
    SELECT 1 FROM "Roles" WHERE "Name" = 'Admin'
);

INSERT INTO "Roles" ("Id", "Name", "Description")
SELECT 'seed-role-owner', 'Owner', 'Business owner role with non-admin module governance'
WHERE NOT EXISTS (
    SELECT 1 FROM "Roles" WHERE "Name" = 'Owner'
);

INSERT INTO "Roles" ("Id", "Name", "Description")
SELECT 'seed-role-user', 'User', 'Standard data-entry user role'
WHERE NOT EXISTS (
    SELECT 1 FROM "Roles" WHERE "Name" = 'User'
);

-- -----------------------------------------------------------------------------
-- 2) Auth users (login users)
-- -----------------------------------------------------------------------------
DELETE FROM "UserRoles"
WHERE "UserId" IN ('seed-auth-admin', 'seed-auth-owner', 'seed-auth-user');

DELETE FROM "Users"
WHERE "Id" IN ('seed-auth-admin', 'seed-auth-owner', 'seed-auth-user');

INSERT INTO "Users" (
    "Id", "Username", "Email", "FirstName", "LastName", "PasswordHash", "IsActive",
    "AccessStatus", "RequestedRoleName", "AccessRequestNotes", "AccessRequestedAt",
    "AccessReviewedBy", "AccessReviewNotes", "AccessReviewedAt", "LastLogin"
) VALUES
(
    'seed-auth-admin',
    'admin.seed',
    'admin.seed@quotation.local',
    'System',
    'Admin',
    'Admin@123',
    TRUE,
    'Approved',
    'Admin',
    'Seeded baseline admin user',
    CURRENT_TIMESTAMP,
    'seed-script',
    'Approved by seed script',
    CURRENT_TIMESTAMP,
    NULL
),
(
    'seed-auth-owner',
    'owner.seed',
    'owner.seed@quotation.local',
    'Business',
    'Owner',
    'Owner@123',
    TRUE,
    'Approved',
    'Owner',
    'Seeded baseline owner user',
    CURRENT_TIMESTAMP,
    'seed-script',
    'Approved by seed script',
    CURRENT_TIMESTAMP,
    NULL
),
(
    'seed-auth-user',
    'user.seed',
    'user.seed@quotation.local',
    'Data',
    'Entry',
    'User@123',
    TRUE,
    'Approved',
    'User',
    'Seeded baseline user user',
    CURRENT_TIMESTAMP,
    'seed-script',
    'Approved by seed script',
    CURRENT_TIMESTAMP,
    NULL
);

INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT 'seed-auth-admin', r."Id"
FROM "Roles" r
WHERE r."Name" = 'Admin';

INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT 'seed-auth-owner', r."Id"
FROM "Roles" r
WHERE r."Name" = 'Owner';

INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT 'seed-auth-user', r."Id"
FROM "Roles" r
WHERE r."Name" = 'User';

-- -----------------------------------------------------------------------------
-- 3) Admin module groups and users (used by admin feature/permission pages)
-- -----------------------------------------------------------------------------
DELETE FROM "AdminPermissions"
WHERE "Id" LIKE 'seed-perm-%';

DELETE FROM "AdminUsers"
WHERE "Id" IN ('seed-admin-admin', 'seed-admin-owner', 'seed-admin-user');

DELETE FROM "AdminUserGroups"
WHERE "Id" IN ('seed-group-admin', 'seed-group-owner', 'seed-group-user');

DELETE FROM "AdminFeatures"
WHERE "Id" LIKE 'seed-feature-%';

INSERT INTO "AdminUserGroups" (
    "Id", "Name", "Description", "PermissionsJson", "ParentGroup", "MembersJson", "IsDeleted"
) VALUES
(
    'seed-group-admin',
    'Admin Group',
    'Full access group for administrators. Includes wildcard permission for future modules.',
    '["*"]',
    NULL,
    '["seed-admin-admin"]',
    FALSE
),
(
    'seed-group-owner',
    'Owner Group',
    'Owner group with access to all non-admin modules and non-admin approvals.',
    '["module.*","approve.nonadmin","request.review","request.reject"]',
    NULL,
    '["seed-admin-owner"]',
    FALSE
),
(
    'seed-group-user',
    'User Group',
    'User group with accounts data submit-only capability.',
    '["accounts.submit"]',
    NULL,
    '["seed-admin-user"]',
    FALSE
);

INSERT INTO "AdminUsers" (
    "Id", "Username", "Email", "FirstName", "LastName", "Role", "Status", "CreatedAt", "LastLoginAt", "GroupsJson", "IsDeleted"
) VALUES
(
    'seed-admin-admin',
    'admin.seed',
    'admin.seed@quotation.local',
    'System',
    'Admin',
    'Admin',
    'active',
    CURRENT_TIMESTAMP,
    NULL,
    '["seed-group-admin"]',
    FALSE
),
(
    'seed-admin-owner',
    'owner.seed',
    'owner.seed@quotation.local',
    'Business',
    'Owner',
    'Owner',
    'active',
    CURRENT_TIMESTAMP,
    NULL,
    '["seed-group-owner"]',
    FALSE
),
(
    'seed-admin-user',
    'user.seed',
    'user.seed@quotation.local',
    'Data',
    'Entry',
    'User',
    'active',
    CURRENT_TIMESTAMP,
    NULL,
    '["seed-group-user"]',
    FALSE
);

-- -----------------------------------------------------------------------------
-- 4) Feature toggles across modules
--    Admin has all features.
--    Owner has all business features, but no admin governance features.
--    User has accounts submit-only feature.
-- -----------------------------------------------------------------------------
INSERT INTO "AdminFeatures" (
    "Id", "Name", "Description", "Key", "IsActive", "EnabledRolesJson", "CreatedAt", "UpdatedAt", "IsDeleted"
) VALUES
('seed-feature-dashboard', 'Dashboard', 'Main dashboard module', 'module.dashboard', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-quotation', 'Quotation', 'Quotation module', 'module.quotation', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-customer', 'Customer', 'Customer module', 'module.customer', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-order-details', 'Order Details', 'Invoice and order details module', 'module.order-details', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-sales', 'Sales', 'Sales module', 'module.sales', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-employee', 'Employee', 'Employee module', 'module.employee', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-accounts', 'Accounts', 'Accounts module', 'module.accounts', TRUE, '["Admin","Owner","User"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-inventory', 'Inventory', 'Inventory module', 'module.inventory', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-items', 'Items', 'Items module', 'module.items', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-lov', 'List Of Values', 'LOV module', 'module.list-of-values', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-access-requests', 'Access Requests', 'Approve or reject role access requests', 'module.access-requests', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-admin-governance', 'Admin Governance', 'Admin governance surfaces: users/groups/features/settings/permissions/audit', 'module.admin-governance', TRUE, '["Admin"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-accounts-submit', 'Accounts Submit', 'Submit-only access for accounts data entry', 'module.accounts.submit', TRUE, '["Admin","Owner","User"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
('seed-feature-accounts-approve', 'Accounts Approve', 'Approval actions in accounts workflows', 'module.accounts.approve', TRUE, '["Admin","Owner"]', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE);

-- -----------------------------------------------------------------------------
-- 5) Permissions per group x feature
-- -----------------------------------------------------------------------------
INSERT INTO "AdminPermissions" (
    "Id", "GroupId", "FeatureId", "PermissionsJson", "GrantedAt", "GrantedBy", "IsDeleted"
) VALUES
('seed-perm-admin-dashboard', 'seed-group-admin', 'seed-feature-dashboard', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-quotation', 'seed-group-admin', 'seed-feature-quotation', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-customer', 'seed-group-admin', 'seed-feature-customer', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-order-details', 'seed-group-admin', 'seed-feature-order-details', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-sales', 'seed-group-admin', 'seed-feature-sales', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-employee', 'seed-group-admin', 'seed-feature-employee', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-accounts', 'seed-group-admin', 'seed-feature-accounts', '["read","write","submit","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-inventory', 'seed-group-admin', 'seed-feature-inventory', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-items', 'seed-group-admin', 'seed-feature-items', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-lov', 'seed-group-admin', 'seed-feature-lov', '["read","write","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-access-requests', 'seed-group-admin', 'seed-feature-access-requests', '["read","approve","reject","admin","approve.admin"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-governance', 'seed-group-admin', 'seed-feature-admin-governance', '["read","write","delete","approve","admin","*"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-accounts-submit', 'seed-group-admin', 'seed-feature-accounts-submit', '["submit","read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-admin-accounts-approve', 'seed-group-admin', 'seed-feature-accounts-approve', '["read","approve","reject"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),

('seed-perm-owner-dashboard', 'seed-group-owner', 'seed-feature-dashboard', '["read","write"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-quotation', 'seed-group-owner', 'seed-feature-quotation', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-customer', 'seed-group-owner', 'seed-feature-customer', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-order-details', 'seed-group-owner', 'seed-feature-order-details', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-sales', 'seed-group-owner', 'seed-feature-sales', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-employee', 'seed-group-owner', 'seed-feature-employee', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-accounts', 'seed-group-owner', 'seed-feature-accounts', '["read","write","submit","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-inventory', 'seed-group-owner', 'seed-feature-inventory', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-items', 'seed-group-owner', 'seed-feature-items', '["read","write","approve"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-lov', 'seed-group-owner', 'seed-feature-lov', '["read","write"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-access-requests', 'seed-group-owner', 'seed-feature-access-requests', '["read","approve","reject","approve.nonadmin"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-accounts-submit', 'seed-group-owner', 'seed-feature-accounts-submit', '["submit","read","write"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-owner-accounts-approve', 'seed-group-owner', 'seed-feature-accounts-approve', '["read","approve","reject"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),

('seed-perm-user-accounts', 'seed-group-user', 'seed-feature-accounts', '["read","submit"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE),
('seed-perm-user-accounts-submit', 'seed-group-user', 'seed-feature-accounts-submit', '["submit"]', CURRENT_TIMESTAMP, 'usersetup-script', FALSE);

COMMIT;
