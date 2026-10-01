# ClefCraft — Project Review & Prioritized Next Steps

## Context
This review covers the current source in the three repos (`ClefCraft-Frontend`, `ClefCraft-Backend`, `clef_ai`), with no git history used. **The plan is approved and is being carried out in phases**, in the order given in section 5. This file is the source of truth for that work. The decisions are in section 6. The project is local-only, has no users, and no longer uses Render. That makes now the cheapest time for breaking changes, data resets, migration squashing and framework upgrades.

The findings below describe the code as it was at review time. As each phase lands, the items it resolves are no longer current.

**Baseline measured during the review (all green):**
- Backend: **315 tests pass** (Application 217, Identity 36, Persistence 34, Api 28). There are about 40 nullable warnings.
- Frontend: **258 Karma specs pass** (39 spec files).
- AI: 4 pytest smoke tests exist but **could not be run locally**. There is no venv, and `pytest` isn't installed in the global Python 3.10.

**Recommendation:** strengthen the foundation **before** building more features. Spend about 2 weeks on cleanup, dev setup, framework upgrades and the AI pipeline, then resume features. The test suites are green and nothing is deployed, so the codebase will never be in a better state for an upgrade. .NET 8 reaches end of support in about 6 weeks. Angular 18 is already unsupported and has open security advisories.

---

## 1. Versions currently in use

| Layer | In use | Current / target | Status |
|---|---|---|---|
| .NET (all 9 projects) | `net8.0` (+ dead `ClassLibrary1` on `net7.0`) | .NET 10 LTS | EOL 2026‑11‑10 |
| Local .NET SDK | only **9.0.318** SDK + 8.0.31 runtime; no `global.json` | 10.0 SDK + `global.json` | — |
| Dockerfile / backend CI | `sdk:8.0`, `aspnet:8.0`, `setup-dotnet 8.0.x` | 10.0 | — |
| EF Core / Npgsql | mixed **8.0.28** and **8.0.11** across projects | 10.x | Version drift |
| Angular / CDK / Material | **18.2.x** | 22.2.x (4 hops: 19→20→21→22) | **Unsupported** |
| TypeScript | 5.4.5 | Angular 22 requires **TS ≥6.0 <6.1** | — |
| Node | local **20.16.0**, CI `node-version: 20` | Angular 22 requires **^22.22.3 or ^24.15** (Angular 20/21 already need ≥20.19) | Blocks the upgrade |
| zone.js | 0.14.10 | 0.15/0.16 | — |
| Test runner (FE) | Karma + Jasmine | Vitest is the default from v21 | Karma is deprecated |
| Python (AI) | 3.10.9 local, CI `3.10` | 3.12/3.13 | 3.10 reaches EOL in Oct 2026 |
| AI libs | fastapi 0.116.1, sklearn 1.7.2, pandas 2.2.3, numpy 2.2.6, pydantic 2.11.9 | Bump together; **re-pickle the model** | Pickles are tied to the sklearn version |

### Dependency problems (outdated, vulnerable, or blocking an upgrade)
**Backend (NuGet), from `dotnet list package --vulnerable`:**
- **AutoMapper 13.0.1 has a high-severity advisory (GHSA‑rvv3‑g6hj‑g44x).** The fixed versions are commercially licensed. It is used in 18 files plus 4 profiles in `ClefCraft.Application/MappingProfiles/`, and `Features/Comments/CommentMapper.cs` already does hand-written mapping. → **Remove AutoMapper** and map by hand.
- **`Microsoft.AspNet.SignalR.Core 2.4.3`** in `ClefCraft.Infrastructure.csproj` is the legacy .NET Framework SignalR. It pulls in **Microsoft.Owin 2.1.0 (high-severity advisory GHSA‑hxrm‑9w7p‑39cc)** and causes NU1701 warnings. It is unused because the API uses ASP.NET Core SignalR. → Delete.
- `Microsoft.AspNetCore.Hosting`/`.Abstractions 2.3.0` (Infrastructure), `Microsoft.AspNetCore.Http.Features 5.0.17` (Application) and `Microsoft.Extensions.Options.ConfigurationExtensions 7.0.0` (Identity, Persistence) are legacy or deprecated. → Remove, or use `FrameworkReference Microsoft.AspNetCore.App`.
- `MediatR.Extensions.Microsoft.DependencyInjection 11.1.0` is deprecated, and MediatR 13+ is commercially licensed. → Pin **MediatR 12.5.x**, the last Apache-licensed version, which has DI built in.
- `Hellang.Middleware.ProblemDetails 6.5.1` is unmaintained, sits in the wrong layer (Persistence), and is unused because `ExceptionMiddleware` handles this. → Delete.
- `SendGrid 9.29.3`: `IEmailSender` is registered but never used. → Delete it, or keep it only if email is on the roadmap.
- `Microsoft.EntityFrameworkCore.SqlServer` in Persistence.IntegrationTests is unused because the database is Postgres. `SQLitePCLRaw 2.1.6` (transitive, test-only) has a high-severity advisory. Both go away when the packages are bumped.
- Swashbuckle 7.0.0 together with `Microsoft.OpenApi.Models` usage in `Program.cs` breaks on .NET 10 (see 4.1).
- Every csproj has **duplicate `ProjectReference`s**, and versions are spread across projects. → Add `Directory.Build.props` (TFM, Nullable, `TreatWarningsAsErrors` later) and `Directory.Packages.props` (central package versions).

