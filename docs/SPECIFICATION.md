# Shared Expense Tracker — Specification

## 1. Purpose & Scope

A web application for a group of people (initially: friends on a trip; the
domain is intentionally generic so the same app later covers other shared or
personal recurring expenses, e.g. monthly household spending) to log shared
expenses, tag who each expense is for, and see who owes whom — with the
system computing the minimum number of transfers needed to settle up. The app
does not move money itself; it only calculates and displays debts.

This document is the specification: use cases, functional requirements, and
non-functional requirements. Architecture/tech-stack decisions are captured
in §7 as the constraints those requirements impose.

## 2. Actors

- **Member** — a person authenticated via their Google account. Once a Member
  belongs to a Topic (see §3), they can create subtopics, log and edit
  expenses, tag participants, and invite others — all with equal rights.
  **Deletion is the one exception:** a Member may delete a Topic node or an
  expense only if they are the one who created it. There is no separate
  owner/admin tier beyond this creator-only delete rule.
- **Google Identity Services** — external actor providing authentication only
  (no data beyond identity: email, name, avatar).

## 3. Key Concept: Topic

Everything the user organizes is a `Topic` in one self-referencing tree —
there is no separate "Trip" entity. A **root topic** (no parent) is the
container that owns membership and the invite link — e.g. "Portugal Trip" or
"August 2026 Expenses." Any topic, root or nested, can have child topics under
it (Root → "Fuel" → "Rental Car"), and expenses can be logged at any node.
Balances and settlements are always computed for the whole root topic,
aggregating every descendant subtopic — subtopics are for categorization and
reporting, not separate ledgers.

## 4. Use Cases

### UC-1 — Sign in with Google
- **Actor:** Unauthenticated visitor
- **Preconditions:** none
- **Main flow:** visitor opens the app, clicks "Sign in with Google," completes
  Google's sign-in flow, is returned to the app authenticated as a Member.
- **Postconditions:** a `User` record exists for this Google account; the
  Member sees the list of root Topics they belong to (empty on first sign-in).

### UC-2 — Create a new (root) Topic
- **Actor:** Member
- **Main flow:** Member provides a name (and optional description), submits;
  system creates a root Topic and adds the creator as its first member.
- **Postconditions:** Topic appears in the Member's Topic list; an invite code
  exists for it.

### UC-3 — Invite someone to a Topic
- **Actor:** Member (of that root Topic)
- **Main flow:** Member requests/opens the invite link for a root Topic and
  shares it (any channel — text, WhatsApp, etc.) with the person they want to
  add.
- **Alt flow:** Member can rotate the invite code to invalidate the previous
  link (e.g. it was shared too widely).

### UC-4 — Join a Topic via invite link
- **Actor:** Visitor or existing Member
- **Preconditions:** holds a valid, non-rotated invite code/link
- **Main flow:** opens the link, signs in with Google if not already (UC-1),
  system adds them as a member of that root Topic.
- **Postconditions:** the joining user is a full Member of the Topic and every
  existing subtopic under it; they immediately see its expense history.

### UC-5 — Create a subtopic
- **Actor:** Member
- **Preconditions:** Member belongs to the parent Topic's root
- **Main flow:** Member selects a parent Topic (root or nested), provides a
  name, submits; a new child Topic is created under it. Nesting depth is
  unlimited.

### UC-6 — Delete a Topic or subtopic
- **Actor:** the Member who created that specific Topic node
- **Preconditions:** the requesting Member is that node's creator; any other
  Member (even another member of the same root Topic) is denied.
- **Main flow:** Member deletes a Topic node they created.
- **Business rule:** deleting a node deletes its subtopics and their
  expenses too (cascading), regardless of who created those descendants —
  the system must warn and require confirmation before a delete that would
  remove any expense.
- **Constraint:** deleting the root Topic removes the whole group, its
  members, and all history — requires explicit, harder confirmation (e.g.
  type-to-confirm the Topic name), and only the root's creator can do it.

### UC-7 — Log an expense
- **Actor:** Member
- **Preconditions:** Member belongs to the Topic (root or subtopic) they're
  logging under
- **Main flow:** Member picks a Topic node, enters description, amount (€),
  date, and selects one or more Members to tag as participants (defaults to
  all current Topic members, editable); submits.
- **Business rule:** the amount is split equally among tagged participants;
  each participant's share is computed and stored at creation time.
- **Postconditions:** the expense appears in that Topic's expense list and
  is included in balance/settlement calculations immediately.

