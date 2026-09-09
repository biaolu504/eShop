# Initial engineering backlog

This backlog is grounded only in repository source and the [repository map](repository-map.md), [AI comparison](ai-comparison-01-architecture.md), [environment guide](development-environment.md), and [verified baseline](development-baseline.md). The baseline records 122 .NET tests and 4 Playwright tests passing; it does not establish coverage of the failure cases below. No tests were run to create this document.

Priorities: **P0** immediate containment of an established critical incident; **P1** next corrective work or a deployment prerequisite; **P2** planned hardening/design; **P3** later improvement. No P0 incident is established by the available evidence. Estimates are relative implementation scope, including verification, not elapsed-time promises. Acceptance criteria describe future work. IDs are stable; grouping does not imply execution order.

## Reliability

### ENG-01 — Retain the basket after an unsuccessful Ordering response

**Category:** Reliability · **Priority:** P1 · **Estimate:** Small

- **Evidence:** `src/WebApp/Services/OrderingService.cs`, `OrderingService.CreateOrder`, returns `SendAsync` as `Task` without inspecting the response. `src/WebApp/Services/BasketState.cs`, `BasketState.CheckoutAsync`, awaits it and then calls `DeleteBasketAsync`.
- **Why it matters:** A returned HTTP error can reach basket deletion. Transport exceptions already interrupt the await; these are different paths. Actual user basket loss has not been demonstrated in this review.
- **Acceptance criteria:** Inspect and dispose of the HTTP response; propagate an explicit failure for non-success status codes; skip client-side deletion and preserve retry context on failure; give checkout a useful error state. Verify successful checkout still clears the basket and failed responses retain it.
- **Dependencies/cautions:** Coordinate with ENG-02 and ENG-12. This cannot detect the API's current false-result/OK behavior. Basket deletion also occurs independently through `OrderStartedIntegrationEvent`; do not assume an HTTP error proves no order committed.

### ENG-02 — Return non-success when CreateOrder returns false

**Category:** Reliability · **Priority:** P1 · **Estimate:** Small

- **Evidence:** `src/Ordering.API/Apis/OrdersApi.cs`, `OrdersApi.CreateOrderAsync`, logs a false command result and still returns `TypedResults.Ok()`.
- **Why it matters:** The current response cannot distinguish that processing failure from success.
- **Acceptance criteria:** Define and document the status/Problem Details contract for false results; return a non-success response on that branch; preserve successful and invalid-request behavior; verify true, false, and invalid request-ID cases. Review resulting OpenAPI changes.
- **Dependencies/cautions:** ENG-01 must consume the new result safely; ENG-03 defines exception and duplicate-request semantics. Avoid exposing command or payment payloads in error details.

### ENG-03 — Propagate command exceptions and define idempotent retries

**Category:** Reliability · **Priority:** P1 · **Estimate:** Medium

- **Evidence:** `src/Ordering.API/Application/Commands/IdentifiedCommandHandler.cs`, `IdentifiedCommandHandler<T,R>.Handle`, records the request ID before dispatch, catches exceptions, and returns default. `src/Ordering.Infrastructure/Idempotency/RequestManager.cs`, `RequestManager.CreateRequestForCommandAsync`, saves the ID. `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`, `TransactionBehavior<TRequest,TResponse>.Handle`, controls the outer transaction.
- **Why it matters:** Swallowing an exception may leave a committable transaction with an ID that suppresses retry. This is conditional on transaction state, not a claim that every failure commits.
- **Acceptance criteria:** Let unexpected exceptions reach transaction rollback and centralized error handling; define outcomes for failed, completed, concurrent duplicate, and uncertain requests; verify same-ID retry after failure and duplicate delivery after success without duplicate orders. Document when callers reuse an ID.
- **Dependencies/cautions:** Coordinate ENG-01/02/12. Preserve intentional duplicate handling in `CreateOrderIdentifiedCommandHandler` in `src/Ordering.API/Application/Commands/CreateOrderCommandHandler.cs`; do not simply remove request tracking or add blind retries after an uncertain commit.

