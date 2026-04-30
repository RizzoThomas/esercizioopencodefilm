using FilmAPI.Services;

namespace FilmAPI.Endpoints;

public static class PagamentoEndpoints
{
    public static void MapPagamentoEndpoints(this WebApplication app)
    {
        app.MapPost("/payments/stripe/webhook", async (HttpContext context, IPagamentoService service) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var payload = await reader.ReadToEndAsync();
            var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();

            try
            {
                await service.HandleStripeWebhookAsync(payload, signature);
                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).AllowAnonymous();
    }
}
