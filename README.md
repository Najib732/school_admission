# School Admission System

Online admission for a school where seats go to whoever applies first. A parent applies for a class
(one shift), the seat is held for a limited time, and paying the admission fee in that time confirms it.
If the time runs out, the seat goes back to other applicants.

- `backend/`: ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL
- `frontend/`: Next.js (React 19, TypeScript)
- `docker-compose.yml`: PostgreSQL + backend + frontend

## Run with Docker

Only Docker is needed.

```bash
docker compose up --build
```

| What | Address |
|---|---|
| Frontend (open this) | http://localhost:3000 |
| Backend API | http://localhost:8080 |

The first start takes a few minutes while the images build. The backend creates the tables by itself.

### Pages

| Page | Address |
|---|---|
| Classes (home) | http://localhost:3000/ |
| Admin (create classes) | http://localhost:3000/admin |
| My applications | http://localhost:3000/applications |
| One application | http://localhost:3000/applications/{holdId} |

### Data on first start

The database starts empty, with no classes. Open **Admin** first and create a class
(e.g. `Class 6 - Morning Shift`, 120 seats). After that, students can apply from the home page.

Stop with `docker compose down`. The data stays in the `pgdata` volume, so `docker compose up` again
shows everything as it was. `docker compose down -v` deletes the data.

### Settings

Set these environment variables when starting, e.g. `DB_CONNECTION="..." docker compose up --build`:

| Variable | Meaning | Default |
|---|---|---|
| `DB_CONNECTION` | Database connection string the backend uses | the `db` container |
| `API_URL` | Backend address the browser calls | `http://localhost:8080` |

For a PostgreSQL server on your own machine, use `Host=host.docker.internal` in `DB_CONNECTION`.
Inside Docker, `localhost` is the container itself.

All other settings are in `docker-compose.yml`, under each service's `environment:`. The backend and
frontend have no settings of their own.

| Service | Variable | Meaning |
|---|---|---|
| backend | `PORT` | Port the backend listens on inside the container (`8080`) |
| backend | `HOLD_DURATION_SECONDS` | How long a seat is held before it expires (`600`) |
| backend | `HOLD_EXPIRY_INTERVAL_SECONDS` | How often expired holds are released (`5`) |
| backend | `Cors__AllowedOrigins__0` | Frontend address allowed to call the API |

`API_URL` is read when the frontend container starts, not when the image is built, so changing it
needs no rebuild. It must be an address the browser can reach.

## Using the app

1. **Admin** page: create classes, one per shift, e.g. `Class 6 - Morning Shift` with 120 seats.
2. Type the child's 17-digit birth registration number in the box at the top. It is saved in this
   browser, so it is still there after a refresh or after closing the browser. Bangla digits work too.
3. **Classes** page: search by class name (e.g. `class 6`), see total / held / confirmed / free seats
   (refreshed every 3 seconds), and press **Apply**.
4. The application page shows the time left. **Pay admission fee** confirms the seat (the payment is
   simulated); **Cancel application** gives the seat back. When the time runs out the page shows
   `EXPIRED` and the pay button turns off, without a reload.
5. **My applications** lists every application for the number in the box, with its status.

## API

Base URL `http://localhost:8080`, JSON. The student is the `X-Student-Id` header (17 digits).

| Method | Path | Success |
|---|---|---|
| POST | `/classes` | 201 |
| GET | `/classes` | 200 |
| GET | `/classes/{classId}` | 200 |
| POST | `/classes/{classId}/holds` | 201 (409 if no seats or already applied) |
| GET | `/holds/{holdId}` | 200 |
| POST | `/holds/{holdId}/confirm` | 200 |
| DELETE | `/holds/{holdId}` | 204 |
| GET | `/me/holds` | 200 |

Errors are 400 (bad input), 404 (not found, or someone else's application) or 409 (the state doesn't
allow it), each with a readable message.

## Tests

```bash
docker compose --profile test run --rm backend-tests
```

### What the backend tests cover

- 60 students applying for 5 seats at the same moment: exactly 5 holds, 55 refusals (409).
- The same student double-clicking Apply: one hold only.
- Paying for the same application 11 times: confirmed seats go up by one.
- A hold expires with no API call at all, shows `EXPIRED`, can't be paid, and the seat is free again.
- Only the student's own application can be paid or cancelled.
- Bad input returns 400/404/409, never 500.
