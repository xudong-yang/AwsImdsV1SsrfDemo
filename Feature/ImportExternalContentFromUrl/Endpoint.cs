using FastEndpoints;

namespace SsrfDemo.Feature.ImportExternalContentFromUrl;

public class Endpoint : Endpoint<Request>
{
    private readonly HttpClient _httpClient;

    public Endpoint(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    public override void Configure()
    {
        Post("/import-external-content-from-url");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var command = new Command(req.Url, _httpClient);
        var response = await command.ExecuteAsync(ct);

        var hasError = response.Error != null;

        if (hasError)
        {
            await Send.ResponseAsync(response, StatusCodes.Status400BadRequest, ct);
            return;
        }

        await Send.OkAsync(response, ct);
    }
}