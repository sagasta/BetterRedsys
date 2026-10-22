# Configuración

BetterRedsys ofrece una configuración moderna adaptada a los estándares de ASP.NET Core, con separación limpia entre los entornos de **Desarrollo** y **Producción**, validación temprana en el arranque (*eager validation*) y soporte para `appsettings.json` y variables de entorno.

---

## 1. Enfoque recomendado: `appsettings.json` por entorno

Es la forma más limpia y segura, ya que evita escribir claves o URLs bancarias fijas en el código C#.

### En `appsettings.json` (Entorno de Producción):
```json
{
  "Redsys": {
    "Environment": "Production",
    "Fuc": "TU_FUC_COMERCIO",
    "Terminal": "1",
    "SecretKey": "TU_CLAVE_SECRETA_PRODUCCION",
    "Currency": "978",
    "MerchantUrl": "https://tutienda.com/api/pagos/webhook",
    "UrlOk": "https://tutienda.com/pedido-exitoso",
    "UrlKo": "https://tutienda.com/pedido-cancelado"
  }
}
```

### En `appsettings.Development.json` (Entorno de Desarrollo):
```json
{
  "Redsys": {
    "Environment": "Test",
    "Fuc": "999008881",
    "Terminal": "1",
    "SecretKey": "sq7HjrUOBfKmC576ILgskD5srURiUz6U",
    "Currency": "978",
    "MerchantUrl": "/api/pagos/webhook",
    "UrlOk": "/pedido-exitoso",
    "UrlKo": "/pedido-cancelado"
  }
}
```

### En `Program.cs`:
ASP.NET Core carga automáticamente la sección `Redsys` del archivo correspondiente al entorno activo:

```csharp
using BetterRedsys.Configuration;
using BetterRedsys.Simulator;

var builder = WebApplication.CreateBuilder(args);

// Carga la configuración automáticamente según el entorno (Dev o Prod)
var redsys = builder.Services.AddBetterRedsys()
    .AddHealthCheck();

// En desarrollo, activa el simulador local interactivo sin ngrok
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

app.Run();
```

---

## 2. Enfoque por código: Diferenciando Dev y Prod

Si prefieres registrar la configuración directamente en C# diferenciando los entornos:

```csharp
using BetterRedsys.Configuration;
using BetterRedsys.Simulator;

var builder = WebApplication.CreateBuilder(args);

var redsys = builder.Services.AddBetterRedsys(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 1. Configuración de DESARROLLO (credenciales del Sandbox oficial de Redsys)
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
    }
    else
    {
        // 2. Configuración de PRODUCCIÓN (datos reales desde variables de entorno o Key Vault)
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
    }
})
.AddHealthCheck();

if (builder.Environment.IsDevelopment())
{
    redsys.AddSimulator();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseRedsysSimulator();
}

app.Run();
```

---

## 3. Presets disponibles

* `options.UseTest(fuc, terminal, secretKey)`: Establece automáticamente la pasarela oficial de pruebas (`https://sis-t.redsys.es:25443/sis/realizarPago` y endpoint REST de pruebas).
* `options.UseProduction(fuc, terminal, secretKey)`: Establece la pasarela oficial de producción segura de Redsys (`https://sis.redsys.es/sis/realizarPago` y endpoint REST de producción).
* `options.WithEuros()`: Configura la moneda estándar Euro (`978`).
* `options.WithCurrency("840")`: Configura una moneda personalizada (ej: Dólar estadounidense).
* `options.WithWebhooks(notificationUrl, okUrl, koUrl)`: Configura las tres URLs clave del ciclo de compra.
* `options.WithDefaultLanguage(RedsysLanguage.Spanish)`: Idioma predeterminado para la pasarela bancaria.

---

## 4. Validación temprana en el arranque (`ValidateOnStart`)

BetterRedsys valida las opciones en el arranque del servidor (`ValidateOnStart()`):
* El **FUC** no puede estar vacío y debe ser numérico (hasta 9 dígitos).
* El **Terminal** debe ser numérico (1 a 3 dígitos).
* La **SecretKey** no puede estar vacía.
* La **Moneda** debe constar de 3 dígitos ISO 4217.

Si falta alguno de estos parámetros en producción, la aplicación lanzará una excepción clara en el inicio en lugar de fallar en la primera transacción de un cliente.