**Frontend (npm), from `npm audit` (13 vulnerabilities, 4 high):**
- `@angular/core` 18: XSS and sanitization-bypass advisories (GHSA‑jj27‑h5hq‑8x99, GHSA‑hh8m‑fm6v‑7cvg). The only fix is upgrading.
- `quill 2.0.3`: XSS in the HTML export (GHSA‑v3m3‑f69x‑jf25). This matters because comment HTML is rendered with `[innerHTML]` in `components/comment-thread/comment-thread.component.html:29,65`.
- **`ngx-toastr` blocks the Angular 22 upgrade.** Its latest version (20.0.5) supports Angular ^21 only. It is used only in `components/login` and `components/registration`, plus `ToastrModule.forRoot()` in `main.ts`. The rest of the app uses `MatSnackBar`. → Replace it with MatSnackBar.
- `ngx-quill` must step up with each Angular major: 27 (ng19), 28 (ng20), 30 (ng21), 31 (ng22).
- `ngx-mat-timepicker` is maintained and has releases up to 22.1.0. It isn't a blocker.
- `quill-mention` 6.1.1 was last published in May 2025. It has no Angular peer dependency, so it doesn't block the upgrade, but watch it.
- `bootstrap` and `jquery` only appear in the **test** config of `angular.json` (styles/scripts). The app doesn't use them. → Remove.

---

## 2. Prioritized recommendations

Effort is in person-days (d). Impact: H = high, M = medium, L = low.

### P0 — Foundation (do first, about 1.5–2 weeks)

**P0.1 Repo hygiene and dead-code removal: 0.5–1d, impact M, risk very low**
- Backend: delete `ClassLibrary1/` (net7.0, not in the .sln, 13 `obj/` files committed) and the `ClefCraft.BlazorUI/` folder (only `obj/`). Delete the SQL Server-era scripts `DummyData.sql` and `DeleteAllData.sql` (`dbo.`, `GETDATE()`), plus `README.docx`, `~$README.docx`, `structure.txt`, `ProjectStructure.txt` and `project-structure.txt`. The README describes `HR.LeaveManagement` and NSwag, which no longer apply.
- Backend dead code: `AITrainingService`/`IAITrainingService` (never registered or called), `IEmailSender`/`EmailSender`, the commented-out `AddPersistenceServices` in `ClefCraft.Persistence/PersistenceServiceRegistration.cs`, the unused `port` variable in `Program.cs`, and the `<Compile Remove="Features\BoardBoardColumn\**">` entry in Application.csproj. `IUserService` and `IUserIdProvider` are registered twice (`Program.cs` and `IdentityServicesRegistration.cs`).
- Frontend: `README.docx`, `ProjectStructure.txt` and `relationship engine prompt.txt` are tracked. Also remove the commented-out sidebar nav (`sidebar.component.html`), `//provideHttpClient()` (`app.config.ts`), and the `Login` link that the sidebar shows to logged-in users.
- AI: there is **no `.gitignore`**. `__pycache__/*.pyc` is committed, `main.py` is empty, and the README says `generate_dataset.py` "exports calendar data from the database", but it actually generates synthetic data.

