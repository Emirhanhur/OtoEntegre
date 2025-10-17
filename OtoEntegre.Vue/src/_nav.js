export default [
  {
    component: 'CNavItem',
    name: 'Anasayfa',
    to: '/anasayfa',
    icon: 'cil-speedometer',
  /*badge: {
      color: 'primary',
      text: 'NEW',
    },*/
  },
  {
    component: 'CNavTitle',
    name: 'Entegrasyonlar',
  },
  {
    component: 'CNavItem',
    name: 'Trendyol Entegrasyon',
    to: '/trendyol-entegrasyon',
    icon: 'cil-pencil',
  },
 /* {
    component: 'CNavItem',
    name: 'Ürünler',
    to: '/urunler',
    icon: 'cil-pencil',
  },
  {
    component: 'CNavItem',
    name: 'HepsiBurada Entegrasyon',
    to: '/hepsiburada-entegrasyon',
    icon: 'cil-pencil',
  },
  {
    component: 'CNavItem',
    name: 'N11 Entegrasyon',
    to: '/nOnBir-entegrasyon',
    icon: 'cil-pencil',
  },*/
 {
    component: 'CNavItem',
    name: 'Satış Özeti',
    to: '/ozet',
    icon: 'cil-pencil', // veya cil-chart-line / cil-trending-up
  }
  ,
 /* {
    component: 'CNavItem',
    name: 'Kredi Yükle',
    to: '/admin/kredi-yukle',
    icon: 'cil-pencil',
    meta: { requiresAdmin: true },
  } */
]
