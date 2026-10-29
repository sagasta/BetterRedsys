<p align="center">
  <img src="assets/icon.png" width="160" height="160" alt="BetterRedsys Logo" />
</p>

# BetterRedsys

[![CI](https://github.com/sagasta/BetterRedsys/actions/workflows/ci.yml/badge.svg)](https://github.com/sagasta/BetterRedsys/actions/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue)](https://sagasta.github.io/BetterRedsys/)
[![NuGet](https://img.shields.io/nuget/v/BetterRedsys.svg)](https://www.nuget.org/packages/BetterRedsys/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-Ready-brightgreen)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-orange?logo=buy-me-a-coffee&logoColor=white)](https://buymeacoffee.com/sagasta)

Librería moderna, de alto rendimiento y compatible con **Native AOT** para integrar la pasarela de pagos **Redsys y Bizum** en **ASP.NET Core** y **.NET 10** (C# 13/14).

---

## Características

* **Rendimiento y Native AOT:** Cero reflexión para JSON mediante `System.Text.Json` Source Generators. Preparada para contenedores mínimos y microservicios serverless.
* **Criptografía estricta:** Diversificación de clave 3DES-CBC por número de pedido, firma HMAC-SHA256 y verificación en tiempo constante (`FixedTimeEquals`) para mitigar ataques de temporización.
* **Bizum de primer nivel:** Soporte nativo para Bizum (`.WithBizum()`, `.WithCard()`, `.WithAllPaymentMethods()`) y notificaciones con detección automática de pagos Bizum (`IsBizum`).
* **Arquitectura dual:**
  * `.ToFormData()`: DTO limpio para aplicaciones desacopladas (React, Next.js, Vue, Blazor, apps móviles).
  * `.ToAutoSubmitHtml()`: Redirección automática accesible desde el servidor.
* **Minimal APIs Webhook:** Middleware `MapRedsysWebhook` con validación criptográfica automática antes de ejecutar tu lógica de negocio.
* **Operaciones REST directas (S2S):** Consulta de pedidos (tipo J), devoluciones, cobros con tarjeta guardada (Card-On-File) y preautorizaciones.
* **Redsys InSite:** Soporte para cobros embebidos con token de operación `DS_MERCHANT_IDOPER`.
* **Telemetría y Observabilidad:** Health checks integrados de ASP.NET Core, trazas distribuidas con `ActivitySource` y métricas con `Meter`.
* **Simulador local incluido:** Paquete complementario `BetterRedsys.Simulator` para pruebas locales interactivas sin necesidad de túneles (`ngrok`).

---

## Instalación

```bash
# Cliente principal, criptografía y soporte para webhooks
dotnet add package BetterRedsys

# Opcional: Simulador interactivo en memoria para entorno de desarrollo
dotnet add package BetterRedsys.Simulator
```

---

## Inicio Rápido (5 minutos)

### 1. Configuración del servicio (`Program.cs`)

```csharp
using BetterRedsys.Configuration;
using BetterRedsys.Endpoints;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator;

var builder = WebApplication.CreateBuilder(args);

// Configuración diferenciando Desarrollo y Producción
var redsys = builder.Services.AddBetterRedsys(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Desarrollo: Credenciales del entorno de pruebas de Redsys
        options.UseTest(
            fuc: "999008881",
            terminal: "1",
            secretKey: "sq7HjrUOBfKmC576ILgskD5srU870gJ7"
        )
        .WithWebhooks(
            notificationUrl: "/api/pagos/webhook",
            okUrl: "/pedido-exitoso",
            koUrl: "/pedido-cancelado"
        );
    }
    else
    {
        // Producción: Credenciales reales desde variables de entorno o Key Vault
        options.UseProduction(
            fuc: builder.Configuration["Redsys:Fuc"]!,
            terminal: builder.Configuration["Redsys:Terminal"]!,
            secretKey: builder.Configuration["Redsys:SecretKey"]!
        )
        .WithWebhooks(
            notificationUrl: "https://tutienda.com/api/pagos/webhook",
            okUrl: "https://tutienda.com/pedido-exitoso",
            koUrl: "https://tutienda.com/pedido-cancelado"
        );
    }

    options.WithEuros();
})
.AddHealthCheck();

// En desarrollo, activa el simulador local de pruebas (sin ngrok)
if (builder.Environment.IsDevelopment())
{
    redsys.AddSimulator();
}

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseRedsysSimulator();
}
```

### 2. Generar el pago desde un endpoint

```csharp
app.MapPost("/api/checkout/crear-pago", (IRedsysClient redsysClient, PedidoDto pedido) =>
{
    var formData = redsysClient
        .CreatePayment(order: pedido.NumeroPedido, amount: pedido.Importe)
        .WithDescription("Compra en tienda online")
        .WithAllPaymentMethods()
        .ToFormData();

    return Results.Ok(formData);
});
```

### 3. Recibir y validar la notificación (Webhook)

```csharp
app.MapRedsysWebhook("/api/pagos/webhook", async (RedsysNotification notification) =>
{
    // Si la firma es inválida, BetterRedsys responde 400 Bad Request automáticamente
    if (!notification.IsSuccess)
    {
        Console.WriteLine($"Pago rechazado: {notification.ResponseDescription}");
        return Results.Ok();
    }

    Console.WriteLine($"Pago autorizado: Pedido {notification.Order}, Importe: {notification.Amount.ToDisplayString()}");
    Console.WriteLine($"¿Procesado con Bizum?: {notification.IsBizum}");

    await ConfirmarPedidoEnBaseDeDatosAsync(notification.Order);
    return Results.Ok();
});

app.Run();
```

---

## Documentación Completa

> 🌐 **Sitio web oficial interactivo con buscador:** [https://sagasta.github.io/BetterRedsys/](https://sagasta.github.io/BetterRedsys/)

Para guías detalladas y ejemplos de cada funcionalidad, consulta la web o la carpeta `docs/`:

* [Configuración y Opciones](docs/configuration.md): Presets de test/producción, `appsettings.json`, validación temprana y builder pattern.
* [Pagos y Bizum](docs/payments-and-bizum.md): Creación de pagos, frontends desacoplados (React/Vue), redirección HTML, reglas de Bizum y 3DS v2.
* [Webhooks y Notificaciones](docs/webhooks.md): Verificación criptográfica automática, catálogo de respuestas bilingüe e idempotencia.
* [Operaciones REST Servidor a Servidor](docs/rest-operations.md): Consulta de pedidos online (tipo J), devoluciones directas y cobro con tarjeta almacenada (COF).
* [Redsys InSite](docs/insite.md): Integración con formulario embebido mediante `DS_MERCHANT_IDOPER`.
* [Telemetría, Salud y Auditoría](docs/telemetry.md): ASP.NET Core Health Checks, trazas distribuidas con OpenTelemetry y métricas Prometheus.
* [Simulador Local](docs/simulator.md): Pruebas completas en desarrollo local sin `ngrok` ni dependencias externas.
* [Homologación y Paso a Producción](docs/homologacion-produccion.md): Checklist oficial para bancos (CaixaBank, Santander, BBVA...), tarjetas de test y pase a producción.

---

## 💖 Support & Sponsoring

Si **BetterRedsys** te ha ayudado a integrar la pasarela de pagos, ahorrar tiempo de desarrollo o asegurar tus transacciones con Native AOT, considera apoyar el mantenimiento continuo del proyecto:

<p align="left">
  <a href="https://buymeacoffee.com/sagasta" target="_blank">
    <img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="48" />
  </a>
</p>

---

## Licencia

Distribuido bajo licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más información.

