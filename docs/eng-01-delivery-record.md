# ENG-01 delivery record: retain the basket on unsuccessful Ordering responses

## 1. Problem and customer impact

`OrderingService.CreateOrder` previously returned `HttpClient.SendAsync` without checking the HTTP status. `BasketState.CheckoutAsync` then deleted the basket after the task completed, including when Ordering returned an error response. This created a risk of losing a customer's basket after unsuccessful checkout; the source finding does not establish actual customer incidents.

## 2. Decision and implementation

In `src/WebApp/Services/OrderingService.cs`, `CreateOrder` now awaits the response and calls `EnsureSuccessStatusCode()`. Request and response are safely disposed through `using` declarations, including when status validation throws. The public `Task` contract remains unchanged.

`BasketState.CheckoutAsync` in `src/WebApp/Services/BasketState.cs` already awaited Ordering before calling `DeleteBasketAsync`. Propagating unsuccessful responses therefore prevents that client-side deletion without changing BasketState production code.

The dedicated `tests/WebApp.UnitTests` project uses controllable HTTP handlers and test-local dependencies. `OrderingServiceTests` covers successful responses, 400/500 errors, transport and cancellation propagation, and POST/request-ID behavior. `BasketStateTests` verifies no underlying gRPC deletion and unchanged basket contents on failure, with successful deletion as a control.

## 3. Scope deliberately excluded

- Ordering can still return 200 when its internal command result is false; this is ENG-02.
- Retry policy and retry/idempotency design were unchanged.
- UI error presentation was unchanged.
- Event-driven basket deletion was unchanged. Retaining the basket on the client-side failure path does not guarantee retention if an order committed and an integration event independently clears it.

## 4. Validation

Recorded delivery results:

| Command | Result |
| --- | --- |
| `dotnet test .\tests\WebApp.UnitTests\WebApp.UnitTests.csproj` | 10 passed. |
| `dotnet test --solution eShop.Web.slnf` | Passed. |
| `npm run test:e2e` | 4 passed in about 1.6 minutes. |

The full-suite and E2E results are supplied delivery evidence. No tests were rerun to create this record.

## 5. AI-assisted engineering workflow

Codex created the implementation plan and initial change/tests. VS Code Chat (Auto) independently reviewed the uncommitted change. Its useful finding was that the original plan proposed extending `Application.UnitTests`, whereas implementation used a dedicated `WebApp.UnitTests` project. The [plan's implementation outcome](eng-01-implementation-plan.md) was updated to explain that deliberate choice while preserving the original plan.

Suggested broader contract, caller-driven cancellation, and 3xx coverage was consciously not added because it would not materially improve this narrow issue. The delivered `TaskCanceledExceptionPropagatesUnchanged` test covers direct exception propagation, verifying that the exact exception instance propagates unchanged; it does not cover broader caller-driven cancellation scenarios. Review suggestions were evaluated against source, existing coverage, and issue boundaries rather than accepted automatically.

## 6. Interview story

**Situation:** A .NET storefront could clear a basket after an unsuccessful Ordering response. **Task:** Correct the client boundary without expanding into API or distributed-workflow redesign. **Action:** I used AI-assisted planning and implementation, independent review, and focused tests against HTTP and gRPC boundaries, then reconciled the plan with the delivered project structure. **Result:** Failure-path basket retention was verified, and focused tests, the Web solution test suite, and E2E validation passed. For a senior .NET/AWS delivery discussion, this demonstrates bounded changes and evidence-based delivery; it does not claim an AWS deployment.

## 7. Next candidate

**ENG-02:** Return non-success when order command processing returns false; this is a separate API-contract change because it changes server response semantics rather than client handling of an already unsuccessful response.
