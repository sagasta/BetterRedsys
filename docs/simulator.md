# Simulador de Pruebas Local (`BetterRedsys.Simulator`)

`BetterRedsys.Simulator` es un paquete NuGet complementario diseñado para el desarrollo y pruebas locales.

Permite probar el flujo de compra completo, verificar firmas criptográficas y simular webhooks en segundo plano sin necesidad de túneles como `ngrok`, sin exponer tu equipo a internet y sin depender de la pasarela de test de Redsys.

---

## 1. Instalación

En tu proyecto ASP.NET Core:

```bash
dotnet add package BetterRedsys.Simulator
```

---

## 2. Configuración en `Program.cs`

El simulador se activa condicionalmente solo en entorno de desarrollo (`IsDevelopment()`):

```csharp
using BetterRedsys.Configuration;
using BetterRedsys.Simulator;

var builder = WebApplication.CreateBuilder(args);

var redsys = builder.Services.AddBetterRedsys(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseTest(
            fuc: "999008881",
            terminal: "1",
            secretKey: "sq7HjrUOBfKmC576ILgskD5srURiUz6U"
        );
    }
    else
    {
        options.UseProduction(
            fuc: builder.Configuration["Redsys:Fuc"]!,
            terminal: builder.Configuration["Redsys:Terminal"]!,
            secretKey: builder.Configuration["Redsys:SecretKey"]!
        );
    }

    options.WithEuros()
           .WithWebhooks(
               notificationUrl: "/api/pagos/webhook",
               okUrl: "/pedido-exitoso",
               koUrl: "/pedido-cancelado"
           );
});

// Registrar servicios del simulador exclusivamente en desarrollo
if (builder.Environment.IsDevelopment())
{
    redsys.AddSimulator();
}

var app = builder.Build();

// Activar el middleware del simulador
if (app.Environment.IsDevelopment())
{
    app.UseRedsysSimulator();
}

app.Run();
```

---

## 3. ¿Qué simula?

### A. Pagos por Redirección y Bizum
Cuando tu aplicación redirige al usuario para pagar, el middleware intercepta la petición y muestra la pasarela virtual local:
* **Interfaz auténtica:** Aspecto fiel al TPV Virtual real de Redsys (selector de idioma oficial, marcas de tarjeta, importes y resumen de compra).
* **Tarjeta y Bizum:** Permite probar pagos con tarjeta bancaria o mediante Bizum introduciendo número de teléfono.
* **Resultados simulables:** Puedes forzar autorizaciones (`0000`), saldo insuficiente (`0116`), tarjeta caducada (`0180`), fallo 3D Secure (`0184`), o cancelación voluntaria (`9915`).
* **Despacho de Webhooks firmado:** Al pulsar en *Pagar*, el simulador calcula la firma HMAC-SHA256 auténtica con tu clave secreta y realiza un HTTP POST a tu endpoint de webhook en segundo plano.

### B. Operaciones REST Servidor a Servidor (`trataPeticionREST`)
El simulador también intercepta las llamadas REST directas:
* Consultas de pedidos online (Tipo `J`).
* Devoluciones de importes.
* Cobros con tarjeta almacenada (Card-On-File).
* Cobros embebidos con Redsys InSite (`idOper`).
* Confirmación y anulación de preautorizaciones.

---

## 4. Rutas y Opciones Personalizadas

Por defecto, el simulador escucha en `/redsys-simulator/pay`. Si deseas modificar la ruta:

```csharp
if (builder.Environment.IsDevelopment())
{
    redsysBuilder.AddSimulator(options =>
    {
        options.Path = "/tpv-pruebas";
    });
}
```
