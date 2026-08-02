# Shared Expense Tracker — TDD Implementation Plan

## Context

`docs/SPECIFICATION.md` (13 use cases, FR-1..FR-18 incl. FR-8a/FR-12a, NFR-1..
NFR-17) is approved and committed. The repo is otherwise empty. The plan is to
build this test-first: every use case gets at least one test before the code
that satisfies it exists, and every FR/NFR gets a test wherever a test is
actually meaningful (a few NFRs are architectural/operational constraints
rather than testable behavior — those are called out explicitly rather than
faked with a hollow test).

This document defines: the test tooling per layer, the test-double boundaries
needed to make Google auth and Postgres testable, a full requirement→test
traceability matrix, and the slice-by-slice red→green→refactor build order.

**Toolchain note:** the spec (NFR-3) requires .NET 10. The dev machine had
only .NET 9 installed as of this writing; .NET 10 SDK (10.0.302) has since
been installed side-by-side via winget.

## 1. Test Tooling

| Layer | Framework | Notes |
|---|---|---|
| `Core` (pure domain/business logic) | xUnit | No EF/ASP.NET/Npgsql references at all — enforced structurally (see NFR-17 row in the matrix), so these tests need no DB and run in milliseconds. |
| `Api` (endpoints, auth, authorization) | xUnit + `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` | Integration tests boot the real API pipeline against a real, ephemeral Postgres container — faithful to NFR-6, and consistent with the project already being container-first. |
| `client` (components) | Vitest + React Testing Library + MSW | MSW mocks the API so component tests aren't coupled to a running backend. |
| `client` (end-to-end) | Playwright | A small number of critical-path specs (sign-in → create topic → log expense → view settlement) plus the mobile-viewport/responsiveness checks. Run against `chromium`, `webkit`, `firefox` projects as the automated proxy for NFR-16 browser compatibility. |

**Test doubles needed at the boundaries** (so TDD doesn't require hitting real
external services):
- `IGoogleTokenValidator` (interface in `Core`, real impl in `Infrastructure`
  using `Google.Apis.Auth`) — integration tests register a
  `FakeGoogleTokenValidator` that deterministically accepts/rejects known
  fake tokens, so auth tests never call real Google servers.
- `IClock` (interface in `Core`) wrapping `DateTimeOffset.UtcNow` — lets tests
  assert on `CreatedAt`/`ExpenseDate` deterministically instead of dealing
  with timing flakiness.

## 2. Requirement → Test Traceability Matrix

### Use cases (one row = at least one test)

| UC | Test(s) | Layer |
|---|---|---|
| UC-1 Sign in with Google | `AuthTests.PostGoogleAuth_WithValidToken_CreatesUserAndSetsSessionCookie`; `AuthTests.PostGoogleAuth_WithInvalidToken_Returns401` | Api integration |
| UC-2 Create root Topic | `TopicTests.PostTopics_CreatesRootTopicWithCreatorAsFirstMember` | Api integration |
| UC-3 Invite to a Topic | `TopicInviteTests.RotateInviteCode_InvalidatesPreviousLink` | Api integration |
| UC-4 Join via invite link | `TopicInviteTests.JoinTopic_WithValidCode_AddsMember`; `..._WithRotatedCode_IsRejected` | Api integration |
| UC-5 Create subtopic | `SubtopicTests.PostSubtopic_UnderAnyNode_CreatesChildAtUnlimitedDepth` | Api integration |
| UC-6 Delete Topic/subtopic | `TopicDeletionTests.DeleteTopic_ByCreator_CascadesToDescendantsAndExpenses`; `..._ByNonCreator_Returns403`; `..._RootTopic_RemovesMembersAndAllHistory` | Api integration |
| UC-7 Log an expense | `ExpenseTests.PostExpense_SplitsAmountEquallyAmongTaggedParticipants` | Api integration |
| UC-8 Edit an expense | `ExpenseTests.PutExpense_ByAnyMember_RecomputesParticipantShares` | Api integration |
| UC-9 Delete an expense | `ExpenseTests.DeleteExpense_ByCreator_Succeeds`; `..._ByNonCreator_Returns403` | Api integration |
| UC-10 View balances | `BalanceTests.GetBalances_AggregatesAcrossAllDescendantSubtopics` | Api integration |
| UC-11 View settlement plan | `SettlementServiceTests` (Core, several scenarios — see below) + `SettlementApiTests.GetSettlements_ReturnsMinimalTransferList` | Core unit + Api integration |
| UC-12 Mark settlement paid | `SettlementTests.PostMarkPaid_ExcludesTransferFromFutureRecomputation` | Api integration |
| UC-13 Browse expense history | `ExpenseTests.GetExpenses_ListsAcrossSubtopicTreeWithPayerAndParticipants` | Api integration |

