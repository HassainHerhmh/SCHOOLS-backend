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
- فحص سريع: `GET http://127.0.0.1:5088/api/health`

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
3. في Angular: تعيين `environment.apiUrl` إلى عنوان هذا الـ API على السيرفر.

## ملاحظة

مشروع `schools222` يحتوي بالفعل على `SchoolsManagement.Api` كامل؛ يمكنك إما **دفع ذلك المجلد** إلى هذا الريموت، أو الإبقاء على هذا الهيكل الخفيف ونسخ الملفات **واحدة واحدة** كما خططت.