### UC-8 — Edit an expense
- **Actor:** Member (any member of the Topic — editing has no creator
  restriction, only deletion does; see UC-9)
- **Main flow:** Member changes description, amount, date, payer, and/or
  tagged participants of an existing expense; on save, participant shares are
  recomputed for that expense.

### UC-9 — Delete an expense
- **Actor:** the Member who created that expense (the one who logged it —
  not necessarily the payer, if someone logs an expense on another member's
  behalf)
- **Preconditions:** the requesting Member is that expense's creator; any
  other Member is denied.
- **Main flow:** Member removes an expense they created; it's excluded from
  balance and settlement calculations immediately.

### UC-10 — View balances
- **Actor:** Member
- **Main flow:** Member opens a Topic's "Balances" view and sees, for every
  member of that root Topic, their net position (positive = owed money,
  negative = owes money), aggregated across the whole subtopic tree.

### UC-11 — View the settlement plan
- **Actor:** Member
- **Main flow:** Member opens "Settle Up" for a root Topic and sees the
  minimal list of suggested transfers (who pays whom, how much) that would
  bring every balance to zero.

### UC-12 — Mark a settlement as paid
- **Actor:** Member
- **Main flow:** after settling a suggested transfer outside the app (cash,
  bank transfer, etc.), a Member marks it as paid.
- **Postconditions:** that transfer is recorded as settled and excluded from
  future settlement-plan calculations (future plans are recomputed from
  remaining unsettled expenses/settlements).

### UC-13 — Browse expense history
- **Actor:** Member
- **Main flow:** Member browses a Topic's subtopic tree and views the list of
  expenses logged at any node, with payer, amount, date, and tagged
  participants visible per expense.

## 5. Functional Requirements

**Authentication & Membership**
- FR-1: The system SHALL authenticate users exclusively via Google Sign-In; no
  username/password authentication exists.
- FR-2: The system SHALL create a User record automatically on first
  successful Google sign-in, keyed by the Google account's stable subject ID.
- FR-3: The system SHALL restrict all Topic content (view, create, edit,
  delete) to Members of that Topic's root.
- FR-4: The system SHALL let any Member of a root Topic generate/share and
  rotate its invite link.
- FR-5: The system SHALL add a user as a Member of a root Topic when they
  redeem a valid invite link.

**Topics**
- FR-6: The system SHALL allow a Member to create a new root Topic, becoming
  its first Member.
- FR-7: The system SHALL allow any Member to create a subtopic under any
  Topic node they can access, at unlimited nesting depth.
- FR-8: The system SHALL allow any Member to rename any Topic node they can
  access.
- FR-8a: The system SHALL allow a Topic node to be deleted only by the
  Member who created it, cascading deletion to descendant subtopics and
  expenses regardless of who created those descendants; the system SHALL
  reject a delete request from any other Member.
- FR-9: The system SHALL require explicit confirmation before deleting a
  Topic node that has expenses or subtopics under it, and a stronger
  confirmation before deleting a root Topic.

**Expenses**
- FR-10: The system SHALL allow any Member to log an expense against any
  Topic node they can access, capturing description, amount, date, payer, and
  one or more tagged participants.
- FR-11: The system SHALL split an expense's amount equally among its tagged
  participants and persist each participant's computed share at creation
  time.
- FR-12: The system SHALL allow any Member to edit any expense in a Topic
  they belong to (no restriction to the original creator/payer for edits).
- FR-12a: The system SHALL allow an expense to be deleted only by the
  Member who created (logged) it; the system SHALL reject a delete request
  from any other Member, including the payer if they are not the creator.
- FR-13: Editing an expense's amount or participant list SHALL recompute and
  re-persist that expense's participant shares.

**Balances & Settlement**
- FR-14: The system SHALL compute each Member's net balance for a root Topic
  as the sum of amounts they paid minus the sum of their shares owed, across
  every expense in that root Topic and all of its subtopics.
- FR-15: The system SHALL compute a settlement plan — a minimal set of
  member-to-member transfers that brings every balance in a root Topic to
  zero — using a greedy largest-creditor/largest-debtor matching algorithm.
- FR-16: The system SHALL allow a Member to mark a suggested transfer as
  settled, and SHALL exclude settled transfers from subsequent settlement-plan
  recomputation.
- FR-17: Balances and settlement plans SHALL reflect the current state of
  expenses in real time (recomputed on read, not cached stale).

**Currency**
- FR-18: The system SHALL denominate all amounts in EUR; no currency
  selection or conversion is supported.