**Render leftovers (for you to decide; I recommend removing all of them):**
| File | What's there |
|---|---|
| `ClefCraft-Frontend/src/environments/environment.prod.ts` | `apiUrl: 'https://clefcraft-backend.onrender.com/api'` |
| `ClefCraft-Backend/ClefCraft.Api/Program.cs` | CORS origin `https://clefcraft-frontend.onrender.com`; `ForwardedHeaders` block with comments about Render's proxy; the `TEMP(forwarded-ip-check)` middleware; the "Render + Production safe" comment; the unused `PORT` env var |
| `ClefCraft-Backend/ClefCraft.Api/appsettings.Production.json` | `AIService:BaseUrl: https://clef-ai.onrender.com` |
| `ClefCraft-Backend/.github/workflows/ci.yml` | Comment: "Linux, like the Render container" |
| `ClefCraft-Backend/Dockerfile` | Generic, but `EXPOSE 8080` and the `PORT` default exist for Render. Keep it as the future container base and update it to 10.0. |
No `render.yaml` was found in any repo.

**P0.2 Local dev setup: 1.5–2.5d, impact H**

Today, getting a working stack takes a manually installed Postgres, plus at least 6 user-secrets nobody has documented: the connection string, `JwtSettings:Key`, `DevSeed:AdminPassword`/`UserPassword`, `AIService:PredictApiKey`, and a matching `AI_PREDICT_API_KEY` environment variable in the Python process. You also have to run manual psql seed scripts and know the HTTPS port (7287). None of this is documented. **In the default setup, attendance predictions fail silently.** When the AI keys aren't configured, the AI service returns 401, and `AttendancePredictionService` turns that into `null` scores. An empty JWT key only fails at the first authenticated request.
- Add a `docker-compose.yml` in the backend repo or a small umbrella folder, with Postgres and the AI service (built from a new `clef_ai/Dockerfile`). Keep running the API and Angular natively for hot reload.
- Add an `.env.example` or a documented `dotnet user-secrets` script, with **matching AI keys** for both processes.
- Add options validation with `ValidateOnStart()` for `JwtSettings` (key length ≥ 32 bytes), the connection string and `AIService`, so a bad config fails at startup with a clear message. The relevant code is in `IdentityServicesRegistration.cs` and `Program.cs`.
- **Squash migrations** (data reset is acceptable). Persistence has 12 migrations, including two "Initial" ones (`InitialPostgresPersistence` and `InitialMigration`) and three backfill migrations. Identity has 5, including `DisableSeededAccounts`. Replace them with one `Initial` migration per context, and delete the backfill tests (`BoardDataMigrationTests`, `BoardOwnerMembershipBackfillTests`). **Watch out:** the dev users currently come from data seeded by old migrations. `DevelopmentUserSeeder` only sets passwords and returns early if the user doesn't exist, so after the squash it must **create** the users.
- Move the board/calendar/event-type seed SQL (`board_seed_postgresql.sql`, `calendar_seed_postgresql.sql`, `event_types_postgresql.sql`) into the Development-only seeder, or a `--seed` command, so a fresh database comes up populated.
- Consider merging `ClefCraftIdentityDbContext` and `ClefCraftDatabaseContext`. Both point at the same database and connection string. That's optional, and the squash is the bigger win.
- Write one root README per repo covering prerequisites (Node 24, .NET 10 SDK, Python 3.12, Docker), the setup commands, the run order, and the test commands. Add a Python venv with `requirements*.txt`.

**P0.3 Framework upgrades: 5–8d total, impact H. Do them in this phase, right after P0.1.** See section 4 for the breaking changes.
1. **.NET 8 → 10** (1–2d, including the package cleanup from section 1). Do this first because it's small and the backend has the best test coverage.
2. **Node 20.16 → 24 LTS** (local and CI), then **Angular 18 → 19 → 20 → 21 → 22** (3–5d), one hop at a time with `ng update`. Commit and run tests after each hop.
3. **Python 3.10 → 3.12/3.13**, bump the libraries together, and retrain or re-pickle the model (0.5d).

### P1 — Correctness and fragile areas (after P0, about 1–1.5 weeks)

