# What to build next (services and infrastructure)

OmniCore is a learning project first. The best way to keep learning *and* get something you actually use is to pick services that solve a problem you have, each small enough to finish. Everything below follows the same recipe: `scaffold_service.ps1`, an ADR, own database, events instead of cross-service calls, integration test, gateway route, README per layer. Effort: S ≈ a weekend, M ≈ 1-2 weeks, L ≈ a month, at your pace.

## Foundation first (unlocks the rest)
| # | Item | Why now | Effort |
|---|---|---|---|
| 1 | **Notification service** (e-mail first; Mailpit in dev, SMTP/Resend in prod; later push/Telegram) | Auth cannot verify e-mails or reset passwords without it (AA4). Teaches event consumers, templates, retries, idempotency. Every later service reuses it. | M |
| 2 | **Observability stack** (Serilog -> Seq, OpenTelemetry traces/metrics -> Prometheus + Grafana) as compose profile, not a service | You cannot learn from the cloud simulation without seeing what happens. Fixes the blind spot behind A18. | S-M |
| 3 | **File/Media service** (MinIO behind presigned URLs, thumbnails, virus/size rules) | Avatar upload currently lives in User; every later feature with attachments needs it. Teaches streaming, signed URLs, background jobs. | M |

## Services you could really use
| Service | What it does for you | Owns | Talks via events | Learns you | Effort |
|---|---|---|---|---|---|
| **Notes / bookmarks / read-later** | Personal knowledge base with tags and full-text search | notes, tags | `NoteCreated` -> search index | PostgreSQL full-text search, soft delete, optimistic concurrency, later OpenSearch | M |
| **Application / job tracker** (if you are job-hunting; also works for freelance leads) | Kanban of applications, contacts, interview dates, follow-up reminders | applications, stages, contacts | reminders -> Notification | Quartz scheduling, state machines, CQRS read models | M |
| **Expense / budget tracker** | Daily spending, categories, monthly budgets, CSV import | transactions, budgets | `BudgetExceeded` -> Notification | money as decimal and currency, idempotent import, reporting queries | M-L |
| **Fitness / nutrition log** (ties to your SmartCaloBE idea) | Meals, calories, weight, habits, weekly summary | entries, foods | weekly summary -> Notification | large reference data, aggregation, caching (S6 done properly) | L |
| **Scheduler / reminders** | One place for "remind me at X" used by all the above | jobs | consumes `ScheduleReminder` | Quartz clustering, time zones, exactly-once-ish delivery | M |
| **Webhook / integration hub** | Receives GitHub/Telegram/etc. hooks and turns them into events | endpoints, deliveries | publishes normalised events | signature verification, retries, dead letters | M |

## Platform work worth doing as you go
- **BFF or API composition** for a future front-end (one call that joins Auth + User data), instead of the front-end calling each service.
- **Search service** (OpenSearch) only once Notes needs more than Postgres FTS: do not add it earlier.
- **Caddy/Traefik in front of the gateway** for real TLS locally and on a VPS.
- **Deploy it for real**: a small VPS with the cloud-sim override + a domain is the cheapest way to run your own tools 24/7, and the point where "personal use" starts.
- Contracts and docs: OpenAPI per service, AsyncAPI for events (Phase 5).

## Suggested order
1. Fix the entity items that unblock events (roadmap Phase 10), then **Notification** (verify e-mail, reset password), with **Mailpit** and **Seq** added to compose.
2. **Observability** slice, then run the cloud-sim drills with it on.
3. **Notes** (simplest real CRUD you will use daily) -> **Scheduler** -> **Job tracker** or **Expense tracker** (pick whichever you would open first thing in the morning).
4. **File/Media** when the first feature needs attachments.
5. Deploy to a VPS; fitness and webhook hub when the platform feels boring.

## Guardrails
- One service = one database; no cross-database reads; ids from the token.
- Do not add a service just to use a technology: write the ADR with the problem first.
- Keep the stack small: Kafka stays out until a real streaming need appears (Phase 6).
- Decide multi-tenancy (ADR 0001) **before** the third service, or every service gets retrofitted.
