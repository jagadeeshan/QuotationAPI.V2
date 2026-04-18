-- SQL Script to Update Rate Values in AdminSystemSetting Table
-- Date: April 10, 2026
-- Purpose: Update EB rate, Salary per shift, Gum rate, and Pin rate with new values

-- Update EB Rate from 3200 to 150.00 (per day)
UPDATE AdminSystemSetting 
SET Settingvalue = '150', 
    Lastmodifieddate = GETUTCDATE(),
    Lastmodifiedby = 'Admin'
WHERE Settingkey = 'ebRateDefault';

-- Update Salary Per Shift from 850 to 600.00 (per person)
UPDATE AdminSystemSetting 
SET Settingvalue = '600', 
    Lastmodifieddate = GETUTCDATE(),
    Lastmodifiedby = 'Admin'
WHERE Settingkey = 'salaryPerShiftDefault';

-- Update Gum Rate from 610 to 1320.00 (per bag)
UPDATE AdminSystemSetting 
SET Settingvalue = '1320', 
    Lastmodifieddate = GETUTCDATE(),
    Lastmodifiedby = 'Admin'
WHERE Settingkey = 'gumRateDefault';

-- Update Pin Rate from 940 to 98.00 (per kg)
UPDATE AdminSystemSetting 
SET Settingvalue = '98', 
    Lastmodifieddate = GETUTCDATE(),
    Lastmodifiedby = 'Admin'
WHERE Settingkey = 'pinRateDefault';

-- Verify the updates
SELECT Settingkey, Settingvalue, Lastmodifieddate, Lastmodifiedby 
FROM AdminSystemSetting 
WHERE Settingkey IN ('ebRateDefault', 'salaryPerShiftDefault', 'gumRateDefault', 'pinRateDefault')
ORDER BY Settingkey;
