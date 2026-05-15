# SCHOOLS-backend

مشروع باكند أساسي (.NET 8) لاستقبال نقل الدوال من تطبيق Angular تدريجياً — بدون Supabase في الواجهة؛ الاعتماد على **API + SQL Server** (مثلاً على [سيرفر رويال](https://github.com/HassainHerhmh/SCHOOLS-backend)).

## المتطلبات

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## التشغيل محلياً

```bash
cd src/Schools.Api
dotnet run
```

- Swagger: `http://127.0.0.1:5088/swagger`
- فحص سريع: `GET http://127.0.0.1:5088/api/Health` (أو `/api/health` حسب التوجيه)

## ربط المستودع على GitHub (مرة واحدة)

من مجلد `SCHOOLS-backend` داخل جهازك:

```bash
git init
git add .
git commit -m "Initial API skeleton for step-by-step migration"
git branch -M main
git remote add origin https://github.com/HassainHerhmh/SCHOOLS-backend.git
git push -u origin main
```

إذا كان المستودع يحتوي README افتراضياً من GitHub:

```bash
git pull origin main --rebase
git push -u origin main
```

## الخطوة التالية (نقل الدوال)

1. إضافة `Microsoft.EntityFrameworkCore.SqlServer` + DbContext عند جاهزية جداول SQL.
2. نسخ أو إعادة تسمية Controllers من مشروع `schools222/backend/SchoolsManagement.Api` حسب الحاجة.
3. في Angular: تعيين `environment.apiUrl` إلى عنوان الـ API الفعلي (انظر أدناه).

## تسجيل الدخول الحقيقي (Angular)

**لا يوجد في هذا المستودع مستخدمون تجريبيون.** تسجيل الدخول الفعلي (ASP.NET Identity + JWT + SQL Server) موجود في:

`schools222/backend/SchoolsManagement.Api` — المسار `POST /api/auth/login` كما تستدعيه `SessionAuthService` في Angular.

ضع في `environment.apiUrl` عنوان تشغيل **SchoolsManagement.Api** (محلياً مثل `http://127.0.0.1:5000` أو عنوان السيرفر بعد نشره). مشروع **SCHOOLS-backend** هنا هيكل خفيف للنشر التدريجي فقط حتى تُنقل إليه الـ Controllers أو تستبدله بنشر المشروع الكامل.

## النشر على Railway

إذا ظهر **Build failed** أثناء *Build image*:

1. تأكد أن المستودع يحتوي على **`Dockerfile`** في الجذر (موجود في هذا المشروع).
2. في Railway: **Settings → Build → Builder** اختر **Dockerfile** إن لم يُكتشف تلقائياً، أو اترك الاكتشاف التلقائي بعد الدفع.
3. **Root Directory** اتركه فارغاً (جذر المستودع) حيث يوجد `Dockerfile`.
4. لا حاجة لمتغيرات خاصة للمنفذ: Railway يمرّر `PORT` و`CMD` في الصورة يستمع على `0.0.0.0:${PORT}`.

## ملاحظة

مشروع `schools222` يحتوي بالفعل على `SchoolsManagement.Api` كامل؛ يمكنك إما **دفع ذلك المجلد** إلى هذا الريموت، أو الإبقاء على هذا الهيكل الخفيف ونسخ الملفات **واحدة واحدة** كما خططت.
