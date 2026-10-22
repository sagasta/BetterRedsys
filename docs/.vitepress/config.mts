import { defineConfig } from 'vitepress';

export default defineConfig({
  title: 'BetterRedsys',
  description: 'Documentación oficial de BetterRedsys - Pasarela de pago Redsys y Bizum para .NET 10 y ASP.NET Core',
  base: '/BetterRedsys/',
  lang: 'es-ES',
  head: [
    ['link', { rel: 'icon', type: 'image/png', href: '/BetterRedsys/icon.png' }]
  ],
  themeConfig: {
    logo: '/icon.png',
    siteTitle: 'BetterRedsys',
    search: {
      provider: 'local',
      options: {
        locales: {
          root: {
            translations: {
              button: {
                buttonText: 'Buscar',
                buttonAriaLabel: 'Buscar'
              },
              modal: {
                noResultsText: 'No se encontraron resultados para',
                resetButtonTitle: 'Limpiar búsqueda',
                footer: {
                  selectText: 'para seleccionar',
                  navigateText: 'para navegar',
                  closeText: 'para cerrar'
                }
              }
            }
          }
        }
      }
    },
    nav: [
      { text: 'Guía Rápida', link: '/configuration' },
      { text: 'Pagos y Bizum', link: '/payments-and-bizum' },
      { text: 'Simulador', link: '/simulator' },
      { text: 'Paso a Producción', link: '/homologacion-produccion' },
      {
        text: 'Ecosistema',
        items: [
          { text: 'BetterRedsys (NuGet)', link: 'https://www.nuget.org/packages/BetterRedsys' },
          { text: 'BetterRedsys.Simulator (NuGet)', link: 'https://www.nuget.org/packages/BetterRedsys.Simulator' },
          { text: 'HybridCache.Plus', link: 'https://github.com/sagasta/HybridCachePlus' },
          { text: 'Invítame a un café ☕', link: 'https://buymeacoffee.com/sagasta' }
        ]
      }
    ],
    sidebar: [
      {
        text: 'Comenzando',
        items: [
          { text: 'Configuración y Opciones', link: '/configuration' },
          { text: 'Homologación Bancaria', link: '/homologacion-produccion' }
        ]
      },
      {
        text: 'Pasarela de Pagos',
        items: [
          { text: 'Pagos con Tarjeta y Bizum', link: '/payments-and-bizum' },
          { text: 'Redsys InSite (Embebido)', link: '/insite' },
          { text: 'Operaciones REST (Devoluciones y COF)', link: '/rest-operations' }
        ]
      },
      {
        text: 'Backend y Seguridad',
        items: [
          { text: 'Webhooks y Validación Criptográfica', link: '/webhooks' },
          { text: 'Telemetría y Health Checks', link: '/telemetry' }
        ]
      },
      {
        text: 'Entorno de Desarrollo',
        items: [
          { text: 'Simulador Local (sin ngrok)', link: '/simulator' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/sagasta/BetterRedsys' }
    ],
    footer: {
      message: 'Distribuido bajo la Licencia MIT.',
      copyright: 'Copyright © 2026 Jose Sagasta'
    }
  }
});
