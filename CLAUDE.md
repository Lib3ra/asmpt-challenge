# Order Manager: working agreement

Conventions for everyone working in this repository, human or AI. Product decisions, open questions
and the PR plan live in [`docs/project-handbook.html`](docs/project-handbook.html); this file covers
how we write and ship code. Change it through a pull request like any other code.

## Scope rules

- The challenge is in `ASMPT_Coding_Challenge_EN.pdf`. Do not invent binding requirements beyond it.
- Domain rules (validation, unique names, bulk operations, search, link minimums, delete behavior,
  download format) are decided and listed under "Domain rules" in the handbook. Implement them as
  written. Anything not covered there is decided with the project owner first and then added to the
  handbook's decision log. Do not settle new rules silently in code.
- `Component.Quantity` stays on `Component` (copies of the component per board). No quantity on the
  board–component link, no stock management, no calculation from board dimensions.
- Out of scope: cloud deployment, authentication.
- Prefer established, widely known tools. Ask before adopting a template default or a brand-new major
  version that the team may not know.

## Repository layout

```
src/OrderManagement.Api/          Minimal API; one folder per feature under Features/
  Common/Validation/              Validation rules shared by all entities (EntityRules, ValidationErrors)
  Data/                           DbContext, startup migration, EF Core migrations (generated)
tests/OrderManagement.Api.Tests/  NUnit tests, mirroring the Features/ structure
frontend/                         React + TypeScript + Vite
  src/api/                        HTTP calls and API types, one module per resource
  src/features/<feature>/         Pages and feature-specific components
  src/hooks/                      Shared hooks (useNameSearch, useSelection)
  src/ui/                         Reusable UI pieces (field errors, delete selection)
docs/                             Project handbook
```

No additional backend projects or architecture frameworks unless agreed.

## Backend (C#)

- Formatting and style come from `.editorconfig`; `dotnet format --verify-no-changes` must pass.
- Shared settings in `Directory.Build.props`: nullable enabled, implicit usings, warnings as errors,
  code style enforced at build time.
- Always use explicit types instead of `var` (`HttpClient client = ...`). Target-typed `new()` is fine.
  Enforced by IDE0008 during the build.
- File-scoped namespaces matching the folder path (`OrderManagement.Api.Features.Health`).
- One folder per feature (`Features/Components`, `Features/Boards`, ...) containing its entity,
  endpoints, DTOs and repository.
- Endpoints contain no database code. Data access goes through a repository per aggregate
  (`IComponentRepository` / `ComponentRepository`), registered as scoped in `Program.cs`.
  No additional controller, processor or service layers unless agreed.
- Register endpoints with an extension method per feature, e.g. `app.MapComponentEndpoints()`.
  Keep `Program.cs` limited to wiring.
- Never expose EF Core entities from the API. Use request and response DTOs declared as `record`.
- Entity mapping is configured centrally in `AppDbContext.OnModelCreating`; entities stay plain classes.
- Demo data lives in `Data/SeedData.cs` and is applied with `HasData`, so it ships as a migration.
- Schema changes always come with an EF Core migration (`dotnet ef migrations add`, local tool). Never edit
  or remove a migration that is on `main`; migrations that exist only on an unmerged branch may be
  regenerated. Generated files under `Data/Migrations` are excluded from style rules.
- String properties get a maximum length (`HasMaxLength`) from a constant on the entity, e.g.
  `Component.NameMaxLength`, so database mapping and validation share one value.
- Rely on EF Core conventions (table names, keys) and configure only what deviates from them.
- Descriptive names, no abbreviations: `componentRepository`, `cancellationToken` (not `repository`, `ct`).
- Repository methods describe the domain action and do not mirror EF Core method names:
  `CreateComponentsAsync`, `UpdateComponentAsync`, `DeleteComponentsAsync` (not `AddRangeAsync`, `UpdateAsync`).
