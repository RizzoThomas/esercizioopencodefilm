// Avvio del frontend ASP.NET: creo il builder dell'applicazione e preparo il runtime.
var builder = WebApplication.CreateBuilder(args);

// Costruzione dell'istanza applicativa usata per configurare middleware, file statici e route.
var app = builder.Build();

// Esposizione dei file statici del frontend, inclusi HTML, CSS, JS e asset grafici.
app.UseStaticFiles();
app.UseDefaultFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

// Avvio effettivo dell'applicazione e messa in ascolto del server web.
app.Run();
