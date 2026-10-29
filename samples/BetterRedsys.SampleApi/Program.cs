using BetterRedsys.Configuration;
using BetterRedsys.Endpoints;
using BetterRedsys.Models;
using BetterRedsys.SampleApi;
using BetterRedsys.Services;
using BetterRedsys.Simulator;
using BetterRedsys.Testing;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración moderna con IBetterRedsysBuilder, ValidateOnStart y métodos fluidos
var redsysBuilder = builder.Services.AddBetterRedsys(options =>
{
    options.UseTest(
        fuc: builder.Configuration["Redsys:Fuc"] ?? RedsysTestCredentials.Fuc,
        terminal: builder.Configuration["Redsys:Terminal"] ?? RedsysTestCredentials.Terminal,
        secretKey: builder.Configuration["Redsys:SecretKey"] ?? RedsysTestCredentials.SecretKey)
    .WithEuros()
    .WithWebhooks(
        notificationUrl: "/api/pagos/webhook",
        okUrl: "/pedido-exitoso",
        koUrl: "/pedido-cancelado");
})
.AddHealthCheck(); // Registra el HealthCheck mediante IBetterRedsysBuilder

// 2. Activa el simulador desacoplado en entorno de desarrollo local
if (builder.Environment.IsDevelopment())
{
    redsysBuilder.AddSimulator();
}

var app = builder.Build();

app.MapHealthChecks("/health");

// 4. Middleware interactivo Simulator (activo en desarrollo)
if (app.Environment.IsDevelopment())
{
    app.UseRedsysSimulator();
}

// 3. Endpoint para Front-ends desacoplados (React, Next.js, Vue, Blazor, Mobile Apps)
// Devuelve un DTO con Ds_MerchantParameters y Ds_Signature para montar el formulario en el cliente
app.MapPost("/api/checkout/crear-pago", (IRedsysClient redsysClient, CheckoutRequest request) =>
{
    var paymentData = redsysClient
        .CreatePayment(request.Order, request.Amount)
        .WithDescription(request.Description ?? "Compra en Tienda Online")
        .WithAllPaymentMethods() // Muestra todos los métodos activos en el terminal
        .WithSuccessUrl($"/pedido-exitoso?order={request.Order}")
        .WithCancelUrl($"/pedido-cancelado?order={request.Order}")
        .ToFormData();

    return Results.Ok(paymentData);
});

// 4. Endpoint para suscripción con almacenamiento de tarjeta (Card-On-File / Tokenización)
app.MapPost("/api/checkout/iniciar-suscripcion", (IRedsysClient redsysClient, SubscriptionRequest request) =>
{
    var paymentData = redsysClient
        .CreatePayment(request.Order, request.FirstMonthAmount)
        .WithDescription($"Alta de suscripción: {request.PlanName}")
        .WithInitialCardOnFile() // Solicita tokenización COF recurrente
        .WithSuccessUrl($"/pedido-exitoso?order={request.Order}")
        .WithCancelUrl($"/pedido-cancelado?order={request.Order}")
        .ToFormData();

    return Results.Ok(paymentData);
});

// 5. Endpoint para cobrar cuota recurrente usando token guardado (Server-to-Server REST)
app.MapPost("/api/suscripciones/cobrar-cuota", async (IRedsysClient redsysClient, RecurringChargeRequest request) =>
{
    var response = await redsysClient.ExecuteStoredCardChargeAsync(
        order: request.Order,
        amount: request.Amount,
        cardToken: request.CardToken,
        cofType: RedsysCofType.Recurring
    );

    return response.IsSuccess
        ? Results.Ok(new { success = true, order = response.Order, authCode = response.AuthorisationCode })
        : Results.BadRequest(new { success = false, code = response.ResponseCode, error = response.ResponseDescription });
});

// 6. Endpoint para devoluciones directas (Refund REST server-to-server)
app.MapPost("/api/pedidos/{order}/devolver", async (string order, RefundRequest request, IRedsysClient redsysClient) =>
{
    var response = await redsysClient.ExecuteRefundAsync(
        order: order,
        amount: request.Amount
    );

    return response.IsSuccess
        ? Results.Ok(new { success = true, order = response.Order, authCode = response.AuthorisationCode })
        : Results.BadRequest(new { success = false, code = response.ResponseCode, error = response.ResponseDescription });
});

// 7. Endpoint para cobro directo InSite (iframe embebido sin redirección externa)
app.MapPost("/api/checkout/insite", async (InsitePaymentRequest request, IRedsysClient redsysClient) =>
{
    var response = await redsysClient.ExecuteInsitePaymentAsync(
        order: request.Order,
        amount: request.Amount,
        idOper: request.IdOper,
        registerCardOnFile: RedsysCofType.Recurring
    );

    return response.IsSuccess
        ? Results.Ok(new { success = true, order = response.Order, authCode = response.AuthorisationCode, token = response.MerchantIdentifier })
        : Results.BadRequest(new { success = false, code = response.ResponseCode, error = response.ResponseDescription });
});

