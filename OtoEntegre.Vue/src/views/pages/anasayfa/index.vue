<script>
import api from "../../axios";
import OrderDetailModal from "./OrderDetailModal.vue";

export default {
  name: "Siparislerim",
  components: { OrderDetailModal },
  data() {
    return {
      orders: [],
      currentPage: 0,
      pageSize: 10,
      totalOrders: 0,
      isLoading: false,
      showModal: false,
      selectedOrder: null,
      bayiId: localStorage.getItem("bayi_id"),
    };
  },
  computed: {
    totalPages() {
      return Math.ceil(this.totalOrders / this.pageSize);
    },
    totalAmount() {
      return this.orders.reduce((sum, order) => sum + (order.summary?.overall || 0), 0);
    },
  },
  methods: {
    async fetchOrders() {
      try {
        this.isLoading = true;
        const res = await api.get(
          `api/siparisler/by-user/${this.bayiId}?page=${this.currentPage}&pageSize=${this.pageSize}`
        );
        this.orders = res.data;

        if (this.totalOrders === 0) {
          const countRes = await api.get(
            `api/siparisler/by-user/${this.bayiId}/count`
          );
          this.totalOrders = countRes.data;
        }
      } catch (error) {
        console.error("Siparişler alınamadı:", error);
      } finally {
        this.isLoading = false;
      }
    },
    async fetchOrdersOnly() {
      try {
        this.isLoading = true;
        const res = await api.get(
          `api/siparisler/by-user/${this.bayiId}?page=${this.currentPage}&pageSize=${this.pageSize}`
        );
        this.orders = res.data;
      } catch (error) {
        console.error("Siparişler alınamadı:", error);
      } finally {
        this.isLoading = false;
      }
    },
    goToPage(page) {
      if (page < 0 || page >= this.totalPages) return;
      this.currentPage = page;
      this.fetchOrdersOnly();
    },
    formatDate(dateStr) {
      if (!dateStr) return "";
      const d = new Date(dateStr);
      return (
        d.toLocaleDateString("tr-TR", {
          year: "numeric",
          month: "2-digit",
          day: "2-digit",
        }) +
        " " +
        d.toLocaleTimeString("tr-TR", {
          hour: "2-digit",
          minute: "2-digit",
        })
      );
    },
    formatCurrency(amount) {
      if (amount == null || isNaN(amount)) return "0,00 ₺";
      return new Intl.NumberFormat("tr-TR", {
        style: "currency",
        currency: "TRY",
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(amount);
    },
    openModal(order) {
      this.selectedOrder = order;
      this.showModal = true;
    },
    closeModal() {
      this.showModal = false;
      this.selectedOrder = null;
    },
  },
  mounted() {
    this.fetchOrders();
  },
};
</script>
<template>
  <div class="dashboard-container p-4">
    <h1 class="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">Siparişlerim</h1>

    <!-- Özet Kartlar -->
    <div class="summary-cards mb-4 d-flex flex-wrap gap-3">
      <div class="card bg-primary text-white p-3 rounded shadow" style="min-width: 180px;">
        <div class="text-lg fw-semibold">Toplam Sipariş</div>
        <div class="fs-3">{{ totalOrders }}</div>
      </div>
      <div class="card bg-success text-white p-3 rounded shadow" style="min-width: 180px;">
        <div class="text-lg fw-semibold">Toplam Tutar</div>
        <div class="fs-3">{{ formatCurrency(totalAmount) }}</div>
      </div>
    </div>

    <!-- Loading Spinner -->
    <div v-if="isLoading" class="d-flex justify-content-center align-items-center py-5">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Yükleniyor...</span>
      </div>
      <span class="ms-2 fw-semibold">Siparişler yükleniyor...</span>
    </div>

    <!-- Sipariş Tablosu -->
    <div v-else>
      <div class="table-responsive rounded shadow">
        <table class="table table-bordered table-hover">
          <thead class="table-light">
            <tr>
              <th>Sipariş No</th>
              <th>Toplam Tutar</th>
              <th>Tarih</th>
              <th>Detay</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="order in orders" :key="order.order.id">
              <td class="font-monospace">{{ order.order.code }}</td>
              <td class="fw-semibold">{{ formatCurrency(order.summary.overall) }}</td>
              <td>{{ formatDate(order.order.createdAt) }}</td>
              <td>
                <button class="btn btn-sm btn-info" @click="openModal(order)">Detay</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <nav class="d-flex justify-content-center mt-3">
        <ul class="pagination">
          <li :class="['page-item', currentPage === 0 ? 'disabled' : '']">
            <button class="page-link" @click="goToPage(currentPage - 1)">&laquo;</button>
          </li>
          <li v-for="page in totalPages" :key="page" :class="['page-item', currentPage === page - 1 ? 'active' : '']">
            <button class="page-link" @click="goToPage(page - 1)">{{ page }}</button>
          </li>
          <li :class="['page-item', currentPage === totalPages - 1 ? 'disabled' : '']">
            <button class="page-link" @click="goToPage(currentPage + 1)">&raquo;</button>
          </li>
        </ul>
      </nav>
    </div>

    <!-- Sipariş Detay Modalı -->
    <OrderDetailModal :show="showModal" :order="selectedOrder" @close="closeModal" />
  </div>
</template>
