# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Build and run (from repo root, where `AlAmalBusiness.sln` lives):
```
dotnet build
dotnet run --project AlAmalBusiness.Api
```

EF Core migrations (always pass both flags — `AppDbContext` lives in Infrastructure, `Program.cs`/config lives in Api):
```
dotnet ef migrations add <Name> --project AlAmalBusiness.Infrastructure --startup-project AlAmalBusiness.Api
dotnet ef database update --project AlAmalBusiness.Infrastructure --startup-project AlAmalBusiness.Api
```

There is no test project in this solution yet.

Dev DB: SQL Server at `localhost\MSSQLSERVER01`, database `AlAmalBusiness` (see `AlAmalBusiness.Api/appsettings.json`). Swagger UI is available at `/swagger` in the Development environment.

## Architecture

Four-project layered/clean architecture, referenced in one direction: `Api → Application/Infrastructure → Domain`. `Infrastructure` and `Application` do not reference each other directly — `Api`'s `Program.cs` is the composition root wiring `I<X>Repo` (Infrastructure) and `I<X>Service` (Application) into DI.

- **Domain** — entity models (`Models/`), repository interfaces (`IRepositories/`), enums (`Constants/`). No EF, no business logic.
- **Application** — DTOs (`DTOs/`) and business logic (`Services/Interface/`, `Services/Imp/`). Services depend only on repository *interfaces* from Domain, never on Infrastructure directly.
- **Infrastructure** — `AppDbContext` (EF Core + ASP.NET Identity), repository implementations (`Repository/Imp/`), migrations, `Seeding/DbInitializer` (seeds `AppRoles` on startup).
- **Api** — controllers, `Program.cs` (all middleware/DI wiring), SignalR hubs.

Every feature follows **Controller → Service → Repository → `AppDbContext`/EF model**, with DTOs per feature under `Application/DTOs/<Feature>/`. `DepartmentRepo`/`DepartmentService`/`DepartmentController` is the reference template for a plain lookup-list CRUD feature: repo does raw EF + an `IsXExist(name, excludeId)` uniqueness check, service returns a `{Success, Message, Data}` response DTO, no separate PATCH-status endpoint (`IsActive` travels in the same Create/Update DTO). **Every lookup DTO must include `Id`** — `DoctorDTO`/`ProcedureDTO`/`ReferalSourceDTO`/`ClosedReasonDTO` shipped without it initially (found 2026-08-31 wiring the frontend: a client has no way to reference an entity it can't get the id for), now fixed.

Enums serialize as their string name everywhere (`"Cash"`, `"Pending"`), not the numeric value — `Program.cs`'s `AddControllers().AddJsonOptions(...)` registers a global `JsonStringEnumConverter`. Don't remove this; without it every enum-typed request/response field silently becomes a bare number and any client sending/expecting a string name gets a 400 or garbage data.

### CRM / Lead feature

Lives under `Area/CRM/` in Api (`Area/CRM/Controllers/`, `Area/CRM/Hubs/`) and `DTOs/CRM/` in Application (`DTOs/CRM/Lead/`, `DTOs/CRM/LeadManageList/` for Doctors/Procedures/ReferralSources/ClosedReasons, `DTOs/CRM/Stats/`). This was ported from a separate legacy CRM app (`CRMS`) with two deliberate behavior changes from that original:

- **No "Forward" workflow** — a lead's owner only changes via Claim, never transferred to another user.
- **No separate "Failed" status** — `LeadStatus` is `New, Pending, Waiting, Success, Closed`. Closing a lead requires picking a `ClosedReason` (what the old app called `FailureReason`), enforced in `LeadService.FollowUpAsync`.

`Lead.CreatedById`/`ClaimedById` are `string` FKs straight to the local `AspNetUsers` table (via `IdentityUser`-derived `User`) — display names are read through EF navigation (`lead.CreatedBy.UserName`), no username-snapshot columns. Every workflow action (`Created`, `Claimed`, `ReOpened`, `Edited`, `FollowUp`) is appended to `LeadHistory` as an audit timeline, read back via `ILeadHistoryRepo`.

`LeadHub` (`/hubs/leads`) is a plain SignalR broadcast — `LeadCreated`/`LeadStatusChanged` events go to every connected client, no persisted notification inbox, no per-user targeting. `ILeadNotifier` is the Application-side abstraction (implemented in Api as `SignalRLeadNotifier`) so Application stays free of a SignalR package reference; notifier calls are always best-effort (wrapped so a push failure never fails the underlying write).

`Lead.CreatedDate` defaults to `DateTime.Now` (local time, not UTC) — `LeadRepo`'s date-range filters compare against local day boundaries accordingly. Don't introduce UTC-conversion logic there without changing the model default too.

`Lead.PhoneNum` is `string?` (not `int?` — was fixed 2026-08-31, storing as int silently drops a leading `0`, which every Jordanian mobile number starts with). Lead search (`LeadRepo.PageLeadsAsync`) matches `Name`/`NickName`/`PhoneNum`, and is leading-zero-tolerant on phone (matches whether the searcher types the `0` or not).

**Performance shape (found 2026-09-05, production = SmarterASP 1 GB shared pool + a separate shared SQL box, so every query is a network round trip):** list endpoints project straight into `Domain/IRepositories/CRM/LeadListRow` (`LeadRepo.ToRow`) — never `Include` six entities for a list row again (that pulled both `AspNetUsers` rows with password hashes plus the `Description`/`ClinicSignature` LOBs for every row). `GetQueueCountsAsync` is one `GroupBy(l => 1)` conditional-count query, not five `CountAsync` calls. `GetDashboardKpisAsync` runs two per-day bucket queries and sums in memory, not three queries per tile. Admin-dashboard aggregates (`kpis`, `status-counts`, `sources`, `employee-cases`, `stats`) are cached in `IMemoryCache` by `LeadService.CachedAsync` (60s TTL) and dropped by `InvalidateStats()` on every lead write — `ReloadActionResponse` is the single choke point every mutation flow passes through, plus `CreateLeadAsync`/`DeleteLeadAsync`; a new mutation path must reach one of those. Per-user data (`queue-counts`, `paged`) is deliberately not cached. `Leads` has indexes on `(Status, CreatedDate)` and `CreatedDate`, and `Name`/`NickName`/`PhoneNum`/`CountryKey` have max lengths (200/100/32/10) so they aren't LOBs. The Api csproj forces Workstation, non-concurrent GC for the 1 GB pool. `deploy.yml` must stay a RID-less framework-dependent publish launched via `dotnet <dll>` — a `-r win-x64` ReadyToRun publish (tried 2026-09-05) switches web.config to the `.exe` apphost and the host answered 500.30. Cold starts are handled by the frontend's 5-minute keep-alive ping instead.

`LeadStatsController` (`Area/CRM/Controllers`) has three actions with different access: `GET stats` (all-time `AdminStatsDTO`, Admin-only), `GET kpis` (`DashboardKpiDTO` — leads/successes this month vs today, each with a 7-day trend and a delta vs. the prior period, any CRM role), `GET employee-cases?period=today|month` (per-employee created-lead counts, any CRM role). `PaymentWays` intentionally stays `Cash`/`Insurance` only — those are the only two real payment methods.

### Filter persistence (no Redis — see hosting note below)

List endpoints that take filter/paging query params (`LeadController.GetPaged`/`GetCreatedByMePaged`, `HospitalManagerController.GetStats`/`ExportStats`) remember each user's last-used filter via `IFilterCacheService` (`Application/Services/Interface`), so a bare request with **no query string** restores it instead of falling back to hard defaults — any query string present is used exactly as sent and becomes the new saved value. Cache key is `filter:{userId}:{endpointKey}`.

This is backed by `IDistributedCache` (`AddDistributedMemoryCache()` in `Program.cs`, in-process only) rather than Redis — the production host is **smartasp.net shared hosting**, which can't run Redis or any other separately-hosted service. `IFilterCacheRepo` (Domain) → `FilterCacheRepo` (Infrastructure, wraps `IDistributedCache`) is the only place that would need to change to move to Redis later (`AddStackExchangeRedisCache(...)` instead of `AddDistributedMemoryCache()`); everything above that talks only to the abstraction. TTL is `FilterCacheSettings:TtlDays` in config (default 30). Keep this constraint in mind before adding any other infra that needs its own running process.

### Auth & authorization

JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) + ASP.NET Identity (`User : IdentityUser`, `IdentityRole`). Roles are fixed strings in `AlAmalBusiness.Domain.Constants.AppRoles`: `Admin`, and per-department worker tiers `CManager/CEmployee/CUser` (CRM) and `FManager/FEmployee/FUser` (Finance — not yet built out). `DbInitializer.SeedRolesAsync` seeds these on startup.

