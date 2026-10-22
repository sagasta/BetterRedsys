---
layout: home

hero:
  name: "BetterRedsys"
  text: "Pasarela Redsys y Bizum moderna para .NET 10"
  tagline: "Fuertemente tipada, Native AOT, criptografía estricta en tiempo constante y simulador bancario local en memoria."
  image:
    src: /icon.png
    alt: BetterRedsys Logo
  actions:
    - theme: brand
      text: Empezar con la guía
      link: /configuration
    - theme: alt
      text: Ver en GitHub
      link: https://github.com/sagasta/BetterRedsys
    - theme: alt
      text: Paquete NuGet
      link: https://www.nuget.org/packages/BetterRedsys

features:
  - icon: 🚀
    title: Rendimiento y Native AOT
    details: Cero reflexión para JSON mediante Source Generators de System.Text.Json. Diseñado para contenedores mínimos y microservicios serverless en .NET 10.
  - icon: 🔒
    title: Criptografía Estricta
    details: Diversificación de clave 3DES-CBC por número de pedido, firma HMAC-SHA256 y comparación en tiempo constante para mitigar ataques de temporización.
  - icon: 📱
    title: Bizum de Primer Nivel
    details: Soporte nativo para Bizum y pagos combinados con tarjeta, incluyendo validación de números móviles españoles.
  - icon: ⚡
    title: Arquitectura Dual
    details: DTOs desacoplados para React, Next.js, Blazor, Vue o Apps móviles (.ToFormData()) o redirección HTML accesible automática (.ToAutoSubmitHtml()).
  - icon: 🛡️
    title: Webhooks Seguros
    details: Middleware MapRedsysWebhook con validación criptográfica automática previa a tu lógica de negocio y catálogo de respuestas oficiales bilingüe.
  - icon: 🧪
    title: Simulador Local sin ngrok
    details: Paquete complementario BetterRedsys.Simulator para probar el flujo de compra y webhooks en desarrollo local sin dependencias bancarias externas.
---
