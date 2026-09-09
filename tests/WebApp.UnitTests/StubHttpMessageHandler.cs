namespace eShop.WebApp.UnitTests;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
}
