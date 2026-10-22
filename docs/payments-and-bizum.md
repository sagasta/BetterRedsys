# Pagos y Bizum

BetterRedsys ofrece una API fuertemente tipada para generar pagos tanto con tarjeta bancaria como con Bizum. Soporta tanto aplicaciones desacopladas (React, Vue, Blazor, móviles) como renderizado tradicional desde el servidor.

---

## 1. Crear un pago para Frontends desacoplados (React / Next.js / Vue)

El método `.ToFormData()` devuelve un DTO con la URL de la pasarela y los parámetros firmados (`Ds_SignatureVersion`, `Ds_MerchantParameters` y `Ds_Signature`).

### En ASP.NET Core:

```csharp
app.MapPost("/api/checkout/crear-pago", (IRedsysClient redsysClient, PedidoDto pedido) =>
{
    var formData = redsysClient
        .CreatePayment(order: pedido.NumeroPedido, amount: pedido.Importe)
        .WithDescription("Compra en tienda online")
        .WithCardOrBizum() // Permite tanto Tarjeta como Bizum
        .WithCustomerPhone(pedido.TelefonoMovil)
        .WithSuccessUrl($"/pedido-exitoso?order={pedido.NumeroPedido}")
        .WithCancelUrl($"/pedido-cancelado?order={pedido.NumeroPedido}")
        .ToFormData();

    return Results.Ok(formData);
});
```

### En el Frontend (React / JavaScript):

El frontend recibe el DTO y envía un formulario POST estándar hacia `formData.gatewayUrl`:

```javascript
// La respuesta recibida desde /api/checkout/crear-pago:
const { gatewayUrl, signatureVersion, merchantParameters, signature } = await response.json();

const form = document.createElement('form');
form.method = 'POST';
form.action = gatewayUrl;

const addField = (name, value) => {
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = name;
    input.value = value;
    form.appendChild(input);
};

addField('Ds_SignatureVersion', signatureVersion);
addField('Ds_MerchantParameters', merchantParameters);
addField('Ds_Signature', signature);

document.body.appendChild(form);
form.submit();
```

---

## 2. Redirección automática desde el Servidor (`ToAutoSubmitHtml`)

Si no usas un framework de frontend y quieres redirigir directamente al usuario al TPV desde un controlador o Minimal API:

```csharp
app.MapGet("/checkout/pagar/{order}", (string order, IRedsysClient redsysClient) =>
{
    var html = redsysClient
        .CreatePayment(order, 49.95m)
        .WithCardOrBizum()
        .WithCustomerPhone("+34 612 345 678")
        .WithDescription("Auriculares Inalámbricos")
        .ToAutoSubmitHtml();

    return Results.Content(html, "text/html");
});
```

El navegador recibe una página HTML accesible y ligera que envía automáticamente el formulario POST hacia Redsys mediante JavaScript nativo, con un botón de fallback manual si JavaScript estuviera deshabilitado.

---

## 3. Pagos con Bizum

Para forzar exclusivamente el método Bizum en la pasarela, utiliza `.WithBizum()`:

```csharp
var pago = redsysClient
    .CreatePayment("2026102201", 25.00m)
    .WithBizum()
    .WithCustomerPhone("+34 600 123 456") // Valida y normaliza teléfonos móviles españoles
    .ToFormData();
```

### Reglas de validación de móvil:
* Admite prefijo internacional (`+34` o `0034`).
* Comprueba que conste de 9 dígitos y empiece por `6` o `7`.

---

## 4. Multiidioma en la pasarela

Puedes fijar el idioma en el que se mostrará la pantalla de Redsys con el enum `RedsysLanguage`:

```csharp
var pago = redsysClient
    .CreatePayment("2026102202", 30.00m)
    .WithConsumerLanguage(RedsysLanguage.English) // Idioma oficial "002"
    .ToFormData();
```

Idiomas soportados: `Spanish` (001), `English` (002), `Catalan` (003), `Galician` (004), `French` (005), `German` (006), `Basque` (013), entre otros.

---

## 5. El objeto de valor `RedsysAmount`

Redsys exige que los importes se envíen en céntimos enteros (ejemplo: 49,95 € son `4995`). Para evitar errores de coma flotante y redondeo:

```csharp
// Creación desde decimal
RedsysAmount importe = 49.95m;

// Conversión a céntimos Redsys
string centimos = importe.ToRedsysCents(); // "4995"

// Formateo visual
string texto = importe.ToDisplayString(); // "49,95 €"
```

---

## 6. Parámetros 3D Secure v2 y Exenciones PSD2 (EMV3DS)

Para transacciones seguras bajo la normativa europea PSD2 SCA o para solicitar exenciones de autenticación (Low Value, Transaction Risk Analysis):

```csharp
var pago = redsysClient
    .CreatePayment("2026102203", 15.00m)
    .With3DsExemption("TRA") // Exención por análisis de riesgo
    .WithEmv3Ds(new RedsysEmv3Ds
    {
        Email = "cliente@ejemplo.com",
        CardholderName = "Alejandro Gómez",
        ChallengeWindowSize = "05" // Pantalla completa en móvil
    })
    .ToFormData();
```

---

## 7. Generación y Validación de Números de Pedido (`RedsysOrderNumber`)

Redsys exige que el número de pedido (`Ds_Merchant_Order`) cumpla estrictamente tres reglas:
* Longitud entre **4 y 12 caracteres**.
* **Los 4 primeros caracteres deben ser obligatoriamente dígitos numéricos** (`0-9`).
* Todos los caracteres deben ser alfanuméricos ASCII (letras y números, sin guiones ni espacios).

Para evitar el error bancario `SIS0014: Error en el formato de Ds_Merchant_Order`, utiliza la clase `RedsysOrderNumber`:

```csharp
using BetterRedsys.Models;

// 1. Generar automáticamente un número de pedido válido de 12 caracteres (yyMMdd + 6 dígitos aleatorios)
string orderId = RedsysOrderNumber.Generate(); // Ejemplo: "261022481920"

// 2. Validar un número de pedido antes de procesarlo
if (RedsysOrderNumber.IsValid(orderId, out string? error))
{
    // El número de pedido cumple la especificación de Redsys
}
else
{
    Console.WriteLine($"Número de pedido inválido: {error}");
}

// 3. Validación con excepción si no es válido
RedsysOrderNumber.Validate(orderId);
```
