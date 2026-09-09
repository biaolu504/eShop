using System.Net;
using System.Net.Http.Json;
using eShop.WebApp.Services;

namespace eShop.WebApp.UnitTests;

[TestClass]
public class OrderingServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow(HttpStatusCode.OK)]
    [DataRow(HttpStatusCode.NoContent)]
    public async Task SuccessfulResponseCompletes(HttpStatusCode status)
    {
        using var client = CreateClient(_ => Task.FromResult(new HttpResponseMessage(status)));

        await new OrderingService(client).CreateOrder(CreateRequest(), Guid.NewGuid());
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task UnsuccessfulResponseThrowsWithStatus(HttpStatusCode status)
    {
        using var client = CreateClient(_ => Task.FromResult(new HttpResponseMessage(status)));

        var exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            new OrderingService(client).CreateOrder(CreateRequest(), Guid.NewGuid()));

        Assert.AreEqual(status, exception.StatusCode);
    }

    [TestMethod]
    public async Task TransportExceptionPropagates()
    {
        var expected = new HttpRequestException("Simulated transport failure");
        using var client = CreateClient(_ => Task.FromException<HttpResponseMessage>(expected));

        var actual = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            new OrderingService(client).CreateOrder(CreateRequest(), Guid.NewGuid()));

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async Task TaskCanceledExceptionPropagatesUnchanged()
    {
        var expected = new TaskCanceledException("Simulated canceled request");
        using var client = CreateClient(_ => Task.FromException<HttpResponseMessage>(expected));

        var actual = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() =>
            new OrderingService(client).CreateOrder(CreateRequest(), Guid.NewGuid()));

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async Task SendsPostWithRequestIdAndJsonBody()
    {
        var requestId = Guid.NewGuid();
        var payload = CreateRequest();
        var calls = 0;
        using var client = CreateClient(async request =>
        {
            calls++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/api/Orders/", request.RequestUri!.AbsolutePath);
            Assert.AreEqual(requestId.ToString(), request.Headers.GetValues("x-requestid").Single());
            Assert.AreEqual("application/json", request.Content!.Headers.ContentType!.MediaType);
            var body = await request.Content.ReadFromJsonAsync<CreateOrderRequest>(TestContext.CancellationToken);
            Assert.IsNotNull(body);
            Assert.AreEqual(payload.UserId, body.UserId);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await new OrderingService(client).CreateOrder(payload, requestId);

        Assert.AreEqual(1, calls);
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
        => new(new StubHttpMessageHandler(send)) { BaseAddress = new Uri("http://ordering.test") };

    private static CreateOrderRequest CreateRequest()
        => new("buyer", "Test Buyer", "City", "Street", "State", "Country", "Zip",
            "", "", DateTime.UnixEpoch, "", 1, "buyer", []);
}