**P1.1 The AI attendance pipeline: 2–4d, impact H (product correctness).** It runs on every calendar load, but in its current form it produces numbers that don't mean anything:
- **Bug: label-encoder corruption.** In `clef_ai/model.py` `prepare_features`, any unseen user ID is **added to the global `LabelEncoder` and the classes are re-sorted on every `/predict` call**. That silently shifts the integer code of every existing user. It is also global mutable state shared across FastAPI's threadpool without a lock. Every real user is "unseen", because the model was trained on `user_001` only.
- `UserIdEnc` as an ordinal feature doesn't generalize. → Drop it.
- **Timezone mismatch:** training uses naive local times from the CSV, while prediction converts to UTC (`events_to_df`), so `Hour` and `DayOfWeek` are shifted. The backend computes `HourOfDay`, `DayOfWeek` and `DurationMinutes`, but the model ignores them and recomputes. `CalendarEvent.TimeZoneId` exists but isn't used for features.
- **Feedback loop and writes on reads:** `GetCalendarEventsQueryHandler` (step 8) inserts a VIEW signal (+0.2) for every event on **every GET**, and `ViewSignalValue` is itself a model feature. The (unused) training heuristic labels "any engagement" as attended, so viewing the calendar makes events look "attended". The signal table also grows without bound.
- **On the critical path:** each calendar fetch waits on a synchronous HTTP call with a 5-second timeout (`AIService.cs`).
- `/train` re-fits the whole model on whatever batch it receives, which throws away everything learned before, but nothing calls it anyway. The model is trained on synthetic single-user data (`generate_dataset.py`).
- **Recommendation:** decide what this feature is for (see section 6). Then either (a) replace it with a transparent rule-based score computed in .NET and drop the Python service for now, or (b) keep the ML but do it properly: fix the encoder, drop the user ID, compute features in the event's time zone, remove view tracking from GET (or debounce it), take prediction off the request path (cache scores and refresh them in the background), and use FastAPI `lifespan` instead of the deprecated `@app.on_event("startup")`.

**P1.2 Backend architecture cleanup: 1.5–2.5d, impact M.** Remove AutoMapper (a vulnerability plus a licensing issue). Pin MediatR 12.5. Add central package management and `Directory.Build.props`. Fix the roughly 40 nullable warnings (`UserService.cs`, `AuthService.cs`, Domain entities, `IdentityServicesRegistration.cs:71`), then enable `TreatWarningsAsErrors`. Move the side effect out of the GET query handler. Optional: a Testcontainers Postgres test that applies the migrations, because the current integration tests use only InMemory/SQLite and won't catch Npgsql-specific issues.

**P1.3 Frontend structure: 3–5d, can be done incrementally, impact M–H for future feature speed.**
- Three oversized components: `pages/calendar/calendar.component.ts` (1122 lines, plus 1297 lines of CSS), `components/relationship-graph/relationship-graph.component.ts` (1121) and `pages/calendar-dialog/calendar-dialog.component.ts` (899). Extract state into services or signals and split them into sub-components. **Do this after the Angular upgrade**, so you're using signals and `@if`/`@for` from the start.
- Bootstrap config is duplicated and conflicting. `main.ts` imports `BrowserModule`, `BrowserAnimationsModule`, `ToastrModule` *and* `provideAnimationsAsync()`, and also spreads `app.config.ts`. → Move everything into one `appConfig`.
- Weak typing: `type CurrentUser = any` (`_services/auth.service.ts`) and about 30 uses of `any` in `calendar.service.ts`, `calendar.component.ts`, `calendar-dialog.component.ts` and `board.service.ts`. Add typed DTOs, and consider generating them from OpenAPI (for example, `openapi-typescript`) so the API contract changes stay in sync.
- Subscriptions: many `.subscribe()` calls without cleanup (13 in `calendar.component.ts`, `currentUser$` in `home.component.ts` with no teardown). → Use `takeUntilDestroyed`.
- Debug `console.log` calls in `notification-realtime.service.ts` (7) and `calendar.component.ts` (7).
- Low test coverage where the logic is most complex: `calendar-engine/` has 1 spec for about 25 files, and the relationship-engine and relationship components have none.

### P2 — Before the first deployment (not urgent)
- **Config separation:** `environment.prod.ts` hardcodes a URL. Use a runtime `config.json` or a build-time replacement instead. The CORS origins are hardcoded in `Program.cs`; move them to config (`Cors:AllowedOrigins`). `AIService:BaseUrl` defaults to `localhost:8000` in the base `appsettings.json`. `hubUrl = environment.apiUrl.replace('/api','')` in `notification-realtime.service.ts` is fragile; add a separate `hubUrl`.
- **Tokens in localStorage.** This is documented as a trade-off in `auth.service.ts`. Move to httpOnly cookies, or a BFF on the same origin, once the deployment topology is known.
- **Stored HTML:** comment and item HTML is stored without server-side sanitization. Angular sanitizes `[innerHTML]` today, but add a server-side allow-list sanitizer (for example, `HtmlSanitizer`) before HTML ends up in emails or other clients.
- **Attachments on local disk** (`App_Data/calendar-attachments`, `InfrastructureServicesRegistration.cs`) will be lost on a container restart. → Use a volume or blob storage.
- **Migrations on startup** (`Program.cs`) is fine for one instance but risky with more than one. → Move to a migration bundle or a separate step.
- Serilog file sink `./logs/` in a container → stdout only. Add `/health` checks for the API, the database and the AI service. Containerize all three services. Re-introduce `ForwardedHeaders` with the real proxy's known networks. Don't expose the AI service publicly.
- Secrets: none were found in source (good: user-secrets plus env vars, and the AI service fails closed when keys are unset). Keep it that way with a secret store.

