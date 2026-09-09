# ENG-01 implementation plan: retain the basket on Ordering HTTP failure

## Implementation outcome

The original plan below was prepared before implementation and is preserved as written.
- The implementation deliberately uses a dedicated `tests/WebApp.UnitTests` project instead of the originally proposed `Application.UnitTests` extension, keeping WebApp-specific tests isolated.
- `OrderingService.CreateOrder` now awaits the HTTP response and calls `EnsureSuccessStatusCode()`, with request and response disposal handled safely by `using` declarations.
- `tests/WebApp.UnitTests` contains direct `OrderingService` tests plus basket-retention coverage.
- Focused validation with `dotnet test .\tests\WebApp.UnitTests\WebApp.UnitTests.csproj` passed **10 tests**.
- The Ordering API returning 200 when its command result is false remains deliberately out of scope.

## Original plan

Status: proposed plan only. No implementation or test execution was performed to produce this document.

The smallest safe fix is to make `OrderingService.CreateOrder` await the response and call `EnsureSuccessStatusCode()`. Keep its public `Task` signature. `BasketState.CheckoutAsync` already awaits it before deleting the basket, so a failed task prevents that deletion.

## 1. Exact proposed changes

| File | Symbol/change |
| --- | --- |
| `src/WebApp/Services/OrderingService.cs` | Change `OrderingService.CreateOrder` to `async Task`; dispose the request and response with `using`; await `SendAsync`; call `EnsureSuccessStatusCode()`. |
| `tests/Application.UnitTests/Application.UnitTests.csproj` | Add a project reference to `src/WebApp/WebApp.csproj`. |
| `tests/Application.UnitTests/OrderingServiceTests.cs` | Proposed new tests for HTTP outcomes and request preservation. |
| `tests/Application.UnitTests/BasketStateTests.cs` | Proposed new tests exercising the real `BasketState.CheckoutAsync` and `OrderingService.CreateOrder` together. |

No production change is necessary in `src/WebApp/Services/BasketState.cs`: its existing sequential awaits provide the required behavior.

## 2. Smallest safe client behavior

| Outcome | Behavior |
| --- | --- |
| 4xx | Throw `HttpRequestException` through `EnsureSuccessStatusCode`; do not reach client-side basket deletion. |
| 5xx | Same behavior after the configured HTTP pipeline returns its final response. |
| Transport failure | Preserve the existing exception propagation; do not delete the basket. |
| Timeout/cancellation | Propagate cancellation/failure; do not delete the basket. |
| 2xx | Complete successfully and allow the existing deletion path. |

Dispose the request only after the awaited send completes, and dispose the response even when status validation throws. Do not read or expose error bodies containing potentially sensitive information.

## 3. Public signature

Keep `Task CreateOrder(CreateOrderRequest request, Guid requestId)`.

Adding `async` changes implementation, not the public contract. A boolean, response object, new result type, or service interface is unnecessary for this issue.

## 4. Existing test project and seam

Use `tests/Application.UnitTests/Application.UnitTests.csproj`. It already uses MSTest, NSubstitute, and tests application services—for example, `tests/Application.UnitTests/WebhookClientTests.cs`. It currently lacks a WebApp reference.

Use an in-memory `HttpMessageHandler` with a real `HttpClient` and `OrderingService`. This avoids Docker and exercises the actual missing HTTP status check.

For basket-retention tests, instantiate the real `BasketState` with:

- Real `OrderingService` backed by the controllable HTTP handler.
- Real `BasketService` backed by a fake gRPC `CallInvoker` or substituted generated `Basket.BasketClient`.
- Real `CatalogService` backed by an HTTP handler returning matching product details.
- A test `AuthenticationStateProvider` supplying the claims consumed by `GetBuyerIdAsync` and `GetUserNameAsync` in `src/WebApp/Extensions/Extensions.cs`.

`BasketService` and its methods are concrete/non-virtual; fake its underlying gRPC boundary rather than introducing an interface solely for tests.

## 5. Specific tests

| Case | Required assertions |
| --- | --- |
| Representative 4xx: 400, 401, 403, 409 | `CreateOrder` throws `HttpRequestException` with the returned status. |
| Representative 5xx: 500, 503 | Same status-bearing failure. |
| Transport exception | Exception propagates without conversion to success. |
| Canceled send | Cancellation propagates. |
| 200 and 204 | `CreateOrder` completes without requiring response content. |
| Request contract | POST, existing route, JSON payload, and supplied `x-requestid` remain intact. |
| Basket checkout failure matrix | Awaiting `CheckoutAsync` fails; basket delete and update calls are zero; seeded product IDs and quantities remain unchanged. |
| Basket checkout success | Delete is called exactly once after successful Ordering completion; the fake backing basket becomes empty. |

For retention proof, inspect the fake backing basket or read through `BasketService`; **do not rely solely on `BasketState.GetBasketItemsAsync`**, because `_cachedBasket` could conceal a deletion.

## 6. Risks, edge cases, and scope boundaries

- **Assumption:** ENG-01 means preventing WebApp's explicit deletion after unsuccessful responses; it does not guarantee distributed basket retention.
- `OrderStartedIntegrationEventHandler.Handle` in `src/Basket.API/IntegrationEvents/EventHandling/OrderStartedIntegrationEventHandler.cs` independently deletes the basket. An error or lost response does not prove the order failed to commit.
- The Ordering API's false-command-result/200 defect remains out of scope. Such responses will still permit deletion.
- `Checkout.HandleValidSubmitAsync` in `src/WebApp/Components/Pages/Checkout/Checkout.razor` has no local exception handler. Failure prevents navigation to orders, but this fix alone does not provide a friendly inline error. **Assumption:** dedicated error presentation is separate work.
- Preserve existing request-ID behavior; do not add retries or claim retries are safe. Shared HTTP resilience is configured in `src/eShop.ServiceDefaults/Extensions.cs`, `AddServiceDefaults`.
- Do not change route casing, basket caching, API contracts, authentication, or event handling.
- Browser request interception is not the preferred seam: the Ordering call originates in WebApp, not directly in the browser.

## 7. Implementation and validation sequence

1. Add the WebApp test reference and minimal boundary fakes.
2. Add failure and success tests, including backing-basket assertions.
3. When implementation is authorized, run those tests against the old code to establish that the HTTP-failure cases expose the defect.
4. Make the single-method production change in `OrderingService.CreateOrder`.
5. Run the focused application tests; confirm failures retain the basket and success still deletes it.
6. Run `dotnet test --solution eShop.Web.slnf` with Docker available for functional tests.
7. Run the existing `npm run test:e2e` happy-path regression with the documented local configuration.
8. Inspect the diff and generated Catalog OpenAPI changes; preserve only intended changes.

These are future implementation and validation steps, not a record of completed work.
