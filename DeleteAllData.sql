-- First, delete data from the child table (BoardItems) if it exists
DELETE FROM dbo.BoardItems;

-- Then, delete from the next child table (BoardColumns)
DELETE FROM dbo.BoardColumns;

-- Finally, delete from the parent table (Boards)
DELETE FROM dbo.Boards;

-- If you want to reset identity columns, use TRUNCATE instead (if no foreign key constraints)
-- TRUNCATE TABLE dbo.BoardItems;
-- TRUNCATE TABLE dbo.BoardColumns;
-- TRUNCATE TABLE dbo.Boards;

-- To reset the identity values (if using DELETE)
DBCC CHECKIDENT ('dbo.Boards', RESEED, 0);
DBCC CHECKIDENT ('dbo.BoardColumns', RESEED, 0);
DBCC CHECKIDENT ('dbo.BoardItems', RESEED, 0);
