export default [
  {
    component: 'CNavItem',
    name: 'Anasayfa',
    to: '/anasayfa',
    icon: 'cil-speedometer',
  },

  {
    component: 'CNavTitle',
    name: 'Platform Entegrasyonları',
  },

  // ⭐ TRENDYOL MENÜ GRUBU
  {
    component: 'CNavGroup',
    name: 'Trendyol',
    icon: 'cil-cart',
    items: [
      {
        component: 'CNavItem',
        name: 'Trendyol Entegrasyon',
        to: '/trendyol-entegrasyon',
        icon: 'cil-settings',        // Material UI: settings
        style: { paddingLeft: '22px' }
      },
      {
        component: 'CNavItem',
        name: 'Trendyol Ekstre İşlemler',
        to: '/ekstreler',
        icon: 'cil-file-paper',   // ✔ ÇALIŞAN İKON
        style: { paddingLeft: '22px' }
      },
      {
        component: 'CNavItem',
        name: 'Ürünler',
        to: '/urunler',
        icon: 'cil-list',            // Material UI: list
        style: { paddingLeft: '22px' }
      },
      {
        component: 'CNavItem',
        name: 'Satış Özeti',
        to: '/ozet',
        icon: 'cil-chart-line',      // Material UI: trending_up
        style: { paddingLeft: '22px' }
      }
    ],
  },

  {
    component: 'CNavItem',
    name: 'Kredi Yükle',
    to: '/admin/kredi-yukle',
    icon: 'cil-money',
    meta: { requiresAdmin: true },
  }
]
