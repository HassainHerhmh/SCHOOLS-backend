# SCHOOLS-backend

ط¨ط§ظƒظ†ط¯ **ظ…ظ†طµط© ط§ظ„ظ…ط¯ط§ط±ط³** (.NET 8). ط§ظ„ظ…طµط¯ط± ظ…ظ† `schools222/backend/SchoolsManagement.Api`.

## ظپط­طµ ط§ظ„ظ‚ط§ط¹ط¯ط© ط¨ط¹ط¯ ط§ظ„ظ†ط´ط±

`GET /api/health/db` â€” ظٹط¹ط±ط¶ ط§ظ„ط¬ط¯ط§ظˆظ„طŒ ط§ظ„ظ‡ط¬ط±ط§طھطŒ AspNetUsersطŒ ظˆطھط­ط°ظٹط± ط¥ظ† ظˆظڈط¬ط¯ MySQL ط¨ط§ظ„ط®ط·ط£.

## ط³ط¬ظ„ ط§ظ„ط£ط®ط·ط§ط،

`ErrorLest.txt` ط¨ط¬ط§ظ†ط¨ ط§ظ„طھط·ط¨ظٹظ‚ â€” ط£ط®ط·ط§ط، ط§ظ„ط®ط§ط¯ظ… ظ…ط¹ ط§ط³ظ… ط§ظ„ط¯ط§ظ„ط© ظˆط±ظ‚ظ… ط§ظ„ط³ط·ط± (ط§ظ„ط¹ظ…ظٹظ„ ظٹط±ظ‰ ط±ط³ط§ظ„ط© ط¹ط§ظ…ط© ظپظ‚ط·).

## Railway Variables (SQL Server â€” ظ„ظٹط³ MySQL)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server cloud connection string |
| `Jwt__SecretKey` | 32+ chars |
| `Jwt__Issuer` | SchoolsManagement.Api |
| `Jwt__Audience` | SchoolsManagement.Client |
| `ParentsRoyal__SyncApiKey` | Same as school `appsettings.Secrets.json` |
| `ParentsRoyal__SchoolId` | e.g. al-rowad-schools |
| `BusRoyal__SyncApiKey` | Same as school `appsettings.Secrets.json` |
| `BusRoyal__SchoolId` | e.g. al-rowad-schools |

Do **not** use `MYSQL_PUBLIC_URL` for this API.

## Bus App API

| Endpoint | Description |
|----------|-------------|
| `POST /api/bus-auth/login` | Bus driver login |
| `GET /api/bus-app/students` | Students for logged-in driver (JWT) |
| `GET /api/bus-app/location` | Latest bus location |
| `GET /api/bus-app/route` | Route waypoints |
| `POST /api/bus-driver/location` | Update GPS from driver app |
| `POST /api/sync/ingest-bus` | Receive bus sync payload (`X-Bus-Sync-Key`) |
| `GET /api/sync/bus-data-status` | Published bus row counts |

