-- Rerunnable SQL Server role, feature-toggle, and permission seed
-- Creates three baseline users: admin, owner, and user
-- Applies restrictions using AdminFeatures + AdminPermissions + Auth roles

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- -------------------------------------------------------------------------
    -- 1) Auth roles (used by JWT/authz)
    -- -------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Admin')
    BEGIN
        INSERT INTO [Roles] ([Id], [Name], [Description])
        VALUES (N'seed-role-admin', N'Admin', N'Application administrator with full access');
    END

    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Owner')
    BEGIN
        INSERT INTO [Roles] ([Id], [Name], [Description])
        VALUES (N'seed-role-owner', N'Owner', N'Business owner role with non-admin module governance');
    END

    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'User')
    BEGIN
        INSERT INTO [Roles] ([Id], [Name], [Description])
        VALUES (N'seed-role-user', N'User', N'Standard data-entry user role');
    END

    -- -------------------------------------------------------------------------
    -- 2) Auth users (login users)
    -- -------------------------------------------------------------------------
    DELETE FROM [UserRoles]
    WHERE [UserId] IN (N'seed-auth-admin', N'seed-auth-owner', N'seed-auth-user');

    DELETE FROM [Users]
    WHERE [Id] IN (N'seed-auth-admin', N'seed-auth-owner', N'seed-auth-user');

    INSERT INTO [Users] (
        [Id], [Username], [Email], [FirstName], [LastName], [PasswordHash], [IsActive],
        [AccessStatus], [RequestedRoleName], [AccessRequestNotes], [AccessRequestedAt],
        [AccessReviewedBy], [AccessReviewNotes], [AccessReviewedAt], [LastLogin]
    ) VALUES
    (
        N'seed-auth-admin', N'admin.seed', N'admin.seed@quotation.local', N'System', N'Admin', N'Admin@123', 1,
        N'Approved', N'Admin', N'Seeded baseline admin user', SYSUTCDATETIME(),
        N'seed-script', N'Approved by seed script', SYSUTCDATETIME(), NULL
    ),
    (
        N'seed-auth-owner', N'owner.seed', N'owner.seed@quotation.local', N'Business', N'Owner', N'Owner@123', 1,
        N'Approved', N'Owner', N'Seeded baseline owner user', SYSUTCDATETIME(),
        N'seed-script', N'Approved by seed script', SYSUTCDATETIME(), NULL
    ),
    (
        N'seed-auth-user', N'user.seed', N'user.seed@quotation.local', N'Data', N'Entry', N'User@123', 1,
        N'Approved', N'User', N'Seeded baseline user user', SYSUTCDATETIME(),
        N'seed-script', N'Approved by seed script', SYSUTCDATETIME(), NULL
    );

    INSERT INTO [UserRoles] ([UserId], [RoleId])
    SELECT N'seed-auth-admin', r.[Id] FROM [Roles] r WHERE r.[Name] = N'Admin';

    INSERT INTO [UserRoles] ([UserId], [RoleId])
    SELECT N'seed-auth-owner', r.[Id] FROM [Roles] r WHERE r.[Name] = N'Owner';

    INSERT INTO [UserRoles] ([UserId], [RoleId])
    SELECT N'seed-auth-user', r.[Id] FROM [Roles] r WHERE r.[Name] = N'User';

    -- -------------------------------------------------------------------------
    -- 3) Admin module groups and users (used by admin feature/permission pages)
    -- -------------------------------------------------------------------------
    DELETE FROM [AdminPermissions]
    WHERE [Id] LIKE N'seed-perm-%';

    DELETE FROM [AdminUsers]
    WHERE [Id] IN (N'seed-admin-admin', N'seed-admin-owner', N'seed-admin-user');

    DELETE FROM [AdminUserGroups]
    WHERE [Id] IN (N'seed-group-admin', N'seed-group-owner', N'seed-group-user');

    DELETE FROM [AdminFeatures]
    WHERE [Id] LIKE N'seed-feature-%';

    INSERT INTO [AdminUserGroups] (
        [Id], [Name], [Description], [PermissionsJson], [ParentGroup], [MembersJson], [IsDeleted]
    ) VALUES
    (
        N'seed-group-admin',
        N'Admin Group',
        N'Full access group for administrators. Includes wildcard permission for future modules.',
        N'["*"]',
        NULL,
        N'["seed-admin-admin"]',
        0
    ),
    (
        N'seed-group-owner',
        N'Owner Group',
        N'Owner group with access to all non-admin modules and non-admin approvals.',
        N'["module.*","approve.nonadmin","request.review","request.reject"]',
        NULL,
        N'["seed-admin-owner"]',
        0
    ),
    (
        N'seed-group-user',
        N'User Group',
        N'User group with accounts data submit-only capability.',
        N'["accounts.submit"]',
        NULL,
        N'["seed-admin-user"]',
        0
    );

    INSERT INTO [AdminUsers] (
        [Id], [Username], [Email], [FirstName], [LastName], [Role], [Status], [CreatedAt], [LastLoginAt], [GroupsJson], [IsDeleted]
    ) VALUES
    (
        N'seed-admin-admin',
        N'admin.seed',
        N'admin.seed@quotation.local',
        N'System',
        N'Admin',
        N'Admin',
        N'active',
        SYSUTCDATETIME(),
        NULL,
        N'["seed-group-admin"]',
        0
    ),
    (
        N'seed-admin-owner',
        N'owner.seed',
        N'owner.seed@quotation.local',
        N'Business',
        N'Owner',
        N'Owner',
        N'active',
        SYSUTCDATETIME(),
        NULL,
        N'["seed-group-owner"]',
        0
    ),
    (
        N'seed-admin-user',
        N'user.seed',
        N'user.seed@quotation.local',
        N'Data',
        N'Entry',
        N'User',
        N'active',
        SYSUTCDATETIME(),
        NULL,
        N'["seed-group-user"]',
        0
    );

    -- -------------------------------------------------------------------------
    -- 4) Feature toggles across modules
    -- -------------------------------------------------------------------------
    INSERT INTO [AdminFeatures] (
        [Id], [Name], [Description], [Key], [IsActive], [EnabledRolesJson], [CreatedAt], [UpdatedAt], [IsDeleted]
    ) VALUES
    (N'seed-feature-dashboard', N'Dashboard', N'Main dashboard module', N'module.dashboard', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-quotation', N'Quotation', N'Quotation module', N'module.quotation', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-customer', N'Customer', N'Customer module', N'module.customer', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-order-details', N'Order Details', N'Invoice and order details module', N'module.order-details', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-sales', N'Sales', N'Sales module', N'module.sales', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-employee', N'Employee', N'Employee module', N'module.employee', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-accounts', N'Accounts', N'Accounts module', N'module.accounts', 1, N'["Admin","Owner","User"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-inventory', N'Inventory', N'Inventory module', N'module.inventory', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-items', N'Items', N'Items module', N'module.items', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-lov', N'List Of Values', N'LOV module', N'module.list-of-values', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-access-requests', N'Access Requests', N'Approve or reject role access requests', N'module.access-requests', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-admin-governance', N'Admin Governance', N'Admin governance surfaces: users/groups/features/settings/permissions/audit', N'module.admin-governance', 1, N'["Admin"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-accounts-submit', N'Accounts Submit', N'Submit-only access for accounts data entry', N'module.accounts.submit', 1, N'["Admin","Owner","User"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
    (N'seed-feature-accounts-approve', N'Accounts Approve', N'Approval actions in accounts workflows', N'module.accounts.approve', 1, N'["Admin","Owner"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

    -- -------------------------------------------------------------------------
    -- 5) Permissions per group x feature
    -- -------------------------------------------------------------------------
    INSERT INTO [AdminPermissions] (
        [Id], [GroupId], [FeatureId], [PermissionsJson], [GrantedAt], [GrantedBy], [IsDeleted]
    ) VALUES
    (N'seed-perm-admin-dashboard', N'seed-group-admin', N'seed-feature-dashboard', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-quotation', N'seed-group-admin', N'seed-feature-quotation', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-customer', N'seed-group-admin', N'seed-feature-customer', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-order-details', N'seed-group-admin', N'seed-feature-order-details', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-sales', N'seed-group-admin', N'seed-feature-sales', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-employee', N'seed-group-admin', N'seed-feature-employee', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-accounts', N'seed-group-admin', N'seed-feature-accounts', N'["read","write","submit","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-inventory', N'seed-group-admin', N'seed-feature-inventory', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-items', N'seed-group-admin', N'seed-feature-items', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-lov', N'seed-group-admin', N'seed-feature-lov', N'["read","write","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-access-requests', N'seed-group-admin', N'seed-feature-access-requests', N'["read","approve","reject","admin","approve.admin"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-governance', N'seed-group-admin', N'seed-feature-admin-governance', N'["read","write","delete","approve","admin","*"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-accounts-submit', N'seed-group-admin', N'seed-feature-accounts-submit', N'["submit","read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-admin-accounts-approve', N'seed-group-admin', N'seed-feature-accounts-approve', N'["read","approve","reject"]', SYSUTCDATETIME(), N'usersetup-script', 0),

    (N'seed-perm-owner-dashboard', N'seed-group-owner', N'seed-feature-dashboard', N'["read","write"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-quotation', N'seed-group-owner', N'seed-feature-quotation', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-customer', N'seed-group-owner', N'seed-feature-customer', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-order-details', N'seed-group-owner', N'seed-feature-order-details', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-sales', N'seed-group-owner', N'seed-feature-sales', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-employee', N'seed-group-owner', N'seed-feature-employee', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-accounts', N'seed-group-owner', N'seed-feature-accounts', N'["read","write","submit","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-inventory', N'seed-group-owner', N'seed-feature-inventory', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-items', N'seed-group-owner', N'seed-feature-items', N'["read","write","approve"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-lov', N'seed-group-owner', N'seed-feature-lov', N'["read","write"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-access-requests', N'seed-group-owner', N'seed-feature-access-requests', N'["read","approve","reject","approve.nonadmin"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-accounts-submit', N'seed-group-owner', N'seed-feature-accounts-submit', N'["submit","read","write"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-owner-accounts-approve', N'seed-group-owner', N'seed-feature-accounts-approve', N'["read","approve","reject"]', SYSUTCDATETIME(), N'usersetup-script', 0),

    (N'seed-perm-user-accounts', N'seed-group-user', N'seed-feature-accounts', N'["read","submit"]', SYSUTCDATETIME(), N'usersetup-script', 0),
    (N'seed-perm-user-accounts-submit', N'seed-group-user', N'seed-feature-accounts-submit', N'["submit"]', SYSUTCDATETIME(), N'usersetup-script', 0);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