### ENG-04 — Recover failed and stranded integration events durably

**Category:** Reliability · **Priority:** P1 · **Estimate:** Large

- **Evidence:** `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs`, `RetrieveEventLogsPendingToPublishAsync`, selects only `NotPublished` events for one transaction; `MarkEventAsFailedAsync` writes `PublishedFailed` and `MarkEventAsInProgressAsync` writes `InProgress`. Publication follows commit in `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`, `PublishEventsThroughEventBusAsync`. The repository map found no automatic recovery mechanism.
- **Why it matters:** Failed or interrupted publication can strand workflow progress; the source review does not establish a production incident.
- **Acceptance criteria:** Implement durable scanning/claiming, bounded backoff, attempt tracking, stale-claim recovery, and an operator replay path for Ordering and Catalog. Cover stranded `NotPublished`, `PublishedFailed`, and expired `InProgress` records. Fault-injection checks demonstrate recovery across process restart and safe coordination between workers.
- **Dependencies/cautions:** Agree duplicate-handling contracts with ENG-05/06 before enabling replay; coordinate ENG-13 telemetry. A crash after publish but before status persistence can cause duplicates. Broker retries alone do not provide outbox recovery or exactly-once delivery.

### ENG-05 — Replace acknowledgement-on-failure with bounded consumer recovery

**Category:** Reliability · **Priority:** P1 · **Estimate:** Large

- **Evidence:** `src/EventBusRabbitMQ/RabbitMQEventBus.cs`, `RabbitMQEventBus.OnMessageReceived`, catches handler exceptions and subsequently calls `BasicAckAsync`.
- **Why it matters:** A handler failure can be acknowledged without completing its business effect, potentially losing workflow progress.
- **Acceptance criteria:** Acknowledge only successful handling or a confirmed durable transfer to a retry/dead-letter destination. Define transient versus permanent failures, bounded attempts, delayed retry, quarantine, and replay ownership. Verify failure, poison-message, restart, and redelivery cases; audit handlers for duplicate side effects.
- **Dependencies/cautions:** Coordinate ENG-04/06/13. Avoid infinite immediate requeue loops. Preserve event identity and trace context; partial execution across multiple handlers makes idempotency necessary.

### ENG-06 — Define and enforce inventory consistency

**Category:** Reliability · **Priority:** P1 · **Estimate:** Large

- **Evidence:** `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`, `Handle`, checks stock and skips missing products. `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToPaidIntegrationEventHandler.cs`, `Handle`, removes stock only after Paid.
- **Why it matters:** Concurrent orders may validate against the same stock; skipped products may escape rejection. These are inferred consistency risks, not observed overselling incidents.
- **Acceptance criteria:** Record the chosen reservation or alternative consistency contract; explicitly reject missing products; specify concurrency control, payment/cancellation behavior, reservation expiry if applicable, and duplicate-event handling. Demonstrate invariants with concurrent orders, missing items, cancellation, and repeated Paid events.
- **Dependencies/cautions:** Coordinate ENG-04/05 before replaying inventory events. Account for compensation and late messages; do not label the workflow a formal saga without implementing and verifying that contract.

### ENG-07 — Separate Ordering migrations from API startup

**Category:** Reliability · **Priority:** P2 · **Estimate:** Medium

- **Evidence:** `src/Ordering.API/Extensions/Extensions.cs`, `AddApplicationServices`, registers `AddMigration<OrderingContext, OrderingContextSeed>`. `src/Shared/MigrateDbContextExtensions.cs`, `MigrationHostedService<TContext>.StartAsync`, runs migration/seeding at startup. `src/eShop.AppHost/Program.cs`, the order-processor resource's `WaitFor(orderingApi)`, explicitly waits for that migration owner.
- **Why it matters:** Deployment and processor readiness depend on API startup; independent rollout needs a schema-readiness contract.
- **Acceptance criteria:** Provide a separately invocable migration step with clear success/failure exit status and controlled credentials; gate API/processor readiness on compatible schema; verify fresh and existing databases and failed migration behavior; preserve a documented local startup workflow.
- **Dependencies/cautions:** Coordinate ENG-08/14/15. Plan safe schema evolution and concurrent rollout; do not remove the existing startup gate until its replacement is effective. Other services also own migrations and need an explicit follow-up scope decision.

