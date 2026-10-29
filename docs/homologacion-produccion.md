# Guía de Homologación Bancaria y Paso a Producción

Todos los bancos y cajas de ahorros en España (CaixaBank, Santander, BBVA, Banco Sabadell, Bankinter, Unicaja, Kutxabank, Cajamar, etc.) exigen superar una **batería de pruebas de homologación en el entorno SIS-T de Redsys** antes de activar el TPV Virtual en producción.

Esta guía detalla el proceso paso a paso para completar la homologación de forma rápida y sin incidencias.

---

## 1. El Proceso de Homologación en 4 Pasos

```
[1. Datos de Test]  -->  [2. Operaciones Obligatorias]  -->  [3. Notificar al Banco]  -->  [4. Activación en Producción]
El banco entrega         1 pago OK (0000)                El gestor bancario          El banco activa tu FUC real
FUC de pruebas           1 pago KO (0116)                revisa los logs del         y cambias las credenciales
y clave secreta          Webhook devuelve HTTP 200       servidor Redsys             en appsettings.json
```

---

## 2. Paso 1: Configurar el entorno de pruebas

En tu `appsettings.Development.json` (o mediante `options.UseTest`):

```json
{
  "Redsys": {
    "Environment": "Test",
    "Fuc": "999008881",
    "Terminal": "1",
    "SecretKey": "sq7HjrUOBfKmC576ILgskD5srU870gJ7",
    "MerchantUrl": "https://tudominio.com/api/pagos/webhook",
    "UrlOk": "https://tudominio.com/pedido-exitoso",
    "UrlKo": "https://tudominio.com/pedido-cancelado"
  }
}
```

> **Nota:** Puedes usar el FUC público de pruebas (`999008881`) o el FUC temporal que tu entidad bancaria te haya proporcionado en la carta de bienvenida.

---

## 3. Paso 2: Ejecutar las pruebas obligatorias del banco

El banco exige registrar al menos dos operaciones reales en la pasarela de test:

### Operación 1: Pago Autorizado (Código 0000)
1. Inicia una compra desde tu aplicación.
2. En la pantalla de Redsys introduce los datos de la tarjeta de prueba:
   * **Número de tarjeta:** `4548 8100 0000 0003` (o `RedsysTestCards.Authorized`)
   * **Caducidad:** Cualquier fecha futura (ej: `12/28`)
   * **CVV:** `123`
   * **Código SMS 3DS:** `1234`
3. Comprueba que:
   * El navegador aterriza en `UrlOk` (`/pedido-exitoso`).
   * Tu servidor recibe el webhook en `MerchantUrl` y responde con **HTTP 200 OK**.

### Operación 2: Pago Denegado (Código 0116 - Saldo insuficiente)
1. Inicia otra compra desde tu aplicación.
2. Introduce la tarjeta de saldo insuficiente:
   * **Número de tarjeta:** `4548 8100 0000 0116` (o `RedsysTestCards.InsufficientFunds`)
   * **Caducidad:** Fecha futura
   * **CVV:** `123`
3. Comprueba que:
   * El navegador aterriza en `UrlKo` (`/pedido-cancelado`).
   * Tu servidor recibe el webhook informando del rechazo (`notification.IsSuccess == false`, `notification.ResponseCode == "0116"`) y responde igualmente con **HTTP 200 OK**.

---

## 4. Tabla de Tarjetas Oficiales de Prueba (`RedsysTestCards`)

Todas estas constantes están disponibles directamente en la librería:

```csharp
using BetterRedsys.Testing;
```

| Tarjeta | Constante en C# | Resultado bancario | Código Redsys |
| :--- | :--- | :--- | :--- |
| `4548 8100 0000 0003` | `RedsysTestCards.Authorized` | Transacción autorizada | `0000` |
| `4548 8100 0000 0116` | `RedsysTestCards.InsufficientFunds` | Saldo insuficiente | `0116` |
| `4548 8100 0000 0180` | `RedsysTestCards.Expired` | Tarjeta caducada | `0180` |
| `4548 8100 0000 0184` | `RedsysTestCards.Authentication3DsError` | Fallo de autenticación 3DS | `0184` |
| `4548 8100 0000 0190` | `RedsysTestCards.IssuerDeclined` | Denegación genérica del emisor | `0190` |

* **PIN de SMS (OTP) de prueba:** `1234` (`RedsysTestCards.SmsCode`)
* **CVV de prueba:** `123` (`RedsysTestCards.DefaultCvv`)

---

## 5. Paso 3: Notificar al banco

Una vez realizadas las dos pruebas, envía un email a tu gestor del banco o al soporte técnico de Redsys con la siguiente plantilla:

```text
Asunto: Homologación TPV Virtual finalizada - FUC: [TU_FUC]

Estimados señores,

Hemos completado satisfactoriamente las pruebas de homologación en el entorno SIS-T para el comercio [NOMBRE_COMERCIO] con FUC [NUMERO_FUC]:

- Operación autorizada (0000): Pedido [NUMERO_PEDIDO_1] - Importe [X,XX €]
- Operación denegada (0116): Pedido [NUMERO_PEDIDO_2] - Importe [X,XX €]

En ambas operaciones la notificación online (MerchantURL) ha sido recibida y validada criptográficamente con éxito por nuestro servidor.

Solicitamos la activación del TPV y el envío de la clave secreta SHA-256 de producción.

Atentamente,
[Tu Nombre]
```

---

## 6. Paso 4: Activación en Producción

Cuando el banco confirme el paso a producción, te entregará:
* Tu **FUC definitivo** de producción.
* Tu número de **Terminal** (habitualmente `"1"` o `"001"`).
* Tu **Clave Secreta HMAC-SHA256 definitiva**.

Simplemente actualiza tu archivo `appsettings.json` o tus variables de entorno / Azure Key Vault:

```json
{
  "Redsys": {
    "Environment": "Production",
    "Fuc": "123456789",
    "Terminal": "1",
    "SecretKey": "CLAVE_REAL_ENTREGADA_POR_EL_BANCO",
    "MerchantUrl": "https://tutienda.com/api/pagos/webhook",
    "UrlOk": "https://tutienda.com/pedido-exitoso",
    "UrlKo": "https://tutienda.com/pedido-cancelado"
  }
}
```

No necesitas cambiar nada más en el código de tu aplicación. BetterRedsys cambiará automáticamente al dominio seguro de producción de Redsys (`https://sis.redsys.es/sis/realizarPago`).
