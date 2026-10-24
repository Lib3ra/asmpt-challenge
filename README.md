# Order Manager (ASMPT coding challenge)

Web application for managing orders, boards and components in an SMT (Surface-Mount Technology)
manufacturing environment. The task description is in [`ASMPT_Coding_Challenge_EN.pdf`](ASMPT_Coding_Challenge_EN.pdf);
decisions, conventions and the delivery plan are tracked in [`docs/project-handbook.html`](docs/project-handbook.html).

> Status: project scaffold (PR 1). Domain features follow in later pull requests.

## Tech stack

| Area     | Choice                                                   |
| -------- | -------------------------------------------------------- |
| Backend  | C# / ASP.NET Core Minimal API on .NET 10                 |
| Frontend | React + TypeScript, built with Vite                      |
| Tests    | NUnit (`tests/OrderManagement.Api.Tests`)                |
| CI       | GitHub Actions (`.github/workflows/ci.yml`)              |

## Repository layout

```
src/OrderManagement.Api/          ASP.NET Core Minimal API, endpoints grouped by feature
tests/OrderManagement.Api.Tests/  NUnit tests (unit and integration)
frontend/                         React + TypeScript + Vite app
docs/                             Project handbook (decisions, conventions, plan)
```

## Prerequisites

Tools you install yourself:

| Tool       | Version                   | Check with         |
| ---------- | ------------------------- | ------------------ |
| .NET SDK   | 10.0 (see `global.json`)  | `dotnet --version` |
| Node.js    | 24 LTS or newer           | `node --version`   |
| npm        | ships with Node.js        | `npm --version`    |

Docker is not needed yet. It becomes an alternative way to run the app in PR 2.

## Dependencies

All other dependencies are restored automatically (NuGet and npm). The project files are the source
of truth for exact versions: `*.csproj` for the backend, `frontend/package.json` and
`frontend/package-lock.json` for the frontend.

| Part     | Package                                              | Purpose                                     |
| -------- | ---------------------------------------------------- | ------------------------------------------- |
| Backend  | ASP.NET Core (shared framework, no extra packages)   | Minimal API, health checks, logging, JSON   |
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
