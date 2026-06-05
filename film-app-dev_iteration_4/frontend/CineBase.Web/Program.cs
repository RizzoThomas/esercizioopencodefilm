var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var backendUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL") ?? "http://localhost:5000";

app.Use(async (ctx, next) =>
{
    var body = ctx.Response.Body;
    using var ms = new MemoryStream();
    ctx.Response.Body = ms;
    await next();
    ctx.Response.Body = body;
    if (ctx.Response.ContentType?.Contains("text/html") == true)
    {
        ms.Seek(0, SeekOrigin.Begin);
        var html = await new StreamReader(ms).ReadToEndAsync();
        ctx.Response.Headers.ContentLength = null;
        await ctx.Response.WriteAsync(
            html.Replace("</head>", $"<script>window.API_BASE_URL='{backendUrl.TrimEnd('/')}';</script></head>")
        );
    }
    else
    {
        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(ctx.Response.Body);
    }
});

app.UseStaticFiles();
app.UseDefaultFiles();
app.MapGet("/", () => Results.Redirect("/index.html"));
app.Run();