- Write numeric literals without digit separators (`10000`, not `10_000`).
- Validation uses FluentValidation: one `AbstractValidator<T>` per request type (`ComponentRequestValidator`,
  `CreateComponentsRequestValidator` for the bulk list), built from the shared rule extensions in
  `Common/Validation` (`EntityName`, `EntityDescription`, `UniqueNames`). Validators are registered in DI and
  injected into endpoints as `IValidator<T>`; they never access the database. Query parameters that need
  validation are bound into a request record with `[AsParameters]` (`SearchRequest`); enum-like values are
  bound as text and checked by the validator, so an unknown value gets a field error instead of a binding error. Checks that need the database
  (unique names, existence) run afterwards through the repository.
- Validation errors are returned with `TypedResults.ValidationProblem(result.ToProblemErrors())`, keyed in JSON
  casing: `name` for a single object, `components[1].quantity` for list items (`OverridePropertyName`).
- Validator unit tests use FluentValidation's `TestValidate` / `ShouldHaveValidationErrorFor`.
- Document every endpoint in code with `WithSummary` / `WithDescription` (rules, all-or-nothing behavior);
  it ends up in the OpenAPI document and Swagger UI. The README does not list endpoints.
- "One or more" endpoints: `DELETE ?ids=` for every entity; `POST` with a JSON array for components only
  (boards and orders are created one at a time). Bulk operations are all-or-nothing.
- Error responses come from `Common/Endpoints/ProblemResults`: 400 `IdsRequired`, 404 `IdsNotFound`,
  409 `InUse` (delete blocked because the record is still linked). Name conflicts use `NameConflicts`.
- Links between entities are many-to-many join tables without extra data, configured in `AppDbContext`
  with `UsingEntity`: the owning side cascades (deleting a board removes its links), the referenced side is
  `Restrict` (a linked component cannot be deleted). Requests reference linked records by id lists
  (`componentIds`); the validator checks "at least one" and duplicates, the endpoint checks existence
  through the repository in step 2.
- SQLite cannot translate correlated `SelectMany` (needs SQL APPLY); load with `Include` and project in memory.
- Dates without time (`Order.OrderDate`) are `DateOnly`, sent as `yyyy-MM-dd`; SQLite stores them as sortable text.
- User-facing messages are culture-invariant (no server-locale number formatting).
- Logging through `ILogger<T>` with message templates, not string interpolation:
  `logger.LogInformation("Created component {ComponentId}", id)`.
- Apply SOLID and DRY pragmatically; introduce an abstraction only when it has a second use or makes
  a test possible.

## Tests (NUnit)

- Framework: NUnit 4 with the constraint model (`Assert.That(actual, Is.EqualTo(expected))`).
- Test names: `Method_Situation_ExpectedResult`, e.g. `GetHealth_WhenDatabaseIsReachable_ReturnsHealthyAsJson`.
- Structure every test with `// Arrange`, `// Act`, `// Assert` comments. Omit `// Arrange` only when
  there is nothing to set up.
- Group related assertions with `using (Assert.EnterMultipleScope())` so all failures are reported.
- API integration tests use `WebApplicationFactory<Program>` (`SqliteApiFactory`), created per test in
  `[SetUp]` and disposed in `[TearDown]`, so every test starts from its own freshly migrated database.
- Persistence tests use an isolated SQLite database per test or fixture, never the developer database.
- Test relevant behavior only; no tests that exist to raise the test count.

## Frontend (React + TypeScript)

- Lint with ESLint (`eslint.config.js`); `npm run lint`, `npm run typecheck` and `npm run build` must pass.
- Function components and hooks only.
- All HTTP calls live in `src/api/`; components never call `fetch` directly. API paths start with
  `/api` so the Vite dev proxy and the production host both work.
- Keep the UI minimal: plain forms and tables, no extra widgets or styling beyond what a task needs.
- Pages are routed with React Router (`/components`, `/boards`, `/orders`, `/search`); the header uses `NavLink`.
- Filters live in the URL query (`/boards?name=led`, `/search?term=led&type=boards`) so search results can link
  to a filtered list page. `useNameSearch` reads and writes `?name=`.
- No UI library or global state manager until there is a concrete need.
- Show server validation messages next to the field they belong to; never use `alert`/`confirm`, confirm
  destructive actions in the page.

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
docker compose up --build -d
```
