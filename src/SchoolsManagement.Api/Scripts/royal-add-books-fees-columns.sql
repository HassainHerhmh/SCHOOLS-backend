-- إضافة عمود الكتب المدرسية (books_fees) لجداول مزامنة تطبيق الآباء
-- نفّذ على: SQL Server المحلي + MySQL رويال (Railway)

/* ========== SQL Server (المدرسة المحلية + إن وُجد parents_* محلياً) ========== */
IF COL_LENGTH(N'dbo.parents_students_summary', N'books_fees') IS NULL
BEGIN
    ALTER TABLE dbo.parents_students_summary
    ADD books_fees decimal(18,2) NOT NULL
        CONSTRAINT DF_parents_students_books DEFAULT ((0));
END;

IF COL_LENGTH(N'dbo.parents_classes', N'books_fees') IS NULL
BEGIN
    ALTER TABLE dbo.parents_classes
    ADD books_fees decimal(18,2) NOT NULL
        CONSTRAINT DF_parents_classes_books DEFAULT ((0));
END;

/* ========== MySQL (سيرفر رويال — Railway Query) ========== */
-- parents_students_summary
SET @has_col := (
    SELECT COUNT(*) FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'parents_students_summary'
      AND column_name = 'books_fees'
);
SET @sql := IF(@has_col = 0,
    'ALTER TABLE parents_students_summary ADD COLUMN books_fees decimal(18,2) NOT NULL DEFAULT 0',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- parents_classes
SET @has_col := (
    SELECT COUNT(*) FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'parents_classes'
      AND column_name = 'books_fees'
);
SET @sql := IF(@has_col = 0,
    'ALTER TABLE parents_classes ADD COLUMN books_fees decimal(18,2) NOT NULL DEFAULT 0',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- بعد الإضافة: من المدرسة «إعادة رفع الكامل إلى رويال» لملء القيم.
