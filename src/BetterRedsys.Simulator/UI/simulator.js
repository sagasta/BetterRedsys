let currentMethod = 'T';
let currentLang = '001';

const translations = {
    '001': { // Español / Castellano
        summaryHeading: 'Detalle de la compra',
        amountLabel: 'Importe a pagar',
        orderLabel: 'Número de Pedido',
        merchantLabel: 'Comercio (FUC)',
        descLabel: 'Concepto',
        dateLabel: 'Fecha y hora',
        techDetails: 'Parámetros técnicos de integración',
        cardTab: 'Tarjeta bancaria',
        cardNumberLabel: 'Número de tarjeta',
        tryLabel: 'Probar:',
        expiryLabel: 'Caducidad',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Pagar ',
        cancelBtn: 'Cancelar y volver a la tienda',
        bizumTitle: 'Pago seguro con Bizum',
        bizumDesc: 'Introduce tu número de teléfono y tu clave Bizum de 4 dígitos.',
        phoneLabel: 'Teléfono móvil',
        pinLabel: 'Clave Bizum (4 dígitos)',
        connectingTitle: 'Conectando con la entidad emisora...',
        connectingDesc: 'Por favor, no cierre ni refresque esta ventana mientras se procesa la transacción bancaria.',
        successTitle: 'Pago completado con éxito',
        successDesc: 'La transacción ha sido autorizada por su entidad bancaria.',
        declinedTitle: 'Operación no autorizada',
        declinedDesc: 'La entidad bancaria ha denegado la transacción.',
        redirectBtn: 'Volver al comercio inmediatamente'
    },
    '002': { // English
        summaryHeading: 'Order details',
        amountLabel: 'Amount to pay',
        orderLabel: 'Order Number',
        merchantLabel: 'Merchant (FUC)',
        descLabel: 'Description',
        dateLabel: 'Date and time',
        techDetails: 'Technical integration parameters',
        cardTab: 'Credit / Debit Card',
        cardNumberLabel: 'Card number',
        tryLabel: 'Quick test:',
        expiryLabel: 'Expiration',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Pay ',
        cancelBtn: 'Cancel and return to store',
        bizumTitle: 'Secure payment with Bizum',
        bizumDesc: 'Enter your mobile phone number and your 4-digit Bizum PIN.',
        phoneLabel: 'Mobile phone',
        pinLabel: 'Bizum PIN (4 digits)',
        connectingTitle: 'Connecting to issuing bank...',
        connectingDesc: 'Please do not close or refresh this window while the transaction is being processed.',
        successTitle: 'Payment completed successfully',
        successDesc: 'The transaction has been approved by your financial institution.',
        declinedTitle: 'Transaction declined',
        declinedDesc: 'The financial institution has declined the transaction.',
        redirectBtn: 'Return to merchant now'
    },
    '003': { // Català
        summaryHeading: 'Detall de la compra',
        amountLabel: 'Import a pagar',
        orderLabel: 'Número de Comanda',
        merchantLabel: 'Comerç (FUC)',
        descLabel: 'Concepte',
        dateLabel: 'Data i hora',
        techDetails: 'Paràmetres tècnics d\'integració',
        cardTab: 'Targeta bancària',
        cardNumberLabel: 'Número de targeta',
        tryLabel: 'Provar:',
        expiryLabel: 'Caducitat',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Pagar ',
        cancelBtn: 'Cancel·lar i tornar a la botiga',
        bizumTitle: 'Pagament segur amb Bizum',
        bizumDesc: 'Introdueix el teu número de telèfon i la teva clau Bizum de 4 dígits.',
        phoneLabel: 'Telèfon mòbil',
        pinLabel: 'Clau Bizum (4 dígits)',
        connectingTitle: 'Connectant amb l\'entitat emissora...',
        connectingDesc: 'Si us plau, no tanqueu ni recarregueu aquesta finestra mentre es processa la transacció.',
        successTitle: 'Pagament completat amb èxit',
        successDesc: 'La transacció ha estat autoritzada per la vostra entitat bancària.',
        declinedTitle: 'Operació no autoritzada',
        declinedDesc: 'L\'entitat bancària ha denegat la transacció.',
        redirectBtn: 'Tornar al comerç immediatament'
    },
    '004': { // Galego
        summaryHeading: 'Detalle da compra',
        amountLabel: 'Importe a pagar',
        orderLabel: 'Número de Pedido',
        merchantLabel: 'Comercio (FUC)',
        descLabel: 'Concepto',
        dateLabel: 'Data e hora',
        techDetails: 'Parámetros técnicos de integración',
        cardTab: 'Tarxeta bancaria',
        cardNumberLabel: 'Número de tarxeta',
        tryLabel: 'Probar:',
        expiryLabel: 'Caducidade',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Pagar ',
        cancelBtn: 'Cancelar e volver á tenda',
        bizumTitle: 'Pago seguro con Bizum',
        bizumDesc: 'Introduce o teu número de teléfono e a túa clave Bizum de 4 díxitos.',
        phoneLabel: 'Teléfono móbil',
        pinLabel: 'Clave Bizum (4 díxitos)',
        connectingTitle: 'Conectando coa entidade emisora...',
        connectingDesc: 'Por favor, non peche nin actualice esta ventá mentres se procesa a transacción.',
        successTitle: 'Pago completado con éxito',
        successDesc: 'A transacción foi autorizada pola súa entidade bancaria.',
        declinedTitle: 'Operación non autorizada',
        declinedDesc: 'A entidade bancaria denegou a transacción.',
        redirectBtn: 'Volver ao comercio inmediatamente'
    },
    '013': { // Euskara
        summaryHeading: 'Erosketaren xehetasunak',
        amountLabel: 'Ordaindu beharreko zenbatekoa',
        orderLabel: 'Eskaera-zenbakia',
        merchantLabel: 'Saltokia (FUC)',
        descLabel: 'Kontzeptua',
        dateLabel: 'Data eta ordua',
        techDetails: 'Integrazio-parametro teknikoak',
        cardTab: 'Banku-txartela',
        cardNumberLabel: 'Txartelaren zenbakia',
        tryLabel: 'Probatu:',
        expiryLabel: 'Iraungipena',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Ordaindu ',
        cancelBtn: 'Ezeztatu eta dendara itzuli',
        bizumTitle: 'Ordainketa segurua Bizum bidez',
        bizumDesc: 'Sartu zure telefono zenbakia eta 4 digituko Bizum pasahitza.',
        phoneLabel: 'Telefono mugikorra',
        pinLabel: 'Bizum pasahitza (4 digitu)',
        connectingTitle: 'Erakunde jaulkitzailearekin konektatzen...',
        connectingDesc: 'Mesedez, ez itxi leihoa transakzioa prozesatzen ari den bitartean.',
        successTitle: 'Ordainketa arrakastatsua',
        successDesc: 'Banku-erakundeak transakzioa baimendu du.',
        declinedTitle: 'Baimendu gabeko eragiketa',
        declinedDesc: 'Banku-erakundeak ez du baimendu eragiketa.',
        redirectBtn: 'Itzuli saltokira'
    },
    '005': { // Français
        summaryHeading: 'Détail de la commande',
        amountLabel: 'Montant à payer',
        orderLabel: 'Numéro de Commande',
        merchantLabel: 'Commerçant (FUC)',
        descLabel: 'Description',
        dateLabel: 'Date et heure',
        techDetails: 'Paramètres techniques d\'intégration',
        cardTab: 'Carte bancaire',
        cardNumberLabel: 'Numéro de carte',
        tryLabel: 'Tester:',
        expiryLabel: 'Date d\'expiration',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Payer ',
        cancelBtn: 'Annuler et retourner à la boutique',
        bizumTitle: 'Paiement sécurisé par Bizum',
        bizumDesc: 'Entrez votre numéro de téléphone et votre code Bizum à 4 chiffres.',
        phoneLabel: 'Téléphone mobile',
        pinLabel: 'Code Bizum (4 chiffres)',
        connectingTitle: 'Connexion à l\'établissement émetteur...',
        connectingDesc: 'Veuillez ne pas fermer cette fenêtre pendant le traitement.',
        successTitle: 'Paiement effectué avec succès',
        successDesc: 'La transaction a été autorisée par votre banque.',
        declinedTitle: 'Opération refusée',
        declinedDesc: 'L\'établissement bancaire a refusé la transaction.',
        redirectBtn: 'Retourner immédiatement au marchand'
    },
    '006': { // Deutsch
        summaryHeading: 'Bestelldetails',
        amountLabel: 'Zu zahlender Betrag',
        orderLabel: 'Bestellnummer',
        merchantLabel: 'Händler (FUC)',
        descLabel: 'Beschreibung',
        dateLabel: 'Datum und Uhrzeit',
        techDetails: 'Technische Integrationsparameter',
        cardTab: 'Kredit- / Debitkarte',
        cardNumberLabel: 'Kartennummer',
        tryLabel: 'Testen:',
        expiryLabel: 'Gültig bis',
        cvvLabel: 'CVV / CVC',
        payBtnPrefix: 'Bezahlen ',
        cancelBtn: 'Abbrechen und zum Shop zurückkehren',
        bizumTitle: 'Sichere Zahlung mit Bizum',
        bizumDesc: 'Geben Sie Ihre Handynummer und Ihre 4-stellige Bizum-PIN ein.',
        phoneLabel: 'Mobiltelefon',
        pinLabel: 'Bizum-PIN (4 Ziffern)',
        connectingTitle: 'Verbindung zur kartenausgebenden Bank...',
        connectingDesc: 'Bitte schließen oder aktualisieren Sie dieses Fenster nicht.',
        successTitle: 'Zahlung erfolgreich',
        successDesc: 'Die Transaktion wurde von Ihrer Bank autorisiert.',
        declinedTitle: 'Transaktion abgelehnt',
        declinedDesc: 'Ihre Bank hat die Transaktion abgelehnt.',
        redirectBtn: 'Sofort zum Händler zurückkehren'
    }
};

