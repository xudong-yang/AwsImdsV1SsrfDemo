namespace SsrfDemo.Feature.ImportExternalContentFromUrl;

public record Response
{
    public string? Error { get; set; }
    public string? Message { get; set; }
}