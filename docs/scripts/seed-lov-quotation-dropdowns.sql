SET NOCOUNT ON;
BEGIN TRY
  BEGIN TRAN;

  DECLARE @now NVARCHAR(100) = CONVERT(NVARCHAR(100), SYSUTCDATETIME(), 126) + 'Z';

  DECLARE @unitId INT;
  SELECT @unitId = Id FROM dbo.LovItems WHERE Parentvalue IS NULL AND Name = N'Unit';
  IF @unitId IS NULL
  BEGIN
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (NULL, NULL, N'Unit', NULL, N'Length unit options', N'CATEGORY', 100, N'Y', N'system', N'system', @now, @now);
    SET @unitId = SCOPE_IDENTITY();
  END

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @unitId AND Value = 1)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Unit', @unitId, N'Inches', 1, N'Inches', N'CATEGORY_VALUE', 1, N'Y', N'system', N'system', @now, @now);

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @unitId AND Value = 2)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Unit', @unitId, N'Milli Meter', 2, N'Millimeter', N'CATEGORY_VALUE', 2, N'Y', N'system', N'system', @now, @now);

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @unitId AND Value = 3)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Unit', @unitId, N'Centi Meter', 3, N'Centimeter', N'CATEGORY_VALUE', 3, N'Y', N'system', N'system', @now, @now);

  DECLARE @jointId INT;
  SELECT @jointId = Id FROM dbo.LovItems WHERE Parentvalue IS NULL AND Name = N'Joint';
  IF @jointId IS NULL
  BEGIN
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (NULL, NULL, N'Joint', NULL, N'Joint options for box calculations', N'CATEGORY', 101, N'Y', N'system', N'system', @now, @now);
    SET @jointId = SCOPE_IDENTITY();
  END

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @jointId AND Value = 1)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Joint', @jointId, N'Normal', 1, N'Default joint multiplier', N'CATEGORY_VALUE', 1, N'Y', N'system', N'system', @now, @now);

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @jointId AND Value = 2)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Joint', @jointId, N'Double', 2, N'Double joint multiplier', N'CATEGORY_VALUE', 2, N'Y', N'system', N'system', @now, @now);

  IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @jointId AND Value = 4)
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (N'Joint', @jointId, N'Four', 4, N'Four joint multiplier', N'CATEGORY_VALUE', 3, N'Y', N'system', N'system', @now, @now);

  DECLARE @modelId INT;
  SELECT @modelId = Id FROM dbo.LovItems WHERE Parentvalue IS NULL AND Name = N'Model';
  IF @modelId IS NULL
  BEGIN
    INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
    VALUES (NULL, NULL, N'Model', NULL, N'Box model options', N'CATEGORY', 102, N'Y', N'system', N'system', @now, @now);
    SET @modelId = SCOPE_IDENTITY();
  END

  DECLARE @i INT = 1;
  WHILE @i <= 15
  BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.LovItems WHERE Parentvalue = @modelId AND Value = @i)
      INSERT INTO dbo.LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
      VALUES (
        N'Model',
        @modelId,
        CASE @i
          WHEN 1 THEN N'Universal'
          WHEN 2 THEN N'Ltype'
          WHEN 3 THEN N'Top&Bottom'
          WHEN 4 THEN N'Pizza Box'
          WHEN 5 THEN N'InterLock'
          WHEN 6 THEN N'SelfLock'
          WHEN 7 THEN N'Without Top'
          WHEN 8 THEN N'Ltype-Ring'
          WHEN 9 THEN N'Bottom Lock'
          WHEN 10 THEN N'Narrow lock'
          WHEN 11 THEN N'Dual Bottom Lock'
          WHEN 12 THEN N'OverFlap'
          WHEN 13 THEN N'Only Top'
          WHEN 14 THEN N'Universal_Length'
          WHEN 15 THEN N'Universal - close'
        END,
        @i,
        CONCAT(N'Box model ', @i),
        N'CATEGORY_VALUE',
        @i,
        N'Y',
        N'system',
        N'system',
        @now,
        @now
      );

    SET @i += 1;
  END

  COMMIT;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK;
  THROW;
END CATCH;

SELECT c.Name AS CategoryName, v.Name AS ItemName, v.Value, v.Displayorder
FROM dbo.LovItems c
JOIN dbo.LovItems v ON v.Parentvalue = c.Id
WHERE c.Parentvalue IS NULL
  AND c.Name IN (N'Unit', N'Joint', N'Model')
ORDER BY c.Name, v.Displayorder, v.Name;
