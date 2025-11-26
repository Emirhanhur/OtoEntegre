<script>
import { formatCurrency } from '../../../utils/format'
import { Modal } from 'bootstrap';
import { nextTick } from 'vue';
import api from "../../axios";

export default {
  data() {
    return {
      orders: [],
      selectedStatus: null,
      currentPage: 1,
      pageSize: 10,
      isLoading: false,
      selectedOrder: null,
      searchQuery: "", // 🔍 Arama inputu için
      orderStatuses: [
        { key: null, label: 'Tümü', count: 0 },
        { key: 'CREATED', label: 'Oluşturuldu', count: 0 },
        { key: 'SHIPPED', label: 'Taşıma Durumunda', count: 0 },
        { key: 'PICKING', label: 'İşleme Alındı', count: 0 },
        { key: 'DELIVERED', label: 'Teslim Edildi', count: 0 },
        { key: 'INVOICED', label: 'Faturalandı', count: 0 },
        { key: 'CANCELLED', label: 'İptal Edildi', count: 0 },
        { key: 'UNDELIVERED', label: 'Teslim Edilemedi', count: 0 },
        { key: 'RETURNED', label: 'İade Edildi', count: 0 },
        { key: 'UNSUPPLIED', label: 'Temin Edilmemiş', count: 0 },
        { key: 'AWAITING', label: 'Bekleniyor', count: 0 },
        { key: 'UNPACKED', label: 'Pakete Çıktı', count: 0 },
        { key: 'AT_COLLECTION_POINT', label: 'Teslimat Noktasında', count: 0 },
        { key: 'VERIFIED', label: 'Doğrulandı', count: 0 }
      ]
    };
  },
  computed: {
    totalPages() {
      return Math.ceil(this.filteredOrders.length / this.pageSize) || 1;
    },
    filteredOrders() {
      let list = this.orders;

      // Statüye göre filtre
      if (this.selectedStatus) {
        list = list.filter(order => order.originalStatus === this.selectedStatus);
      }

      // 🔍 Arama filtresi
      if (this.searchQuery.trim() !== "") {
        const q = this.searchQuery.toLowerCase();
        list = list.filter(order =>
          (order.siparisNumarasi && order.siparisNumarasi.toString().toLowerCase().includes(q)) ||
          (order.musteriAdSoyad && order.musteriAdSoyad.toLowerCase().includes(q)) ||
          (order.urunAdi && order.urunAdi.toLowerCase().includes(q)) // API’den ürün adı geliyorsa
        );
      }

      return list;
    },
    paginatedOrders() {
      const start = (this.currentPage - 1) * this.pageSize;
      return this.filteredOrders.slice(start, start + this.pageSize);
    },
  },
  watch: {
    selectedStatus() {
      this.currentPage = 1;
    },
    searchQuery() {
      this.currentPage = 1; // Arama yapıldığında ilk sayfaya dön
    }
  },
  async mounted() {
    this.loadOrders();
  },
  beforeUnmount() {
    clearInterval(this.pollingInterval);
  },
  methods: {
    async loadOrders(durum = null) {
      try {
        this.isLoading = true;
        let url = "/api/siparisler?sort=desc";
        if (durum !== null) url += `?durum=${durum}`;
        const res = await api.get(url);
        this.orders = res.data;
        this.orders.forEach(element => {
          element.originalStatus = element.durum?.toUpperCase() || '';

          const statusMap = {
            CREATED: "Oluşturuldu",
            SHIPPED: "Taşıma Durumunda",
            PICKING: "İşleme Alındı",
            DELIVERED: "Teslim Edildi",
            INVOICED: "Faturalandı",
            CANCELLED: "İptal Edildi",
            UNDELIVERED: "Teslim Edilemedi",
            RETURNED: "İade Edildi",
            UNSUPPLIED: "Temin Edilmemiş",
            AWAITING: "Bekleniyor",
            UNPACKED: "Pakete Çıktı",
            AT_COLLECTION_POINT: "Teslimat Noktasında",
            VERIFIED: "Doğrulandı"
          };

          element.durum = statusMap[element.originalStatus] || element.originalStatus;
        });

        this.updateStatusCounts();
      } catch (err) {
        console.error("Siparişler yüklenemedi", err);
      } finally {
        this.isLoading = false;
      }
    },

    updateStatusCounts() {
      this.orderStatuses.forEach(status => status.count = 0);
      this.orderStatuses[0].count = this.orders.length;

      this.orders.forEach(order => {
        const statusObj = this.orderStatuses.find(s => s.key === order.originalStatus);
        if (statusObj) statusObj.count++;
      });
    },

    selectStatus(statusKey) {
      this.selectedStatus = statusKey;
    },

    async sendTelegram(orderId) {
      try {
        const res = await api.post(`/api/entegrasyonlar/send-siparis-telegram/${orderId}`);
        if (res.data.sent) {
          alert("Sipariş Mesaj olarak gönderildi!");
          this.loadOrders(this.selectedStatus);
        } else {
          alert("Gönderilemedi.");
        }
      } catch (err) {
        console.error(err);
        alert("Hata oluştu.");
      }
    },

    formatMoney(amount, currency) {
      return formatCurrency(amount, currency);
    },

    formatOrderDate(val) {
      const d = new Date(val);
      d.setHours(d.getHours() - 3);
      return d.toLocaleString('tr-TR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    },

    async openDetailModal(order) {
      this.selectedOrder = order;
      await nextTick();
      if (this.$refs.orderModal && typeof this.$refs.orderModal.showModal === 'function') {
        this.$refs.orderModal.showModal();
      }
    }
  }
};
</script>

<template>
  <div>
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h2>Hepsi Burada Siparişleri</h2>
      <!-- 🔍 Arama Inputu -->
      <input v-model="searchQuery" type="text" class="form-control w-25"
        placeholder="Sipariş no, müşteri adı veya ürün adı ara..." />
    </div>

    <!-- Durum Tabları -->
    <div class="d-flex flex-wrap gap-2 mb-3">
      <button v-for="status in orderStatuses" :key="status.key" class="btn d-flex align-items-center gap-2" :class="{
        'btn-primary': selectedStatus === status.key,
        'btn-outline-secondary': selectedStatus !== status.key
      }" @click="selectStatus(status.key)">
        <span>{{ status.label }}</span>
        <span class="badge ms-1" :class="{
          'bg-danger': status.count === 0,
          'bg-success': status.count > 0 && selectedStatus !== status.key
        }">
          {{ status.count }}
        </span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="text-center py-4">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-2 text-secondary">Siparişler yükleniyor...</p>
    </div>

    <!-- Tablo -->
    <div v-else class="table-responsive">
      <table class="table table-bordered table-hover">
        <thead class="table-light">
          <tr>
            <th>Sıra No</th>
            <th>Sipariş No</th>
            <th>Müşteri Adı</th>
            <th>Durum</th>
            <th>Tarih</th>
            <th>Toplam</th>
            <th class="text-center">Mesaj Durumu</th>
            <th class="text-center">#</th>
          </tr>
        </thead>
        <!-- <tbody>
          <tr v-for="(order, index) in paginatedOrders" :key="order.id">
            <td>{{ (currentPage - 1) * pageSize + index + 1 }}</td>
            <td>{{ order.siparisNumarasi }}</td>
            <td>{{ order.musteriAdSoyad }}</td>
            <td>
              <span class="badge" :class="{
                'bg-success': order.originalStatus === 'DELIVERED',
                'bg-primary': order.originalStatus === 'SHIPPED',
                'bg-warning text-dark': ['CREATED', 'AWAITING'].includes(order.originalStatus),
                'bg-danger': order.originalStatus === 'CANCELLED',
                'bg-secondary': !['DELIVERED', 'SHIPPED', 'CREATED', 'AWAITING', 'CANCELLED'].includes(order.originalStatus)
              }">
                {{ order.durum }}
              </span>
            </td>
            <td>{{ formatOrderDate(order.createdAt) }}</td>
            <td>{{ formatMoney(order.toplamTutar, "TRY") }}</td>
              <td class="text-center">
              <span v-if="order.telegramSent" class="text-success">
                <span class="material-icons">check_circle</span>
              </span>
              <button v-else class="btn btn-primary btn-sm" @click="sendTelegram(order.id)">
                Gönder
              </button>
            </td>
            <td class="text-center">
              <button class="btn btn-outline-primary btn-sm" @click="openDetailModal(order)">
                <span class="material-icons">visibility</span>
              </button>
            </td>
          </tr>
        </tbody> 
        <tfoot v-if="paginatedOrders.length < 1">
          <tr>
              <td colspan="8" class="text-center py-4 text-secondary">
              <span class="material-icons fs-1 mb-2 align-middle">inbox</span>
              <p>{{ selectedStatus ? 'Bu durumda sipariş bulunmuyor' : 'Henüz sipariş yok' }}</p>
            </td>
          </tr>
        </tfoot>-->
      </table>
    </div>

    <!-- Pagination
    <div v-if="!isLoading && paginatedOrders.length > 0" class="d-flex justify-content-center mt-3 gap-2">
      <button class="btn btn-outline-secondary" :disabled="currentPage === 1" @click="currentPage--">
        <i class="fas fa-chevron-left me-1"></i> Önceki
      </button>
      <span class="align-self-center">
        Sayfa {{ currentPage }} / {{ totalPages }} ({{ filteredOrders.length }} sipariş)
      </span>
      <button class="btn btn-outline-secondary" :disabled="currentPage === totalPages" @click="currentPage++">
        Sonraki <i class="fas fa-chevron-right ms-1"></i>
      </button>
    </div>
 -->
  </div>
</template>
