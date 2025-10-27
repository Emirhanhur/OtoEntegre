<script>
import { Bar, Line as LineChart, Doughnut } from "vue-chartjs";
import api from "../../axios";
import { formatCurrency } from "../../../utils/format";

import {
    Chart,
    BarElement,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    ArcElement,
    Tooltip,
    Legend,
} from "chart.js";

Chart.register(BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Tooltip, Legend);

export default {
    components: { Bar, LineChart, Doughnut },
    data() {
        return {
            orders: [],
            isLoading: true,
            entegrasyonId: null,
            selectedDays: 30,
            chartOptions: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: "bottom" } },
                scales: { x: { ticks: { autoSkip: true, maxTicksLimit: 10 } } }
            },
            chartCurrencyOptions: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: "bottom" } },
                scales: {
                    y: {
                        ticks: {
                            callback: (val) => formatCurrency(val, "TRY"),
                        },
                    },
                },
            },
            statusMap: {
                Created: "Oluşturuldu",
                Picking: "Hazırlanıyor",
                Invoiced: "Faturalandı",
                Shipped: "Kargoya Verildi",
                Cancelled: "İptal Edildi",
                Delivered: "Teslim Edildi",
                UnDelivered: "Teslim Edilemedi",
                Returned: "İade Edildi",
                Repack: "Tekrar Paketlenecek",
                UnSupplied: "Tedarik Edilemedi",
            },
            showSiparisTab: "grafik", // "grafik" veya "siparis"
            siparisBazliData: [],
            isLoadingSiparis: false,
            // Yeni filtreleme ve sıralama için
            filterStatus: "",
            sortBy: "karZarar",
            searchTerm: "",
            // UI state: hangi siparişlerin açık olduğu (siparisNo set)
            openGroups: new Set(),
        };
    },
    watch: {
        showSiparisTab(newVal) {
            if (newVal === "siparis" && this.siparisBazliData.length === 0) {
                this.fetchSiparisBazli();
            }
        }
    },
    computed: {
        chart30DaysData() {
            const { labels, counts } = this.computeLastNDaysCounts();
            return {
                labels,
                datasets: [
                    {
                        label: "Sipariş Adedi",
                        data: counts,
                        borderColor: "#36a2eb",
                        backgroundColor: "rgba(54,162,235,0.2)",
                    },
                ],
            };
        },
        chartStatusData() {
            const statusCounts = {};
            for (const o of this.orders) {
                const k = o.durum || "Unknown";
                statusCounts[k] = (statusCounts[k] || 0) + 1;
            }
            const labels = Object.keys(statusCounts).map((k) => this.statusMap[k] || k);
            const data = Object.values(statusCounts);
            const colors = ["#36a2eb", "#ff6384", "#ffcd56", "#4bc0c0", "#9966ff", "#ff9f40", "#8dd17e", "#f67019", "#00a950", "#b3b3b3"];
            return {
                labels,
                datasets: [{ data, backgroundColor: labels.map((_, i) => colors[i % colors.length]) }],
            };
        },
        chartDailyTotalData() {
            const { labels, map } = this.computeLastNDaysMap();
            const totals = labels.map((l) => map[l]?.total || 0);
            return {
                labels,
                datasets: [
                    {
                        label: "Günlük Toplam Tutar",
                        data: totals,
                        backgroundColor: "rgba(75,192,192,0.5)",
                    },
                ],
            };
        },
        isAnyLoading() {
            return this.isLoading || this.isLoadingSiparis;
        },
        // ---- Yeni: siparisBazliData'dan siparişlere göre gruplanmış liste
        groupedOrders() {
            // Map siparisNo -> { siparisNo, siparisTarihi, items: [], totals... }
            const map = new Map();

            for (const item of this.siparisBazliData || []) {
                const key = item.siparisNo || "UNKNOWN";
                if (!map.has(key)) {
                    map.set(key, {
                        siparisNo: key,
                        siparisTarihi: item.siparisTarihi || item.SiparisTarihi || null,
                        items: [],
                        totalAdet: 0,
                        totalCiro: 0,
                        totalKar: 0,
                        matchedCount: 0
                    });
                }

                const g = map.get(key);
                // Adet ve fiyat korunur (adet * trendyolFiyat = ciro)
                const adet = Number(item.adet || 0);
                const trendyolBirim = Number(item.trendyolFiyat || 0);
                const ciro = adet * trendyolBirim;
                const kar = Number(item.karZarar || 0);

                g.items.push(item);
                g.totalAdet += adet;
                g.totalCiro += ciro;
                g.totalKar += kar;
                if (item.otostickerEslesti) g.matchedCount += 1;

                // If no siparisTarihi set yet, try to set from first valid
                if (!g.siparisTarihi && item.siparisTarihi) g.siparisTarihi = item.siparisTarihi;
            }

            // Convert to array and compute derived fields
            const arr = Array.from(map.values()).map(g => {
                const performancePct = g.totalCiro !== 0 ? (g.totalKar / g.totalCiro) * 100 : 0;
                const matchRate = g.items.length ? (g.matchedCount / g.items.length) * 100 : 0;
                return {
                    ...g,
                    performancePct,
                    matchRate,
                };
            });

            // Sorting by latest order date by default
            return arr.sort((a, b) => {
                const da = new Date(a.siparisTarihi || 0);
                const db = new Date(b.siparisTarihi || 0);
                return db - da;
            });
        },

        // Filtrelenmiş ve sıralanmış sipariş verisi — burada groupedOrders baz alınır
        filteredGroupedOrders() {
            const term = (this.searchTerm || "").toLowerCase();

            let list = this.groupedOrders;

            // Eğer filterStatus set ise sadece bu koşulu sağlayan siparişleri göster
            if (this.filterStatus === "matched") {
                list = list.filter(g => g.items.every(i => i.otostickerEslesti));
            } else if (this.filterStatus === "unmatched") {
                list = list.filter(g => g.items.every(i => !i.otostickerEslesti));
            }

            // Arama: siparisNo, ürün adı veya otosticker adı
            if (term) {
                list = list.filter(g =>
                    g.siparisNo.toLowerCase().includes(term) ||
                    g.items.some(i => (i.urunAdi || "").toLowerCase().includes(term) || (i.otostickerAdi || "").toLowerCase().includes(term))
                );
            }

            // Sıralama; burada sıralamayı groupedOrders üzerinde uygulamak uygun
            list = list.sort((a, b) => {
                switch (this.sortBy) {
                    case "karZarar":
                        return (b.totalKar || 0) - (a.totalKar || 0);
                    case "karZararYuzde":
                        return (b.performancePct || 0) - (a.performancePct || 0);
                    case "eslesmeSkoru":
                        // average eslesmeSkoru across items
                        const avgA = a.items.reduce((s, x) => s + (x.eslesmeSkoru || 0), 0) / (a.items.length || 1);
                        const avgB = b.items.reduce((s, x) => s + (x.eslesmeSkoru || 0), 0) / (b.items.length || 1);
                        return avgB - avgA;
                    case "siparisTarihi":
                        return new Date(b.siparisTarihi) - new Date(a.siparisTarihi);
                    default:
                        return 0;
                }
            });

            return list;
        },
    },
    methods: {
        computeLastNDaysCounts(days = this.selectedDays) {
            const end = new Date();
            const start = new Date();
            start.setDate(end.getDate() - (days - 1));

            const labels = [];
            const map = {};
            for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
                const key = d.toISOString().slice(0, 10);
                labels.push(key);
                map[key] = { count: 0, total: 0 };
            }

            for (const o of this.orders) {
                if (!o.createdAt) continue;

                let parsed = new Date(o.createdAt);
                if (isNaN(parsed)) continue;

                const key = parsed.toISOString().slice(0, 10);
                if (map[key]) {
                    map[key].count += 1;
                    map[key].total += Number(o.toplamTutar || 0);
                }
            }

            const counts = labels.map((l) => map[l]?.count || 0);
            return { labels, counts };
        },

        computeLastNDaysMap(days = this.selectedDays) {
            const end = new Date();
            const start = new Date();
            start.setDate(end.getDate() - (days - 1));

            const labels = [];
            const map = {};
            for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
                const key = d.toISOString().slice(0, 10);
                labels.push(key);
                map[key] = { count: 0, total: 0 };
            }

            for (const o of this.orders) {
                if (!o.createdAt) continue;

                let parsed = new Date(o.createdAt);
                if (isNaN(parsed)) continue;

                const key = parsed.toISOString().slice(0, 10);
                if (map[key]) {
                    map[key].count += 1;
                    map[key].total += Number(o.toplamTutar || 0);
                }
            }

            return { labels, map };
        },

        updateCharts() {
            this.$forceUpdate();
        },

        async fetchSiparisBazli() {
            this.isLoadingSiparis = true;
            try {
                const kullaniciId = localStorage.getItem("kullanici_id");
                const res = await api.get(
                    `/api/siparisler/urun-kar-zarar?siparisBazli=true&kullaniciId=${kullaniciId}`
                );
                this.siparisBazliData = res.data.siparisBazli || [];
            } catch (err) {
                console.error("API Error:", err);
                this.siparisBazliData = [];
            } finally {
                this.isLoadingSiparis = false;
            }
        },

        // Yeni: toggle grup aç/kapa
        toggleGroup(siparisNo) {
            if (this.openGroups.has(siparisNo)) {
                this.openGroups.delete(siparisNo);
            } else {
                this.openGroups.add(siparisNo);
            }
            // Force reactivity for Set
            this.openGroups = new Set(this.openGroups);
        },

        isOpen(siparisNo) {
            return this.openGroups.has(siparisNo);
        },

        // Yeni metodlar (hesaplamalar)
        getTotalProfitLoss() {
            return this.siparisBazliData.reduce((total, s) => total + (Number(s.karZarar) || 0), 0);
        },

        getMatchedProductsCount() {
            return this.siparisBazliData.filter(s => s.otostickerEslesti).length;
        },

        getAverageProfitPercentage() {
            const matchedProducts = this.siparisBazliData.filter(s => s.otostickerEslesti && s.karZararYuzde !== undefined);
            if (matchedProducts.length === 0) return 0;
            return matchedProducts.reduce((total, s) => total + Number(s.karZararYuzde || 0), 0) / matchedProducts.length;
        },

        getTotalRevenue() {
            return this.siparisBazliData.reduce((total, s) => total + ((Number(s.trendyolFiyat) || 0) * (Number(s.adet) || 0)), 0);
        },

        getRowClass(karZarar) {
            if (karZarar > 0) return 'table-success';
            if (karZarar < 0) return 'table-danger';
            return '';
        },

        getMatchBadgeClass(durum) {
            switch (durum) {
                case 'Mükemmel': return 'bg-success';
                case 'İyi': return 'bg-primary';
                case 'Orta': return 'bg-warning';
                case 'Zayıf': return 'bg-secondary';
                default: return 'bg-danger';
            }
        },

        formatDate(dateString) {
            if (!dateString) return '-';
            const date = new Date(dateString);
            return date.toLocaleDateString('tr-TR', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        },

        // Helper: format money
        fmtMoney(val) {
            return formatCurrency(Number(val || 0), "TRY");
        }
    },
    async mounted() {

        this.isLoading = true;
        try {
            const kullaniciId = localStorage.getItem("kullanici_id");
            const entegrasyonRes = await api.get(`api/entegrasyonlar/by-user/${kullaniciId}`);
            this.entegrasyonId = entegrasyonRes.data.id;
            if (!this.entegrasyonId) {
                this.isLoading = false;
                return;
            }

            const res = await api.get(`/api/Siparisler/kullanici/${kullaniciId}?sort=desc`);
            this.orders = res.data.data;
            // If user opens siparis tab immediately, fetch
            if (this.showSiparisTab === "siparis") {
                this.fetchSiparisBazli();
            }
        } catch (err) {
            console.error(err);
            this.orders = [];
        } finally {
            this.isLoading = false;
        }
    },
};
</script>

