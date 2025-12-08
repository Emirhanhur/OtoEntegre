<script>
import api from "../../axios";
import { formatCurrency } from "../../../utils/format";

export default {
    name: "KarZarar",
    data() {
        return {
            karZararData: [],
            isLoading: false,
            errorMessage: "",
            searchQuery: "",
            durumFilter: "",
            sortBy: "karZarar",
            satisIstatistikleri: null,
            isLoadingSatis: false,
            selectedGunSayisi: 30,
            activeTab: "karZarar",
            // Animated values
            animatedToplamUrun: 0,
            animatedToplamKarZarar: 0,
            animatedKarEdenUrunler: 0,
            animatedZararEdenUrunler: 0,
            animatedToplamSatilanAdet: 0,
            animatedToplamCiro: 0,
            animatedToplamUrunSayisi: 0,
        };
    },
    computed: {
        filteredProducts() {
            let filtered = [...this.karZararData];

            // Arama filtresi
            if (this.searchQuery.trim()) {
                const query = this.searchQuery.toLowerCase();
                filtered = filtered.filter(
                    (urun) =>
                        urun.urunAdi?.toLowerCase().includes(query) ||
                        urun.trendyolBarcode?.toLowerCase().includes(query) ||
                        urun.otostickerBarcode?.toLowerCase().includes(query) ||
                        urun.category?.toLowerCase().includes(query) ||
                        urun.brand?.toLowerCase().includes(query)
                );
            }

            // Durum filtresi
            if (this.durumFilter) {
                filtered = filtered.filter((urun) => urun.durum === this.durumFilter);
            }

            // Sıralama
            switch (this.sortBy) {
                case "karZarar":
                    filtered.sort((a, b) => (b.karZarar || 0) - (a.karZarar || 0));
                    break;
                case "karZararAsc":
                    filtered.sort((a, b) => (a.karZarar || 0) - (b.karZarar || 0));
                    break;
                case "urunAdi":
                    filtered.sort((a, b) =>
                        (a.urunAdi || "").localeCompare(b.urunAdi || "")
                    );
                    break;
                case "karZararYuzdesi":
                    filtered.sort(
                        (a, b) => (b.karZararYuzdesi || 0) - (a.karZararYuzdesi || 0)
                    );
                    break;
            }

            return filtered;
        },
        toplamKarZarar() {
            return this.filteredProducts.reduce((sum, urun) => sum + (urun.karZarar || 0), 0);
        },
        karEdenUrunler() {
            return this.filteredProducts.filter((u) => u.karZarar > 0).length;
        },
        zararEdenUrunler() {
            return this.filteredProducts.filter((u) => u.karZarar < 0).length;
        },
    },
    mounted() {
        this.loadKarZarar();
        this.loadSatisIstatistikleri(30);
    },
    watch: {
        toplamKarZarar(newVal, oldVal) {
            if (oldVal !== undefined && oldVal !== newVal) {
                this.animateValue('animatedToplamKarZarar', this.animatedToplamKarZarar, newVal, 1000);
            }
        },
        karEdenUrunler(newVal, oldVal) {
            if (oldVal !== undefined && oldVal !== newVal) {
                this.animateValue('animatedKarEdenUrunler', this.animatedKarEdenUrunler, newVal, 800);
            }
        },
        zararEdenUrunler(newVal, oldVal) {
            if (oldVal !== undefined && oldVal !== newVal) {
                this.animateValue('animatedZararEdenUrunler', this.animatedZararEdenUrunler, newVal, 800);
            }
        },
    },
    methods: {
        formatCurrency,
        async loadKarZarar() {
            this.isLoading = true;
            this.errorMessage = "";

            try {
                const kullaniciId = localStorage.getItem("kullanici_id");
                if (!kullaniciId) {
                    this.errorMessage = "Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                    this.isLoading = false;
                    return;
                }

                const response = await api.get(`/api/urunler/kar-zarar/${kullaniciId}`);

                if (response.data.success) {
                    this.karZararData = response.data.data || [];
                    // Animasyonları başlat
                    this.$nextTick(() => {
                        this.animateValue('animatedToplamUrun', 0, this.karZararData.length, 1000);
                        this.animateValue('animatedToplamKarZarar', 0, this.toplamKarZarar, 1500);
                        this.animateValue('animatedKarEdenUrunler', 0, this.karEdenUrunler, 1200);
                        this.animateValue('animatedZararEdenUrunler', 0, this.zararEdenUrunler, 1200);
                    });
                } else {
                    this.errorMessage = response.data.message || "Kar/zarar verileri alınamadı.";
                }
            } catch (error) {
                console.error("Kar/zarar yükleme hatası:", error);
                this.errorMessage =
                    error.response?.data?.message ||
                    "Kar/zarar verileri yüklenirken bir hata oluştu.";
            } finally {
                this.isLoading = false;
            }
        },
        handleImageError(event) {
            event.target.style.display = "none";
        },
        async loadSatisIstatistikleri(gunSayisi) {
            this.isLoadingSatis = true;
            this.selectedGunSayisi = gunSayisi;

            try {
                const kullaniciId = localStorage.getItem("kullanici_id");
                if (!kullaniciId) {
                    return;
                }

                const response = await api.get(`/api/urunler/satis-istatistikleri/${kullaniciId}?gunSayisi=${gunSayisi}`);

                if (response.data.success) {
                    this.satisIstatistikleri = response.data;
                    // Animasyonları başlat
                    this.$nextTick(() => {
                        if (this.satisIstatistikleri) {
                            this.animateValue('animatedToplamSatilanAdet', 0, this.satisIstatistikleri.toplamSatilanAdet || 0, 1500);
                            this.animateValue('animatedToplamCiro', 0, this.satisIstatistikleri.toplamCiro || 0, 1500);
                            this.animateValue('animatedToplamUrunSayisi', 0, this.satisIstatistikleri.toplamUrunSayisi || 0, 1200);
                        }
                    });
                } else {
                    console.error("Satış istatistikleri alınamadı:", response.data.message);
                }
            } catch (error) {
                console.error("Satış istatistikleri yükleme hatası:", error);
            } finally {
                this.isLoadingSatis = false;
            }
        },
        formatDate(dateString) {
            if (!dateString) return "-";
            const date = new Date(dateString);
            return date.toLocaleDateString("tr-TR", {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit"
            });
        },
        animateValue(property, start, end, duration) {
            if (start === end) return;

            const startTime = performance.now();
            const isDecimal = property === 'animatedToplamKarZarar' || property === 'animatedToplamCiro';

            const animate = (currentTime) => {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);

                // Easing function (ease-out)
                const easeOut = 1 - Math.pow(1 - progress, 3);
                const current = start + (end - start) * easeOut;

                if (isDecimal) {
                    this[property] = Math.round(current * 100) / 100;
                } else {
                    this[property] = Math.round(current);
                }

                if (progress < 1) {
                    requestAnimationFrame(animate);
                } else {
                    this[property] = end;
                }
            };

            requestAnimationFrame(animate);
        },
    },
};
</script>
<template>
    <div>
        <!-- Tab Navigation -->
        <ul class="nav nav-tabs mb-4">
            <li class="nav-item">
                <button class="nav-link" :class="{ active: activeTab === 'karZarar' }" @click="activeTab = 'karZarar'">
                    Kar/Zarar Hesaplama
                </button>
            </li>
            <li class="nav-item">
                <button class="nav-link" :class="{ active: activeTab === 'satisIstatistikleri' }"
                    @click="activeTab = 'satisIstatistikleri'">
                    Satış İstatistikleri
                </button>
            </li>
        </ul>

        <!-- Kar/Zarar Tab -->
        <div v-show="activeTab === 'karZarar'">
            <!-- Başlık -->
            <div class="card shadow-sm border-0 rounded-4 mb-4">
                <div class="card-header bg-white">
                    <h5 class="mb-0">
                        <strong>Kar/Zarar Hesaplama</strong>
                        <small class="text-muted"> Eşleştirilmiş Ürünler</small>
                    </h5>
                </div>
                <div class="card-body">
                    <!-- Özet Bilgiler -->
                    <div class="row mb-4">
                        <div class="col-md-3 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-cart" style="font-size: 2.5rem; color: #007bff;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Toplam Ürün</h6>
                                    <h3 class="animated-number">{{ animatedToplamUrun }}</h3>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-money" style="font-size: 2.5rem; color: #28a745;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Toplam Kar/Zarar</h6>
                                    <h3 :class="toplamKarZarar >= 0 ? 'text-success' : 'text-danger'">
                                        {{ formatCurrency(animatedToplamKarZarar) }}
                                    </h3>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-thumb-up" style="font-size: 2.5rem; color: #28a745;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Kar Eden Ürünler</h6>
                                    <h3 class="text-success">{{ animatedKarEdenUrunler }}</h3>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-3 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-thumb-down" style="font-size: 2.5rem; color: #dc3545;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Zarar Eden Ürünler</h6>
                                    <h3 class="text-danger">{{ animatedZararEdenUrunler }}</h3>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Filtreler -->
                    <div class="row mb-3">
                        <div class="col-md-4 mb-2">
                            <div class="input-group">
                                <span class="input-group-text">
                                    <i class="cil-magnifying-glass"></i>
                                </span>
                                <input type="text" class="form-control" v-model="searchQuery"
                                    placeholder="Ürün adı, barkod veya kategori ara..." />
                            </div>
                        </div>
                        <div class="col-md-3 mb-2">
                            <select class="form-select" v-model="durumFilter">
                                <option value="">Tüm Durumlar</option>
                                <option value="Kar">Kar</option>
                                <option value="Zarar">Zarar</option>
                                <option value="Başabaş">Başabaş</option>
                            </select>
                        </div>
                        <div class="col-md-3 mb-2">
                            <select class="form-select" v-model="sortBy">
                                <option value="karZarar">Kar/Zarar (Yüksekten Düşüğe)</option>
                                <option value="karZararAsc">Kar/Zarar (Düşükten Yükseğe)</option>
                                <option value="urunAdi">Ürün Adı (A-Z)</option>
                                <option value="karZararYuzdesi">Kar/Zarar % (Yüksekten Düşüğe)</option>
                            </select>
                        </div>
                        <div class="col-md-2 mb-2">
                            <button class="btn btn-primary w-100" @click="loadKarZarar" :disabled="isLoading">
                                <i class="cil-reload"></i> Yenile
                            </button>
                        </div>
                    </div>

                    <!-- Yükleme Durumu -->
                    <div v-if="isLoading" class="text-center py-5">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Yükleniyor...</span>
                        </div>
                        <p class="mt-2 text-secondary">Yükleniyor...</p>
                    </div>

                    <!-- Hata Mesajı -->
                    <div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
                        {{ errorMessage }}
                        <button type="button" class="btn-close" @click="errorMessage = ''" aria-label="Close"></button>
                    </div>

                    <!-- Ürün Listesi -->
                    <div v-if="!isLoading && filteredProducts.length > 0" class="table-responsive">
                        <table class="table table-striped table-hover mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th style="width: 80px">Görsel</th>
                                    <th>Ürün Adı</th>
                                    <th>Trendyol Satış Fiyatı</th>
                                    <th>Trendyol Giderleri</th>
                                    <th>Otosticker Maliyet</th>
                                    <th>Kar/Zarar</th>
                                    <th>Kar/Zarar %</th>
                                    <th>Durum</th>
                                    <th>Stok</th>
                                    <th>#</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="(urun, index) in filteredProducts"
                                    :key="`kar-zarar-${urun.productCode || index}-${index}`">
                                    <td>
                                        <img v-if="urun.images && urun.images.length > 0" :src="urun.images[0]"
                                            alt="Ürün Görseli"
                                            style="width: 60px; height: 60px; object-fit: cover; border-radius: 4px;"
                                            @error="handleImageError" />
                                        <div v-else class="text-muted d-flex align-items-center justify-content-center"
                                            style="width: 60px; height: 60px; background: #f0f0f0; border-radius: 4px;">
                                            <i class="cil-image"></i>
                                        </div>
                                    </td>
                                    <td>
                                        <div>
                                            <strong>{{ urun.urunAdi }}</strong>
                                            <br />
                                            <small class="text-muted">
                                                <i class="cil-barcode"></i> Trendyol: {{ urun.trendyolBarcode }}
                                            </small>
                                            <br v-if="urun.otostickerBarcode" />
                                            <small v-if="urun.otostickerBarcode" class="text-muted">
                                                Otosticker: {{ urun.otostickerBarcode }}
                                            </small>
                                        </div>
                                    </td>
                                    <td>
                                        <strong>{{ formatCurrency(urun.trendyolSatisFiyati) }}</strong>
                                        <br />
                                        <small class="text-muted">Komisyon: %{{ urun.commissionOrani }}</small>
                                    </td>
                                    <td>
                                        <span class="text-warning">{{ formatCurrency(urun.trendyolGiderleri) }}</span>
                                    </td>
                                    <td>
                                        <span class="text-info">{{ formatCurrency(urun.otostickerMaliyet) }}</span>
                                    </td>
                                    <td>
                                        <strong :class="urun.karZarar >= 0 ? 'text-success' : 'text-danger'">
                                            {{ formatCurrency(urun.karZarar) }}
                                        </strong>
                                    </td>
                                    <td>
                                        <strong
                                            :class="(urun.karZararYuzdesi || 0) >= 0 ? 'text-success' : 'text-danger'">
                                            {{ (urun.karZararYuzdesi || 0).toFixed(2) }}%
                                        </strong>
                                    </td>
                                    <td>
                                        <span class="badge"
                                            :class="urun.durum === 'Kar' ? 'bg-success' : urun.durum === 'Zarar' ? 'bg-danger' : 'bg-secondary'">
                                            {{ urun.durum }}
                                        </span>
                                    </td>
                                    <td>
                                        <span class="badge" :class="urun.stock > 0 ? 'bg-success' : 'bg-danger'">
                                            {{ urun.stock }}
                                        </span>
                                    </td>
                                    <td>
                                        <a v-if="urun.productUrl" :href="urun.productUrl" target="_blank"
                                            class="btn btn-link btn-sm p-0">
                                            <i class="cil-external-link"></i> Trendyol
                                        </a>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <!-- Boş Durum -->
                    <div v-if="!isLoading && filteredProducts.length === 0" class="alert alert-info">
                        <i class="cil-info"></i> Eşleştirilmiş ürün bulunamadı veya filtre kriterlerinize uygun ürün
                        yok.
                    </div>
                </div>
            </div>
        </div>
        <!-- End Kar/Zarar Tab -->

        <!-- Satış İstatistikleri Tab -->
        <div v-show="activeTab === 'satisIstatistikleri'">
            <div class="card shadow-sm border-0 rounded-4">
                <div class="card-header bg-white">
                    <h5 class="mb-0">
                        <strong>Satış İstatistikleri</strong>
                        <small class="text-muted"> En Çok Satılan Ürünler</small>
                    </h5>
                </div>
                <div class="card-body">
                    <!-- Tarih Filtreleri -->
                    <div class="row mb-3">
                        <div class="col-md-12">
                            <div class="btn-group" role="group">
                                <button type="button" class="btn"
                                    :class="selectedGunSayisi === 1 ? 'btn-primary' : 'btn-outline-primary'"
                                    @click="loadSatisIstatistikleri(1)">
                                    1 Gün
                                </button>
                                <button type="button" class="btn"
                                    :class="selectedGunSayisi === 7 ? 'btn-primary' : 'btn-outline-primary'"
                                    @click="loadSatisIstatistikleri(7)">
                                    7 Gün
                                </button>
                                <button type="button" class="btn"
                                    :class="selectedGunSayisi === 15 ? 'btn-primary' : 'btn-outline-primary'"
                                    @click="loadSatisIstatistikleri(15)">
                                    15 Gün
                                </button>
                                <button type="button" class="btn"
                                    :class="selectedGunSayisi === 30 ? 'btn-primary' : 'btn-outline-primary'"
                                    @click="loadSatisIstatistikleri(30)">
                                    30 Gün
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Özet İstatistikler -->
                    <div v-if="satisIstatistikleri" class="row mb-4">
                        <div class="col-md-4 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-check-circle" style="font-size: 2.5rem; color: #007bff;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Toplam Satılan Adet</h6>
                                    <h3 class="text-primary">{{ animatedToplamSatilanAdet }}</h3>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-4 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-dollar" style="font-size: 2.5rem; color: #28a745;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Toplam Ciro</h6>
                                    <h3 class="text-success">{{ formatCurrency(animatedToplamCiro) }}</h3>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-4 mb-3">
                            <div class="card text-center border-0 shadow-sm animated-card">
                                <div class="card-body">
                                    <div class="stat-icon mb-2">
                                        <i class="cil-list" style="font-size: 2.5rem; color: #17a2b8;"></i>
                                    </div>
                                    <h6 class="text-muted mb-2">Satılan Ürün Çeşidi</h6>
                                    <h3 class="text-info">{{ animatedToplamUrunSayisi }}</h3>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Yükleme Durumu -->
                    <div v-if="isLoadingSatis" class="text-center py-5">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Yükleniyor...</span>
                        </div>
                        <p class="mt-2 text-secondary">Satış istatistikleri yükleniyor...</p>
                    </div>

                    <!-- Satış İstatistikleri Tablosu -->
                    <div v-if="!isLoadingSatis && satisIstatistikleri && satisIstatistikleri.data && satisIstatistikleri.data.length > 0"
                        class="table-responsive">
                        <table class="table table-striped table-hover mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th style="width: 50px">#</th>
                                    <th style="width: 80px">Görsel</th>
                                    <th>Ürün Adı</th>
                                    <th class="text-center">Satılan Adet</th>
                                    <th class="text-center">Sipariş Sayısı</th>
                                    <th class="text-end">Toplam Ciro</th>
                                    <th class="text-end">Ortalama Fiyat</th>
                                    <th class="text-center">Son Satış</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="(urun, index) in satisIstatistikleri.data" :key="urun.urunId">
                                    <td>
                                        <span class="badge bg-primary">{{ index + 1 }}</span>
                                    </td>
                                    <td>
                                        <img v-if="urun.image" :src="urun.image" alt="Ürün Görseli"
                                            style="width: 60px; height: 60px; object-fit: cover; border-radius: 4px;"
                                            @error="handleImageError" />
                                        <div v-else class="text-muted d-flex align-items-center justify-content-center"
                                            style="width: 60px; height: 60px; background: #f0f0f0; border-radius: 4px;">
                                            <i class="cil-image"></i>
                                        </div>
                                    </td>
                                    <td>
                                        <strong>{{ urun.urunAdi }}</strong>
                                        <br />
                                        <small class="text-muted" v-if="urun.productCode">
                                            Kod: {{ urun.productCode }}
                                        </small>
                                    </td>
                                    <td class="text-center">
                                        <span class="badge bg-success" style="font-size: 1rem;">
                                            {{ urun.toplamSatilanAdet }}
                                        </span>
                                    </td>
                                    <td class="text-center">
                                        <span class="badge bg-info">{{ urun.siparisSayisi }}</span>
                                    </td>
                                    <td class="text-end">
                                        <strong class="text-success">{{ formatCurrency(urun.toplamCiro) }}</strong>
                                    </td>
                                    <td class="text-end">
                                        <small class="text-muted">{{ formatCurrency(urun.ortalamaFiyat) }}</small>
                                    </td>
                                    <td class="text-center">
                                        <small class="text-muted">{{ formatDate(urun.sonSatisTarihi) }}</small>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <!-- Boş Durum -->
                    <div v-if="!isLoadingSatis && (!satisIstatistikleri || !satisIstatistikleri.data || satisIstatistikleri.data.length === 0)"
                        class="alert alert-info">
                        <i class="cil-info"></i> Seçilen dönemde satış verisi bulunamadı.
                    </div>
                </div>
            </div>
        </div>
        <!-- End Satış İstatistikleri Tab -->
    </div>
