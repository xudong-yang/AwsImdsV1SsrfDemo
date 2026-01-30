using FastEndpoints;

namespace SsrfDemo.Feature.HelloWorld;

public class Endpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new Response() { Message = "Hello World!" };
        await Send.OkAsync(response, ct);
    }
}