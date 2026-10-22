async function simulatePayment() {
    const btn = document.getElementById('btnSimulate');
    const statusBox = document.getElementById('statusBox');
    const statusHeader = document.getElementById('statusHeader');
    const statusDetail = document.getElementById('statusDetail');
    const redirectActions = document.getElementById('redirectActions');
    const btnRedirect = document.getElementById('btnRedirect');

    btn.disabled = true;
    btn.innerText = '⏳ Signing & dispatching webhook / Firmando webhook...';
    statusBox.style.display = 'block';
    redirectActions.style.display = 'none';

    const baseData = JSON.parse(document.getElementById('simulatorData').textContent);
    const payload = {
        ...baseData,
        responseCode: document.getElementById('selectResponse').value,
        processedPayMethod: document.getElementById('selectMethod').value,
        authorisationCode: document.getElementById('authCode').value
    };

    const dispatchUrl = document.body.dataset.dispatchUrl;

    try {
        const res = await fetch(dispatchUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        btn.disabled = false;
        btn.innerText = '🚀 Simulate again / Simular de nuevo';

        if (res.ok && data.success) {
            const isHttpOk = data.webhookStatus >= 200 && data.webhookStatus < 300;
            statusHeader.innerHTML = isHttpOk
                ? '<span class="status-badge status-200">HTTP ' + data.webhookStatus + ' OK</span> Webhook processed successfully / Webhook procesado con éxito'
                : '<span class="status-badge status-err">HTTP ' + data.webhookStatus + '</span> Webhook returned error / El webhook devolvió error';

            statusDetail.textContent = 'Endpoint Response: ' + (data.webhookResponse || '(Empty body)') + ' | Signature: Verified HMAC-SHA256.';

            redirectActions.style.display = 'block';
            btnRedirect.href = data.redirectUrl;
            btnRedirect.innerText = 'Navigate to ' + (payload.responseCode === '0000' ? 'URL OK' : 'URL KO') + ' (' + data.redirectUrl + ')';
        } else {
            statusHeader.innerHTML = '<span class="status-badge status-err">ERROR</span> Simulation error';
            statusDetail.textContent = data.error || 'Unknown error';
        }
    } catch (err) {
        btn.disabled = false;
        btn.innerText = '🚀 Retry simulation / Reintentar';
        statusHeader.innerHTML = '<span class="status-badge status-err">NETWORK ERROR</span>';
        statusDetail.textContent = err.message;
    }
}
