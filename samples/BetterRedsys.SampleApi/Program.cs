using BetterRedsys.Configuration;
using BetterRedsys.Endpoints;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración moderna con IBetterRedsysBuilder, ValidateOnStart y métodos fluidos
var redsysBuilder = builder.Services.AddBetterRedsys(options =>
{
    options.UseTest(
        fuc: builder.Configuration["Redsys:Fuc"] ?? "999008881",
        terminal: builder.Configuration["Redsys:Terminal"] ?? "1",
        secretKey: builder.Configuration["Redsys:SecretKey"] ?? "sq7HjrUOBfKmC576ILgskD5srURiUz6U")
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
        .WithCardOrBizum() // Permite tanto Tarjeta como Bizum
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

// 7. Endpoint para redirección directa clásica desde el servidor
// Devuelve una página HTML autoejecutable que envía el formulario automáticamente al cargar
app.MapGet("/checkout/pagar/{order}", (string order, IRedsysClient redsysClient) =>
{
    var html = redsysClient
        .CreatePayment(order, 49.95m)
        .WithBizum() // Forzar Bizum
        .WithCustomerPhone("+34 612 345 678")
        .WithDescription("Suscripción Mensual")
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

    // Si la operación incluyó tokenización de tarjeta (COF)
    if (!string.IsNullOrEmpty(notification.Identifier))
    {
        Console.WriteLine($"[Redsys Webhook] Token de tarjeta (COF) generado: {notification.Identifier}, Caducidad: {notification.ExpiryDate}");
    }

    if (!notification.IsSuccess)
    {
        // Registrar rechazo o alertar al sistema
        return Results.Ok(new { status = "rejected", reason = notification.ResponseDescription });
    }

    // Procesar pedido pagado (actualizar base de datos, guardar token si aplica, enviar email...)
    await Task.Delay(10); // Simulación de lógica de negocio asíncrona

    return Results.Ok(new { status = "success", order = notification.Order });
});

// Páginas de retorno de ejemplo
app.MapGet("/pedido-exitoso", (string? order) => Results.Ok($"¡Gracias por tu compra! Tu pedido {order} ha sido completado con éxito."));
app.MapGet("/pedido-cancelado", (string? order) => Results.Ok($"El pedido {order} fue cancelado o rechazado."));
app.MapGet("/", () => Results.Redirect("/checkout/pagar/2026102201"));

app.Run();

// DTOs para las peticiones de compra y operaciones
public record CheckoutRequest(string Order, decimal Amount, string? Description);
public record SubscriptionRequest(string Order, decimal FirstMonthAmount, string PlanName);
public record RecurringChargeRequest(string Order, decimal Amount, string CardToken, string CardExpiry, string PlanName);
public record RefundRequest(decimal Amount, string? Reason);
public record InsitePaymentRequest(string Order, decimal Amount, string IdOper);