<template>
    <div class="summary-container p-4">
        <h1 class="text-2xl font-bold mb-4">Sipariş Özeti</h1>
        <div class="tabs mb-4">
            <button :class="{ 'active-tab': showSiparisTab === 'grafik' }"
                @click="showSiparisTab = 'grafik'">Grafikler</button>
            <button :class="{ 'active-tab': showSiparisTab === 'siparis' }" @click="showSiparisTab = 'siparis'">Sipariş
                Bazlı</button>
        </div>
        <div v-if="isLoading" class="text-center py-4">Veriler yükleniyor...</div>
        <div v-else>
            <div class="mb-3">
                <label for="daysInput" class="form-label">Grafik için gün sayısı:</label>
                <input id="daysInput" type="number" min="1" v-model.number="selectedDays" class="form-control"
                    style="width:100px;" @change="updateCharts">
            </div>

            <div v-if="showSiparisTab === 'grafik'">
                <div class="grid charts-container">
                    <div class="card p-3 border rounded" style="height:320px;">
                        <h4 class="mb-2">Sipariş Trend</h4>
                        <LineChart :data="chart30DaysData" :options="chartOptions" :height="270" />
                    </div>
                    <div class="card p-3 border rounded" style="height:320px;">
                        <h4 class="mb-2">Duruma Göre Dağılım</h4>
                        <Doughnut :data="chartStatusData" :options="chartOptions" :height="270" />
                    </div>
                    <div class="card p-3 border rounded" style="height:360px;">
                        <h4 class="mb-2">Günlük Toplam Tutar</h4>
                        <Bar :data="chartDailyTotalData" :options="chartCurrencyOptions" :height="300" />
                    </div>
                </div>
            </div>

            <div v-if="showSiparisTab === 'siparis'">
                <!-- Özet Kartları -->
                <div class="row mb-4" v-if="siparisBazliData.length > 0">
                    <div class="col-md-3">
                        <div class="card bg-primary text-white">
                            <div class="card-body">
                                <h5 class="card-title">Toplam Kar/Zarar</h5>
                                <h3 :class="getTotalProfitLoss() >= 0 ? 'text-success' : 'text-danger'">
                                    {{ fmtMoney(getTotalProfitLoss()) }}
                                </h3>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card bg-info text-white">
                            <div class="card-body">
                                <h5 class="card-title">Eşleşen Ürünler</h5>
                                <h3>{{ getMatchedProductsCount() }}</h3>
                                <small>{{ siparisBazliData.length }} üründen</small>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card bg-success text-white">
                            <div class="card-body">
                                <h5 class="card-title">Ortalama Kar %</h5>
                                <h3>{{ getAverageProfitPercentage().toFixed(1) }}%</h3>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card bg-warning text-white">
                            <div class="card-body">
                                <h5 class="card-title">Toplam Ciro</h5>
                                <h3>{{ fmtMoney(getTotalRevenue()) }}</h3>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Filtreler -->
                <div class="row mb-3">
                    <div class="col-md-3">
                        <select v-model="filterStatus" class="form-select">
                            <option value="">Tüm Eşleşmeler</option>
                            <option value="matched">Sadece Eşleşenler</option>
                            <option value="unmatched">Sadece Eşleşmeyenler</option>
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select v-model="sortBy" class="form-select">
                            <option value="karZarar">Kar/Zarar'a Göre</option>
                            <option value="karZararYuzde">Kar %'ye Göre</option>
                            <option value="eslesmeSkoru">Eşleşme Skoruna Göre</option>
                            <option value="siparisTarihi">Sipariş Tarihine Göre</option>
                        </select>
                    </div>
                    <div class="col-md-6">
                        <input v-model="searchTerm" type="text" class="form-control" placeholder="Ürün adında veya sipariş no'da ara...">
                    </div>
                </div>

                <!-- GROUPED TABLE -->
                <div class="table-responsive" v-if="filteredGroupedOrders.length > 0">
                    <table class="table table-bordered table-hover">
                        <thead class="table-dark">
                            <tr>
                                <th></th>
                                <th>Sipariş No</th>
                                <th>Tarih</th>
                                <th>Ürün Adedi</th>
                                <th>Toplam Ciro</th>
                                <th>Toplam Kar</th>
                                <th>Performans %</th>
                                <th>Eşleşme Oranı</th>
                                <th>Detay</th>
                            </tr>
                        </thead>
                        <tbody>
                            <template v-for="group in filteredGroupedOrders" :key="group.siparisNo">
                                <!-- Summary row -->
                                <tr :class="getRowClass(group.totalKar)">
                                    <td>
                                        <i class="fas" :class="isOpen(group.siparisNo) ? 'fa-chevron-down' : 'fa-chevron-right'"></i>
                                    </td>
                                    <td><strong>{{ group.siparisNo }}</strong></td>
                                    <td>{{ formatDate(group.siparisTarihi) }}</td>
                                    <td class="text-center"><span class="badge bg-primary">{{ group.totalAdet }}</span></td>
                                    <td>{{ fmtMoney(group.totalCiro) }}</td>
                                    <td>{{ fmtMoney(group.totalKar) }}</td>
                                    <td>{{ group.performancePct.toFixed(1) }}%</td>
                                    <td>{{ group.matchRate.toFixed(0) }}%</td>
                                    <td>
                                        <button class="btn btn-sm btn-outline-secondary" @click="toggleGroup(group.siparisNo)">
                                            {{ isOpen(group.siparisNo) ? 'Kapat' : 'Detay' }}
                                        </button>
                                    </td>
                                </tr>

                                <!-- Expanded row: product details -->
                                <tr v-if="isOpen(group.siparisNo)">
                                    <td colspan="9" class="p-0">
                                        <div class="p-3 bg-light">
                                            <table class="table table-sm mb-0">
                                                <thead>
                                                    <tr class="table-secondary">
                                                        <th>Ürün Resmi</th>
                                                        <th>Ürün Adı</th>
                                                        <th>Adet</th>
                                                        <th>Trendyol Fiyat</th>
                                                        <th>Otosticker Fiyat</th>
                                                        <th>Kar (TL)</th>
                                                        <th>Kar %</th>
                                                        <th>Eşleşme Durumu</th>
                                                        <th>Eşleşme Skoru</th>
                                                        <th>Otosticker Adı</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    <tr v-for="item in group.items" :key="item.urunId + '-' + item.siparisNo" :class="getRowClass(item.karZarar)">
                                                        <td style="width:90px;">
                                                            <img v-if="item.urunresmi" :src="item.urunresmi" alt="Ürün" style="width:70px; height:70px; object-fit:cover;" class="img-thumbnail" />
                                                            <div v-else style="width:70px; height:70px;" class="bg-light d-flex align-items-center justify-content-center">
                                                                <i class="fas fa-image text-muted"></i>
                                                            </div>
                                                        </td>
                                                        <td><strong>{{ item.urunAdi }}</strong></td>
                                                        <td class="text-center">{{ item.adet }}</td>
                                                        <td>{{ fmtMoney(item.trendyolFiyat) }}</td>
                                                        <td>{{ fmtMoney(item.otostickerFiyat) }}</td>
                                                        <td>{{ fmtMoney(item.karZarar) }}</td>
                                                        <td>{{ Number(item.karZararYuzde || 0).toFixed(1) }}%</td>
                                                        <td><span :class="['badge', getMatchBadgeClass(item.eslesmeDurumu)]">{{ item.eslesmeDurumu }}</span></td>
                                                        <td>{{ item.eslesmeSkoru }}</td>
                                                        <td>{{ item.otostickerAdi }}</td>
                                                    </tr>
                                                </tbody>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                            </template>
                        </tbody>
                    </table>
                </div>

                <div v-else class="text-center py-4">
                    <div class="alert alert-info">
                        <i class="fas fa-info-circle"></i>
                        Filtre kriterlerinize uygun sipariş bulunamadı.
                    </div>
                </div>
            </div>

            <div v-if="isLoadingSiparis" class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Yükleniyor...</span>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.summary-container {
    max-width: 1100px;
    margin: 0 auto;
}

