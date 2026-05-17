-- تشغيل هذا في SSMS على قاعدة SchoolsDb لفهم لماذا فشل INSERT/UPDATE على dbo.accountss
-- Run against your SchoolsDb database.

PRINT N'=== 1) أعمدة الجدول + النوع + إن كان Identity ===';

SELECT c.column_id,
       c.name AS column_name,
       t.name AS type_name,
       c.max_length,
       c.precision,
       c.scale,
       c.is_nullable,
       CAST(ISNULL(COLUMNPROPERTY(c.object_id, c.name, 'IsIdentity'), 0) AS int) AS is_identity,
       CAST(ISNULL(COLUMNPROPERTY(c.object_id, c.name, 'IsRowGuidCol'), 0) AS int) AS is_rowguid
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.accountss')
ORDER BY c.column_id;

PRINT N'=== 2) مفاتيح وفهارس ===';

SELECT i.name AS index_name,
       i.is_unique,
       COL_NAME(ic.object_id, ic.column_id) AS column_name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
WHERE i.object_id = OBJECT_ID(N'dbo.accountss')
  AND i.is_hypothetical = 0
ORDER BY i.index_id, ic.key_ordinal;

PRINT N'=== 3) ملخص سياسية الإدراج (نفس منطق الباك اند — sys.columns) ===';

SELECT
    HasPkColumn = CAST(CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'pk_id'
    ) THEN 1 ELSE 0 END AS int),
    PkIdentity = CAST(CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'pk_id' AND is_identity = 1
    ) THEN 1 ELSE 0 END AS int),
    IdIdentity = CAST(CASE WHEN EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.accountss') AND name = N'id' AND is_identity = 1
    ) THEN 1 ELSE 0 END AS int);