## Security

### ENG-08 — Design secure production health and readiness endpoints

**Category:** Security · **Priority:** P1 · **Estimate:** Medium

- **Evidence:** `src/eShop.ServiceDefaults/Extensions.cs`, `MapDefaultEndpoints`, maps `/health` and `/alive` only in Development; `AddDefaultHealthChecks` adds a live-tagged self check. `src/eShop.AppHost/Program.cs`, `WithHttpHealthCheck`, configures `/health` probes for some services.
- **Why it matters:** Deployment probes need reachable, meaningful endpoints without exposing sensitive diagnostic details. A production outage is not established.
- **Acceptance criteria:** Define liveness/readiness semantics, dependency and schema checks, network/access restrictions, and minimal responses. Verify non-Development endpoints with the intended probe caller, dependency outages, and unauthorized external access; align AppHost probes with the contract.
- **Dependencies/cautions:** Coordinate ENG-07/14. Avoid requiring an interactive user login for platform probes or making liveness depend on every downstream service.

### ENG-09 — Keep payment data out of structured logs

**Category:** Security · **Priority:** P1 · **Estimate:** Medium

- **Evidence:** `src/Ordering.API/Apis/OrdersApi.cs`, `CreateOrderAsync`; `src/Ordering.API/Application/Behaviors/LoggingBehavior.cs`, `Handle`; and `src/Ordering.API/Application/Commands/IdentifiedCommandHandler.cs`, `Handle`, destructure requests/commands. `src/Ordering.API/Application/Commands/CreateOrderCommandHandler.cs`, `Handle`, logs an Order whose domain events can contain `CardSecurityNumber` via `src/Ordering.Domain/Events/OrderStartedDomainEvent.cs`, `OrderStartedDomainEvent`.
- **Why it matters:** Structured serialization may expose payment data depending on the provider. Actual sink exposure has not been confirmed; card-number masking does not protect the separate security-number field.
- **Acceptance criteria:** Replace broad destructuring with approved safe fields across success, failure, and invalid-request paths; review nested domain events and exception logging. Capture structured records through the configured logging pipeline using synthetic sentinels and verify payment fields/values are absent while correlation IDs remain useful.
- **Dependencies/cautions:** No prerequisite; complete before handling real payment data or exporting shared logs. Coordinate ENG-13/15 and inspect sink behavior without copying sensitive logs into tickets.

### ENG-10 — Replace sample signing and authentication assumptions

**Category:** Security · **Priority:** P1 · **Estimate:** Large

- **Evidence:** `src/Identity.API/Program.cs`, `AddDeveloperSigningCredential` and `KeyManagement.Enabled`, uses developer signing credentials and disables key management. `src/eShop.ServiceDefaults/AuthenticationExtensions.cs`, `AddDefaultAuthentication`, disables audience validation and HTTPS metadata requirements.
- **Why it matters:** These source-backed sample settings require a deployment-specific trust and key-lifecycle design; they do not alone establish an exploit.
- **Acceptance criteria:** Select the production identity/signing approach; configure durable protected keys, rotation overlap, issuer/audience boundaries, and HTTPS metadata. Verify token validation across rotation, wrong issuer/audience rejection, and restart; keep development exceptions explicit and environment-scoped.
- **Dependencies/cautions:** Coordinate ENG-14/15. Verify client and service compatibility before enforcing audiences; do not silently reuse developer keys in a deployed environment.

## Testing