## 6. Non-Functional Requirements

**Cost & Hosting**
- NFR-1: The system SHALL run entirely on free-tier hosting with no recurring
  cost at the expected usage scale (a handful of small friend groups).
- NFR-2: The system SHALL be deployable as containers (Docker), independent
  of any specific hosting provider's proprietary runtime.

**Technology**
- NFR-3: The backend SHALL be built on the latest .NET LTS release available
  at implementation time (.NET 10 as of this specification).
- NFR-4: The frontend SHALL be built on the latest stable React release
  available at implementation time.
- NFR-5: Backend and frontend SHALL live in a single repository/solution.
- NFR-6: All persistent data SHALL be stored in a relational database
  (PostgreSQL).

**Usability**
- NFR-7: The UI SHALL be mobile-first and fully usable on phone-sized
  viewports (≥360px wide) — this is a primary usage mode, not a secondary
  one, since expenses are commonly logged on the go.
- NFR-8: Interactive touch targets SHALL be at least 44×44px on mobile
  layouts.
- NFR-9: The UI SHALL also render well on tablet and desktop widths
  (responsive, not mobile-only).

**Security**
- NFR-10: All traffic SHALL be served over HTTPS.
- NFR-11: Session tokens SHALL be stored in httpOnly, Secure cookies, never in
  browser-accessible storage (localStorage/sessionStorage), to limit XSS token
  theft.
- NFR-12: Every API endpoint except the Google sign-in exchange SHALL require
  an authenticated session and SHALL authorize the caller against Topic
  membership before returning or mutating data; delete operations on Topics
  and Expenses SHALL additionally verify the caller is the creator of that
  specific record (see FR-8a, FR-12a).

**Data Integrity**
- NFR-13: An expense's stored participant shares SHALL remain fixed once
  created, changing only through an explicit edit to that expense — adding or
  removing a Topic member elsewhere SHALL NOT retroactively alter past
  expenses.

**Performance & Scale**
- NFR-14: The system is sized for small groups (tens of members per Topic,
  low thousands of expenses per root Topic) — no requirement for high-volume
  or multi-tenant-at-scale performance.
- NFR-15: Cold-start latency from free-tier scale-to-zero hosting (a few
  seconds on first request after idle) is an accepted tradeoff of NFR-1, not
  a defect.

**Compatibility**
- NFR-16: The web app SHALL support current evergreen browsers (Chrome,
  Safari, Firefox, Edge) on both desktop and mobile; no support requirement
  for legacy browsers.

**Maintainability**
- NFR-17: Business logic that is pure computation (notably the settlement
  algorithm) SHALL be isolated from persistence/infrastructure code so it can
  be unit-tested without a database.

## 7. Constraints (from the locked-in architecture decisions)

These are the concrete choices made to satisfy the requirements above; kept
here as constraints on implementation rather than re-litigated per feature.

| Requirement(s) | Constraint |
|---|---|
| NFR-1, NFR-2 | Host on Google Cloud Run (API container, scale-to-zero) + Vercel (React static build) + Neon (Postgres free tier — chosen over Render because Render's free Postgres hard-deletes after 30 days) |
| NFR-3 | Backend: ASP.NET Core, .NET 10 |
| NFR-4, NFR-5 | Frontend: React (latest) + TypeScript + Vite, in the same repo as the backend `.sln` |
| NFR-6 | PostgreSQL via EF Core/Npgsql |
| FR-1, FR-2, NFR-10, NFR-11 | Google Identity Services on the frontend → backend validates the ID token, issues its own httpOnly/Secure/SameSite=None session cookie |
| FR-11, FR-13, FR-15, NFR-17 | Equal-split and settlement logic implemented as a pure, DB-free `SettlementService`/`ExpenseSplitService` in a `Core` project, unit-tested in isolation |
| NFR-7, NFR-8, NFR-9 | Tailwind CSS + shadcn/ui; bottom tab nav and card-based lists on mobile, sidebar/table layouts on desktop |

## 8. Out of Scope (backlog, not blocking MVP)

- Custom/uneven expense splits (exact amounts or percentages per participant)
- Multi-currency support / exchange-rate conversion
- Real payment provider integration (Stripe, PayPal, bank-transfer APIs) —
  the system only calculates and displays debts
- Push notifications
- Broader owner/admin permission tiers beyond the creator-only delete rule
  already in scope (FR-8a, FR-12a) — e.g. no concept of a Topic "owner" who
  can delete things others created, or moderate membership
