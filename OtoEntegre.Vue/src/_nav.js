// localStorage'den bayi_id değerini kontrol et
const getBayiId = () => {
  try {
    const bayiId = localStorage.getItem('bayi_id');
    return bayiId ? parseInt(bayiId, 10) : null;
  } catch (error) {
    return null;
  }
};

// Menüyü dinamik olarak oluşturan fonksiyon
const getNavItems = () => {
  const bayiId = getBayiId();
  const showKarZarar = bayiId === 55;

  // Trendyol menü öğeleri
  const trendyolItems = [
    {
      component: 'CNavItem',
      name: 'Trendyol Entegrasyon',
      to: '/trendyol-entegrasyon',
      icon: 'cil-settings',
      style: { paddingLeft: '70px' }
    },
    {
      component: 'CNavItem',
      name: 'Trendyol Ekstre İşlemler',
      to: '/ekstreler',
      icon: 'cil-file-paper',
      style: { paddingLeft: '70px' }
    },
    {
      component: 'CNavItem',
      name: 'Ürünler',
      to: '/urunler',
      icon: 'cil-list',
      style: { paddingLeft: '70px' }
    },
    {
      component: 'CNavItem',
      name: 'Satış Özeti',
      to: '/ozet',
      icon: 'cil-chart-line',
      style: { paddingLeft: '70px' }
    }
  ];

  // Kar/Zarar Hesaplama menü öğesini sadece bayi_id 55 ise ekle
  if (showKarZarar) {
    trendyolItems.push({
      component: 'CNavItem',
      name: 'Kar/Zarar Hesaplama',
      to: '/kar-zarar',
      icon: 'cil-chart-pie',
      style: { paddingLeft: '70px' }
    });
  }

  // Rol kontrolü
  const getRol = () => {
    try {
      return localStorage.getItem('rol');
    } catch (error) {
      return null;
    }
  };

  const rol = getRol();
  const isAdmin = rol === 'Admin';

  const menuItems = [];

  // Platform entegrasyonlarını ve Anasayfa'yı admin rolü görmesin
  if (!isAdmin) {
    // Anasayfa
    menuItems.push({
      component: 'CNavItem',
      name: 'Anasayfa',
      to: '/anasayfa',
      icon: 'cil-speedometer',
    });

    // Başlık
    menuItems.push({
      component: 'CNavTitle',
      name: 'Platform Entegrasyonları',
    });

    // ⭐ TRENDYOL MENÜ GRUBU
    menuItems.push({
      component: 'CNavGroup',
      name: 'Trendyol',
      icon: 'cil-cart',
      items: trendyolItems,
    });
  }

  // Admin paneli menü grubu
  if (isAdmin) {
    menuItems.push({
      component: 'CNavTitle',
      name: 'Admin Paneli',
    });

    menuItems.push({
      component: 'CNavGroup',
      name: 'Yönetim',
      icon: 'cil-settings',
      items: [
        {
          component: 'CNavItem',
          name: 'Dashboard',
          to: '/admin/dashboard',
          icon: 'cil-speedometer',
          style: { paddingLeft: '70px' }
        },
        {
          component: 'CNavItem',
          name: 'Kullanıcılar',
          to: '/admin/kullanicilar',
          icon: 'cil-people',
          style: { paddingLeft: '70px' }
        },
        {
          component: 'CNavItem',
          name: 'Krediler',
          to: '/admin/krediler',
          icon: 'cil-money',
          style: { paddingLeft: '70px' }
        },
        {
          component: 'CNavItem',
          name: 'Entegrasyonlar',
          to: '/admin/entegrasyonlar',
          icon: 'cil-settings',
          style: { paddingLeft: '70px' }
        },
        {
          component: 'CNavItem',
          name: 'Siparişler',
          to: '/admin/siparisler',
          icon: 'cil-cart',
          style: { paddingLeft: '70px' }
        },
        {
          component: 'CNavItem',
          name: 'Kredi Yükle',
          to: '/admin/kredi-yukle',
          icon: 'cil-wallet',
          style: { paddingLeft: '70px' }
        }
      ],
    });
  }

  return menuItems;
};

// Fonksiyonu export et (default export olarak)
export default getNavItems;
