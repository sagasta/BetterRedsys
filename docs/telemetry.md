# Telemetría, Salud y Auditoría

BetterRedsys incluye instrumentación nativa de observabilidad basada en los estándares de .NET y OpenTelemetry (`System.Diagnostics.DiagnosticSource`), sin dependencias externas obligatorias.

---

## 1. Health Checks de ASP.NET Core

Permite a Kubernetes, Docker, Azure o balanceadores de carga monitorizar la conectividad con la pasarela bancaria.

### Registro:
```csharp
builder.Services.AddBetterRedsys(options => { ... })
    .AddHealthCheck(); // Añade el RedsysHealthCheck

var app = builder.Build();

app.MapHealthChecks("/health");
```

### Respuesta de `/health`:
* **Healthy (`200 OK`)**: Conexión establecida con éxito con el host oficial de Redsys.
* **Unhealthy (`503 Service Unavailable`)**: Caída de red o servidor bancario no accesible.

---

## 2. Trazas distribuidas con OpenTelemetry (`ActivitySource`)

BetterRedsys emite spans de actividad bajo la fuente `"BetterRedsys"` para monitorizar el ciclo de vida de los pagos en herramientas como Jaeger, Grafana Tempo, Zipkin o Application Insights.

### Configuración de OpenTelemetry en `Program.cs`:
```csharp
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("BetterRedsys") // Activa las trazas de BetterRedsys
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });
```

### Spans y etiquetas generadas:
* `Redsys.CreatePayment`: Creación del formulario de pago (tags: `payment.order`, `payment.amount`, `redsys.is_bizum`).
* `Redsys.VerifyWebhook`: Notificación recibida (tags: `payment.order`, `redsys.response_code`, `redsys.is_success`, `redsys.auth_code`).
* `Redsys.S2S.Operation`: Operaciones REST directas (tags: `redsys.operation_type`, `redsys.duration_ms`).

---

## 3. Métricas en tiempo real (`Meter`)

BetterRedsys publica métricas a través de `System.Diagnostics.Metrics` bajo el medidor `"BetterRedsys"` para Prometheus, Grafana o Datadog:

| Métrica | Tipo | Descripción |
| :--- | :--- | :--- |
| `redsys.payments.created` | Contador | Total de formularios de pago generados. |
| `redsys.webhooks.received` | Contador | Total de webhooks recibidos (etiquetados por `status=authorized` o `status=declined`). |
| `redsys.s2s.operations` | Contador | Total de operaciones REST servidor a servidor ejecutadas. |
| `redsys.s2s.duration` | Histograma | Latencia en milisegundos de las llamadas bancarias REST. |

### Configuración con Prometheus / OpenTelemetry:
```csharp
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("BetterRedsys")
            .AddPrometheusExporter();
    });
```

---

## 4. Auditoría y Logs Estructurados

La librería emite registros detallados a través de `ILogger` utilizando plantillas estructuradas de alto rendimiento:

* **Pagos autorizados:** `[Information]` con pedido, importe y código de autorización.
* **Pagos denegados:** `[Information]` con motivo legible y código bancario.
* **Manipulación o firmas inválidas:** `[Warning]` con alerta de seguridad de parámetros alterados o clave errónea.