### ENG-11 — Cover local HTTPS Playwright readiness in CI

**Category:** Testing · **Priority:** P2 · **Estimate:** Medium

- **Evidence:** `playwright.config.ts`, `webServer.ignoreHTTPSErrors` and `webServer.timeout`, scopes the certificate exception to readiness and sets a three-minute local timeout. `.github/workflows/playwright.yml`, `ESHOP_USE_HTTP_ENDPOINTS`, selects HTTP in CI. `docs/development-baseline.md`, “Interpretation and remaining limitation,” records that gap.
- **Why it matters:** Current CI success does not validate the local HTTPS readiness behavior that previously failed.
- **Acceptance criteria:** Add a dedicated CI scenario with the HTTP override disabled that exercises the HTTPS readiness/redirect path and the local timeout branch; verify the scenario actually reaches HTTPS and can detect regression of the readiness certificate handling. Preserve the existing HTTP job and collect sanitized startup diagnostics.
- **Dependencies/cautions:** Account for `CI` selecting five minutes instead of three. Keep `ignoreHTTPSErrors` scoped to readiness; do not globally weaken browser or application TLS checks. Use isolated test credentials and infrastructure.

### ENG-12 — Add checkout failure and basket-retention regression coverage

**Category:** Testing · **Priority:** P1 · **Estimate:** Medium

- **Evidence:** `src/WebApp/Services/OrderingService.cs`, `CreateOrder`; `src/WebApp/Services/BasketState.cs`, `CheckoutAsync`; and `src/Ordering.API/Apis/OrdersApi.cs`, `CreateOrderAsync`, expose the error paths identified in ENG-01/02. `docs/development-baseline.md`, “Verified successful outcomes,” records passing suites but does not establish coverage of these branches.
- **Why it matters:** Successful checkout tests cannot establish correct behavior for HTTP failures, false command results, or retry ambiguity.
- **Acceptance criteria:** Add deterministic response-level cases for 4xx, 5xx, transport failure, and success; verify no client deletion on failure and deletion on success. Exercise false API command results and a visible recoverable checkout error. Assert same-ID retry behavior against the contract from ENG-03 when implemented.
- **Dependencies/cautions:** Develop core regression cases with ENG-01/02; extend for ENG-03 afterward. Distinguish client-side retention from independent event-driven deletion; avoid sleep-based timing and tests that merely mirror implementation details.

## Observability

### ENG-13 — Detect failed events and stuck orders

**Category:** Observability · **Priority:** P2 · **Estimate:** Medium

- **Evidence:** `src/eShop.ServiceDefaults/Extensions.cs`, `ConfigureOpenTelemetry`, provides telemetry plumbing. `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs`, `UpdateEventStatus`, records event state; `src/OrderProcessor/Services/GracePeriodOrdersRepository.cs`, `GetConfirmedGracePeriodOrdersAsync`, queries aged Submitted orders. `src/EventBusRabbitMQ/RabbitMQEventBus.cs`, `OnMessageReceived`, handles consumer errors.
- **Why it matters:** Existing telemetry hooks do not by themselves establish actionable detection of stranded workflow state.
- **Acceptance criteria:** Expose failure counts, oldest pending-event age, stale `InProgress` counts, consumer retry/dead-letter counts, and order-state age. Define thresholds and owners; provide a dashboard and triage/replay runbook. Demonstrate alerts using controlled failures and correlate request, event, and order identifiers without payment payloads.
- **Dependencies/cautions:** Establish ENG-09 logging boundaries first. Coordinate metric semantics with ENG-04/05; keep high-cardinality IDs in traces/logs rather than metric labels. State-age thresholds must reflect expected business delays.

## Deployment/AWS

### ENG-14 — Record an AWS target-architecture decision

**Category:** Deployment/AWS · **Priority:** P2 · **Estimate:** Medium

