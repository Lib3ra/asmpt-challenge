# Order Manager: working agreement

Conventions for everyone working in this repository, human or AI. Product decisions, open questions
and the PR plan live in [`docs/project-handbook.html`](docs/project-handbook.html); this file covers
how we write and ship code. Change it through a pull request like any other code.

## Scope rules

- The challenge is in `ASMPT_Coding_Challenge_EN.pdf`. Do not invent binding requirements beyond it.
- Undecided domain rules (delete behavior with existing links, validation limits, required fields,
  bulk operation semantics, search fields) are decided with the project owner per feature and then
  recorded in the handbook's decision log. Do not settle them silently in code.
- `Component.Quantity` stays on `Component` (copies of the component per board). No quantity on the
  board–component link, no stock management, no calculation from board dimensions.
- Out of scope: cloud deployment, authentication.
- Prefer established, widely known tools. Ask before adopting a template default or a brand-new major
  version that the team may not know.

## Repository layout

```
src/OrderManagement.Api/          Minimal API; one folder per feature under Features/
tests/OrderManagement.Api.Tests/  NUnit tests, mirroring the Features/ structure
frontend/                         React + TypeScript + Vite
docs/                             Project handbook
```

No additional backend projects or architecture frameworks unless agreed.

## Backend (C#)

- Formatting and style come from `.editorconfig`; `dotnet format --verify-no-changes` must pass.
- Shared settings in `Directory.Build.props`: nullable enabled, implicit usings, warnings as errors.
- File-scoped namespaces matching the folder path (`OrderManagement.Api.Features.Health`).
- One folder per feature (`Features/Components`, `Features/Boards`, ...) containing its endpoints,
  DTOs and, where it improves clarity or testability, a service.
- Register endpoints with an extension method per feature, e.g. `app.MapComponentEndpoints()`.
  Keep `Program.cs` limited to wiring.
- Never expose EF Core entities from the API. Use request and response DTOs declared as `record`.
- Logging through `ILogger<T>` with message templates, not string interpolation:
  `logger.LogInformation("Created component {ComponentId}", id)`.
- Apply SOLID and DRY pragmatically; introduce an abstraction only when it has a second use or makes
  a test possible.

## Tests (NUnit)

- Framework: NUnit 4 with the constraint model (`Assert.That(actual, Is.EqualTo(expected))`).
- Test names: `Method_Situation_ExpectedResult`, e.g. `GetHealth_WhenAppIsRunning_ReturnsHealthyAsJson`.
- Structure every test with `// Arrange`, `// Act`, `// Assert` comments. Omit `// Arrange` only when
  there is nothing to set up.
- Group related assertions with `using (Assert.EnterMultipleScope())` so all failures are reported.
- API integration tests use `WebApplicationFactory<Program>`, created once per fixture in
  `[OneTimeSetUp]` and disposed in `[OneTimeTearDown]`.
- Persistence tests use an isolated SQLite database per test or fixture, never the developer database.
- Test relevant behavior only; no tests that exist to raise the test count.

## Frontend (React + TypeScript)

- Lint with ESLint (`eslint.config.js`); `npm run lint`, `npm run typecheck` and `npm run build` must pass.
- Function components and hooks only.
- All HTTP calls live in `src/api/`; components never call `fetch` directly. API paths start with
  `/api` so the Vite dev proxy and the production host both work.
- No UI library or global state manager until there is a concrete need.

## Git and pull requests

- Never commit to `main`. One branch per PR: `feat/...`, `chore/...`, `docs/...`, `fix/...`.
- Commit messages in English, imperative mood, short subject line.
- PRs are small and reviewable, list what was verified, and are merged by the project owner only.
- Tests and documentation (README, handbook) change in the same PR as the feature.
- Never commit secrets, local configuration or database files.

## Language

Everything in English: code, comments, commit messages, PR descriptions, README, this file, the
project handbook and discussions with the project owner.

## Commands

```sh
dotnet test AsmptChallenge.slnx
dotnet format AsmptChallenge.slnx --verify-no-changes
npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run build
```
