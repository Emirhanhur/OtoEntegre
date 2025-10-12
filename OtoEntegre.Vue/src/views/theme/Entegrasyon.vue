<script>
import axios from "axios";
import { Bar, Line, Doughnut } from 'vue-chartjs'
import api from "../axios";
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
} from 'chart.js'
import { formatCurrency } from '../../utils/format'

Chart.register(BarElement, CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Tooltip, Legend)

export default {
  components: { Bar, Line, Doughnut },
  data() {
    return {
      orders: [],
      selectedStatus: "", // seçili filtre
      currentPage: 1,
      pageSize: 10,
      entegrasyonId: null, // her sayfada 10 sipariş göster
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
      chartOptions: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: { x: { ticks: { autoSkip: true, maxTicksLimit: 10 } } }
      },
      chartCurrencyOptions: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: {
          y: {
            ticks: {
              callback: (val) => formatCurrency(val, 'TRY')
            }
          }
        }
      }
    };
  },
  computed: {
    filteredOrders() {
      if (!this.selectedStatus) {
        return this.orders;
      }
      return this.orders.filter(
        (order) => order.status === this.selectedStatus
      );
    },
    totalPages() {
      return Math.ceil(this.filteredOrders.length / this.pageSize) || 1;
    },
    paginatedOrders() {
      const start = (this.currentPage - 1) * this.pageSize;
      return this.filteredOrders.slice(start, start + this.pageSize);
    },
    chart30DaysData() {
      const { labels, counts } = this.computeLast30DaysCounts();
      return {
        labels,
        datasets: [
          {
            label: 'Sipariş Adedi',
            data: counts,
            borderColor: '#36a2eb',
            backgroundColor: 'rgba(54,162,235,0.2)'
          }
        ]
      }
    },
    chartStatusData() {
      const statusCounts = {};
      for (const o of this.orders) {
        const k = o.status || 'Unknown';
        statusCounts[k] = (statusCounts[k] || 0) + 1;
      }
      const labels = Object.keys(statusCounts).map(k => this.statusMap[k] || k);
      const data = Object.values(statusCounts);
      const colors = ['#36a2eb', '#ff6384', '#ffcd56', '#4bc0c0', '#9966ff', '#ff9f40', '#8dd17e', '#f67019', '#00a950', '#b3b3b3'];
      return {
        labels,
        datasets: [{ data, backgroundColor: labels.map((_, i) => colors[i % colors.length]) }]
      }
    },
    chartDailyTotalData() {
      const { labels, map } = this.computeLast30DaysMap();
      const totals = labels.map(l => map[l]?.total || 0);
      return {
        labels,
        datasets: [
          {
            label: 'Günlük Toplam Tutar',
            data: totals,
            backgroundColor: 'rgba(75,192,192,0.5)'
          }
        ]
      }
    }
  },
  watch: {
    // filtre değişince sayfayı 1'e resetle
    selectedStatus() {
      this.currentPage = 1;
    },
  },
  method() { },
  async mounted() {


    try {
      const kullaniciId = localStorage.getItem("kullanici_id") // Buraya kullanıcı_id gelecek

      // 1️⃣ Kullanıcıya ait entegrasyonları çek
      const entegrasyonRes = await api.get(`api/entegrasyonlar/by-user/${kullaniciId}`);
      this.entegrasyonId = entegrasyonRes.data.id; // servisten gelen id

      if (!this.entegrasyonId) {
        console.error("Kullanıcıya ait Trendyol entegrasyonu bulunamadı");
        return;
      }


      const res = await api.get(
        `api/entegrasyonlar/trendyol-orders/${this.entegrasyonId}?sortField=orderDate&sortDir=desc` // entegrasyonId
      );
      this.orders = res.data.orders;
      console.log(res.data)
    } catch (err) {
      console.error("Trendyol siparişleri alınamadı", err);
    }
  },
  methods: {
    formatMoney(amount, currency) {
      return formatCurrency(amount, currency);
    },
    formatOrderDate(val) {
      const d = new Date(val);
      const adj = new Date(d.getTime() - 3 * 60 * 60 * 1000);
      return adj.toLocaleString(undefined, {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit'
      });
    },
    computeLast30DaysCounts() {
      const end = new Date();
      const start = new Date();
      start.setDate(end.getDate() - 29);
      const labels = [];
      const map = {};
      for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
        const key = d.toISOString().slice(0, 10);
        labels.push(key);
        map[key] = { count: 0, total: 0 };
      }
      for (const o of this.orders) {
        const dt = new Date(new Date(o.orderDate).getTime() - 3 * 60 * 60 * 1000);
        const key = new Date(dt.getFullYear(), dt.getMonth(), dt.getDate()).toISOString().slice(0, 10);
        if (map[key]) {
          map[key].count += 1;
          map[key].total += Number(o.totalPrice || 0);
        }
      }
      const counts = labels.map(l => map[l]?.count || 0);
      return { labels, counts };
    },
    computeLast30DaysMap() {
      const end = new Date();
      const start = new Date();
      start.setDate(end.getDate() - 29);
      const labels = [];
      const map = {};
      for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
        const key = d.toISOString().slice(0, 10);
        labels.push(key);
        map[key] = { count: 0, total: 0 };
      }
      for (const o of this.orders) {
        const dt = new Date(new Date(o.orderDate).getTime() - 3 * 60 * 60 * 1000);
        const key = new Date(dt.getFullYear(), dt.getMonth(), dt.getDate()).toISOString().slice(0, 10);
        if (map[key]) {
          map[key].count += 1;
          map[key].total += Number(o.totalPrice || 0);
        }
      }
      return { labels, map };
    }
  }
};
</script>
<template>
  <div>
    <h2 class="text-xl font-bold mb-4">Trendyol Siparişleri</h2>

    <!-- 📊 Charts -->
    <div class="grid" style="display:grid;grid-template-columns:1fr 1fr;gap:16px;" v-if="orders && orders.length">
      <div class="card p-3 border rounded" style="height:320px;">
        <h4 class="mb-2">Son 30 Gün Sipariş Trend</h4>
        <Line :data="chart30DaysData" :options="chartOptions" :height="270" />
      </div>
      <div class="card p-3 border rounded" style="height:320px;">
        <h4 class="mb-2">Duruma Göre Dağılım</h4>
        <Doughnut :data="chartStatusData" :options="chartOptions" :height="270" />
      </div>
      <div class="card p-3 border rounded" style="grid-column: 1 / -1; height:360px;">
        <h4 class="mb-2">Günlük Toplam Tutar (Son 30 Gün)</h4>
        <Bar :data="chartDailyTotalData" :options="chartCurrencyOptions" :height="300" />
      </div>
    </div>

    <!-- 🔽 Status Filtresi -->
    <div class="mb-4">
      <label for="statusFilter" class="mr-2 font-semibold">Duruma Göre Filtrele:</label>
      <select id="statusFilter" v-model="selectedStatus" class="border px-3 py-2 rounded">
        <option value="">Tümü</option>
        <option v-for="(turkish, eng) in statusMap" :key="eng" :value="eng">
          {{ turkish }}
        </option>
      </select>
    </div>

    <!-- 📋 Tablo -->
    <table class="table-auto w-100 border">
      <thead>
        <tr class="bg-gray-100">
          <th class="border px-4 py-2">ID</th>
          <th class="border px-4 py-2">Sipariş No</th>
          <th class="border px-4 py-2">Durum</th>
          <th class="border px-4 py-2">Tarih</th>
          <th class="border px-4 py-2">Toplam</th>
          <th class="border px-4 py-2">Para Birimi</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="order in paginatedOrders" :key="order.id">
          <td class="border px-4 py-2">{{ order.id }}</td>
          <td class="border px-4 py-2">{{ order.orderNumber }}</td>
          <td class="border px-4 py-2">{{ statusMap[order.status] }}</td>
          <td class="border px-4 py-2">
            {{ formatOrderDate(order.orderDate) }}
          </td>
          <td class="border px-4 py-2">{{ formatMoney(order.totalPrice, order.currencyCode) }}</td>
          <td class="border px-4 py-2">{{ order.currencyCode }}</td>
        </tr>
      </tbody>
      <tfoot>
        <div>
          Toplam <strong>{{ filteredOrders.length }}</strong> sipariş bulundu.
        </div>
      </tfoot>
    </table>

    <!-- 📌 Pagination -->
    <div class="d-flex items-align-center justify-content-center mt-4">


      <div class="flex space-x-2">
        <button class="px-3 py-1 border rounded" :disabled="currentPage === 1" @click="currentPage--">
          Önceki
        </button>

        <span>Sayfa {{ currentPage }} / {{ totalPages }}</span>

        <button class="px-3 py-1 border rounded" :disabled="currentPage === totalPages" @click="currentPage++">
          Sonraki
        </button>
      </div>
    </div>
  </div>
</template>