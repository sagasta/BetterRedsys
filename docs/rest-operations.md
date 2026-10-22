# Operaciones REST Servidor a Servidor (S2S)

BetterRedsys soporta la API REST directa de Redsys (`trataPeticionREST`) para ejecutar operaciones de servidor a servidor sin intervención del comprador en el navegador.

Todas las llamadas gestionan automáticamente el cifrado JSON, la firma HMAC-SHA256 y la verificación de la respuesta.

---

## 1. Consulta del estado de un pedido (Operación tipo `J`)

Permite comprobar en tiempo real si un pedido fue pagado, consultar su código de autorización o verificar discrepancias:

```csharp
app.MapGet("/api/pedidos/{order}/consulta", async (string order, IRedsysClient redsysClient) =>
{
    var resultado = await redsysClient.QueryOrderAsync(order);

    if (resultado.IsSuccess)
    {
        return Results.Ok(new
        {
            order = resultado.Order,
            autorizado = true,
            codigoAutorizacion = resultado.AuthorisationCode,
            descripcion = resultado.ResponseDescription
        });
    }

    return Results.NotFound(new
    {
        order = resultado.Order,
        autorizado = false,
        codigo = resultado.ResponseCode,
        motivo = resultado.ResponseDescription
    });
});
```

---

## 2. Devoluciones directas (Refund)

Permite reembolsar total o parcialmente un pedido pagado previamente:

```csharp
app.MapPost("/api/pedidos/{order}/devolver", async (string order, decimal importe, IRedsysClient redsysClient) =>
{
    var resultado = await redsysClient.ExecuteRefundAsync(
        order: order,
        amount: importe
    );

    if (resultado.IsSuccess)
    {
        return Results.Ok(new
        {
            success = true,
            order = resultado.Order,
            authCode = resultado.AuthorisationCode
        });
    }

    return Results.BadRequest(new
    {
        success = false,
        codigo = resultado.ResponseCode,
        error = resultado.ResponseDescription
    });
});
```

---

## 3. Cobro con tarjeta guardada (Card-On-File / Tokenización)

Permite realizar cobros recurrentes de suscripciones o pagos en un solo clic (*1-click checkout*) utilizando el identificador de tarjeta (`DS_MERCHANT_IDENTIFIER`) obtenido en el primer pago.

### Paso 1: Obtener el token en el primer pago
En el pago inicial por redirección o InSite, añade `.WithInitialCardOnFile()`:

```csharp
var pagoInicial = redsysClient
    .CreatePayment(order: "2026102201", amount: 9.99m)
    .WithInitialCardOnFile() // Solicita a Redsys tokenizar la tarjeta
    .ToFormData();
```

Cuando Redsys envíe el webhook de confirmación, guarda el token en tu base de datos:
```csharp
string tokenTarjeta = notification.Identifier!; // Ejemplo: "tok_991823719"
string caducidad = notification.ExpiryDate!;    // Ejemplo: "2812"
```

### Paso 2: Ejecutar el cargo periódico
Para cobrar la cuota mensual o anual desde un proceso en segundo plano (Job o Worker):

```csharp
var resultado = await redsysClient.ExecuteStoredCardChargeAsync(
    order: "2026112201",
    amount: 9.99m,
    cardToken: "tok_991823719",
    cofType: RedsysCofType.Recurring // Recurrente de suscripción
);

if (resultado.IsSuccess)
{
    Console.WriteLine($"Cuota cobrada con éxito. Autorización: {resultado.AuthorisationCode}");
}
else
{
    Console.WriteLine($"Error al cobrar cuota: {resultado.ResponseDescription} ({resultado.ResponseCode})");
}
```

Tipos de COF soportados en `RedsysCofType`:
* `Recurring`: Pagos periódicos de cuotas o suscripciones.
* `Installment`: Pago a plazos.
* `Unscheduled`: Pagos no programados por uso bajo demanda.
* `Industry`: Pagos específicos del sector (hoteles, alquileres).

---

## 4. Confirmación y Anulación de Preautorizaciones

Para comercios que retienen el saldo al inicio y confirman el cargo una vez enviado el pedido:

### Confirmar Preautorización:
```csharp
var resultado = await redsysClient.ConfirmPreauthorizationAsync(
    order: "2026102201",
    amount: 45.00m
);
```

### Cancelar Preautorización (liberar la retención):
```csharp
var resultado = await redsysClient.CancelPreauthorizationAsync(
    order: "2026102201",
    amount: 45.00m
);
```
