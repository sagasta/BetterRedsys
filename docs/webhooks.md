# Webhooks y Notificación Online

Redsys notifica los resultados de las transacciones mediante una petición HTTP POST directa de servidor a servidor (*MerchantURL*). Esta notificación es la única fuente de verdad para confirmar un pago.

BetterRedsys proporciona el método `MapRedsysWebhook` para Minimal APIs, que valida la firma criptográfica antes de ejecutar tu lógica de negocio.

---

## 1. Configuración básica del endpoint

```csharp
using BetterRedsys.Endpoints;
using BetterRedsys.Models;

app.MapRedsysWebhook("/api/pagos/webhook", async (RedsysNotification notification) =>
{
    // Si la firma no fuera válida, BetterRedsys ya habrá respondido con 400 Bad Request
    // y este bloque de código no llegará a ejecutarse.

    if (!notification.IsSuccess)
    {
        // Registro de pago denegado o cancelado
        Console.WriteLine($"Pago denegado para el pedido {notification.Order}. Motivo: {notification.ResponseDescription}");
        return Results.Ok();
    }

    // El pago es válido y está autorizado
    Console.WriteLine($"Pago completado: Pedido {notification.Order}, Importe {notification.Amount.ToDisplayString()}");
    Console.WriteLine($"Código de autorización: {notification.AuthorisationCode}");
    Console.WriteLine($"¿Pagado con Bizum?: {notification.IsBizum}");

    // Token de tarjeta guardada (si se solicitó Card-On-File / suscripción)
    if (!string.IsNullOrEmpty(notification.Identifier))
    {
        Console.WriteLine($"Token de tarjeta: {notification.Identifier}, Caducidad: {notification.ExpiryDate}");
    }

    // Actualiza tu base de datos (marcar pedido como pagado, enviar email...)
    await ConfirmarPedidoEnBaseDeDatosAsync(notification.Order);

    // Responder siempre HTTP 200 OK a Redsys
    return Results.Ok();
});
```

---

## 2. Validación de seguridad criptográfica

El middleware realiza los siguientes pasos de forma automática:
1. Extrae los parámetros `Ds_MerchantParameters` y la firma `Ds_Signature`.
2. Normaliza el formato Base64 (incluyendo padding y variantes URL-safe).
3. Diversifica la clave secreta del comercio mediante 3DES-CBC utilizando el número de pedido como clave de diversificación.
4. Calcula la firma esperada HMAC-SHA256 sobre los parámetros recibidos.
5. Compara la firma recibida con la calculada utilizando `CryptographicOperations.FixedTimeEquals` en tiempo constante para mitigar ataques de temporización (*timing attacks*).

Si la firma es inválida, devuelve un código **HTTP 400 Bad Request**, emite un log de advertencia con los detalles de auditoría y bloquea la ejecución.

---

## 3. Catálogo de respuestas bilingüe

El objeto `RedsysNotification` incluye descripciones legibles de los más de 100 códigos de error oficiales de Redsys en español e inglés:

```csharp
if (!notification.IsSuccess)
{
    // En español: "Disponible o saldo insuficiente."
    string motivoEs = notification.ResponseDescription;

    // En inglés: "Insufficient funds available."
    string motivoEn = notification.ResponseDescriptionEn;
    
    // Código numérico oficial (ej: "0116")
    string codigo = notification.ResponseCode;
}
```

Códigos comunes:
* `0000` a `0099`: Transacción autorizada con éxito.
* `0116`: Saldo o crédito insuficiente.
* `0180`: Tarjeta caducada o rechazada.
* `0184`: Error en la autenticación del titular (3D Secure).
* `0190`: Denegación del emisor sin especificar motivo.
* `9915`: El usuario ha cancelado la operación en la pasarela.

---

## 4. Recomendación de Idempotencia

Redsys puede reintentar el envío de la notificación si tu servidor tarda en responder o si hay cortes de red momentáneos.

Asegúrate de que la actualización de tu base de datos sea idempotente comprobando si el pedido ya fue marcado como pagado previamente:

```csharp
var pedido = await dbContext.Pedidos.FindAsync(notification.Order);

if (pedido is not null && !pedido.EstaPagado)
{
    pedido.EstaPagado = true;
    pedido.MetodoPago = notification.IsBizum ? "Bizum" : "Tarjeta";
    pedido.CodigoAutorizacion = notification.AuthorisationCode;
    await dbContext.SaveChangesAsync();
}
```