`SettlementServiceTests` scenarios (pure `Core` unit tests, the natural TDD
starting point since they need no infrastructure): simple two-person pair,
three-way cycle, single payer for the whole group, already-settled no-op,
uneven amounts producing fractional cents, and a case proving the output is
at most `n − 1` transactions.

`ExpenseSplitServiceTests` scenarios (pure `Core` unit tests, same isolation
level as `SettlementServiceTests` — required by spec §7's "unit-tested in
isolation" constraint for the split logic, not just the settlement logic):
evenly divisible amount split N ways (all shares equal), an amount that
doesn't divide evenly (e.g. €10.00 across 3 participants) proving shares sum
back exactly to the original amount with the remainder cent(s) allocated
deterministically, a single-participant expense (share = full amount), and a
many-participants/small-amount case proving no share is negative and shares
still sum to the total.

### Functional requirements

| FR | Test | Notes |
|---|---|---|
| FR-1 | `AuthTests.PostGoogleAuth_WithInvalidToken_Returns401` | |
| FR-2 | `AuthTests.PostGoogleAuth_FirstSignIn_CreatesUserKeyedByGoogleSubjectId` | |
| FR-3 | `AuthorizationTests.NonMember_Returns403ForTopicEndpoints` (`[Theory]` over every Topic/Expense endpoint) | |
| FR-4, FR-5 | covered by UC-3/UC-4 tests above | |
| FR-6, FR-7 | covered by UC-2/UC-5 tests above | |
| FR-8 | `TopicTests.PatchTopic_ByAnyMember_RenamesSuccessfully` | |
| FR-8a | covered by UC-6 tests above | |
| FR-9 | `TopicDeletionTests.DeleteTopic_WithDescendants_WithoutConfirmation_Returns409WithWarningPayload`; `..._WithConfirmation_Succeeds`; `DeleteRootTopic_WithoutNameConfirmation_Returns400`; `..._WithCorrectNameConfirmation_Succeeds` | The API is the enforcement point (never trust a client-only confirmation dialog) — see note below the matrix. |
| FR-10, FR-11 | `ExpenseTests.PostExpense_SplitsAmountEquallyAmongTaggedParticipants` (API) + `ExpenseSplitServiceTests` (Core, see scenarios above) | Previously only the API happy-path test was cited; the isolated unit suite required by spec §7 was missing. |
| FR-12 | covered by UC-8 test above | |
| FR-12a | covered by UC-9 tests above | |
| FR-13 | covered by UC-8 test above | |
| FR-14 | covered by UC-10 test above | |
| FR-15 | covered by `SettlementServiceTests` | |
| FR-16 | covered by UC-12 test above (`SettlementTests.PostMarkPaid_ExcludesTransferFromFutureRecomputation`) | |
| FR-17 | `BalanceTests.GetBalances_ImmediatelyReflectsNewlyLoggedExpense_NotCachedStale`; `SettlementApiTests.GetSettlements_ImmediatelyReflectsEditedExpense_NotCachedStale` | Distinct from FR-16: this is about no caching layer masking the current state, not about excluding settled transfers — the previously-cited mark-paid test doesn't exercise this. |
| FR-18 | Structural: no currency field/parameter exists on the `Expense` DTO at all (schema-level enforcement), plus `ExpenseTests.PostExpense_ResponseAlwaysReportsAmountInEur` (serialization contract test) | |

**FR-9 confirmation mechanism:** confirmation has to be enforced server-side,
not just as a frontend dialog (a frontend-only gate is trivially bypassable
by calling the API directly, and NFR-12 already establishes the API as the
authorization boundary). Concretely: `DELETE /api/topics/{id}` returns `409`
with a warning payload (counts of descendant subtopics/expenses that would be
removed) unless the request includes `confirmCascade: true`; deleting a
**root** topic additionally requires a `confirmName` field matching the
topic's exact name, returning `400` otherwise. The frontend's confirmation
dialog (built in step 8) is the UX for supplying these fields, but the tests
above hit the API directly so the requirement is verified independent of any
particular frontend implementation.

### Non-functional requirements

| NFR | Test / verification | Notes |
|---|---|---|
| NFR-1 (free-tier cost) | Not automatable | Enforced by hosting choice itself (Cloud Run + Vercel + Neon, per spec §7); no code-level test applies. |
| NFR-2 (containerizable) | CI step: `docker build` on the Api and client Dockerfiles must succeed | Build-time check counts as automated verification. |
| NFR-3 (.NET 10) | `global.json` pins the SDK version; CI fails if it drifts | |
| NFR-4 (React latest) | `package.json` pin + CI `npm ls react` check | |
| NFR-5 (single repo) | Structural — one `.sln`, one repo | Not a test. |
| NFR-6 (Postgres) | Every Api integration test already runs against real Postgres via Testcontainers | The integration suite *is* the evidence. |
| NFR-7, NFR-8, NFR-9 (mobile-first, touch targets, responsive) | Playwright `responsive.spec.ts`: layout assertions at 360/390/768/1280px (bottom nav vs. sidebar, card vs. table lists, computed touch-target size ≥44px) | |
| NFR-10 (HTTPS) | `SecurityHeadersTests.HttpRequest_RedirectsToHttps` | |
| NFR-11 (cookie flags) | `AuthTests` asserts `Set-Cookie` has `HttpOnly`, `Secure`, `SameSite=None` | Same test as UC-1, extra assertions. |
| NFR-12 (authn + authz everywhere) | `AuthenticationTests.NoSessionCookie_Returns401ForAllProtectedEndpoints` (`[Theory]`, the "is there a session at all" half) + `AuthorizationTests` theory (see FR-3, the "is this session a Topic member" half) + delete-specific creator checks (FR-8a/FR-12a tests) | Previously only the authorization half was cited; the requirement has two independent halves (authenticate, then authorize) and each needs its own theory so a regression in one can't hide behind the other passing. |
| NFR-13 (share immutability) | `ExpenseTests.AddingMemberAfterExpenseCreated_DoesNotAlterExistingExpenseShares` | |
| NFR-14 (scale) | Not automated in this phase | Sized for small groups; no load-test infra proposed now — flag as future work if usage grows. |
| NFR-15 (cold start) | Not automated | Accepted operational tradeoff per spec; nothing to assert. |
| NFR-16 (browser compat) | Playwright smoke spec runs on `chromium`/`webkit`/`firefox` projects | |
| NFR-17 (pure Core logic) | `ArchitectureTests.CoreProject_HasNoInfrastructureOrAspNetReferences` (inspect `Core.csproj`'s `PackageReference`/`ProjectReference` items, or use NetArchTest) | Turns a design rule into an enforced, automated test. |

## 3. Repository Additions

```
Expenses-Tracker.sln
global.json                          (pins .NET 10 SDK)
src/
  Api/                                (Program.cs, controllers, auth, CORS)
  Core/                               (entities, DTOs, SettlementService,
                                        ExpenseSplitService, IGoogleTokenValidator,
                                        IClock, repository interfaces)
  Infrastructure/                     (AppDbContext, EF migrations, repo impls,
                                        GoogleTokenValidator, SystemClock)
tests/
  Core.Tests/                         (xUnit, no infra deps — SettlementServiceTests,
                                        ExpenseSplitServiceTests, ArchitectureTests)
  Api.IntegrationTests/               (xUnit + WebApplicationFactory +
                                        Testcontainers.PostgreSql;
                                        FakeGoogleTokenValidator, FakeClock)
client/
  src/...
  src/**/*.test.tsx                   (Vitest + RTL + MSW)
  e2e/
    smoke.spec.ts                     (sign-in → create topic → log expense →
                                        settlement, critical path only)
    responsive.spec.ts                (NFR-7/8/9 viewport checks)
docs/
  SPECIFICATION.md                    (already committed)
  IMPLEMENTATION_PLAN.md              (this document)
docker-compose.yml                    (postgres + api + client, local dev)
.github/workflows/ci.yml              (dotnet test, npm test, playwright test,
                                        docker build — all gate merges)
```

## 4. Build Order (strict red → green → refactor per slice)

Each numbered step is a vertical slice: write the failing test(s) from the
matrix above first, confirm they fail for the right reason (compile error or
assertion failure, not a setup bug), implement the minimal code to pass, then
refactor with tests green.

1. **Scaffold** — empty solution, `global.json` (.NET 10), the four projects,
   `docker-compose.yml`, CI skeleton. `Core.Tests` and `Api.IntegrationTests`
   exist and run (0 tests) to prove the pipeline works before any real test
   is added.
2. **Settlement algorithm** (`Core`, no infra) — write `SettlementServiceTests`
   scenarios first (this is the purest, most natural TDD starting point),
   then implement `SettlementService`. Also add
   `ArchitectureTests.CoreProject_HasNoInfrastructureOrAspNetReferences` here
   since `Core` is fully defined at this point.
3. **Auth slice** (UC-1, FR-1/2, NFR-10/11/12) — `FakeGoogleTokenValidator` +
   `AuthTests` first, then `POST /api/auth/google`, cookie issuance, HTTPS
   redirect middleware. Also start `AuthenticationTests` here (the
   no-session-cookie → 401 theory) with whatever protected endpoint exists at
   this point, and extend it with one more `[InlineData]` case in every
   subsequent slice as new endpoints are added — same pattern already used
   for `AuthorizationTests`.
4. **Topic + membership slice** (UC-2/3/4/5, FR-3/4/5/6/7/8) —
   `TopicTests`/`TopicInviteTests`/`SubtopicTests`/`AuthorizationTests` first,
   then Topic CRUD, invite generation/rotation/redemption, membership checks.
5. **Topic deletion slice** (UC-6, FR-8a/9) — `TopicDeletionTests` first:
   creator-only + cascade, then the confirmation-gate cases specifically
   (missing `confirmCascade` → 409 with warning payload, present → succeeds;
   root delete missing/wrong `confirmName` → 400, correct → succeeds), then
   the delete endpoint, cascade logic, and confirmation checks.
6. **Expense slice** (UC-7/8/9/13, FR-10/11/12/12a/13/18, NFR-13) —
   `ExpenseSplitServiceTests` first (pure `Core`, same TDD-starting-point
   approach as step 2's `SettlementServiceTests`), then `ExpenseTests`
   covering create/split, edit/recompute, delete (creator-only), list, and
   the share-immutability case, then implement `ExpenseSplitService` and the
   Expense CRUD endpoints.
7. **Balances & settlement API slice** (UC-10/11/12, FR-14/15/16/17) —
   `BalanceTests`/`SettlementApiTests`/`SettlementTests` first (these call the
   already-tested `SettlementService` from step 2 through the API), including
   the FR-17 "immediately reflects" cases (log/edit an expense, then read
   balances/settlements in the same test with no cache in between) alongside
   the FR-16 mark-paid exclusion case, then the balances/settlement/mark-paid
   endpoints — built with no response caching, by design.
8. **Frontend component layer** — Vitest+RTL+MSW tests per screen (topic
   list, subtopic tree, add-expense form, balances/settlement view) written
   against the now-stable API contract, then the React components.
9. **Frontend responsiveness + E2E** — `responsive.spec.ts` first (asserting
   nav/layout behavior at each breakpoint before the CSS exists), then the
   mobile-first Tailwind/shadcn layout; `smoke.spec.ts` last, as the final
   full-stack proof once every slice above is green.
10. **CI wiring** — `.github/workflows/ci.yml` runs `dotnet test` (Core +
    integration, Docker available on GH-hosted runners for Testcontainers),
    `npm test`, `npx playwright test`, and `docker build` for both
    Dockerfiles, gating merges to `main`.

## 5. Verification

- `dotnet test` from the repo root runs `Core.Tests` and
  `Api.IntegrationTests` (the latter spins up Testcontainers Postgres
  automatically — requires Docker running locally, same prerequisite the
  project already has via docker-compose).
- `npm test` (Vitest) in `client/` for component tests;
  `npx playwright test` for the e2e/responsive specs (starts the app via
  `docker compose up` or Vite/API dev servers per Playwright config).
- Cross-check the traceability matrix in §2 against the test suite before
  calling any slice "done" — every UC/FR row must have a passing test with
  that exact name (or the plan is out of sync and should be corrected, not
  the test skipped).
