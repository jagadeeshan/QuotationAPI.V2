SET NOCOUNT ON;

BEGIN TRY
	BEGIN TRAN;

	DECLARE @now DATETIME = GETDATE();
	DECLARE @system NVARCHAR(100) = 'system';

	DECLARE @Categories TABLE (
		Name NVARCHAR(255) NOT NULL,
		Description NVARCHAR(500) NULL,
		DisplayOrder INT NOT NULL,
		ItemType NVARCHAR(100) NOT NULL
	);

	INSERT INTO @Categories (Name, Description, DisplayOrder, ItemType)
	VALUES
		('Admin Permission', 'Administrative permission options', 100, 'CATEGORY'),
		('Admin User Role', 'Administrative user roles', 101, 'CATEGORY'),
		('Admin User Status', 'Administrative user statuses', 102, 'CATEGORY'),
		('Commission Type', 'Commission calculation types', 103, 'CATEGORY'),
		('Deduction Type', 'Salary deduction types', 104, 'CATEGORY'),
		('Expense Status', 'Expense entry statuses', 105, 'CATEGORY'),
		('Flute', 'Flute types', 106, 'CATEGORY'),
		('Income Status', 'Income entry statuses', 107, 'CATEGORY'),
		('Material', 'Material types', 108, 'CATEGORY'),
		('OT Multiplier', 'Overtime multiplier options', 109, 'CATEGORY'),
		('Salary Advance Status', 'Salary advance statuses', 110, 'CATEGORY'),
		('Setting Category', 'System setting categories', 111, 'CATEGORY'),
		('Setting Type', 'System setting value types', 112, 'CATEGORY'),
		('Transaction Type', 'Account transaction types', 113, 'CATEGORY');

	INSERT INTO dbo.LovItems (
		Parentname,
		Parentvalue,
		Name,
		Value,
		Description,
		Itemtype,
		Displayorder,
		Isactive,
		Createdby,
		Updatedby,
		Createddt,
		Updateddt
	)
	SELECT
		NULL,
		NULL,
		c.Name,
		NULL,
		c.Description,
		c.ItemType,
		c.DisplayOrder,
		'Y',
		@system,
		@system,
		@now,
		@now
	FROM @Categories c
	WHERE NOT EXISTS (
		SELECT 1
		FROM dbo.LovItems existing
		WHERE existing.Parentvalue IS NULL
		  AND LTRIM(RTRIM(existing.Name)) = c.Name
	);

	DECLARE @Values TABLE (
		CategoryName NVARCHAR(255) NOT NULL,
		Name NVARCHAR(255) NOT NULL,
		Value INT NOT NULL,
		Description NVARCHAR(500) NULL,
		DisplayOrder INT NOT NULL,
		ItemType NVARCHAR(100) NOT NULL
	);

	INSERT INTO @Values (CategoryName, Name, Value, Description, DisplayOrder, ItemType)
	VALUES
		('Admin Permission', 'view', 1, 'Read permission', 1, 'PERMISSION_VALUE'),
		('Admin Permission', 'create', 2, 'Create permission', 2, 'PERMISSION_VALUE'),
		('Admin Permission', 'edit', 3, 'Update permission', 3, 'PERMISSION_VALUE'),
		('Admin Permission', 'delete', 4, 'Delete permission', 4, 'PERMISSION_VALUE'),
		('Admin Permission', 'approve', 5, 'Approve permission', 5, 'PERMISSION_VALUE'),
		('Admin Permission', 'export', 6, 'Export permission', 6, 'PERMISSION_VALUE'),

		('Admin User Role', 'admin', 1, 'Administrator', 1, 'ROLE_VALUE'),
		('Admin User Role', 'manager', 2, 'Manager', 2, 'ROLE_VALUE'),
		('Admin User Role', 'operator', 3, 'Operator', 3, 'ROLE_VALUE'),
		('Admin User Role', 'auditor', 4, 'Auditor', 4, 'ROLE_VALUE'),

		('Admin User Status', 'active', 1, 'Active user', 1, 'STATUS_VALUE'),
		('Admin User Status', 'inactive', 2, 'Inactive user', 2, 'STATUS_VALUE'),
		('Admin User Status', 'locked', 3, 'Locked user', 3, 'STATUS_VALUE'),

		('Commission Type', 'Percent', 1, 'Percentage-based commission', 1, 'CATEGORY_VALUE'),
		('Commission Type', 'Value', 2, 'Fixed-value commission', 2, 'CATEGORY_VALUE'),

		('Deduction Type', 'PF', 1, 'Provident fund', 1, 'CATEGORY_VALUE'),
		('Deduction Type', 'ESI', 2, 'Employee state insurance', 2, 'CATEGORY_VALUE'),
		('Deduction Type', 'TDS', 3, 'Tax deduction at source', 3, 'CATEGORY_VALUE'),
		('Deduction Type', 'Advance', 4, 'Salary advance deduction', 4, 'CATEGORY_VALUE'),
		('Deduction Type', 'Loan', 5, 'Loan recovery', 5, 'CATEGORY_VALUE'),
		('Deduction Type', 'Other', 6, 'Other deductions', 6, 'CATEGORY_VALUE'),

		('Expense Status', 'pending', 1, 'Pending expense', 1, 'STATUS_VALUE'),
		('Expense Status', 'approved', 2, 'Approved expense', 2, 'STATUS_VALUE'),
		('Expense Status', 'paid', 3, 'Paid expense', 3, 'STATUS_VALUE'),
		('Expense Status', 'rejected', 4, 'Rejected expense', 4, 'STATUS_VALUE'),

		('Flute', 'Narrow', 40, 'Legacy flute: Narrow', 1, 'CATEGORY_VALUE'),
		('Flute', 'Broad', 50, 'Legacy flute: Broad', 2, 'CATEGORY_VALUE'),
		('Flute', 'Eflute', 80, 'Legacy flute: Eflute', 3, 'CATEGORY_VALUE'),

		('Income Status', 'pending', 1, 'Pending income', 1, 'STATUS_VALUE'),
		('Income Status', 'received', 2, 'Received income', 2, 'STATUS_VALUE'),
		('Income Status', 'cancelled', 3, 'Cancelled income', 3, 'STATUS_VALUE'),

		('Material', 'Duplex', 1, 'Legacy material: Duplex', 1, 'CATEGORY_VALUE'),
		('Material', 'Top Sheet', 2, 'Legacy material: Top Sheet', 2, 'CATEGORY_VALUE'),
		('Material', 'Base', 3, 'Legacy material: Base', 3, 'CATEGORY_VALUE'),
		('Material', 'Flute', 4, 'Legacy material: Flute', 4, 'CATEGORY_VALUE'),

		('OT Multiplier', '1', 1, 'Overtime multiplier 1x', 1, 'CATEGORY_VALUE'),
		('OT Multiplier', '1.5', 15, 'Overtime multiplier 1.5x', 2, 'CATEGORY_VALUE'),
		('OT Multiplier', '2', 2, 'Overtime multiplier 2x', 3, 'CATEGORY_VALUE'),

		('Salary Advance Status', 'requested', 1, 'Advance requested', 1, 'STATUS_VALUE'),
		('Salary Advance Status', 'approved', 2, 'Advance approved', 2, 'STATUS_VALUE'),
		('Salary Advance Status', 'paid', 3, 'Advance paid', 3, 'STATUS_VALUE'),
		('Salary Advance Status', 'rejected', 4, 'Advance rejected', 4, 'STATUS_VALUE'),

		('Setting Category', 'general', 1, 'General settings', 1, 'CATEGORY_VALUE'),
		('Setting Category', 'security', 2, 'Security settings', 2, 'CATEGORY_VALUE'),
		('Setting Category', 'notification', 3, 'Notification settings', 3, 'CATEGORY_VALUE'),
		('Setting Category', 'finance', 4, 'Finance settings', 4, 'CATEGORY_VALUE'),
		('Setting Category', 'integration', 5, 'Integration settings', 5, 'CATEGORY_VALUE'),

		('Setting Type', 'string', 1, 'Text setting', 1, 'CATEGORY_VALUE'),
		('Setting Type', 'number', 2, 'Numeric setting', 2, 'CATEGORY_VALUE'),
		('Setting Type', 'boolean', 3, 'Boolean setting', 3, 'CATEGORY_VALUE'),
		('Setting Type', 'json', 4, 'JSON setting', 4, 'CATEGORY_VALUE'),

		('Attendance Status', 'weekoff', 5, 'Weekly off', 5, 'STATUS_VALUE'),
		('Attendance Status', 'holiday', 6, 'Holiday', 6, 'STATUS_VALUE'),

		('Transaction Type', 'income', 1, 'Income transaction', 1, 'CATEGORY_VALUE'),
		('Transaction Type', 'expense', 2, 'Expense transaction', 2, 'CATEGORY_VALUE'),
		('Transaction Type', 'initial_balance', 3, 'Initial balance transaction', 3, 'CATEGORY_VALUE'),
		('Transaction Type', 'customer_payment', 4, 'Customer payment transaction', 4, 'CATEGORY_VALUE');

	INSERT INTO dbo.LovItems (
		Parentname,
		Parentvalue,
		Name,
		Value,
		Description,
		Itemtype,
		Displayorder,
		Isactive,
		Createdby,
		Updatedby,
		Createddt,
		Updateddt
	)
	SELECT
		p.Name,
		p.Id,
		v.Name,
		v.Value,
		v.Description,
		v.ItemType,
		v.DisplayOrder,
		'Y',
		@system,
		@system,
		@now,
		@now
	FROM @Values v
	INNER JOIN dbo.LovItems p
		ON p.Parentvalue IS NULL
	   AND p.Name = v.CategoryName
	WHERE NOT EXISTS (
		SELECT 1
		FROM dbo.LovItems existing
		WHERE existing.Parentvalue = p.Id
		  AND LTRIM(RTRIM(existing.Name)) = v.Name
	);

	-- Keep Employee Status values aligned with employee module logic that stores/compares lowercase values.
	UPDATE c
	SET
		c.Name = LOWER(c.Name),
		c.Updatedby = @system,
		c.Updateddt = @now
	FROM dbo.LovItems c
	INNER JOIN dbo.LovItems p
		ON p.Id = c.Parentvalue
	WHERE p.Parentvalue IS NULL
	  AND p.Name = 'Employee Status'
	  AND c.Name IN ('Active', 'Inactive');

	COMMIT TRAN;

	SELECT p.Name AS Category, COUNT(c.Id) AS ChildCount
	FROM dbo.LovItems p
	LEFT JOIN dbo.LovItems c
		ON c.Parentvalue = p.Id
	   AND ISNULL(c.Isactive, 'Y') = 'Y'
	WHERE p.Parentvalue IS NULL
	  AND p.Name IN (
		'Admin Permission', 'Admin User Role', 'Admin User Status', 'Attendance Status',
		'Commission Type', 'Customer Status', 'Customer Type', 'Deduction Type',
		'Designation', 'Employee Department', 'Employee Status', 'Expense Category',
		'Expense Status', 'Flute', 'Income Category', 'Income Status', 'Joint',
		'Material', 'Model', 'OT Multiplier', 'Payment Mode', 'Salary Advance Status',
		'Setting Category', 'Setting Type', 'Transaction Type', 'Unit'
	  )
	GROUP BY p.Name
	ORDER BY p.Name;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRAN;
	THROW;
END CATCH;