.charts-container {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
}

@media (max-width: 768px) {
    .charts-container {
        grid-template-columns: 1fr;
    }

    .charts-container .card {
        width: 100%;
    }
}

.tabs {
    display: flex;
    gap: 8px;
    margin-bottom: 16px;
}

.tabs button {
    padding: 6px 16px;
    border: 1px solid #ccc;
    background: #f5f5f5;
    cursor: pointer;
    border-radius: 4px;
}

.tabs button.active-tab {
    background: #36a2eb;
    color: white;
}

.spinner-border {
    width: 3rem;
    height: 3rem;
    margin: 40px auto;
    display: block;
}

/* Yeni stiller */
.price-info {
    font-size: 0.9em;
}

.trendyol-price {
    color: #ff6b35;
    margin-bottom: 2px;
}

.otosticker-price {
    color: #28a745;
}

.order-info {
    font-size: 0.9em;
}

.table-hover tbody tr:hover {
    background-color: rgba(0, 0, 0, 0.05);
}

.table-success {
    background-color: rgba(40, 167, 69, 0.06);
}

.table-danger {
    background-color: rgba(220, 53, 69, 0.06);
}

.badge {
    font-size: 0.75em;
}

.card {
    box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    border: 1px solid rgba(0, 0, 0, 0.125);
}

.card-body h3 {
    margin-bottom: 0.5rem;
}

.img-thumbnail {
    border-radius: 0.375rem;
}

@media (max-width: 768px) {
    .row .col-md-3 {
        margin-bottom: 1rem;
    }

    .table-responsive {
        font-size: 0.8em;
    }

    .price-info {
        font-size: 0.8em;
    }
}
</style>
