# SCHOOLS-backend

باكند **منصة المدارس** (.NET 8). المصدر من `schools222/backend/SchoolsManagement.Api`.

## فحص القاعدة بعد النشر

`GET /api/health/db` — يعرض الجداول، الهجرات، AspNetUsers، وتحذير إن وُجد MySQL بالخطأ.

## سجل الأخطاء

`ErrorLest.txt` بجانب التطبيق — أخطاء الخادم مع اسم الدالة ورقم السطر (العميل يرى رسالة عامة فقط).

## Railway Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server أو MySQL cloud connection string |
| `Jwt__SecretKey` | 32+ chars |
| `Jwt__Issuer` | SchoolsManagement.Api |
| `Jwt__Audience` | SchoolsManagement.Client |
| `ParentsRoyal__SyncApiKey` | Same as school `appsettings.Secrets.json` |
| `ParentsRoyal__SchoolId` | e.g. al-rowad-schools |
| `BusRoyal__SyncApiKey` | Same as school `appsettings.Secrets.json` |
| `BusRoyal__SchoolId` | e.g. al-rowad-schools |

## Bus App API

| Endpoint | Description |
|----------|-------------|
| `POST /api/bus-auth/login` | Bus driver login |
| `GET /api/bus-app/students` | Students for logged-in driver (JWT) |
| `GET /api/bus-app/location` | Latest bus location |
| `GET /api/bus-app/route` | Route waypoints (centered on driver GPS) |
| `POST /api/bus-driver/location` | Update GPS from driver app |
| `POST /api/sync/ingest-bus` | Receive bus sync payload (X-Bus-Sync-Key) |
| `GET /api/sync/bus-data-status` | Published bus row counts |
