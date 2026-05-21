-- رويال (MySQL على Railway): حذف كل الجداول ما عدا جداول تطبيق الآباء الأربعة.
-- نفّذ من Query في Railway MySQL أو mysql CLI متصل بقاعدة رويال.
-- احتفظ بنسخة احتياطية إن كان عندك بيانات مهمة في parents_*.

SET FOREIGN_KEY_CHECKS = 0;

-- الخطوة 1: شغّل هذا الاستعلام فقط — ينتج أوامر DROP (انسخ الناتج ونفّذه).
SELECT CONCAT('DROP TABLE IF EXISTS `', table_name, '`;') AS drop_sql
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_type = 'BASE TABLE'
  AND table_name NOT IN (
    'parents_students_summary',
    'parents_classes',
    'parents_sections',
    'parents_attendance_summary'
  )
ORDER BY table_name;

-- الخطوة 2: الصق كل أسطر drop_sql من الناتج ونفّذها هنا.
-- مثال:
-- DROP TABLE IF EXISTS `AspNetUsers`;
-- DROP TABLE IF EXISTS `students`;
-- ...

SET FOREIGN_KEY_CHECKS = 1;

-- بعد الحذف: أعد تشغيل API على Railway (أو انتظر deploy) ثم من المدرسة «إعادة رفع الكامل».
