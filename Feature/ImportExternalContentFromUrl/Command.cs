using FastEndpoints;

namespace SsrfDemo.Feature.ImportExternalContentFromUrl;

public record Command(string Url, HttpClient HttpClient) : ICommand<Response>;

public class CommandHandler() : ICommandHandler<Command, Response>
{
    public async Task<Response> ExecuteAsync(Command command, CancellationToken ct)
    {
        var response = await command.HttpClient.GetAsync(command.Url, ct);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrEmpty(contentType) && IsSupportedContentType(contentType))
        {
            return new Response() { Message = "external content imported" };
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return new Response() { Error = $"unable to import. File content: {body}" };
        }
    }

    private static bool IsSupportedContentType(string contentType)
    {
        return contentType.Contains("pdf");
    }
}