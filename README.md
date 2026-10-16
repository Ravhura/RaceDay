# RaceDay

**PROG6212 – Portfolio of Evidence (Parts 1 and 2)**
**Student:** [Sedzani Uhone Ravhura] ([ST10104495])

![CI](https://github.com/[your-username]/[your-repo]/actions/workflows/ci.yml/badge.svg)

## Video demonstration

- **Part 1 (planning, ERD, endpoint plan, SQL script):** [https://youtu.be/HJeMMVDCM8M]
- **Part 2 (RESTful API, authentication, Swagger, unit tests):** [https://youtu.be/ktpbFZJ5dfI?si=VH0yDvzNgM8A4rkE]

## What is RaceDay?

RaceDay is an event management system for South African road running, walking and cycling events. Organisers create and manage events, categories and participant results. Participants browse events, enter an event by choosing a category, and view their own enrolments and results.

This repository currently contains:

- **Part 1:** the planning documents in the `docs` folder (ERD, API endpoint plan and SQL script).
- **Part 2:** a RESTful API built with ASP.NET Core 8 (C#), Entity Framework Core (Code First) and SQL Server, with server-side session authentication, Swagger and unit tests.

## The two roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, edit and delete their own events. Define categories for their events. View all enrolments for their events. Capture finish times and finishing positions. View and update their own profile. |
| **Participant** | Create an account, browse events and categories, enter an event by selecting a category, cancel their own enrolment, view their own enrolments and results. View and update their own profile. |

Role-based access is enforced in the API. A Participant cannot use Organiser endpoints and an Organiser cannot use Participant-only endpoints. Users can only access their own data. For example, an Organiser can only change events they created.

## Technology

- .NET 8, ASP.NET Core Web API (C#)
- Entity Framework Core 8, Code First, SQL Server (LocalDB)
- Server-side session authentication (ASP.NET Core session, cookie holds only a session id)
- Password hashing with the ASP.NET Core Identity `PasswordHasher` (PBKDF2 with a random salt)
- Swagger / OpenAPI (Swashbuckle) with descriptions from XML comments
- xUnit unit tests with the EF Core in-memory database
- GitHub Actions for CI

## Repository structure

```text
RaceDay.slnx
README.md
docs/                      Part 1: ERD, endpoint plan, SQL script, CI screenshot
.github/workflows/ci.yml   GitHub Actions: build and run unit tests on every push
RaceDay.Api/
  Authentication/          Role names and session keys
  Controllers/             6 controllers (33 endpoints) + BaseApiController
  Data/                    DbContext, entity configuration, development seed data
  DTOs/                    Request and response classes with validation rules
  Migrations/              EF Core migration (creates the database)
  Models/                  The 6 entities
  Services/                Business rules, ServiceResult, PasswordHelper
  Program.cs
RaceDay.Tests/             Unit tests
```

## Setup and run instructions

### Requirements

- Visual Studio 2022 or later with the **ASP.NET and web development** workload (or the .NET 8 SDK)
- SQL Server LocalDB (installed with Visual Studio's data storage workload)
- SQL Server Management Studio (optional, to inspect the database)

### Run the API

1. Clone the repository and open `RaceDay.slnx` in Visual Studio.
2. Set **RaceDay.Api** as the startup project and press **F5**.
   Or from a terminal in the repository folder:
```
   dotnet run --project RaceDay.Api --launch-profile https
```
3. Swagger opens at `https://localhost:7100/swagger`. If your browser warns about the development certificate, run `dotnet dev-certs https --trust` once.

On the first run in the Development environment, the app automatically creates the **RaceDayDB** database (server `(localdb)\MSSQLLocalDB`) from the EF Core migration and loads the sample data. No manual SQL is needed. The connection string is in `RaceDay.Api/appsettings.json` and uses Windows authentication, so there is no password in the repository.

### Sample accounts (Development only)

All accounts use the password **`RaceDay123`**.

| Role | Email | Id |
|---|---|---|
| Organiser | `sipho.mokoena@raceday.co.za` | 1 |
| Organiser | `lindiwe.dube@raceday.co.za` | 2 |
| Participant | `sedzanir@gmail.com` | 1 |
| Participant | `amanda.vanwyk@gmail.com` | 2 |

Seeded data: 3 events (Johannesburg City Marathon, Soweto Fun Ride, Durban Coastal Cycle Tour) with categories, 4 enrolments and 2 results.

### Try it in Swagger

1. Call `POST /api/auth/login/organiser` with `{ "email": "sipho.mokoena@raceday.co.za", "password": "RaceDay123" }`. The server starts a session and the browser keeps the session cookie, so the other endpoints work straight away.
2. Try other endpoints. Without logging in, protected endpoints return **401**. With the wrong role or someone else's id, they return **403**.
3. Call `POST /api/auth/logout` to end the session, then log in as another user to switch roles.

### Run the unit tests

- Visual Studio: **Test → Run All Tests**
- Terminal: `dotnet test RaceDay.Tests/RaceDay.Tests.csproj`

There are **79 unit tests**. They cover registration and login (success and failure), password hashing, session behaviour, 401 for every protected endpoint when not logged in, 403 when the wrong role is used, event management by Organisers, enrolments being recorded correctly, results, and request validation. The tests need no SQL Server.

### Reset the database

Delete **RaceDayDB** in SSMS (tick *Close existing connections*) and run the API again. It is recreated and re-seeded. If LocalDB is not running, use `sqllocaldb start MSSQLLocalDB`.

## How authentication works

- **Register:** the role is chosen by the route (`/api/auth/register/participant` or `/api/auth/register/organiser`). Passwords must be 8+ characters with a letter and a digit and are stored only as a salted hash.
- **Login:** `/api/auth/login/participant` or `/api/auth/login/organiser`. On success, the server stores the user's **id and role in the server-side session**. The browser only holds a session-id cookie, so a user cannot change their own role.
- **Every request:** each protected endpoint checks the session. No session gives **401 Unauthorized**. The wrong role, or an id that is not the logged-in user's, gives **403 Forbidden**.
- **Logout:** clears the session.

## API endpoints

All 33 endpoints from the Part 1 plan are implemented. "(own)" means the id or resource must belong to the logged-in user, otherwise 403.

| Method | Route | Who | Success |
|---|---|---|---|
| POST | `/api/auth/register/participant` | Public | 201 |
| POST | `/api/auth/register/organiser` | Public | 201 |
| POST | `/api/auth/login/participant` | Public | 200 |
| POST | `/api/auth/login/organiser` | Public | 200 |
| POST | `/api/auth/refresh-token` | Any logged in | 200 |
| POST | `/api/auth/logout` | Any logged in | 200 |
| GET | `/api/participants/{id}` | Participant (own) | 200 |
| PUT | `/api/participants/{id}` | Participant (own) | 200 |
| GET | `/api/organisers/{id}` | Organiser (own) | 200 |
| PUT | `/api/organisers/{id}` | Organiser (own) | 200 |
| GET | `/api/events` | Any logged in | 200 |
| GET | `/api/events/{id}` | Any logged in | 200 |
| POST | `/api/events` | Organiser | 201 |
| PUT | `/api/events/{id}` | Organiser (own event) | 200 |
| DELETE | `/api/events/{id}` | Organiser (own event) | 204 (409 if it has enrolments) |
| GET | `/api/organisers/{id}/events` | Organiser (own) | 200 |
| GET | `/api/events/{id}/categories` | Any logged in | 200 |
| GET | `/api/categories/{id}` | Any logged in | 200 |
| POST | `/api/events/{id}/categories` | Organiser (own event) | 201 |
| PUT | `/api/categories/{id}` | Organiser (own event) | 200 |
| DELETE | `/api/categories/{id}` | Organiser (own event) | 204 (409 if it has enrolments) |
| POST | `/api/enrolments` | Participant | 201 |
| GET | `/api/participants/{id}/enrolments` | Participant (own) | 200 |
| GET | `/api/enrolments/{id}` | Owner Participant or the event's Organiser | 200 |
| DELETE | `/api/enrolments/{id}` | Participant (own) | 204 (409 if a result exists) |
| GET | `/api/events/{id}/enrolments` | Organiser (own event) | 200 |
| GET | `/api/organisers/{id}/enrolments` | Organiser (own) | 200 |
| POST | `/api/results` | Organiser (own event) | 201 |
| PUT | `/api/results/{id}` | Organiser (own event) | 200 |
| DELETE | `/api/results/{id}` | Organiser (own event) | 204 |
| GET | `/api/results/{id}` | Owner Participant or the event's Organiser | 200 |
| GET | `/api/participants/{id}/results` | Participant (own) | 200 |
| GET | `/api/events/{id}/results` | Organiser (own event) | 200 |

Error responses use these codes: **400** invalid input, **401** not logged in, **403** wrong role or not your data, **404** not found, **409** conflict (duplicate email, duplicate enrolment, or deleting something that has dependants).

## Database

The database is built with EF Core Code First from the six entities in the Part 1 ERD: Organisers, Participants, Events, Categories, Enrolments and Results. The constraints from the Part 1 SQL script are kept, including the composite foreign key on Enrolments `(EventId, CategoryId)` that stops an enrolment using a category from a different event, and the rule that a Participant cannot enrol twice in the same event and category. The Part 1 documents (ERD, endpoint plan and SQL script) are in the `docs` folder.

## Deviations from the Part 1 plan

1. **Sessions instead of tokens.** The Part 2 brief requires session management, so login returns the user's id, name and role and starts a server-side session instead of returning a token. The `POST /api/auth/refresh-token` route is kept but now renews the session, because there is no token to refresh. `logout` clears the session.
2. **Event fields added.** The Part 2 brief requires every event to capture a description, distance and event type. `Description`, `DistanceKm` and `EventType` (Run, Walk or Cycle) were added to the Events table, with a CHECK constraint on `EventType`. No Part 1 column was removed or renamed.
3. **Viewing events and categories.** The `GET` endpoints for events and categories are open to any logged-in user (Part 1 listed Participant only), because the Part 2 brief says both roles must be able to view them.
4. **Text column types.** Organisers, Participants and Categories use `NVARCHAR` as in the ERD (the Part 1 SQL script used `VARCHAR`).
5. **No cascade deletes.** As in Part 1, foreign keys do not cascade. Deleting an event, category or enrolment that has dependants returns 409 Conflict. An event with categories but no enrolments is deleted together with its categories.
6. **Sample data.** Part 1's seed used placeholder text for passwords, which could not be used to log in. The Development seed data stores real password hashes of one documented password (`RaceDay123`). It only runs in the Development environment.
7. **Extra safeguards (not required by the brief):** new event dates cannot be in the past, finish times must be between 00:00:01 and 23:59:59, and positions must be 1 or higher.

## Known limitations

- Anyone can register as an Organiser, because the brief says users choose their role when registering. A real system would require an invitation or approval.
- Sessions are held in the server's memory, so users must log in again after the API restarts.
- The unit tests use the EF Core in-memory database, which does not enforce SQL constraints such as the composite foreign key or unique emails. The services check those rules themselves and the tests prove that, and SQL Server enforces them in the real database.

## CI / GitHub Actions

The workflow in `.github/workflows/ci.yml` restores, builds and runs all unit tests on every push.


