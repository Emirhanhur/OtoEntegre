<script>
import { Bar, Line as LineChart, Doughnut } from "vue-chartjs";
import api from "../../axios";
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
import { formatCurrency } from "../../../utils/format";

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
        };
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
            console.log("Siparişler:", this.orders);
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
        <div v-if="isLoading" class="text-center py-4">Veriler yükleniyor...</div>
        <div v-else>
            <div class="mb-3">
                <label for="daysInput" class="form-label">Grafik için gün sayısı:</label>
                <input id="daysInput" type="number" min="1" v-model.number="selectedDays" class="form-control"
                    style="width:100px;" @change="updateCharts">
            </div>

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
</style>
