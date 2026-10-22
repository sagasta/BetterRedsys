# Configuración

BetterRedsys ofrece una API fluida de configuración con validación temprana en el arranque de la aplicación (*eager validation*) y soporte nativo para `appsettings.json`.

---

## 1. Registro básico en `Program.cs`

Puedes configurar la librería directamente en el código mediante métodos fluidos:

```csharp
using BetterRedsys.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBetterRedsys(options =>
{
    // Preset para entorno de pruebas oficiales de Redsys
    options.UseTest(
        fuc: "999008881",
        terminal: "1",
        secretKey: "sq7HjrUOBfKmC576ILgskD5srURiUz6U"
    )
    .WithEuros()
    .WithWebhooks(
        notificationUrl: "/api/pagos/webhook",
        okUrl: "/pedido-exitoso",
        koUrl: "/pedido-cancelado"
    );
});
```

---

## 2. Configuración mediante `appsettings.json`

Si prefieres separar los datos sensibles por entorno, añade la sección `Redsys` en tu `appsettings.json`:

```json
{
  "Redsys": {
    "Environment": "Test",
    "Fuc": "999008881",
    "Terminal": "1",
    "SecretKey": "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
    "Currency": "978",
    "MerchantUrl": "https://midominio.com/api/pagos/webhook",
    "UrlOk": "https://midominio.com/pedido-exitoso",
    "UrlKo": "https://midominio.com/pedido-cancelado"
  }
}
```

En `Program.cs`, el registro enlaza automáticamente con la sección `Redsys` del `IConfiguration`:

```csharp
builder.Services.AddBetterRedsys();
```

O combinando `appsettings.json` con sobreescrituras en código:

```csharp
builder.Services.AddBetterRedsys(options =>
{
    options.WithWebhooks(
        notificationUrl: "/api/pagos/webhook",
        okUrl: "/pedido-exitoso",
        koUrl: "/pedido-cancelado"
    );
});
```

---

## 3. Entorno de Producción

Para producción, utiliza el preset `UseProduction`:

```csharp
builder.Services.AddBetterRedsys(options =>
{
    options.UseProduction(
        fuc: builder.Configuration["Redsys:Fuc"]!,
        terminal: builder.Configuration["Redsys:Terminal"]!,
        secretKey: builder.Configuration["Redsys:SecretKey"]!
    )
    .WithEuros()
    .WithWebhooks(
        notificationUrl: "https://tutienda.com/api/pagos/webhook",
        okUrl: "https://tutienda.com/pedido-exitoso",
        koUrl: "https://tutienda.com/pedido-cancelado"
    );
});
```

---

## 4. Validación temprana en el arranque (`ValidateOnStart`)

BetterRedsys implementa `IValidateOptions<RedsysOptions>` y ejecuta `ValidateOnStart()` durante el registro de servicios. 

Si falta el FUC, la Terminal o la clave secreta, la aplicación fallará inmediatamente al arrancar con un mensaje descriptivo en lugar de fallar en la primera petición del usuario:

* FUC debe contener hasta 9 dígitos numéricos.
* Terminal debe contener de 1 a 3 dígitos numéricos.
* SecretKey no puede estar vacía.
* La moneda debe tener 3 dígitos (ej: `"978"` para EUR).

---

## 5. El constructor `IBetterRedsysBuilder`

El método `AddBetterRedsys` devuelve una instancia de `IBetterRedsysBuilder` que permite encadenar módulos opcionales:

```csharp
var redsysBuilder = builder.Services.AddBetterRedsys(options => { ... })
    .AddHealthCheck(); // Registra el HealthCheck oficial de Redsys

if (builder.Environment.IsDevelopment())
{
    redsysBuilder.AddSimulator(); // Registra el simulador local de pruebas
}
```