- **Evidence:** `src/eShop.AppHost/Program.cs`, `AddAzureContainerAppEnvironment`, declares an Azure deployment environment alongside PostgreSQL, Redis, RabbitMQ, and application resources. `src/eShop.AppHost/Extensions.cs`, `ConfigureMobileBffRoutes`, defines YARP routes. `docs/repository-map.md`, “Aspire resources and startup,” documents resource and startup dependencies.
- **Why it matters:** An AWS target needs explicit placement and operational decisions; the current Azure declaration is not evidence of a validated AWS deployment.
- **Acceptance criteria:** Create an ADR comparing candidate compute approaches and mapping every runtime component, four logical databases, messaging, cache, ingress/BFF, identity, secrets, telemetry, and migration execution. Record networking, persistence/backup, recovery objectives, cost assumptions, rollout/rollback, open questions, and a bounded validation plan.
- **Dependencies/cautions:** Reference ENG-04 through ENG-10 and ENG-15 as constraints. This issue authorizes a design artifact, not cloud provisioning or a claim that an AWS service is a drop-in replacement. Validate provider capabilities separately before implementation.

## Documentation

### ENG-15 — Document configuration, secrets ownership, and rotation

**Category:** Documentation · **Priority:** P2 · **Estimate:** Small

- **Evidence:** `playwright.config.ts`, `dotenv.config`, loads the ignored root `.env`; `e2e/login.setup.ts`, `process.env.USERNAME1` and `process.env.PASSWORD`, requires login configuration. `src/eShop.ServiceDefaults/AuthenticationExtensions.cs`, `AddDefaultAuthentication`, consumes identity configuration. `docs/development-environment.md`, “Local configuration,” explains local handling but not a complete deployment ownership/rotation contract.
- **Why it matters:** Local setup guidance alone does not assign responsibility for deployed credentials, signing keys, configuration changes, and revocation.
- **Acceptance criteria:** Publish a value-free inventory of configuration names, purpose, consuming symbols, environment, source of truth, owner role, and sensitivity. Define provisioning/access, rotation/revocation, restart requirements, emergency recovery, and verification for identity keys, database/broker credentials, CI test accounts, and dashboard access. Distinguish generated local values from managed deployment secrets.
- **Dependencies/cautions:** Start from source; resolve production storage choices with ENG-10/14. Include no secret values or authenticated links; retain `.env` ignore guidance. Record unresolved ownership rather than inventing an assigned person.

## Recommended first implementation issue

Start with **ENG-01**: it is a small, source-confirmed HTTP boundary correction with a clear basket-retention check. Include focused regression coverage in the change; ENG-12 tracks the wider suite. Follow promptly with ENG-02 because an incorrectly successful API response defeats client-side status checking. ENG-09 remains a prerequisite before any real payment data or shared log export.

## Defer for now

- AWS provisioning, infrastructure migration, and deployment automation until ENG-14 records the target and its prerequisites.
- Broad migration extraction until ENG-07 has an agreed schema-readiness contract; do not remove startup dependencies speculatively.
- Wholesale saga-framework adoption or broker replacement; first define the delivery and inventory contracts in ENG-04/05/06.
- Blanket warning suppression, including `ASPIRE010`; separately evaluate `AspireUseCliBundle` as the baseline advises.
- Treating regenerated Catalog OpenAPI files as planned API changes without inspecting their diffs, or revisiting resolved editor/setup issues without new evidence.

These deferrals do not mean the sample is ready for production. Reliability recovery and security prerequisites remain required before deployment with real workloads.

## Week 1 sequence

1. **ENG-01 — Basket retention:** implement response checking and focused failure/success regression coverage.
2. **ENG-02 — Honest CreateOrder results:** return non-success for false results and verify the API/client contract together.
3. **ENG-09 — Safe structured logging:** remove payment-bearing destructuring and verify synthetic payment sentinels are absent from structured output.

This is a proposed sequence, not a delivery-time guarantee. ENG-12 tracks broader checkout coverage and ENG-03 follows to establish safe retries; neither is implied complete by these three changes.
