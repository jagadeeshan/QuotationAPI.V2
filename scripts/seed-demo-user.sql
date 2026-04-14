-- Seed demo user for testing
-- Username: demo
-- Password: Demo@123

-- Check if user already exists
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'demo')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, IsActive, CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate)
    VALUES (
        'demo',
        'demo@quotation.local',
        -- This is a placeholder - use bcrypt hash for actual password
        '$2a$11$YourBcryptHashHereForDemo@123',
        'Demo',
        'User',
        1,
        'system',
        GETUTCDATE(),
        'system',
        GETUTCDATE()
    )
END

-- Verify insertion
SELECT * FROM Users WHERE Username = 'demo'
