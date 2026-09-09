# AI Comparison 01 — eShop Architecture Mapping

## Objective

Compare OpenAI Codex using GPT-6 Astra with GitHub Copilot Local/Ask using GPT-5.6 Luna on the same read-only eShop architecture-mapping task. Evaluate how well each explains the system, grounds claims in source, and identifies risks useful to an engineer joining the project.

## Controls

- Both received equivalent prompts.
- Both inspected the same clean repository baseline.
- Neither was given the other review.
- Neither was allowed to modify files or run the application.
- Human verification was performed afterward against source.

## Comparison

Scores are qualitative judgments based on the findings below, on a 1–5 scale where 5 is strongest. For human correction required, **5 means very little correction; 1 means extensive correction**. These scores describe this experiment, not general model performance. No elapsed-time comparison is reported.

| Criterion | Codex / GPT-6 Astra | Copilot / GPT-5.6 Luna | Evidence |
| --- | --- | --- | --- |
| Architecture accuracy | 5 | 4 | Codex traced the architecture more precisely. Luna independently confirmed the topology but called the workflow a saga without verifying a formal saga implementation. |
| Repository/file grounding | 5 | 3 | Codex supplied precise paths and symbols. Luna listed repeated filenames such as `Program.cs` and `Extensions.cs` without qualifying project paths. |
| Component completeness | 5 | 4 | Codex more clearly classified applications, resources, and supporting libraries. Luna's classification and counts of AppHost-managed applications were loose. |
| Risk discovery | 5 | 4 | Codex found subtle application risks; both identified message-recovery risk. Luna contributed useful concerns about developer signing credentials and sensitive payment data that Codex emphasized less strongly. |
| Distinction between fact and inference | 5 | 3 | Codex more clearly separated source observations from consequences and investigation items. Luna's unverified saga label blurred that distinction. |
| Usefulness to a new engineer | 5 | 4 | Codex's source tracing and ten-file reading guide offered a stronger starting point. Luna provided a useful independent topology review and production concerns. |
| Human correction required | 4 | 3 | Codex needed some clarification and stronger emphasis on signing credentials and payment-data handling. Luna also needed component classification, saga terminology, and file references corrected or qualified. |

Codex was stronger overall in repository archaeology, precise paths and symbols, component classification, subtle application risks, and identifying the ten files to read. Luna added value as an independent reviewer, particularly by challenging production assumptions around identity and payment data.

## Human verification

Post-review source inspection confirmed the following. Consequences remain qualified where source alone cannot establish runtime behavior.

| Verified finding | Repository evidence |
| --- | --- |
| AppHost declares one PostgreSQL resource with four logical databases, plus Redis, RabbitMQ, a YARP mobile BFF, and explicit startup dependencies. | `src/eShop.AppHost/Program.cs`; BFF routes in `src/eShop.AppHost/Extensions.cs`. |
| WebApp uses HTTP for Catalog and Ordering, and gRPC for Basket. | Client registrations in `src/WebApp/Extensions/Extensions.cs`. |
| Transaction commit precedes RabbitMQ publication. | `TransactionBehavior<TRequest,TResponse>.Handle` in `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`; `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`. |
| Failed events become `PublishedFailed`; no automatic recovery mechanism was found in the source review. | `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs`, `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`, and `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs`. This is a search finding, not proof that recovery is impossible in every deployment. |
| WebApp does not inspect unsuccessful CreateOrder HTTP responses before deleting the basket. | `OrderingService.CreateOrder` in `src/WebApp/Services/OrderingService.cs` and `BasketState.CheckoutAsync` in `src/WebApp/Services/BasketState.cs`. A returned error response can reach deletion; a transport exception interrupts the await. |
| `CreateOrderAsync` returns OK even when the command result is false. | `src/Ordering.API/Apis/OrdersApi.cs`. |
| `IdentifiedCommandHandler` catches inner-command exceptions and returns default. | `src/Ordering.API/Application/Commands/IdentifiedCommandHandler.cs`. |
| Structured command/Order logging may expose `CardSecurityNumber`, depending on serialization and the logging provider. | `src/Ordering.API/Application/Behaviors/LoggingBehavior.cs`, `src/Ordering.API/Application/Commands/CreateOrderCommandHandler.cs`, and `src/Ordering.Domain/Events/OrderStartedDomainEvent.cs`. Masking the card number does not redact this separate field. |
| Shared health endpoints are Development-only, while AppHost configures `/health` probes for some services. | `MapDefaultEndpoints` in `src/eShop.ServiceDefaults/Extensions.cs`; `src/eShop.AppHost/Program.cs`. |

The combined architecture analysis and qualified risk register are recorded in [the repository map](repository-map.md). No tests were run specifically for this documentation-only change; the previously established baseline was 122 .NET tests and 4 Playwright tests passing.

## Final decision

- Prefer Codex for initial large-repository mapping and precise source tracing.
- Use Copilot/Luna as an independent reviewer and production-risk challenger.
- Treat neither output as authoritative until important claims are checked in source.

The combined workflow produced a better architecture map than either review alone: Codex supplied the stronger structural and behavioral trace, Luna added independent confirmation and production concerns, and human source verification corrected terminology and qualified risk claims.

## Interview story

- **Situation:** I needed a reliable architecture map of the distributed eShop sample and wanted to compare two AI assistants on the same task.
- **Task:** Evaluate their accuracy, source grounding, completeness, and risk discovery under equivalent read-only conditions.
- **Action:** I gave Codex and Copilot/Luna equivalent prompts, kept their reviews independent, compared their findings, and checked important claims against source, including checkout transactions, event recovery, logging, and health endpoints.
- **Result:** Codex provided the stronger initial map and source trace; Luna added useful production-risk challenges. Combining both with human verification produced a repository map with nine manually verified findings and a documented rule for selecting and checking AI coding tools.
