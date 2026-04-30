using FilmAPI.DTO;
using FilmAPI.Services;
using System.Security.Claims;

namespace FilmAPI.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        var checkoutGroup = app.MapGroup("/checkout");

        checkoutGroup.MapGet("/shows/{showId}/seat-map", async (
            int showId,
            ClaimsPrincipal user,
            ISeatHoldService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var seatMap = await service.GetSeatMapAsync(showId, userId);
                return Results.Ok(seatMap);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/holds", async (
            SeatHoldRequestDTO dto,
            ClaimsPrincipal user,
            ISeatHoldService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.CreateHoldAsync(dto.ShowId, userId, dto.SalaPostoIds);
                if (result.Conflitti.Count > 0)
                {
                    return Results.Conflict(result);
                }
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/holds/{holdToken}/refresh", async (
            string holdToken,
            ClaimsPrincipal user,
            ISeatHoldService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.RefreshHoldAsync(holdToken, userId);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapDelete("/holds/{holdToken}", async (
            string holdToken,
            ClaimsPrincipal user,
            ISeatHoldService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.ReleaseHoldAsync(holdToken, userId);
            return result ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/orders", async (
            CreateOrdineRequestDTO dto,
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.CreateOrdineAsync(userId, dto);
                return Results.Created($"/checkout/orders/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/orders", async (
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.GetOrdiniByUserAsync(userId);
            return Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/orders/{orderId}", async (
            int orderId,
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.GetOrdineByIdAsync(orderId, userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/orders/{orderId}/pdf", async (
            int orderId,
            ClaimsPrincipal user,
            ICheckoutService checkoutService,
            IBigliettoService bigliettoService,
            IPdfService pdfService) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var order = await checkoutService.GetOrdineByIdAsync(orderId, userId);
            if (order is null)
                return Results.NotFound();

            try
            {
                var ticketDocument = await bigliettoService.GetOrderTicketDocumentAsync(orderId);
                var pdfBytes = pdfService.GenerateOrderTicketsPdf(ticketDocument);
                return Results.File(pdfBytes, "application/pdf", $"biglietti-{ticketDocument.CodiceOrdine}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/orders/{orderId}/pay", async (
            int orderId,
            PayOrdineRequestDTO dto,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

            try
            {
                var result = await service.PayOrdineAsync(userId, orderId, dto, idempotencyKey);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/orders/{orderId}/cancel", async (
            int orderId,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.CancelPendingOrdineAsync(userId, orderId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/tickets", async (
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.GetTicketsByUserAsync(userId);
            return Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/tickets/{ticketId}", async (
            int ticketId,
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var result = await service.GetTicketByIdAsync(ticketId, userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/orders/{orderId}/stripe-checkout-session", async (
            int orderId,
            CreateCheckoutSessionRequestDTO dto,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

            try
            {
                var result = await service.CreateCheckoutSessionAsync(userId, orderId, dto, idempotencyKey);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapGet("/orders/{orderId}/checkout-status", async (
            int orderId,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.GetCheckoutStatusAsync(userId, orderId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");

        checkoutGroup.MapPost("/orders/{orderId}/reconcile-checkout-session", async (
            int orderId,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0)
                return Results.Unauthorized();

            try
            {
                var result = await service.ReconcileCheckoutSessionAsync(userId, orderId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }).RequireAuthorization("Authenticated");
    }
}
