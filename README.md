# Building Work Order Tracker

ASP.NET Core 8 Web API (controllers + EF Core + SQL Server) with an Angular 19 front end.

```
WorkOrderTracker/
├── docker-compose.yml            SQL Server 2022 container
├── WorkOrderTracker.sln
├── api/WorkOrderTracker.Api/     Web API
│   ├── Controllers/              Buildings, Technicians, WorkOrders (full CRUD)
│   ├── Data/AppDbContext.cs      Fluent config, relationships, seed data
│   ├── Dtos/                     Request/response contracts + validation attributes
│   └── Models/                   EF entities
├── tests/WorkOrderTracker.Api.Tests/   xUnit + EF InMemory
└── client/                       Angular app (list + add/edit form)
```

## Data model

| Table        | Key columns                                                         |
|--------------|---------------------------------------------------------------------|
| Buildings    | Id, Name, Address                                                   |
| Technicians  | Id, Name, Email (unique), Trade                                     |
| WorkOrders   | Id, Title, Description, Location, Priority, Status, CreatedAt, DueDate, CompletedAt, BuildingId (FK, restrict), TechnicianId (FK, nullable, set null) |

Business rules (in `WorkOrdersController.ValidateAsync`):
- Building and technician must exist.
- `InProgress` / `Completed` require an assigned technician.
- Moving to `Completed` stamps `CompletedAt`; reopening clears it.
- Due date can't be in the past on create.
- Deleting a building that still has work orders returns **409 Conflict**.

## Run it

Prereqs: .NET 8 SDK, Node 20+, Docker Desktop.

```bash
# 1. Database
docker compose up -d

# 2. EF tooling (pinned in .config/dotnet-tools.json; the InitialCreate migration is already in Migrations/)
dotnet tool restore
cd api/WorkOrderTracker.Api
# after changing models: dotnet ef migrations add <Name>

# 3. Run the API (applies migrations + seed data on startup in Development)
dotnet run            # Swagger UI: http://localhost:5080/swagger

# 4. Tests (from repo root)
dotnet test

# 5. Front end
cd client
npm install
npm start             # http://localhost:4200, /api proxied to :5080
```

## Endpoints

| Method | Route                       | Notes                                  |
|--------|-----------------------------|----------------------------------------|
| GET    | /api/workorders             | `?status=Open&buildingId=1` filters    |
| GET    | /api/workorders/{id}        |                                        |
| POST   | /api/workorders             | 201 + Location header                  |
| PUT    | /api/workorders/{id}        | 204                                    |
| DELETE | /api/workorders/{id}        | 204                                    |
| *      | /api/buildings[/{id}]       | Same CRUD shape; DELETE may return 409 |
| *      | /api/technicians[/{id}]     | Same CRUD shape; email must be unique  |

## Stretch ideas if you have time
- Move the validation rules into a `WorkOrderService` and test that directly.
- Add pagination to `GET /api/workorders`.
- Angular screens for buildings/technicians (the API already supports them).
- An `HttpInterceptor` for global error toasts.
