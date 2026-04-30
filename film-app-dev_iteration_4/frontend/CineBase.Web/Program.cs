var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseStaticFiles();
app.UseDefaultFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