---

## 3. Features: incomplete or in need of polish
- **Stub routes** registered in `app.routes.ts`, each a 12-line component: `event-tracker`, `playalong`, `metronome`, `tuner`. `management` is an "Under Construction" page. The music features are the product's namesake but none of them exist yet. *Decided:* remove them until they're built (see section 6).
- `/protected` (`protected-workspace`) is where unauthenticated users land. Check whether you want this landing page, or a straight redirect to `/login?returnUrl=`.
- The app `<title>` is "Activity Management" (`index.html`, `app.component.ts`). That's a leftover name.

## 4. Breaking changes in the upgrade path that affect this codebase

### 4.1 .NET 8 → 10
- SDK and CI: install the .NET 10 SDK locally (only 9.0 is installed now) and add a `global.json`. Update the `Dockerfile` (`sdk:10.0`/`aspnet:10.0`) and `ci.yml` (`10.0.x`). Change the TFM in all csproj files, ideally through `Directory.Build.props`.
- **OpenAPI:** ASP.NET Core 10 moves to Microsoft.OpenApi 2.x. The `Microsoft.OpenApi.Models` types used in `Program.cs` (`OpenApiSecurityScheme`, `OpenApiReference`, `OpenApiSecurityRequirement`) change shape, and Swashbuckle 7 doesn't support it. → Either switch to the built-in `AddOpenApi()`/`MapOpenApi()` with Scalar or Swagger UI, or update to the Swashbuckle release that supports v2, and rewrite the bearer-scheme block. Also fix the `Version = "v2"` vs `"v1"` doc mismatch.
- `ForwardedHeadersOptions.KnownNetworks` is obsolete in .NET 10 (use `KnownIPNetworks`). This disappears if the Render block is removed.
- EF Core 9+: `Database.Migrate()` **throws** when the model has pending changes (`PendingModelChangesWarning`). That's relevant to the startup migration in `Program.cs`. The migration squash resolves it cleanly. Update Npgsql.EntityFrameworkCore.PostgreSQL to 10.x.
- Test stack: update `Microsoft.AspNetCore.Mvc.Testing`, `EF InMemory`, `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`. Moving to xunit v3 is optional; do it later if at all.
- Serilog.AspNetCore and its settings packages: move to the versions aligned with .NET 10.

### 4.2 Angular 18 → 22 (one major per hop, using `ng update @angular/core@N @angular/cli@N @angular/material@N`)
- **First, Node ≥ 22.22.3 or 24.15**, both locally and in `ClefCraft-Frontend/.github/workflows/ci.yml`. Angular 20/21 already reject the installed 20.16.
- **v19:** components become standalone by default. All 38 already declare `standalone: true`, and the migration removes the flag. Needs TS 5.5+. Bump `ngx-quill` to 27 and `ngx-mat-timepicker` to 19.
- **v20:** `*ngIf`/`*ngFor` are deprecated. Run `ng generate @angular/core:control-flow` (24 template files use `*ngIf`). Optionally move to the `@angular/build` builder (`angular.json` still uses `@angular-devkit/build-angular:application`/`:karma`). `@angular/animations` is deprecated. Remove `BrowserAnimationsModule`/`provideAnimationsAsync` from `main.ts` once nothing depends on them. Needs TS 5.8. `ngx-quill` 28.
- **v21:** Vitest is the default test runner and Karma is deprecated. **Keep Karma through the upgrade** and migrate to Vitest as a separate follow-up, using Angular's Jasmine→Vitest schematic on the 39 specs. HttpClient is provided by default (no effect here). Needs TS 5.9. `ngx-quill` 30. `ngx-toastr` must be gone by the end of this hop.
- **v22:** **TypeScript 6.0** is required. Review the `tsconfig.json` flags (`experimentalDecorators`, `useDefineForClassFields: false`, `esModuleInterop`) against the TS 6 deprecations. zone.js needs ~0.15/0.16. `ngx-quill` 31, `ngx-mat-timepicker` 22.
- **Angular Material 18 → 22:** the global theme comes from `@angular/material/prebuilt-themes/azure-blue.css` (`angular.json`). About 40 overrides target internal `.mat-mdc-*`/`.mdc-*` classes or use `::ng-deep`, mainly in `calendar-dialog.component.css` (12), `comment-composer.component.css` (9), `themes/material-theme.css` (9), `calendar.component.css`, `item-detail-dialog.component.css` and `styles.css`. These are the most likely places for **visual** breakage, and unit tests won't catch it. Plan to move them to the `mat.*-overrides()` token mixins.
- Remove `bootstrap`/`jquery` from `angular.json` test config and `package.json`. Run `npm audit fix` for quill.

