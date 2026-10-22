# Redsys InSite (Pago Embebido)

Redsys InSite permite integrar los campos de número de tarjeta, caducidad y CVV dentro de tu propio formulario de compra mediante un iframe seguro provisto por Redsys, evitando redirigir al comprador fuera de tu dominio.

---

## 1. Cómo funciona la arquitectura de InSite

```
1. Comprador rellena la tarjeta en el formulario embebido (redsys.js de Redsys)
2. redsys.js tokeniza los datos sensibles y devuelve un token temporal 'idOper'
3. Tu frontend envía el 'idOper' a tu API en ASP.NET Core
4. Tu API invoca redsysClient.ExecuteInsitePaymentAsync(order, amount, idOper)
5. Redsys procesa la autorización de forma segura y devuelve el resultado
```

De esta forma, los números de tarjeta nunca tocan tu servidor, manteniendo el cumplimiento normativo PCI-DSS SAQ A-EP.

---

## 2. Implementación en el Backend (ASP.NET Core)

En tu backend solo necesitas recibir el identificador de operación generado por el cliente (`idOper`) y ejecutar la llamada:

```csharp
app.MapPost("/api/checkout/insite", async (InsiteRequest request, IRedsysClient redsysClient) =>
{
    var resultado = await redsysClient.ExecuteInsitePaymentAsync(
        order: request.Order,
        amount: request.Amount,
        idOper: request.IdOper,
        registerCardOnFile: RedsysCofType.Recurring // Opcional: solicita tokenizar para pagos futuros
    );

    if (resultado.IsSuccess)
    {
        return Results.Ok(new
        {
            success = true,
            order = resultado.Order,
            autorizacion = resultado.AuthorisationCode,
            tokenTarjeta = resultado.MerchantIdentifier // Token guardado si se solicitó COF
        });
    }

    return Results.BadRequest(new
    {
        success = false,
        codigo = resultado.ResponseCode,
        error = resultado.ResponseDescription
    });
});

public record InsiteRequest(string Order, decimal Amount, string IdOper);
```

---

## 3. Implementación en el Frontend (JavaScript)

En tu página de checkout incluyes el script oficial de InSite provisto por el banco:

```html
<!-- Entorno de pruebas de Redsys -->
<script src="https://sis-t.redsys.es:25443/sis/NC/sandbox/redsysV3.js"></script>

<div id="card-form">
    <!-- El SDK inyecta los iframes de tarjeta, caducidad y CVV aquí -->
</div>

<button id="btnPagar">Pagar 49,95 €</button>

<script>
    document.getElementById('btnPagar').addEventListener('click', () => {
        // Solicita el token temporal a la librería de Redsys
        getInsiteOperationId((idOper, error) => {
            if (error) {
                alert('Error en los datos de la tarjeta: ' + error);
                return;
            }

            // Envía el token a tu backend
            fetch('/api/checkout/insite', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    order: '2026102201',
                    amount: 49.95,
                    idOper: idOper
                })
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    window.location.href = '/pedido-exitoso';
                } else {
                    alert('Pago denegado: ' + data.error);
                }
            });
        });
    });
</script>
```
