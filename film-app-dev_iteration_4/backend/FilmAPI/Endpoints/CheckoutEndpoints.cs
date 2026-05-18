// ============================================================================
// CheckoutEndpoints.cs — ENDPOINT DEL FLUSSO DI ACQUISTO
// ============================================================================
// Definisce TUTTI gli endpoint REST relativi al checkout.
// Pattern: Minimal API (ASP.NET Core 8) con raggruppamento sotto /checkout
//
// FLUSSO COMPLETO:
//   1. GET /checkout/shows/{showId}/seat-map → vedi piantina posti
//   2. POST /checkout/holds → tieni posti (con TTL)
//   3. POST /checkout/holds/{holdToken}/refresh → estendi TTL
//   4. DELETE /checkout/holds/{holdToken} → rilascia posti
//   5. POST /checkout/orders → crea ordine Pending
//   6. POST /checkout/orders/{orderId}/pay → paga (credito)
//   7. POST /checkout/orders/{orderId}/stripe-checkout-session → Stripe
//   8. GET /checkout/orders/{orderId}/pdf → download biglietti PDF
// ============================================================================

using FilmAPI.DTO;
using FilmAPI.Services;
using System.Security.Claims;

namespace FilmAPI.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        // Raggruppa tutti gli endpoint sotto /checkout
        // Così le route sono: /checkout/shows/..., /checkout/holds/..., etc.
        var checkoutGroup = app.MapGroup("/checkout");

        // ====================================================================
        // GET /checkout/shows/{showId}/seat-map
        // Restituisce la piantina dei posti per uno show con i loro stati
        // (Available, HeldByOther, HeldByMe, Sold)
        // Auth: Authenticated (serve user ID per determinare HeldByMe)
        // ====================================================================
        checkoutGroup.MapGet("/shows/{showId}/seat-map", async (
            int showId,
            ClaimsPrincipal user,       // ← ASP.NET Core inietta automaticamente l'utente JWT
            ISeatHoldService service) =>
        {
            // Legge l'user ID dal claim "sub" del JWT
            // ClaimsPrincipal rappresenta l'utente autenticato
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
        }).RequireAuthorization("Authenticated");  // ← GATE: solo utenti loggati

        // ====================================================================
        // POST /checkout/holds
        // Crea un hold temporaneo su uno o più posti
        // Body: { showId, salaPostoIds[] }
        // Risponde 409 Conflict se qualche posto non è disponibile
        // ====================================================================
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
                    return Results.Conflict(result);  // 409: posti non disponibili
                }
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);  // 400: payload invalido
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(ex.Message);    // 404: show/sala non trovati
            }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // POST /checkout/holds/{holdToken}/refresh
        // Estende la scadenza dell'hold (keep-alive)
        // Il frontend chiama questo endpoint ogni 60 secondi
        // ====================================================================
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

        // ====================================================================
        // DELETE /checkout/holds/{holdToken}
        // Rilascia esplicitamente un hold (utente deseleziona posti)
        // ====================================================================
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

        // ====================================================================
        // POST /checkout/orders
        // Crea un ordine Pending a partire da un hold valido
        // Body: { holdToken, idempotencyKey? }
        // IdempotencyKey: se passata, la stessa chiave restituisce lo stesso ordine
        // ====================================================================
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
                return Results.Created($"/checkout/orders/{result.Id}", result);  // 201 Created
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);  // 409: hold non più valido
            }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // GET /checkout/orders
        // Lista ordini dell'utente corrente (ownership check!)
        // ====================================================================
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

        // ====================================================================
        // GET /checkout/orders/{orderId}
        // Dettaglio di un ordine (con ownership check: solo il proprietario vede)
        // ====================================================================
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

        // ====================================================================
        // GET /checkout/orders/{orderId}/pdf
        // Scarica il PDF multipagina con tutti i biglietti dell'ordine
        // Ownership check: solo il proprietario dell'ordine può scaricare
        // ====================================================================
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

            // Ownership check implicito: GetOrdineByIdAsync richiede userId
            var order = await checkoutService.GetOrdineByIdAsync(orderId, userId);
            if (order is null)
                return Results.NotFound();

            try
            {
                var ticketDocument = await bigliettoService.GetOrderTicketDocumentAsync(orderId);
                var pdfBytes = pdfService.GenerateOrderTicketsPdf(ticketDocument);
                // Results.File restituisce un file binario con content-type corretto
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

        // ====================================================================
        // POST /checkout/orders/{orderId}/pay
        // Paga un ordine con credito o voucher
        // Idempotency-Key passata nell'header HTTP
        // ====================================================================
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

            // L'Idempotency-Key arriva nell'HEADER HTTP, non nel body
            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

            try
            {
                var result = await service.PayOrdineAsync(userId, orderId, dto, idempotencyKey);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)     { return Results.BadRequest(ex.Message); }
            catch (KeyNotFoundException ex)  { return Results.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // POST /checkout/orders/{orderId}/cancel
        // Annulla un ordine Pending o CheckoutInProgress
        // Rilascia posti e, se presente, ripristina credito riservato
        // ====================================================================
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
            catch (KeyNotFoundException ex)      { return Results.NotFound(ex.Message); }
            catch (InvalidOperationException ex)  { return Results.Conflict(ex.Message); }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // GET /checkout/tickets
        // Lista biglietti dell'utente corrente
        // ====================================================================
        checkoutGroup.MapGet("/tickets", async (
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            var result = await service.GetTicketsByUserAsync(userId);
            return Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // GET /checkout/tickets/{ticketId}
        // Dettaglio biglietto (ownership check)
        // ====================================================================
        checkoutGroup.MapGet("/tickets/{ticketId}", async (
            int ticketId,
            ClaimsPrincipal user,
            ICheckoutService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            var result = await service.GetTicketByIdAsync(ticketId, userId);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // POST /checkout/orders/{orderId}/stripe-checkout-session
        // Crea una sessione Stripe Checkout per pagamento carta/misto
        // Restituisce l'URL a cui reindirizzare l'utente
        // ====================================================================
        checkoutGroup.MapPost("/orders/{orderId}/stripe-checkout-session", async (
            int orderId,
            CreateCheckoutSessionRequestDTO dto,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();

            try
            {
                var result = await service.CreateCheckoutSessionAsync(userId, orderId, dto, idempotencyKey);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)        { return Results.BadRequest(ex.Message); }
            catch (KeyNotFoundException ex)     { return Results.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // GET /checkout/orders/{orderId}/checkout-status
        // Restituisce lo stato corrente della sessione Stripe
        // ====================================================================
        checkoutGroup.MapGet("/orders/{orderId}/checkout-status", async (
            int orderId,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            try
            {
                var result = await service.GetCheckoutStatusAsync(userId, orderId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
        }).RequireAuthorization("Authenticated");

        // ====================================================================
        // POST /checkout/orders/{orderId}/reconcile-checkout-session
        // Riconcilia l'ordine dopo il ritorno da Stripe
        // Verifica lo stato della sessione Stripe e finalizza l'ordine
        // ====================================================================
        checkoutGroup.MapPost("/orders/{orderId}/reconcile-checkout-session", async (
            int orderId,
            ClaimsPrincipal user,
            IPagamentoService service) =>
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Results.Unauthorized();

            try
            {
                var result = await service.ReconcileCheckoutSessionAsync(userId, orderId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
        }).RequireAuthorization("Authenticated");
    }
}