### 4.3 Order relative to the other work
Upgrade **before** feature work and before the frontend refactors (P1.3). The suites are green, the code is still small, and there's no production risk. A refactor written now in Angular 18 style (`*ngIf`, NgZone, Karma) would just have to be migrated again. The backend upgrade is independent and can run in parallel with P0.2.

### 4.4 Effort and verification
- Estimate: .NET 1–2d, Angular 3–5d (the 4 hops are mostly mechanical; the time goes into Material visual checks and the toastr swap), Python 0.5d.
- **Before the Angular upgrade, add a small Playwright smoke suite (1–2d).** It runs against the docker-compose stack and covers login and refresh, calendar create/drag/resize/recurrence edit, board drag between columns, comment with @mention, reminder/mention toast, and dark/light theme. Take screenshots of the calendar, the calendar dialog, the board and the item-detail dialog to compare after each hop. Unit tests give good logic coverage but none of the visual coverage that the Material changes need.
- Backend: run `dotnet test` after the TFM bump and again after each package swap (AutoMapper removal especially). Add one Testcontainers Postgres test that applies the squashed migrations and runs the dev seeder.
- AI: create a venv, install `requirements-dev.txt`, run `pytest`, then retrain and commit the new pickles. Add a test that predicts for two different unseen users and checks the encoder doesn't change.

---

## 5. Execution order
This order replaces the one first proposed in the review. Each step is finished, tested against the baseline, and reviewed before the next one starts.

1. **P0.1:** repo hygiene, dead-code removal, and removal of every Render leftover. The Dockerfile stays, minus its Render-specific parts. The stub routes are also removed (see section 6).
2. **.NET 10 upgrade with the NuGet cleanup:** remove AutoMapper, pin MediatR 12.5.x, remove the legacy SignalR and other dead packages, and add `Directory.Build.props` and `Directory.Packages.props`.
3. **Migration squash** (generated with EF Core 10 tooling) **and the P0.2 dev setup.**
4. **Playwright smoke suite.**
5. **Node 24, then Angular 19 → 20 → 21 → 22.**
6. **Replace the AI feature** (see section 6), then **P1.2** backend cleanup and **P1.3** frontend decomposition.

After that, feature work resumes. P2 items are done once a deployment target is chosen.

## 6. Decisions
- **AI attendance feature: option (a) for now.** Replace it with a simple, transparent rule-based score computed in .NET. Remove the VIEW-signal writes from the calendar GET path (`GetCalendarEventsQueryHandler`). Don't delete the `clef_ai` repo; the backend just stops calling it. ML may come back later, once real usage data exists. This narrows P1.1 to the .NET replacement. The `clef_ai` findings in P1.1 stay on record for that later work.
- **Stub routes:** `event-tracker`, `playalong`, `metronome`, `tuner` and `management` are removed from navigation and routing for now (done in step 1). **They are planned features:** a practice/event tracker, play-along, metronome and tuner (the music tools that are the product's namesake), plus an admin management area. They come back when they're actually built.
- **DbContexts (merge `ClefCraftIdentityDbContext` and `ClefCraftDatabaseContext`, or keep them separate):** to be decided before step 3.

## Verification
The baseline to re-run after every phase is `dotnet test ClefCraft.sln` (315 tests passed at review time), `npx ng test --watch=false --browsers=ChromeHeadless` (258 passed at review time), and `pytest` in a venv for `clef_ai`. Test counts may drop only where tests for removed code were deliberately deleted, and each phase lists those tests. Each item above lists its own extra checks.
