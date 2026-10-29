namespace BetterRedsys.SampleApi;

/// <summary>
/// Record storing in-memory payment status updated by the webhook.
/// </summary>
public sealed record PaymentRecord(
    string Order,
    string Amount,
    string ResponseCode,
    string Description,
    bool IsSuccess,
    bool IsBizum,
    string? AuthorisationCode,
    DateTime ReceivedAt);

/// <summary>
/// HTML pages rendering the sample e-commerce checkout flow.
/// </summary>
public static class StorePages
{
    public static string RenderStorefront(string order, decimal amount)
    {
        var displayAmount = amount.ToString("N2") + " €";
        return $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>NovaTech Store • Finalizar Compra</title>
            <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                    background-color: #f3f5f8;
                    color: #1f2937;
                    min-height: 100vh;
                    display: flex;
                    flex-direction: column;
                }
                .navbar {
                    background: #ffffff;
                    border-bottom: 1px solid #e5e7eb;
                    padding: 16px 24px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.04);
                }
                .nav-container {
                    max-width: 960px;
                    margin: 0 auto;
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                }
                .brand {
                    font-size: 1.35rem;
                    font-weight: 800;
                    color: #0f172a;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    text-decoration: none;
                }
                .brand span { color: #2563eb; }
                .badge-demo {
                    background: #eff6ff;
                    color: #1d4ed8;
                    border: 1px solid #bfdbfe;
                    font-size: 0.75rem;
                    font-weight: 700;
                    padding: 4px 10px;
                    border-radius: 9999px;
                }
                .main-content {
                    max-width: 960px;
                    width: 100%;
                    margin: 32px auto;
                    padding: 0 20px;
                    flex: 1;
                }
                .page-title {
                    font-size: 1.5rem;
                    font-weight: 800;
                    margin-bottom: 24px;
                    color: #0f172a;
                }
                .checkout-grid {
                    display: grid;
                    grid-template-columns: 1fr 380px;
                    gap: 32px;
                }
                @media (max-width: 820px) {
                    .checkout-grid { grid-template-columns: 1fr; }
                }
                .card {
                    background: #ffffff;
                    border: 1px solid #e5e7eb;
                    border-radius: 10px;
                    padding: 24px;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.03);
                }
                .card-title {
                    font-size: 1.05rem;
                    font-weight: 700;
                    margin-bottom: 18px;
                    color: #1e293b;
                    border-bottom: 1px solid #f1f5f9;
                    padding-bottom: 12px;
                }
                .form-group {
                    margin-bottom: 16px;
                }
                .form-row {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 16px;
                }
                label {
                    display: block;
                    font-size: 0.82rem;
                    font-weight: 600;
                    color: #475569;
                    margin-bottom: 6px;
                }
                input {
                    width: 100%;
                    padding: 10px 12px;
                    border: 1px solid #cbd5e1;
                    border-radius: 6px;
                    font-size: 0.9rem;
                    outline: none;
                    background: #f8fafc;
                }
                input:focus {
                    border-color: #2563eb;
                    background: #ffffff;
                }
                .product-item {
                    display: flex;
                    align-items: center;
                    gap: 16px;
                    margin-bottom: 20px;
                    padding-bottom: 20px;
                    border-bottom: 1px solid #f1f5f9;
                }
                .product-icon {
                    width: 60px;
                    height: 60px;
                    background: #f1f5f9;
                    border-radius: 8px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 1.8rem;
                }
                .product-info { flex: 1; }
                .product-name { font-weight: 700; font-size: 0.95rem; color: #1e293b; margin-bottom: 4px; }
                .product-qty { font-size: 0.8rem; color: #64748b; }
                .product-price { font-weight: 800; font-size: 1.05rem; color: #0f172a; }
                .summary-line {
                    display: flex;
                    justify-content: space-between;
                    font-size: 0.88rem;
                    color: #64748b;
                    margin-bottom: 10px;
                }
                .summary-total {
                    display: flex;
                    justify-content: space-between;
                    font-size: 1.25rem;
                    font-weight: 800;
                    color: #0f172a;
                    border-top: 2px solid #f1f5f9;
                    padding-top: 14px;
                    margin-top: 14px;
                }
                .btn-submit {
                    width: 100%;
                    background: #ba0c2f;
                    color: #ffffff;
                    border: none;
                    border-radius: 8px;
                    padding: 14px;
                    font-size: 1.05rem;
                    font-weight: 700;
                    cursor: pointer;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 10px;
                    margin-top: 24px;
                    transition: background 0.15s, transform 0.1s;
                    box-shadow: 0 4px 12px rgba(186, 12, 47, 0.25);
                }
                .btn-submit:hover { background: #9e0a27; }
                .btn-submit:active { transform: translateY(1px); }
                .gateway-notice {
                    margin-top: 12px;
                    font-size: 0.78rem;
                    color: #64748b;
                    text-align: center;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 6px;
                }
                .footer {
                    margin-top: auto;
                    padding: 24px;
                    text-align: center;
                    font-size: 0.8rem;
                    color: #94a3b8;
                    border-top: 1px solid #e5e7eb;
                    background: #ffffff;
                }
            </style>
        </head>
        <body>
            <header class="navbar">
                <div class="nav-container">
                    <a class="brand" href="/">Nova<span>Tech</span> Store</a>
                    <span class="badge-demo">Demo BetterRedsys</span>
                </div>
            </header>

            <main class="main-content">
                <h1 class="page-title">Finalizar Pedido y Pago</h1>

                <form method="POST" action="/checkout/pagar" class="checkout-grid">
                    <input type="hidden" name="order" value="{{order}}" />
                    <input type="hidden" name="amount" value="{{amount}}" />

                    <!-- Left: Customer & Shipping Details -->
                    <div class="card">
                        <div class="card-title">1. Datos de Contacto y Envío</div>

                        <div class="form-row">
                            <div class="form-group">
                                <label for="customerName">Nombre y Apellidos</label>
                                <input id="customerName" name="customerName" value="Alejandro Gómez" required />
                            </div>
                            <div class="form-group">
                                <label for="customerEmail">Correo Electrónico</label>
                                <input id="customerEmail" type="email" name="customerEmail" value="alejandro.gomez@ejemplo.com" required />
                            </div>
                        </div>

                        <div class="form-row">
                            <div class="form-group">
                                <label for="customerPhone">Teléfono Móvil</label>
                                <input id="customerPhone" type="tel" name="customerPhone" value="+34 612 345 678" required />
                            </div>
                            <div class="form-group">
                                <label for="orderId">Número de Pedido generado</label>
                                <input id="orderId" value="{{order}}" readonly style="background:#e2e8f0; font-family:monospace; font-weight:700;" />
                            </div>
                        </div>

                        <div class="form-group">
                            <label for="customerAddress">Dirección de Entrega</label>
                            <input id="customerAddress" name="customerAddress" value="Calle Gran Vía 42, 4º B, 28013 Madrid" required />
                        </div>
                    </div>

                    <!-- Right: Order Summary & Checkout Button -->
                    <div class="card">
                        <div class="card-title">2. Resumen del Carrito</div>

                        <div class="product-item">
                            <div class="product-icon">🎧</div>
                            <div class="product-info">
                                <div class="product-name">Auriculares Inalámbricos Pro ANC</div>
                                <div class="product-qty">Cantidad: 1 &bull; Color: Negro Espacial</div>
                            </div>
                            <div class="product-price">{{displayAmount}}</div>
                        </div>

                        <div class="summary-line">
                            <span>Subtotal</span>
                            <span>{{displayAmount}}</span>
                        </div>
                        <div class="summary-line">
                            <span>Envío urgente 24h</span>
                            <span style="color:#059669; font-weight:600;">GRATIS</span>
                        </div>
                        <div class="summary-total">
                            <span>Total</span>
                            <span style="color:#ba0c2f;">{{displayAmount}}</span>
                        </div>

                        <button type="submit" class="btn-submit">
                            <span>Pagar {{displayAmount}}</span>
                        </button>

                        <div class="gateway-notice">
                            <span>🔒 Pago seguro mediante Redsys TPV (Tarjeta o Bizum)</span>
                        </div>
                    </div>
                </form>
            </main>

            <footer class="footer">
                NovaTech Store &bull; Entorno de demostración con BetterRedsys
            </footer>
        </body>
        </html>
        """;
    }

    public static string RenderSuccess(string order, PaymentRecord? record)
    {
        var methodText = (record?.IsBizum ?? false) ? "Bizum" : "Tarjeta bancaria";
        var amountText = record?.Amount ?? "49,95 €";
        var authCode = record?.AuthorisationCode ?? "999888";
        var receivedTime = record?.ReceivedAt.ToString("HH:mm:ss") ?? DateTime.Now.ToString("HH:mm:ss");

        return $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>¡Pago Completado! • NovaTech Store</title>
            <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                    background-color: #f3f5f8;
                    color: #1f2937;
                    min-height: 100vh;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    padding: 40px 20px;
                }
                .success-card {
                    background: #ffffff;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.05);
                    max-width: 580px;
                    width: 100%;
                    padding: 40px 32px;
                    text-align: center;
                }
                .icon-circle {
                    width: 64px;
                    height: 64px;
                    background: #ecfdf5;
                    border: 2px solid #a7f3d0;
                    border-radius: 50%;
                    color: #059669;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 2rem;
                    margin: 0 auto 20px;
                }
                h1 {
                    font-size: 1.55rem;
                    font-weight: 800;
                    color: #0f172a;
                    margin-bottom: 8px;
                }
                p {
                    color: #64748b;
                    font-size: 0.95rem;
                    line-height: 1.5;
                    margin-bottom: 24px;
                }
                .receipt-table {
                    background: #f8fafc;
                    border: 1px solid #e2e8f0;
                    border-radius: 8px;
                    padding: 16px;
                    margin-bottom: 28px;
                    text-align: left;
                    font-size: 0.88rem;
                }
                .receipt-row {
                    display: flex;
                    justify-content: space-between;
                    padding: 8px 0;
                    border-bottom: 1px solid #f1f5f9;
                }
                .receipt-row:last-child { border-bottom: none; }
                .receipt-label { color: #64748b; }
                .receipt-val { font-weight: 600; color: #1e293b; }
                .btn-back {
                    display: inline-block;
                    background: #0f172a;
                    color: #ffffff;
                    text-decoration: none;
                    padding: 12px 28px;
                    border-radius: 6px;
                    font-weight: 700;
                    font-size: 0.95rem;
                    transition: background 0.15s;
                }
                .btn-back:hover { background: #1e293b; }
            </style>
        </head>
        <body>
            <div class="success-card">
                <div class="icon-circle">✓</div>
                <h1>¡Pago Confirmado con Éxito!</h1>
                <p>Tu banco ha autorizado la transacción y el pedido ha quedado registrado correctamente.</p>

                <div class="receipt-table">
                    <div class="receipt-row">
                        <span class="receipt-label">Número de Pedido</span>
                        <span class="receipt-val">{{order}}</span>
                    </div>
                    <div class="receipt-row">
                        <span class="receipt-label">Importe Pagado</span>
                        <span class="receipt-val" style="color:#ba0c2f;">{{amountText}}</span>
                    </div>
                    <div class="receipt-row">
                        <span class="receipt-label">Método Empleado</span>
                        <span class="receipt-val">{{methodText}}</span>
                    </div>
                    <div class="receipt-row">
                        <span class="receipt-label">Código de Autorización</span>
                        <span class="receipt-val">{{authCode}}</span>
                    </div>
                    <div class="receipt-row">
                        <span class="receipt-label">Notificación Webhook S2S</span>
                        <span class="receipt-val" style="color:#059669;">Validada HMAC-SHA256 ({{receivedTime}})</span>
                    </div>
                </div>

                <a class="btn-back" href="/">Realizar otro pedido de prueba</a>
            </div>
        </body>
        </html>
        """;
    }

    public static string RenderCancel(string order, PaymentRecord? record)
    {
        var reason = record?.Description ?? "Transacción cancelada o rechazada";
        var code = record?.ResponseCode ?? "9915";

        return $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Pago No Completado • NovaTech Store</title>
            <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                    background-color: #f3f5f8;
                    color: #1f2937;
                    min-height: 100vh;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    padding: 40px 20px;
                }
                .cancel-card {
                    background: #ffffff;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.05);
                    max-width: 580px;
                    width: 100%;
                    padding: 40px 32px;
                    text-align: center;
                }
                .icon-circle {
                    width: 64px;
                    height: 64px;
                    background: #fef2f2;
                    border: 2px solid #fecaca;
                    border-radius: 50%;
                    color: #dc2626;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 2rem;
                    margin: 0 auto 20px;
                }
                h1 {
                    font-size: 1.55rem;
                    font-weight: 800;
                    color: #0f172a;
                    margin-bottom: 8px;
                }
                p {
                    color: #64748b;
                    font-size: 0.95rem;
                    line-height: 1.5;
                    margin-bottom: 24px;
                }
                .details-box {
                    background: #fffbeb;
                    border: 1px solid #fef3c7;
                    border-radius: 8px;
                    padding: 16px;
                    margin-bottom: 28px;
                    color: #92400e;
                    font-size: 0.88rem;
                    line-height: 1.5;
                }
                .btn-retry {
                    display: inline-block;
                    background: #ba0c2f;
                    color: #ffffff;
                    text-decoration: none;
                    padding: 12px 28px;
                    border-radius: 6px;
                    font-weight: 700;
                    font-size: 0.95rem;
                    transition: background 0.15s;
                }
                .btn-retry:hover { background: #9e0a27; }
            </style>
        </head>
        <body>
            <div class="cancel-card">
                <div class="icon-circle">✕</div>
                <h1>Pago No Completado</h1>
                <p>La operación bancaria no ha podido finalizarse para el pedido #<strong>{{order}}</strong>.</p>

                <div class="details-box">
                    <strong>Motivo informado:</strong> {{reason}} (Código Redsys: {{code}})<br/>
                    No se ha realizado ningún cobro en tu cuenta.
                </div>

                <a class="btn-retry" href="/">Volver a la tienda e intentar de nuevo</a>
            </div>
        </body>
        </html>
        """;
    }
}
