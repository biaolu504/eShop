using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using eShop.Basket.API.Grpc;
using eShop.WebApp.Services;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using Grpc.Core;
using Microsoft.AspNetCore.Components.Authorization;
using GrpcBasketClient = eShop.Basket.API.Grpc.Basket.BasketClient;

namespace eShop.WebApp.UnitTests;

[TestClass]
public class BasketStateTests
{
    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.OK)]
    public async Task CheckoutDeletesBasketOnlyAfterSuccessfulResponse(HttpStatusCode status)
    {
        var grpc = new RecordingBasketClient();
        var basketService = new BasketService(grpc);
        var orderingCalls = 0;
        using var orderingClient = new HttpClient(new StubHttpMessageHandler(_ =>
        {
            orderingCalls++;
            Assert.AreEqual(0, grpc.DeleteCalls);
            return Task.FromResult(new HttpResponseMessage(status));
        })) { BaseAddress = new Uri("http://ordering.test") };
        using var catalogClient = new HttpClient(new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new CatalogItem(1, "Product", "Description", 10m, "", 1,
                        new CatalogBrand(1, "Brand"), 1, new CatalogItemType(1, "Type"))
                })
            }))) { BaseAddress = new Uri("http://catalog.test") };
        var state = new BasketState(basketService, new CatalogService(catalogClient),
            new OrderingService(orderingClient), new TestAuthenticationStateProvider());
        var checkout = new BasketCheckoutInfo { RequestId = Guid.NewGuid() };

        if (status == HttpStatusCode.OK)
        {
            await state.CheckoutAsync(checkout);
            Assert.AreEqual(1, grpc.DeleteCalls);
            Assert.IsEmpty(await basketService.GetBasketAsync());
        }
        else
        {
            var exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => state.CheckoutAsync(checkout));
            Assert.AreEqual(status, exception.StatusCode);
            Assert.AreEqual(0, grpc.DeleteCalls);
            // Read the backing gRPC basket, not BasketState's cached product collection.
            var retained = await basketService.GetBasketAsync();
            Assert.HasCount(1, retained);
            Assert.AreEqual(new BasketQuantity(1, 2), retained.Single());
        }

        Assert.AreEqual(1, orderingCalls);
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "buyer"), new Claim("name", "Test Buyer")], "Test"))));
    }

    private sealed class RecordingBasketClient : GrpcBasketClient
    {
        private readonly CustomerBasketResponse basket = new()
        {
            Items = { new eShop.Basket.API.Grpc.BasketItem { ProductId = 1, Quantity = 2 } }
        };

        public int DeleteCalls { get; private set; }

        public override AsyncUnaryCall<CustomerBasketResponse> GetBasketAsync(
            GetBasketRequest request, CallOptions options) => Completed(basket.Clone());

        public override AsyncUnaryCall<DeleteBasketResponse> DeleteBasketAsync(
            DeleteBasketRequest request, CallOptions options)
        {
            DeleteCalls++;
            basket.Items.Clear();
            return Completed(new DeleteBasketResponse());
        }

        private static AsyncUnaryCall<T> Completed<T>(T value)
            => new(Task.FromResult(value), Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => { });
    }
}
