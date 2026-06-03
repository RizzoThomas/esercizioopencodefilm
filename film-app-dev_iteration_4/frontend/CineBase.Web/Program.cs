// Avvio del frontend ASP.NET: creo il builder dell'applicazione e preparo il runtime.
var builder = WebApplication.CreateBuilder(args);

// Costruzione dell'istanza applicativa usata per configurare middleware, file statici e route.
var app = builder.Build();

// === MIDDLEWARE: Inject API_BASE_URL nelle risposte HTML ===
// Permette al frontend di chiamare il backend (locale: localhost:5000, ACA: URL pubblico/privato)
var backendApiUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL") ?? "http://localhost:5000";
app.Use(async (context, next) =>
{
    // Intercetta solo risposte HTML
    if (context.Response.ContentType != null &&
        context.Response.ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next();

        memStream.Seek(0, SeekOrigin.Begin);
        var html = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Seek(0, SeekOrigin.Begin);

        // Inietta API_BASE_URL prima della chiusura di </head>
        var scriptTag = $"<script>window.API_BASE_URL='{backendApiUrl.TrimEnd('/')}';</script>";
        if (html.Contains("</head>"))
        {
            html = html.Replace("</head>", scriptTag + "</head>");
        }

        context.Response.Body = originalBody;
        await context.Response.WriteAsync(html);
    }
    else
    {
        await next();
    }
});

// Esposizione dei file statici del frontend, inclusi HTML, CSS, JS e asset grafici.
app.UseStaticFiles();
app.UseDefaultFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

// Avvio effettivo dell'applicazione e messa in ascolto del server web.
app.Run();
