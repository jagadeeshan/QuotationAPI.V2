SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Keep only configuration baseline data
    DECLARE @KeepTables TABLE (TableName SYSNAME PRIMARY KEY);
    INSERT INTO @KeepTables (TableName)
    VALUES
        (N'__EFMigrationsHistory'),
        (N'AdminSystemSettings'),
        (N'LovItems');

    DECLARE @sql NVARCHAR(MAX) = N'';

    -- Disable constraints before deleting from many related tables
    SELECT @sql = STRING_AGG(
        N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;',
        CHAR(10)
    )
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name NOT IN (SELECT TableName FROM @KeepTables);

    IF @sql IS NOT NULL AND LEN(@sql) > 0
        EXEC sp_executesql @sql;

    SET @sql = N'';

    -- Delete all transactional/master rows except explicit keep tables
    SELECT @sql = STRING_AGG(
        N'DELETE FROM ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';',
        CHAR(10)
    )
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name NOT IN (SELECT TableName FROM @KeepTables);

    IF @sql IS NOT NULL AND LEN(@sql) > 0
        EXEC sp_executesql @sql;

    SET @sql = N'';

    -- Re-enable and validate constraints
    SELECT @sql = STRING_AGG(
        N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;',
        CHAR(10)
    )
    FROM sys.tables t
    WHERE t.is_ms_shipped = 0
      AND t.name NOT IN (SELECT TableName FROM @KeepTables);

    IF @sql IS NOT NULL AND LEN(@sql) > 0
        EXEC sp_executesql @sql;

    COMMIT TRANSACTION;

    SELECT
        t.name AS TableName,
        SUM(p.rows) AS RemainingRows
    FROM sys.tables t
    JOIN sys.partitions p
      ON p.object_id = t.object_id
     AND p.index_id IN (0, 1)
    WHERE t.is_ms_shipped = 0
      AND t.name IN (N'AdminSystemSettings', N'LovItems')
    GROUP BY t.name
    ORDER BY t.name;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
