<script>
import { Modal } from "bootstrap";
import { formatCurrency } from '../../../utils/format';
import api from "../../axios";

export default {
  name: "SiparisDetayModal",
  props: { order: Object },
  data() {
    return {
      modalInstance: null,    // <- modal instance saklamak için

      selectedCargo: "",
      isLoading: false,
      selectedProducts: [], // ✅ seçilen ürünler
      windowWidth: window.innerWidth,
      cargoOptions: [
        { value: "YKMP", label: "Yurtiçi Kargo" },
        { value: "ARASMP", label: "Aras Kargo" },
        { value: "SURATMP", label: "Sürat Kargo" },
        { value: "HOROZMP", label: "Horoz Lojistik" },
        { value: "DHLECOMMP", label: "DHL" },
        { value: "PTTMP", label: "PTT" },
        { value: "CEVAMP", label: "Ceva" },
        { value: "TEXMP", label: "Trendyol Express" },
        { value: "KOLAYGELSINMP", label: "Kolay Gelsin" }
      ],
      SiparistekiUrunler: [],
      successMessage: "", // ✅ alert için eklendi

    };
  },
  computed: {
    mappedStatus() {
      const statusMap = {
        CREATED: "Oluşturuldu",
        PICKING: "İşleme Alındı",
        SHIPPED: "Taşıma Durumunda",
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

      // API’den gelen durum küçük/büyük harf farkı olabilir
      const key = this.order?.durum?.toUpperCase() || '';
      return statusMap[key] || this.order?.durum || '-';
    }
  },
  mounted() {
    // ✅ pencere yeniden boyutlandığında windowWidth güncellensin
    window.addEventListener("resize", this.updateWindowWidth);
  },
  beforeUnmount() {
    // ✅ bellek sızıntısı olmasın
    window.removeEventListener("resize", this.updateWindowWidth);
    if (this.$refs.modal) {
      this.$refs.modal.removeEventListener('hidden.bs.modal', this.onHidden);
    }
    if (this.modalInstance) {
      try { this.modalInstance.dispose(); } catch (e) {/* ignore */ }
      this.modalInstance = null;
    }
  },
  methods: {
    updateWindowWidth() {
      this.windowWidth = window.innerWidth;
    },
    async getProduct() {
      try {
        this.isLoading = true;
        const res = await api.get(`/api/Siparisler/${this.order.id}/urunler`);
        this.SiparistekiUrunler = res.data.urunler || [];
      } catch (err) {
        console.error("ürünler yüklenemedi", err);
      } finally {
        this.isLoading = false;
      }
    },

    showModal() {
      // Eğer zaten bir instance varsa dispose et
      if (this.modalInstance) {
        try { this.modalInstance.dispose(); } catch (e) {/* ignore */ }
        this.modalInstance = null;
      }

      const name = this.order?.cargoProviderName;

      // Firma adı -> kargoOptions içinden value bul
      const match = this.cargoOptions.find(x => x.label === name);
      this.selectedCargo = match ? match.value : "";

      this.modalInstance = new Modal(this.$refs.modal);
      // hidden olduğunda parent'e bildir
      this.$refs.modal.addEventListener('hidden.bs.modal', this.onHidden);
      this.modalInstance.show();
      this.getProduct();
      this.selectedProducts = []; // modal açıldığında sıfırla
    },
    onHidden() {
      // listener'ı kaldır
      if (this.$refs.modal) {
        this.$refs.modal.removeEventListener('hidden.bs.modal', this.onHidden);
      }
      // parent'e bildir
      this.$emit('close');
    },

    // programatik olarak modalı kapatmak istersen (Kapat butonuna bağla)
    closeModal() {
      if (this.modalInstance && typeof this.modalInstance.hide === 'function') {
        this.modalInstance.hide();
      } else {
        // fallback
        this.$emit('close');
      }
    },
    formatMoney(amount) {
      return formatCurrency(amount, "TRY");
    },

    async saveProductNote(urun) {
      try {
        const res = await api.post('/api/siparisler/update-product-note', {
          ProductId: urun.id,
          OrderId: this.order.id,
          Note: urun.siparisNotu
        });
        if (res.data.success) {
          this.successMessage = "Ürün notu kaydedildi!";

          this.$toast?.success("Ürün notu kaydedildi!");
        } else {
          this.$toast?.error("Ürün notu kaydedilemedi!");
        }
      } catch (err) {
        console.error("Ürün notu kaydedilemedi:", err);
        this.$toast?.error("Ürün notu kaydedilemedi!");
      }
    },

    async splitPackage() {
      if (this.selectedProducts.length === 0) {
        this.$toast?.warning("Lütfen en az bir ürün seçin!");
        return;
      }

      if (!confirm("Seçili ürünleri yeni bir pakete taşımak istiyor musunuz?")) return;

      this.isLoading = true;
      try {
        const res = await api.post('/api/siparisler/split-paket', {
          OrderId: this.order.id,
          ProductIds: this.selectedProducts
        });

        if (res.data.success) {
          this.$toast?.success("Paket başarıyla bölündü!");
          this.getProduct(); // yenile
          this.selectedProducts = [];
        } else {
          this.$toast?.error("Paket bölme işlemi başarısız!");
        }
      } catch (err) {
        console.error("split hatası:", err);
        this.$toast?.error("Paket bölünemedi!");
      } finally {
        this.isLoading = false;
      }
    },
    statusClass(status) {
      switch (status) { case 'DELIVERED': return 'bg-success text-white'; case 'CANCELLED': return 'bg-danger text-white'; case 'SHIPPED': return 'bg-primary text-white'; case 'CREATED': return 'bg-warning text-dark'; default: return 'bg-secondary text-white'; }
    },
    async changeCargoProvider() {
      if (!this.selectedCargo) {
        alert("Lütfen kargo firması seçin!");
        return;
      }

      this.isLoading = true;
      try {
        const res = await api.put(`/api/Siparisler/siparisler/${this.order.paketNumarasi}/kargo-firmasi`, {
          cargoProvider: this.selectedCargo,
          entegrasyonId: this.order.entegrasyonId
        });

        if (res.data?.success) {
          const msg = res.data?.message || "Kargo firması başarıyla değiştirildi!";
          this.$toast?.success(msg);
          alert("✅ " + msg);
        } else {
          const msg = res.data?.message || "Kargo firması değiştirilemedi.";
          this.$toast?.error(msg);
          alert("⚠️ " + msg);
        }
      } catch (err) {
        console.error("Kargo değişimi hatası:", err);

        let msg = "Kargo firması değiştirilemedi!";
        if (err.response?.data?.message) {
          msg = err.response.data.message;
        } else if (err.message) {
          msg = err.message;
        }

        this.$toast?.error(msg);
        alert("❌ " + msg);
      } finally {
        this.isLoading = false;
      }
    },
    async openOrderModalByNumber(orderNumber) {
      try {
        const res = await api.get(`/api/Siparisler/by-order-number/${orderNumber}`);
        const order = res.data;

        // Status mapping
        const statusMap = {
          CREATED: "Oluşturuldu",
          PICKING: "İşleme Alındı",
          SHIPPED: "Taşıma Durumunda",
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

        order.originalStatus = order.durum?.toUpperCase() || '';
        order.durum = statusMap[order.originalStatus] || order.originalStatus;

        this.selectedOrder = order;

        await nextTick();
        if (this.$refs.orderModal?.showModal) {
          this.$refs.orderModal.showModal();
        }
      } catch (err) {
        console.error("Sipariş detayı alınamadı:", err);
        alert("Sipariş detayı alınamadı.");
      }
    }

  }
};
</script>

<template>
  <div class="modal fade" ref="modal">
    <div class="modal-dialog modal-lg modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header bg-primary text-white">
          <h5 class="modal-title">Sipariş Detayı</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
        </div>

        <div class="modal-body" v-if="order">
          <!-- Kargo firması -->
          <div class="mb-3">
            <label class="form-label fw-bold">Kargo Firması</label>
            <div class="input-group">
              <select v-model="selectedCargo" class="form-select">
                <option disabled value="">Seçiniz...</option>
                <option v-for="opt in cargoOptions" :key="opt.value" :value="opt.value">
                  {{ opt.label }}
                </option>
              </select>
              <button class="btn btn-outline-primary d-flex align-items-center" @click="changeCargoProvider"
                :disabled="isLoading">
                <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                Değiştir
              </button>
            </div>
          </div>

          <!-- Sipariş Bilgileri -->
          <div class="row mb-3">
            <div class="col-md-6">
              <h5>Sipariş Bilgileri</h5>
              <p><strong>Sipariş No:</strong> {{ order.siparisNumarasi }}</p>
              <p><strong>Durum:</strong>
                <span class="badge" :class="statusClass(order.originalStatus)">{{ mappedStatus }}</span>
              </p>

              <p><strong>Toplam Tutar:</strong>
                {{formatMoney(order?.siparisUrunleri?.reduce((sum, item) => sum + (item?.toplam_Fiyat || 0), 0))}}
              </p>
              <p><strong>Kargo Firması:</strong> {{ order.cargoProviderName }}</p>
              <p><strong>Kargo Ücreti:</strong> {{ formatMoney(order.kargoUcreti) }}</p>
              <p><strong>Kargo Takip No:</strong> {{ order.kargoTakipNumarasi }}</p>
              <p><strong>Paket No:</strong> {{ order.paketNumarasi }}</p>
            </div>
            <div class="col-md-6">
              <h5>Müşteri Bilgileri</h5>
              <p><strong>Ad Soyad:</strong> {{ order.musteriAdSoyad }}</p>
              <p><strong>Adres:</strong></p>
              <div class="p-2 border rounded" style="max-height: 100px; overflow-y: auto;">
                {{ order.musteriAdres }}
              </div>
              <p><strong>Beden:</strong> {{ order.beden || '-' }}</p>
              <p><strong>Renk:</strong> {{ order.renk || '-' }}</p>
            </div>
          </div>

          <!-- Ürün Bilgileri -->
          <h6>Ürün Bilgileri</h6>
          <div class="table-responsive">
            <table class="table dark:table-dark table-bordered table-hover table-sm">
              <thead class="table-light">
                <tr>
                  <th>
                    <input type="checkbox" @change="selectedProducts = $event.target.checked
                      ? SiparistekiUrunler.map(u => u.id)
                      : []">
                  </th>
                  <th>Resim</th>
                  <th>Ürün Adı</th>
                  <!-- Adet sütunu sadece desktop -->
                  <th class="d-none d-md-table-cell">Adet</th>
                  <th v-if="windowWidth >= 768">Trendyol Kod</th>
                  <th>Not</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(urun, index) in SiparistekiUrunler" :key="index">
                  <td><input type="checkbox" v-model="selectedProducts" :value="urun.id" /></td>
                  <td class="product-img-cell">
                    <img :src="urun.image" v-if="urun.image" style="max-width: 80px; max-height: 80px;">
                    <!-- mobilde adet resmi altında göster -->
                    <div class="d-block d-md-none text-center text-muted mt-1 small">
                      Adet: {{ urun.adet }}
                    </div>
                  </td>
                  <td>{{ urun.ad }}</td>
                  <td class="d-none d-md-table-cell">{{ urun.adet }}</td>
                  <td v-if="windowWidth >= 768">{{ urun.productCode }}</td>
                  <td>
                    <div class="note-input-group">
                      <textarea class="form-control form-control-sm note-input" v-model="urun.siparisNotu"
                        placeholder="Not ekle..."></textarea>
                      <button class="btn btn-outline-primary btn-sm mt-1" @click="saveProductNote(urun)"
                        :disabled="isLoading">
                        <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                        Kaydet
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="text-end mt-3 d-flex justify-content-between">
            <button class="btn btn-outline-warning" @click="splitPackage" :disabled="isLoading">
              <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
              Seçili Ürünleri Yeni Pakete Taşı
            </button>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="closeModal">Kapat</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Mobilde not alanını genişlet ve rahat yazılabilir yap */
@media (max-width: 768px) {
  .note-input {
    width: 50% !important;
    min-width: 120px !important;
    min-height: 100% !important;
    font-size: 15px;
    padding: 8px 10px;
  }

  /* Adet sütununu gizle (sadece masaüstünde görünsün) */
  th.d-md-table-cell,
  td.d-md-table-cell {
    display: none !important;
  }

  /* Ürün resminin altındaki adet yazısı */
  .product-img-cell {
    text-align: center;
  }

  /* Tabloda taşmaları önle */
  table.table-sm td {
    white-space: normal !important;
    word-break: break-word;
  }
}
</style>