Authorization is **secure-by-default**: `Program.cs` sets a global `FallbackPolicy` requiring an authenticated user, so any new endpoint needs an explicit `[Authorize]`/`[AllowAnonymous]` — nothing is anonymous by omission. `AuthController.Login` is the one `[AllowAnonymous]` endpoint. `UserController` and `DepartmentController`'s admin-only actions are role-gated via `[Authorize(Roles = nameof(AppRoles.X))]`. `GET /api/Auth/me` returns the caller's own `GetUserResponse` (id/username/fullname/roles) — covered by the fallback policy, no explicit attribute. The JWT itself carries `sub`, `unique_name`, `name` (= `FullName`, added 2026-09-05) and `role` claims, and the Next.js console reads the current user from those claims instead of calling `/me` on every page — keep `name` in the token (`TokenService`) or the console's header/role gates lose the display name.

`AppDbContext.OnModelCreating` must call `base.OnModelCreating(modelBuilder)` first — without it, Identity's own entity configuration (including .NET 10's Passkey entities) silently isn't applied, which breaks `dotnet ef migrations add` entirely with an opaque "requires a primary key" error.

Rate limiting is two-layer, configured in `Program.cs`: a global per-IP sliding-window safety net (`GlobalLimiter`), plus a `"PerUserLimit"` policy (tighter per-authenticated-user limit, looser per-anonymous-IP limit) that controllers opt into via `[EnableRateLimiting("PerUserLimit")]`. `RateLimiterSettings:WhitelistedIps` in config bypasses both layers.

SignalR hub auth: browsers can't set an `Authorization` header on a WebSocket handshake, so the JWT bearer's `OnMessageReceived` event reads the token from `?access_token=` on requests under `/hubs` (see `Program.cs`) — needed for any future hub, not just `LeadHub`.