// 8. Endpoint para consultar el estado de una operación (REST Consulta tipo J)
app.MapGet("/api/pedidos/{order}/consulta", async (string order, IRedsysClient redsysClient) =>
{
    var response = await redsysClient.QueryOrderAsync(order);

    return Results.Ok(new
    {
        order = response.Order,
        isSuccess = response.IsSuccess,
        responseCode = response.ResponseCode,
        description = response.ResponseDescription,
        authCode = response.AuthorisationCode
    });
});

// Memoria para registrar pedidos pagados y correlacionar con los webhooks en tiempo real
var ordersDb = new System.Collections.Concurrent.ConcurrentDictionary<string, PaymentRecord>();

// 7. Endpoint para iniciar el pago desde la tienda online
app.MapPost("/checkout/pagar", async (HttpRequest req, IRedsysClient redsysClient) =>
{
    var form = await req.ReadFormAsync();
    var order = form["order"].ToString();
    if (string.IsNullOrWhiteSpace(order)) order = DateTime.UtcNow.ToString("yyMMddHHmmss");

    var html = redsysClient
        .CreatePayment(order, 49.95m)
        .WithAllPaymentMethods() // Muestra todos los métodos activos en el terminal
        .WithDescription("Auriculares Inalámbricos Pro ANC")
        .WithSuccessUrl($"/pedido-exitoso?order={order}")
        .WithCancelUrl($"/pedido-cancelado?order={order}")
        .ToAutoSubmitHtml();

    return Results.Content(html, "text/html");
});

// Endpoint directo por URL para pruebas rápidas
app.MapGet("/checkout/pagar/{order}", (string order, IRedsysClient redsysClient) =>
{
    var html = redsysClient
        .CreatePayment(order, 49.95m)
        .WithAllPaymentMethods()
        .WithDescription("Compra Directa")
        .WithSuccessUrl($"/pedido-exitoso?order={order}")
        .WithCancelUrl($"/pedido-cancelado?order={order}")
        .ToAutoSubmitHtml();

    return Results.Content(html, "text/html");
});

// 8. Minimal API Webhook: Recibe la notificación online server-to-server de Redsys / Bizum
// Valida la firma criptográfica automáticamente antes de entrar al handler (400 Bad Request si es inválida)
app.MapRedsysWebhook("/api/pagos/webhook", async notification =>
{
    // Loguear detalles de la transacción
    Console.WriteLine($"[Redsys Webhook] Pedido: {notification.Order}, Importe: {notification.Amount.ToDisplayString()}");
    Console.WriteLine($"[Redsys Webhook] Estado: {(notification.IsSuccess ? "AUTORIZADO" : "DENEGADO")}, Código: {notification.ResponseCode}");
    Console.WriteLine($"[Redsys Webhook] ¿Procesado con Bizum?: {notification.IsBizum}");
    Console.WriteLine($"[Redsys Webhook] Detalle: {notification.ResponseDescription}");

    // Guardar en la base de datos en memoria para alimentar la pantalla de confirmación
    ordersDb[notification.Order] = new PaymentRecord(
        Order: notification.Order,
        Amount: notification.Amount.ToDisplayString(),
        ResponseCode: notification.ResponseCode,
        Description: notification.ResponseDescription,
        IsSuccess: notification.IsSuccess,
        IsBizum: notification.IsBizum,
        AuthorisationCode: notification.AuthorisationCode,
        ReceivedAt: DateTime.Now
    );

    if (!string.IsNullOrEmpty(notification.Identifier))
    {
        Console.WriteLine($"[Redsys Webhook] Token de tarjeta (COF) generado: {notification.Identifier}, Caducidad: {notification.ExpiryDate}");
    }

    if (!notification.IsSuccess)
    {
        return Results.Ok(new { status = "rejected", reason = notification.ResponseDescription });
    }

    await Task.Delay(10); // Simulación de lógica de negocio asíncrona
    return Results.Ok(new { status = "success", order = notification.Order });
});

// Páginas del flujo de compra e-commerce
app.MapGet("/", () =>
{
    var newOrder = DateTime.UtcNow.ToString("yyMMddHHmmss");
    return Results.Content(StorePages.RenderStorefront(newOrder, 49.95m), "text/html");
});

app.MapGet("/pedido-exitoso", (string? order) =>
{
    var orderKey = order ?? "";
    ordersDb.TryGetValue(orderKey, out var record);
    return Results.Content(StorePages.RenderSuccess(orderKey, record), "text/html");
});

app.MapGet("/pedido-cancelado", (string? order) =>
{
    var orderKey = order ?? "";
    ordersDb.TryGetValue(orderKey, out var record);
    return Results.Content(StorePages.RenderCancel(orderKey, record), "text/html");
});

app.Run();

// DTOs para las peticiones de compra y operaciones
public record CheckoutRequest(string Order, decimal Amount, string? Description);
public record SubscriptionRequest(string Order, decimal FirstMonthAmount, string PlanName);
public record RecurringChargeRequest(string Order, decimal Amount, string CardToken, string CardExpiry, string PlanName);
public record RefundRequest(decimal Amount, string? Reason);
public record InsitePaymentRequest(string Order, decimal Amount, string IdOper);
