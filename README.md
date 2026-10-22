# BetterRedsys 🚀

[![NuGet](https://img.shields.io/nuget/v/BetterRedsys.svg)](https://www.nuget.org/packages/BetterRedsys/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/AOT-Ready-brightgreen.svg)]()

Modern, strongly-typed, high-performance, and **Native AOT Ready** Redsys and Bizum payment gateway library for **ASP.NET Core** and **.NET 10** (C# 13/14).

*Librería moderna, fuertemente tipada y Native AOT para la pasarela de pagos Redsys y Bizum en ASP.NET Core y .NET 10.*

---

## ✨ Key Features / Características Principales

- **⚡ Idiomatic .NET 10 & C# 13/14:** File-scoped namespaces, primary constructors, readonly record structs, and pattern matching.
- **🛡️ Native AOT Ready:** Zero reflection for JSON via `System.Text.Json` Source Generators (`JsonSerializerContext`). Suitable for lightweight containers and serverless microservices.
- **🌍 Full Multilanguage Support (EN / ES / Regional):**
  - Strongly-typed `RedsysLanguage` enum covering all 13 official Redsys language codes (Spanish `"001"`, English `"002"`, Catalan `"003"`, French `"004"`, German `"005"`, etc.).
  - Automatic `CultureInfo` mapping.
  - Bilingual response catalog ([RedsysResponseCatalog](file:///c:/Users/JoseSagastaBru/source/repos/BetterRedsys/src/BetterRedsys/Models/RedsysResponseCatalog.cs)) providing detailed, human-readable error descriptions in both English and Spanish (`notification.ResponseDescription` and `notification.ResponseDescriptionEn`).
  - Bilingual automatic redirect HTML templates ([RedsysPaymentBuilder.ToAutoSubmitHtml](file:///c:/Users/JoseSagastaBru/source/repos/BetterRedsys/src/BetterRedsys/Services/RedsysPaymentBuilder.cs#L225)).
- **🔐 Strict Redsys Cryptography:**
  - 3DES-CBC key diversification with zero IV (`new byte[8]`) and right-padded zero bytes over the merchant order ID.
  - HMAC-SHA256 signature computation over Base64 merchant parameters.
  - Constant-time verification (`CryptographicOperations.FixedTimeEquals`) to prevent timing attacks.
  - Automatic normalization of URL-safe Base64 signatures (`-` and `_`) and dynamic padding.
- **📱 First-Class Native Bizum:**
  - Fluent `.WithBizum()` and `.WithCardOrBizum()` methods.
  - Spanish mobile number validation and normalization (9 digits starting with 6 or 7, supporting `+34` / `0034`).
  - Automatic Bizum detection on webhook notification (`notification.IsBizum`).
- **💶 Immutable Value Object `RedsysAmount`:** Eliminates rounding and floating-point errors by strictly handling integer cents with implicit conversions.
- **🚀 Dual Mode: Decoupled Front-Ends & Direct Server Redirect:**
  - `.ToFormData()`: Returns clean DTO ([RedsysPaymentResponseData](file:///c:/Users/JoseSagastaBru/source/repos/BetterRedsys/src/BetterRedsys/Models/RedsysPaymentResponseData.cs)) for React, Next.js, Blazor, Vue, or Mobile apps.
  - `.ToAutoSubmitHtml()`: Produces self-submitting HTML with fallback styling.
- **🎯 Minimal APIs Webhook Handler:**
  - `app.MapRedsysWebhook("/api/payments/webhook", async (RedsysNotification notification) => ...)`
  - Automatic cryptographic verification before invoking business logic. Responds with `400 Bad Request` if tampered.
- **💳 S2S REST Operations & Redsys InSite:**
  - Tokenized recurring / 1-click charges (`ExecuteStoredCardChargeAsync`).
  - Direct refunds & preauthorizations (`ExecuteRefundAsync`, `ConfirmPreauthorizationAsync`, `CancelPreauthorizationAsync`).
  - Embedded iframe payments without external redirection via Redsys InSite (`ExecuteInsitePaymentAsync`).
  - Order status consultation query (`QueryOrderAsync`).
- **🛡️ 3D Secure v2 & PSD2 SCA (EMV3DS):**
  - Strongly-typed `RedsysEmv3Ds` context and fluent exemption configuration (`.With3DsExemption()`).
- **🩺 ASP.NET Core Health Checks:**
  - Built-in `builder.AddHealthCheck()` or `services.AddHealthChecks().AddRedsys()` probe verifying live gateway connectivity.
- **📊 OpenTelemetry Distributed Tracing & Metrics:**
  - Zero-dependency runtime instrumentation via `ActivitySource("BetterRedsys")` and `Meter("BetterRedsys")`.
  - Tracks operations, latencies, success rates, and structured audit logs.
- **🧪 BetterRedsys.Simulator (Separate NuGet Package):**
  - In-memory local payment simulation (`app.UseRedsysSimulator()`).
  - Interactive bilingual UI to simulate approvals, insufficient funds, expired cards, 3DS failures, and cancelations.
  - Dispatches an authentic HMAC-SHA256 signed HTTP POST to the local webhook without `ngrok`.
  - Simulates direct server-to-server REST operations (refunds, COF charges, InSite, order queries, preauthorizations).

---

## 📦 Installation / Instalación

```bash
# Core payment client, cryptography & OpenTelemetry
dotnet add package BetterRedsys

# Optional: In-memory local payment & REST gateway simulator for development
dotnet add package BetterRedsys.Simulator
```

---

## 🛠️ Quick Start / Inicio Rápido

### 1. Fluent Dependency Injection (`Program.cs`)

```csharp
using BetterRedsys.Configuration;
using BetterRedsys.Endpoints;
using BetterRedsys.Models;
using BetterRedsys.Services;
using BetterRedsys.Simulator; // When using BetterRedsys.Simulator

var builder = WebApplication.CreateBuilder(args);

// Fluent configuration with eager startup validation (ValidateOnStart)
var redsysBuilder = builder.Services.AddBetterRedsys(options =>
{
    options.UseTest(fuc: "999008881", terminal: "1", secretKey: "sq7HjrUOBfKmC576ILgskD5srURiUz6U")
           .WithEuros()
           .WithWebhooks(notificationUrl: "/api/payments/webhook", okUrl: "/checkout/success", koUrl: "/checkout/cancel");
})
.AddHealthCheck(); // Built-in Health Check

// Optional: Enable simulator in development
if (builder.Environment.IsDevelopment())
{
    redsysBuilder.AddSimulator();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseRedsysSimulator();
}
```

### 2. Decoupled Checkout for React / Next.js

```csharp
app.MapPost("/api/checkout/create-payment", (IRedsysClient redsysClient) =>
{
    RedsysPaymentResponseData formData = redsysClient
        .CreatePayment(order: "2026102201", amount: 29.99m)
        .WithDescription("Premium Subscription")
        .WithCardOrBizum()
        .WithConsumerLanguage(RedsysLanguage.English)
        .WithCustomerPhone("+34 612 345 678")
        .ToFormData();

    return Results.Ok(formData);
});
```

### 3. Server Redirect with Multilanguage HTML

```csharp
app.MapGet("/checkout/pay/{order}", (string order, IRedsysClient redsysClient) =>
{
    string html = redsysClient
        .CreatePayment(order, 19.95m)
        .WithBizum()
        .WithConsumerLanguage(RedsysLanguage.English)
        .ToAutoSubmitHtml(RedsysLanguage.English);

    return Results.Content(html, "text/html");
});
```

### 4. Verified Webhook Endpoint

```csharp
app.MapRedsysWebhook("/api/payments/webhook", async (RedsysNotification notification) =>
{
    if (!notification.IsSuccess)
    {
        // Bilingual descriptions available:
        Console.WriteLine($"[KO] Spanish: {notification.ResponseDescription}");
        Console.WriteLine($"[KO] English: {notification.ResponseDescriptionEn}");
        return Results.Ok();
    }

    if (notification.IsBizum)
    {
        Console.WriteLine($"Paid with Bizum: {notification.Amount.ToDisplayString()}");
    }

    await CompleteOrderAsync(notification.Order);
    return Results.Ok();
});
```

---

## 📄 License

MIT © Jose Sagasta
