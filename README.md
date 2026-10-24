# Order Manager (ASMPT coding challenge)

Web application for managing orders, boards and components in an SMT (Surface-Mount Technology)
manufacturing environment. The task description is in [`ASMPT_Coding_Challenge_EN.pdf`](ASMPT_Coding_Challenge_EN.pdf);
decisions and the delivery plan are tracked in [`docs/project-handbook.html`](docs/project-handbook.html).
Code style and working conventions are in [`CLAUDE.md`](CLAUDE.md).

> Status: scaffold, SQLite persistence and Docker delivery (PR 2). Domain features follow in later pull requests.

## Tech stack

| Area        | Choice                                                         |
| ----------- | -------------------------------------------------------------- |
| Backend     | C# / ASP.NET Core Minimal API on .NET 10                       |
| Persistence | SQLite via EF Core; migrations are applied at startup          |
| Frontend    | React + TypeScript, built with Vite                            |
| Tests       | NUnit (`tests/OrderManagement.Api.Tests`)                      |
| Delivery    | One Docker image (API + frontend), Docker Compose with a volume |
| CI          | GitHub Actions (`.github/workflows/ci.yml`)                    |

## Repository layout

```
src/OrderManagement.Api/          ASP.NET Core Minimal API
  Data/                           DbContext, startup migration, EF Core migrations
  Features/                       One folder per feature (endpoints, DTOs, entities)
tests/OrderManagement.Api.Tests/  NUnit tests (unit and integration)
frontend/                         React + TypeScript + Vite app
docs/                             Project handbook (decisions, plan)
Dockerfile, compose.yaml          Container build and local deployment
```

## Prerequisites

Tools you install yourself:

| Tool       | Version                   | Needed for                        | Check with               |
| ---------- | ------------------------- | --------------------------------- | ------------------------ |
| .NET SDK   | 10.0 (see `global.json`)  | Development and tests             | `dotnet --version`       |
| Node.js    | 24 LTS or newer           | Frontend development              | `node --version`         |
| npm        | ships with Node.js        | Frontend development              | `npm --version`          |
| Docker     | Docker Desktop or Engine with Compose v2 | Running the container | `docker compose version` |

To only run the app in Docker, Docker alone is enough.

## Dependencies

All other dependencies are restored automatically (NuGet, npm, local .NET tools). The project files are
the source of truth for exact versions: `*.csproj` for the backend, `frontend/package.json` and
`frontend/package-lock.json` for the frontend, `dotnet-tools.json` for .NET tools.

| Part     | Package                                              | Purpose                                     |
| -------- | ---------------------------------------------------- | ------------------------------------------- |
| Backend  | ASP.NET Core (shared framework)                      | Minimal API, health checks, logging, JSON   |
| Backend  | Microsoft.EntityFrameworkCore.Sqlite                 | SQLite persistence                          |
| Backend  | Microsoft.EntityFrameworkCore.Design                 | Design-time support for migrations          |
| Backend  | Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | Database health check          |
| Tools    | dotnet-ef (local tool)                               | Creating EF Core migrations                 |
| Tests    | NUnit, NUnit3TestAdapter, NUnit.Analyzers            | Test framework, runner and analyzers        |
| Tests    | Microsoft.AspNetCore.Mvc.Testing                     | Runs the API in memory for integration tests |
| Tests    | Microsoft.NET.Test.Sdk, coverlet.collector           | `dotnet test` host and code coverage        |
| Frontend | react, react-dom                                     | UI                                          |
| Frontend | vite, @vitejs/plugin-react                           | Dev server with API proxy, production build |
| Frontend | typescript                                           | Type checking                               |
| Frontend | eslint, typescript-eslint, eslint-plugin-react-hooks, eslint-plugin-react-refresh | Linting |

## Run locally (development)

The API and the Vite dev server run as separate processes. Vite forwards every request under `/api`
to the API, so the browser only talks to `http://localhost:5173`.

First-time setup, from the repository root:

```sh
dotnet restore AsmptChallenge.slnx
dotnet tool restore
npm ci --prefix frontend
```

Start both parts in two terminals:

```sh
# Terminal 1: API on http://localhost:5080
dotnet run --project src/OrderManagement.Api

# Terminal 2: frontend on http://localhost:5173
cd frontend
npm run dev
```

Open http://localhost:5173. The page shows the API status reported by `GET /api/health`.

To point Vite at a different API address, set `API_URL`, for example `API_URL=http://localhost:5000 npm run dev`.

### Local database

The API creates `src/OrderManagement.Api/App_Data/app.db` on first start and applies pending migrations
automatically. A fresh database contains five demo components (defined in `Data/SeedData.cs`). The file is git-ignored. To start with an empty database, stop the API and delete the
`App_Data` folder.

