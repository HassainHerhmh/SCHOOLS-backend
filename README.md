# SCHOOLS-backend

باكند **منصة المدارس** (.NET 8): Identity + JWT + SQL Server، محاسبة، رواتب، طلاب، صلاحيات الصفحات.

المصدر المزامَن من `schools222/backend/SchoolsManagement.Api`.

## المتطلبات

- .NET 8 SDK
- SQL Server (سلسلة اتصال في `appsettings.json` أو متغيرات البيئة)

## التشغيل محلياً

```bash
cd src/SchoolsManagement.Api
dotnet run
```

- Swagger: `http://127.0.0.1:5000/swagger` (أو المنفذ في `launchSettings.json`)
- فحص: `GET /api/health`

## تسجيل الدخول

`POST /api/auth/login` — يرجع JWT وقائمة `permissions` لصلاحيات الصفحات.

مستخدم Admin (مثل `mansour.admin`) يملك كل الصلاحيات. باقي المستخدمين تُعيَّن صلاحياتهم عبر `PUT /api/permissions/users/{id}`.

## النشر على Railway

1. اربط المستودع [HassainHerhmh/SCHOOLS-backend](https://github.com/HassainHerhmh/SCHOOLS-backend)
2. Builder: **Dockerfile** من جذر المستودع
3. عيّن `ConnectionStrings__DefaultConnection` و`Jwt__SecretKey` و`Jwt__Issuer` و`Jwt__Audience` في متغيرات البيئة

## Angular

في `environment.apiUrl` ضع عنوان الـ API المنشور (مثلاً `https://your-app.up.railway.app`).