document.addEventListener('DOMContentLoaded', () => {
    // Initialize date & time
    const dtElem = document.getElementById('currentDateTime');
    if (dtElem) {
        const now = new Date();
        dtElem.textContent = now.toLocaleDateString('es-ES', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        }) + ' ' + now.toLocaleTimeString('es-ES', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    // Initialize Language selector based on initial data
    const langSel = document.getElementById('langSelector');
    if (langSel && langSel.dataset.initialLang) {
        const initial = langSel.dataset.initialLang;
        if (translations[initial]) {
            langSel.value = initial;
            changeLanguage(initial);
        }
    }

    // Format credit card input with spaces
    const cardInput = document.getElementById('cardNumber');
    if (cardInput) {
        cardInput.addEventListener('input', (e) => {
            let val = e.target.value.replace(/\D/g, '');
            let formatted = '';
            for (let i = 0; i < val.length; i++) {
                if (i > 0 && i % 4 === 0) formatted += ' ';
                formatted += val[i];
            }
            e.target.value = formatted;
        });
    }

    // Format expiry input with slash
    const expiryInput = document.getElementById('cardExpiry');
    if (expiryInput) {
        expiryInput.addEventListener('input', (e) => {
            let val = e.target.value.replace(/\D/g, '');
            if (val.length >= 2) {
                e.target.value = val.substring(0, 2) + '/' + val.substring(2, 4);
            } else {
                e.target.value = val;
            }
        });
    }
});

function changeLanguage(langCode) {
    currentLang = langCode;
    const t = translations[langCode] || translations['001'];
    const amountVal = document.getElementById('valAmount')?.textContent || '';

    const setTxt = (id, txt) => {
        const elem = document.getElementById(id);
        if (elem) elem.textContent = txt;
    };

    setTxt('lblSummaryHeading', t.summaryHeading);
    setTxt('lblAmount', t.amountLabel);
    setTxt('lblOrder', t.orderLabel);
    setTxt('lblMerchant', t.merchantLabel);
    setTxt('lblConcept', t.descLabel);
    setTxt('lblDateTime', t.dateLabel);
    setTxt('lblTechDetails', t.techDetails);
    setTxt('lblTabCard', t.cardTab);
    setTxt('lblCardNumber', t.cardNumberLabel);
    setTxt('lblTry', t.tryLabel);
    setTxt('lblCardExpiry', t.expiryLabel);
    setTxt('lblCardCvv', t.cvvLabel);
    setTxt('lblBtnPay', t.payBtnPrefix + amountVal);
    setTxt('lblBtnCancel', t.cancelBtn);
    setTxt('lblBizumTitle', t.bizumTitle);
    setTxt('lblBizumDesc', t.bizumDesc);
    setTxt('lblBizumPhone', t.phoneLabel);
    setTxt('lblBizumPin', t.pinLabel);
}

function switchMethod(method) {
    currentMethod = method;
    const tabCard = document.getElementById('tabCard');
    const tabBizum = document.getElementById('tabBizum');
    const cardSec = document.getElementById('cardSection');
    const bizumSec = document.getElementById('bizumSection');
    const btnPay = document.getElementById('btnPay');

    if (method === 'z') {
        tabCard.classList.remove('active');
        tabBizum.classList.add('active');
        cardSec.classList.remove('active');
        bizumSec.classList.add('active');
        btnPay.classList.add('bizum-btn');
    } else {
        tabBizum.classList.remove('active');
        tabCard.classList.add('active');
        bizumSec.classList.remove('active');
        cardSec.classList.add('active');
        btnPay.classList.remove('bizum-btn');
    }
}

function quickFill(code, cardNumber) {
    document.getElementById('cardNumber').value = cardNumber;
    document.getElementById('selectResponse').value = code;
}

function onOutcomeChange() {
    const code = document.getElementById('selectResponse').value;
    if (code === '0000') {
        document.getElementById('cardNumber').value = '4548 1234 5678 9010';
    } else if (code === '0116') {
        document.getElementById('cardNumber').value = '4548 0000 0000 0116';
    } else if (code === '0180') {
        document.getElementById('cardNumber').value = '4548 0000 0000 0180';
    } else if (code === '0184') {
        document.getElementById('cardNumber').value = '4548 0000 0000 0184';
    }
}

async function submitPayment() {
    const t = translations[currentLang] || translations['001'];
    const modal = document.getElementById('processingModal');
    const spinner = document.getElementById('modalSpinner');
    const modalTitle = document.getElementById('modalTitle');
    const modalDesc = document.getElementById('modalDesc');
    const statusContainer = document.getElementById('modalStatusContainer');
    const statusPill = document.getElementById('modalStatusPill');
    const statusDetail = document.getElementById('modalStatusDetail');
    const btnRedirect = document.getElementById('btnRedirect');

    modal.style.display = 'flex';
    spinner.style.display = 'block';
    modalTitle.textContent = t.connectingTitle;
    modalDesc.textContent = t.connectingDesc;
    statusContainer.style.display = 'none';

    const baseData = JSON.parse(document.getElementById('simulatorData').textContent);
    const responseCode = document.getElementById('selectResponse').value;

    const payload = {
        ...baseData,
        responseCode: responseCode,
        processedPayMethod: currentMethod,
        authorisationCode: responseCode === '0000' ? '999888' : null
    };

    const dispatchUrl = document.body.dataset.dispatchUrl;

    try {
        const res = await fetch(dispatchUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        spinner.style.display = 'none';
        statusContainer.style.display = 'block';

        if (res.ok && data.success) {
            const isOk = responseCode === '0000';
            if (isOk) {
                modalTitle.textContent = t.successTitle;
                modalDesc.textContent = t.successDesc;
                statusPill.className = 'modal-status-pill success';
                statusPill.textContent = 'AUTORIZADA • Código 0000';
            } else {
                modalTitle.textContent = t.declinedTitle;
                modalDesc.textContent = t.declinedDesc;
                statusPill.className = 'modal-status-pill declined';
                statusPill.textContent = 'DENEGADA • Código ' + responseCode;
            }

            statusDetail.textContent = 'Notificación webhook enviada (HTTP ' + data.webhookStatus + ' OK).';
            btnRedirect.href = data.redirectUrl;
            btnRedirect.textContent = t.redirectBtn;

            // Auto-redirect after 1.5 seconds for a realistic banking experience
            setTimeout(() => {
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                }
            }, 1500);

        } else {
            modalTitle.textContent = 'Error en la pasarela';
            modalDesc.textContent = data.error || 'No se pudo comunicar con el comercio.';
            statusPill.className = 'modal-status-pill declined';
            statusPill.textContent = 'ERROR EN LA OPERACIÓN';
            btnRedirect.href = payload.urlKo || '#';
            btnRedirect.textContent = 'Volver al comercio';
        }
    } catch (err) {
        spinner.style.display = 'none';
        statusContainer.style.display = 'block';
        modalTitle.textContent = 'Error de conexión';
        modalDesc.textContent = err.message;
        statusPill.className = 'modal-status-pill declined';
        statusPill.textContent = 'ERROR DE RED';
        btnRedirect.href = '#';
        btnRedirect.textContent = 'Cerrar ventana';
    }
}

function cancelPayment() {
    document.getElementById('selectResponse').value = '9915';
    submitPayment();
}