The connection string is configured as `ConnectionStrings:DefaultConnection` (in `appsettings.json`, or
the environment variable `ConnectionStrings__DefaultConnection`). A relative path is resolved against the
API project folder.

To add a migration after changing the model:

```sh
dotnet ef migrations add <Name> --project src/OrderManagement.Api --output-dir Data/Migrations
```

## Run with Docker

### From source

```sh
docker compose up --build -d
```

Open http://localhost:8080. The same container serves the React app and the API. It runs as the non-root
user `app`.

Logs: `docker compose logs -f app`.

### How data is stored

The SQLite database lives at `/data/app.db` inside the container. `/data` is a Docker **named volume**
(`order-manager_order-manager-data`): storage managed by Docker (inside the Docker Desktop VM on macOS and
Windows), not a folder in this repository. The whole directory is persisted, including SQLite's `-wal` and
`-shm` files.

- **Nothing local is copied.** `.dockerignore` excludes database files and `App_Data`, so the image only
  contains an empty `/data` folder. The container database and the local development database
  (`src/OrderManagement.Api/App_Data/app.db`) are independent; changes in one never appear in the other.
- **First start.** The volume is new and empty. Docker fills it with the image's `/data` folder, including its
  owner: the image creates `/data` owned by the non-root user `app` (uid 1654), so the app can write to the
  volume. The app then creates `app.db` and applies all migrations, which also insert the five demo
  components. The log shows `Applying 1 migration(s) to /data/app.db`.
- **Later starts.** Restarting, recreating the container or starting a newer image mounts the same volume
  again. The database already exists, so the log shows `Database /data/app.db is up to date`, or a newer
  image applies only its new migrations to the existing data.
- **Inspect the volume:** `docker volume inspect order-manager_order-manager-data`.

Data and the volume:

| Command                  | Containers | Data (volume) |
| ------------------------ | ---------- | ------------- |
| `docker compose restart` | restarted  | kept          |
| `docker compose down`    | removed    | **kept**      |
| `docker compose down -v` | removed    | **deleted**   |

The app is designed to run as a single instance.

### From the CI artifact (no source build)

Every successful CI run on `main` publishes the image as an artifact named `order-manager-image-<commit-sha>`.
It contains `order-manager-<commit-sha>.tar.gz`, and the image inside is tagged `order-manager:<commit-sha>`.

1. Download the artifact from the workflow run on GitHub (Actions → CI → run → Artifacts) and unzip it,
   or use the GitHub CLI: `gh run download <run-id> --name order-manager-image-<commit-sha>`.
2. Load the image:

   ```sh
   docker load --input order-manager-<commit-sha>.tar.gz
   ```

3. Start it with a persistent volume, either with plain Docker:

   ```sh
   docker run -d --name order-manager -p 8080:8080 \
     -v order-manager_order-manager-data:/data \
     order-manager:<commit-sha>
   ```

   or with `compose.yaml` from the repository, without building:

   ```sh
   IMAGE_TAG=<commit-sha> docker compose up -d --no-build
   ```

Both commands use the same volume name, so data is shared between the two ways of starting. If the volume
was first created by `docker run`, Compose warns that it "already exists but was not created by Docker
Compose". The warning is harmless; Compose uses the existing volume and its data.
CI images are built for `linux/amd64`. On Apple Silicon, Docker Desktop runs them through emulation.

## Tests and checks

```sh
# Backend: build and run all tests
dotnet test AsmptChallenge.slnx

# Backend: verify formatting against .editorconfig
dotnet format AsmptChallenge.slnx --verify-no-changes

# Frontend: lint, typecheck and production build
cd frontend
npm run lint
npm run typecheck
npm run build
```

Integration tests run the API in memory against a temporary SQLite file per test fixture. They never
touch `App_Data/app.db`.

CI runs on every pull request and every push to `main`:

1. Backend: restore, build, test.
2. Frontend: `npm ci`, lint, typecheck, build.
3. Docker: build the image and smoke-test `/api/health` in a running container.
   On `main` only, the image is tagged with the commit SHA, exported with `docker save`, gzip-compressed
   and uploaded as an artifact (kept for 30 days).

## API

| Method | Route                  | Description                                                    |
| ------ | ---------------------- | -------------------------------------------------------------- |
| GET    | `/api/health`          | Health status including the database, e.g. `{"status":"Healthy"}` |
| GET    | `/api/components`      | All components                                                 |
| GET    | `/api/components/{id}` | One component, or 404                                          |
| POST   | `/api/components`      | Create a component: `{"name":"...","description":"...","quantity":1}` |

The component endpoints are a minimal persistence flow. Full CRUD, search and validation follow in PR 3.
