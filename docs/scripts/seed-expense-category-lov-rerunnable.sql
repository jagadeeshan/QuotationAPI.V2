-- Rerunnable PostgreSQL seed script for Expense Category LOV values
-- Deletes existing Expense Category child values and re-inserts the canonical list.

BEGIN;

DO $$
DECLARE
    expense_category_id INTEGER;
BEGIN
    SELECT "Id"
    INTO expense_category_id
    FROM "LovItems"
    WHERE "Parentvalue" IS NULL
      AND LOWER("Name") = 'expense category'
    LIMIT 1;

    IF expense_category_id IS NULL THEN
        INSERT INTO "LovItems" (
            "Parentname",
            "Parentvalue",
            "Name",
            "Value",
            "Description",
            "Itemtype",
            "Displayorder",
            "Isactive",
            "Createdby",
            "Updatedby",
            "Createddt",
            "Updateddt",
            "IsDeleted"
        ) VALUES (
            NULL,
            NULL,
            'Expense Category',
            NULL,
            'Business expense categories',
            'CATEGORY',
            1000,
            'Y',
            'system',
            'system',
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP,
            FALSE
        ) RETURNING "Id" INTO expense_category_id;
    END IF;

    DELETE FROM "LovItems"
    WHERE "Parentvalue" = expense_category_id;

    INSERT INTO "LovItems" (
        "Parentname",
        "Parentvalue",
        "Name",
        "Value",
        "Description",
        "Itemtype",
        "Displayorder",
        "Isactive",
        "Createdby",
        "Updatedby",
        "Createddt",
        "Updateddt",
        "IsDeleted"
    ) VALUES
        ('Expense Category', expense_category_id, 'Tea settlement', 1, 'Expense category', 'CATEGORY_VALUE', 1, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Van Expenses', 2, 'Expense category', 'CATEGORY_VALUE', 2, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Personal', 3, 'Expense category', 'CATEGORY_VALUE', 3, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Fevicol', 4, 'Expense category', 'CATEGORY_VALUE', 4, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Rope', 5, 'Expense category', 'CATEGORY_VALUE', 5, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Petrol', 6, 'Expense category', 'CATEGORY_VALUE', 6, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Salary Advance', 7, 'Expense category', 'CATEGORY_VALUE', 7, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Salary Settlement', 8, 'Expense category', 'CATEGORY_VALUE', 8, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Watercan', 9, 'Expense category', 'CATEGORY_VALUE', 9, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Reel Stock', 10, 'Expense category', 'CATEGORY_VALUE', 10, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Extra Labour', 11, 'Expense category', 'CATEGORY_VALUE', 11, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'EB', 12, 'Expense category', 'CATEGORY_VALUE', 12, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Pin', 13, 'Expense category', 'CATEGORY_VALUE', 13, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Gum Powder', 14, 'Expense category', 'CATEGORY_VALUE', 14, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Repair Charges', 15, 'Expense category', 'CATEGORY_VALUE', 15, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Festival Expense', 16, 'Expense category', 'CATEGORY_VALUE', 16, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Bonus', 17, 'Expense category', 'CATEGORY_VALUE', 17, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Assest procurement', 18, 'Expense category', 'CATEGORY_VALUE', 18, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Food Expense', 19, 'Expense category', 'CATEGORY_VALUE', 19, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Other Expense', 20, 'Expense category', 'CATEGORY_VALUE', 20, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Printing Charges', 21, 'Expense category', 'CATEGORY_VALUE', 21, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Dye Making Charges', 22, 'Expense category', 'CATEGORY_VALUE', 22, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Plate Block Making Charges', 23, 'Expense category', 'CATEGORY_VALUE', 23, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Commision Charges', 24, 'Expense category', 'CATEGORY_VALUE', 24, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Paper Conversion', 25, 'Expense category', 'CATEGORY_VALUE', 25, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Civil Repair', 26, 'Expense category', 'CATEGORY_VALUE', 26, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Design Charges', 27, 'Expense category', 'CATEGORY_VALUE', 27, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'GST Payment', 28, 'Expense category', 'CATEGORY_VALUE', 28, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Adjustment', 29, 'Expense category', 'CATEGORY_VALUE', 29, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Credit Card', 30, 'Expense category', 'CATEGORY_VALUE', 30, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Company Loan', 31, 'Expense category', 'CATEGORY_VALUE', 31, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'LABOUR ROOM RENT', 32, 'Expense category', 'CATEGORY_VALUE', 32, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Flop pasting', 33, 'Expense category', 'CATEGORY_VALUE', 33, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE),
        ('Expense Category', expense_category_id, 'Rent payment', 34, 'Expense category', 'CATEGORY_VALUE', 34, 'Y', 'system', 'system', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, FALSE);
END $$;

COMMIT;
