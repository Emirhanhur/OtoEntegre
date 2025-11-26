<template>
  <div class="container mt-4">
    <h4>Trendyol Cari Hesap Ekstresi</h4>

    <!-- 🔹 FİLTRELER -->
    <div class="filters d-flex flex-wrap gap-2 mb-3">
      <input type="date" v-model="startDate" class="form-control" />
      <input type="date" v-model="endDate" class="form-control" />
      <select v-model="transactionType" class="form-select">
        <option value="Sale">Satış</option>
        <option value="Return">İade</option>
        <option value="Discount">İndirim</option>
        <option value="CommissionNegative">Komisyon Negatif</option>
      </select>
      <button class="btn btn-primary" @click="getEkstre">Sorgula</button>
    </div>

    <!-- 🔹 YÜKLENİYOR -->
    <div v-if="isLoading" class="text-center mt-3">
      <div class="spinner-border text-primary"></div>
    </div>

    <!-- 🔹 SATIŞ ÖZETİ -->
    <div v-if="!isLoading && ekstre.length" class="card mb-3 shadow-sm">
      <div class="card-body">
        <h5 class="card-title mb-3">📊 Satış Özeti ({{ formatDateRange() }})</h5>
        <div class="row text-center">
          <div class="col-md-6 col-12 mb-2">
            <div><strong>Toplam Satış:</strong></div>
            <div class="text-success fs-5">{{ formatCurrency(summary.totalSales) }}</div>
          </div>
          <div class="col-md-6 col-12 mb-2">
            <div><strong>Toplam Kâr:</strong></div>
            <div class="text-primary fs-5">{{ formatCurrency(summary.totalProfit) }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 🔹 TABLO -->
    <div class="table-responsive">
      <table v-if="!isLoading && ekstre.length" class="table dark:table-dark table-bordered table-hover">
      <thead class="table-light">
        <tr>
          <th>Tarih</th>
          <th>Borç</th>
          <th>Alacak</th>
          <th>Komisyon Oranı</th>
          <th>Komisyon Tutarı</th>
          <th>Hakediş Tutarı</th>
          <th>Sipariş Numarası</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(item, i) in ekstre" :key="i">
          <td>{{ formatDate(item.transactionDate) }}</td>
          <td class="text-danger">{{ item.debt != null ? formatCurrency(item.debt) : '' }}</td>
          <td class="text-success">{{ item.credit != null ? formatCurrency(item.credit) : '' }}</td>
          <td>{{ item.commissionRate != null ? (Number(item.commissionRate).toFixed(2) + ' %') : '' }}</td>
          <td>{{ item.commissionAmount != null ? formatCurrency(item.commissionAmount) : '' }}</td>
          <td>{{ item.sellerRevenue != null ? formatCurrency(item.sellerRevenue) : '' }}</td>

          <td> <!-- Kullanıcıya ait order ise TIKLANABİLİR --> <a
              v-if="userOrders.some(u => u.orderNumber === item.orderNumber)"
              @click.prevent="goToOrderDetail(item.orderNumber)" class="text-primary" style="cursor:pointer;"> {{ item.orderNumber }} </a>
            <!-- Değilse tıklanamaz gri yazı --> <span v-else class="text-muted" style="cursor:not-allowed;"> {{
              item.orderNumber }} </span> </td>
        </tr>
      </tbody>
    </table>
    </div>
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div id="uyariToast" class="toast align-items-center text-bg-danger border-0">
        <div class="d-flex">
          <div class="toast-body">{{ toastMessage }}</div>
          <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
      </div>
    </div>

    <!-- 🔹 KAYIT YOK -->
    <div v-if="!isLoading && !ekstre.length" class="alert alert-warning mt-3">
      Kayıt bulunamadı.
    </div>
  </div>
</template>

<script>
import api from "../../axios";
import { formatCurrency } from "../../../utils/format";

export default {
  name: "CariEkstre",
  data() {
    return {
      kullaniciId: localStorage.getItem("kullanici_id"),
      userOrders: [], // 👉 kullanıcının sahip olduğu sipariş numaraları
    toastMessage: ""
,
      entegrasyonId: "",
      startDate: "",
      endDate: "",
      transactionType: "Sale",
      ekstre: [],
      summary: {
        totalSales: 0,
        totalProfit: 0,
      },
      isLoading: false,
    };
  },
  async mounted() {
    await this.loadUserOrders();
  },
  methods: {
    // Para biçimlendirme helper (utils içindeki fonksiyonu sarıyoruz)
    formatCurrency(value) {
      return value != null && value !== '' ? formatCurrency(value, 'TRY') : "";
    },
    showToast(msg) {
    this.toastMessage = msg;
    const toast = new bootstrap.Toast(document.getElementById('uyariToast'));
    toast.show();
  },
    async loadUserOrders() {
      try {
        const res = await api.get(`/api/Siparisler/kullanici/${this.kullaniciId}?sort=desc`);
        this.userOrders = res.data.data.map(s => ({
          orderNumber: s.siparisNumarasi,
          id: s.id
        }));
        console.log("Kullanıcı sipariş numaraları yüklendi:", this.userOrders);
      } catch (e) {
        console.error("Sipariş listesi alınamadı:", e);
      }
    },
    async getEkstre() {
      if (!this.startDate || !this.endDate) {
        alert("Lütfen tarih aralığı seçiniz");
        return;
      }

      // 🔥 1) Tarih aralığı kontrolü — max 15 gün
      const start = new Date(this.startDate);
      const end = new Date(this.endDate);
      const diffDays = (end - start) / (1000 * 60 * 60 * 24);

      if (diffDays > 15) {
       this.showToast("Tarih aralığı 15 günden uzun olamaz!");

        return;
      }

      this.isLoading = true;
      try {
        // 🔹 Entegrasyon bilgisi
        const entegrasyonRes = await api.get(`/api/Entegrasyonlar/by-user/${this.kullaniciId}`);
        if (!entegrasyonRes.data || !entegrasyonRes.data.id) {
          alert("Bu kullanıcıya ait entegrasyon bulunamadı.");
          return;
        }

        // 🔥 2) API'ye size=500 ekle (default)
        const params = new URLSearchParams({
          sellerId: entegrasyonRes.data.seller_Id,
          kullaniciId: this.kullaniciId,
          transactionType: this.transactionType || "",
          startDate: this.startDate,
          endDate: this.endDate,
          size: 500
        }).toString();

        // 🔹 Finans verisi
        const res = await api.get(`/api/trendyolfinance/get-cari-ekstre?${params}`);
        const data = res.data?.data || res.data?.content || [];

        // 🔥 3) 500 üzeri kayıt varsa kullanıcıyı uyar
        if (data.length >= 500) {
          alert(
            "Bu tarih aralığında 500'den fazla ekstre verisi bulundu!\n\n" +
            "Tarih aralığını 15 günden kısa olacak şekilde daraltınız."
          );
        }

        this.ekstre = data;

        // 🔹 Özet hesapla
        this.calculateSummary();
      } catch (err) {
        console.error("Veri çekme hatası:", err);
        alert("Veri çekme hatası: " + err.message);
      } finally {
        this.isLoading = false;
      }
    }
    ,
    goToOrderDetail(orderNumber) {
      const order = this.userOrders.find(u => u.orderNumber === orderNumber);

      if (!order) {
        alert("Bu sipariş bu kullanıcıya ait değil!");
        return;
      }
      this.$router.push({
        name: "trendyol-entegrasyon",
        query: {
          orderNumber,
          orderId: order.id  // 🔥 id gönderiyoruz
        }
      });
    }
    ,
    // 🔹 Tarih biçimlendirme
    formatDate(dateStr) {
      const date = new Date(dateStr);
      return isNaN(date.getTime())
        ? "-"
        : date.toLocaleString("tr-TR", {
          dateStyle: "short",
          timeStyle: "short",
        });
    },

    // 🔹 Tarih aralığını yazdırmak için
    formatDateRange() {
      const start = new Date(this.startDate).toLocaleDateString("tr-TR");
      const end = new Date(this.endDate).toLocaleDateString("tr-TR");
      return `${start} - ${end}`;
    },

    // 🔹 Seçilen tarih aralığına göre toplamlar
    calculateSummary() {
      if (!this.ekstre.length) return;

      const start = new Date(this.startDate);
      const end = new Date(this.endDate);
      end.setHours(23, 59, 59, 999);

      let totalSales = 0;
      let totalProfit = 0;

      this.ekstre.forEach((t) => {
        const date = new Date(t.transactionDate);
        if (isNaN(date)) return;
        if (date < start || date > end) return;

        const saleAmount = Number(t.credit || 0);
        const komisyon = Number(t.commissionAmount || 0);

        if (saleAmount > 0) {
          totalSales += saleAmount;
          totalProfit += saleAmount - komisyon;
        }
      });

      this.summary = { totalSales, totalProfit };
    },
  },
};
</script>

<style scoped>
.filters input,
.filters select {
  max-width: 180px;
}

.card {
  border-radius: 12px;
}
</style>
