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
        .WithAllPaymentMethods() // Muestra todos los métodos activos en el terminal
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
        .WithAllPaymentMethods()
        .WithDescription("Auriculares Inalámbricos")
        .ToAutoSubmitHtml();

    return Results.Content(html, "text/html");
});
```

El navegador recibe una página HTML accesible y ligera que envía automáticamente el formulario POST hacia Redsys mediante JavaScript nativo, con un botón de fallback manual si JavaScript estuviera deshabilitado.

---

## 3. Pagos con Bizum

Para forzar exclusivamente el método Bizum en la pasarela, utiliza `.WithBizum()` (`DS_MERCHANT_PAYMETHODS = "z"`):

```csharp
var pago = redsysClient
    .CreatePayment("2026102201", 25.00m)
    .WithBizum()
    .ToFormData();
```

La propia página de Bizum pide el número de móvil al cliente, así que no hace falta enviarlo.

### Métodos de pago disponibles

| Método | `DS_MERCHANT_PAYMETHODS` | Resultado |
|---|---|---|
| `.WithBizum()` | `z` | Solo Bizum |
| `.WithCard()` | `C` | Solo tarjeta |
| `.WithAllPaymentMethods()` | *(no se envía)* | Todos los métodos activos en el terminal |

### Ofrecer tarjeta y Bizum a la vez

Redsys no tiene ningún valor de `DS_MERCHANT_PAYMETHODS` que muestre solo tarjeta y Bizum. Cualquier valor que contenga `z` (`zC`, `Cz`, `z,T`…) envía directamente a Bizum, y que Bizum aparezca en el selector de `.WithAllPaymentMethods()` depende de cómo tengas configurado el terminal.

La forma fiable es que el cliente elija el método en tu web y generar el pago según lo que elija:

```csharp
var builder = redsysClient.CreatePayment(pedido.NumeroPedido, pedido.Importe);

var formData = (pedido.MetodoPago == "bizum" ? builder.WithBizum() : builder.WithCard())
    .WithDescription("Compra en tienda online")
    .ToFormData();
```

> [!NOTE]
> `.WithCardOrBizum()` está obsoleto desde la versión 2.0.1. En la 1.x enviaba `"z,T"`, que en la práctica llevaba solo a Bizum. En la 2.0.0 equivale a `.WithAllPaymentMethods()`.

### Teléfono del cliente (Paygold)

`.WithCustomerPhone()` rellena `DS_MERCHANT_CUSTOMER_MOBILE`, el campo que usan las operaciones Paygold (pago por enlace enviado por SMS). **No** precarga el teléfono en la página de Bizum.

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

Idiomas soportados: `Spanish` (001), `English` (002), `Catalan` (003), `French` (004), `German` (005), `Dutch` (006), `Italian` (007), `Swedish` (008), `Portuguese` (009), `Valencian` (010), `Polish` (011), `Galician` (012) y `Basque` (013).

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

// 1. Generar automáticamente un número de pedido válido de 12 caracteres (yyMM + 8 alfanuméricos aleatorios)
string orderId = RedsysOrderNumber.Generate(); // Ejemplo: "2610K7Q2M9XA"

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