</template>


<style scoped>
.text-success {
    color: #28a745 !important;
}

.text-danger {
    color: #dc3545 !important;
}

.text-warning {
    color: #ffc107 !important;
}

.text-info {
    color: #17a2b8 !important;
}

/* Tab navigation for bootstrap's nav-tabs */
.nav-tabs .nav-link {
    cursor: pointer;
}

/* Animated Cards */
.animated-card {
    transition: all 0.3s ease;
    border-left: 4px solid transparent !important;
}

.animated-card:hover {
    transform: translateY(-5px);
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
}

.animated-card:nth-child(1) {
    border-left-color: #007bff !important;
}

.animated-card:nth-child(2) {
    border-left-color: #28a745 !important;
}

.animated-card:nth-child(3) {
    border-left-color: #28a745 !important;
}

.animated-card:nth-child(4) {
    border-left-color: #dc3545 !important;
}

/* Stat Icon Animation */
.stat-icon {
    animation: iconBounce 0.6s ease-out;
}

@keyframes iconBounce {
    0% {
        transform: scale(0);
        opacity: 0;
    }

    50% {
        transform: scale(1.1);
    }

    100% {
        transform: scale(1);
        opacity: 1;
    }
}

/* Number Animation */
.animated-number {
    font-weight: bold;
    transition: all 0.3s ease;
}

.animated-number:hover {
    transform: scale(1.1);
}
</style>
