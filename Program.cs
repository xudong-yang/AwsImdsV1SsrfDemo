using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseFastEndpoints();

app.Run();