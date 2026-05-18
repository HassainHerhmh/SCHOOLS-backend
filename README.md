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
3. متغيرات البيئة (مهم):
   - `ConnectionStrings__DefaultConnection` — **SQL Server** (ليس MySQL)
   - `Jwt__SecretKey` و`Jwt__Issuer` و`Jwt__Audience`
   - `ParentsRoyal__SyncApiKey` — مفتاح مزامنة تطبيق الآباء (نفس المفتاح على جهاز المدرسة)
   - `ParentsRoyal__SchoolId` — مثل `al-rowad-schools`
4. بعد النشر: `GET /api/health` و`POST /api/sync/ingest-parents` (مع هيدر `X-Parents-Sync-Key`)

### مزامنة أولياء الأمور

- **المدرسة المحلية** ترسل: `POST /api/sync/publish-to-parents` (مع `ParentsRoyal:RemoteApiUrl` = عنوان Railway)
- **سيرفر Railway** يستقبل: `POST /api/sync/ingest-parents`
- **تطبيق الآباء** يقرأ: `GET /api/parents/students?parent_phone=...`

جداول `parents_*` تُنشأ تلقائياً عند أول استقبال (أو نفّذ `Scripts/royal-ensure-all-tables.sql` على SQL Server).

## Angular

في `environment.apiUrl` ضع عنوان الـ API المنشور (مثلاً `https://your-app.up.railway.app`).
