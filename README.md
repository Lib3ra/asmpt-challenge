# SMT Order Manager (ASMPT coding challenge)

Web application for managing orders, boards and components in an SMT (Surface-Mount Technology)
manufacturing environment. The task description is in [`ASMPT_Coding_Challenge_EN.pdf`](ASMPT_Coding_Challenge_EN.pdf);
decisions, conventions and the delivery plan are tracked in [`docs/project-handbook.html`](docs/project-handbook.html).

> Status: project scaffold (PR 1). Domain features follow in later pull requests.

## Tech stack

| Area     | Choice                                                   |
| -------- | -------------------------------------------------------- |
| Backend  | C# / ASP.NET Core Minimal API on .NET 10                 |
| Frontend | React + TypeScript, built with Vite                      |
| Tests    | xUnit (`tests/OrderManagement.Api.Tests`)                |
| CI       | GitHub Actions (`.github/workflows/ci.yml`)              |

## Repository layout

```
src/OrderManagement.Api/          ASP.NET Core Minimal API, endpoints grouped by feature
tests/OrderManagement.Api.Tests/  xUnit tests (unit and integration)
frontend/                         React + TypeScript + Vite app
docs/                             Project handbook (decisions, conventions, plan)
```

## Prerequisites

- .NET SDK 10.0 (see `global.json`)
- Node.js 24 LTS or newer with npm

## Run locally (development)

The API and the Vite dev server run as separate processes. Vite forwards every request under `/api`
to the API, so the browser only talks to `http://localhost:5173`.

```sh
# Terminal 1: API on http://localhost:5080
dotnet run --project src/OrderManagement.Api

# Terminal 2: frontend on http://localhost:5173
cd frontend
npm ci
npm run dev
```

Open http://localhost:5173. The page shows the API status reported by `GET /api/health`.

To point Vite at a different API address, set `API_URL`, for example `API_URL=http://localhost:5000 npm run dev`.

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

CI runs the same backend and frontend checks on every pull request and on every push to `main`.

## API

| Method | Route         | Description                                      |
| ------ | ------------- | ------------------------------------------------ |
| GET    | `/api/health` | Health status as JSON, e.g. `{"status":"Healthy"}` |
