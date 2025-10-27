<script>
import { Modal } from "bootstrap";
import { formatCurrency } from '../../../utils/format';
import api from "../../axios";

export default {
    name: "SiparisDetayModal",
    props: { order: Object },
    data() {
        return {
            selectedCargo: "",
            isLoading: false,
            selectedProducts: [], // ✅ seçilen ürünler
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
            SiparistekiUrunler: []
        };
    },

    methods: {
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
            const modalInstance = new Modal(this.$refs.modal);
            modalInstance.show();
            this.getProduct();
            this.selectedProducts = []; // modal açıldığında sıfırla
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
            console.log(this.order)
            if (!this.selectedCargo) {
                this.$toast?.error("Lütfen kargo firması seçin!");
                return;
            }
            this.isLoading = true;
            try {
                await api.put(`/api/Siparisler/siparisler/${this.order.paketNumarasi}/kargo-firmasi`, {
                    cargoProvider: this.selectedCargo,
                    entegrasyonId: this.order.entegrasyonId
                });
                this.$toast?.success("Kargo firması başarıyla değiştirildi!");
            } catch (err) {
                console.error(err);
                this.$toast?.error("Kargo firması değiştirilemedi!");
            } finally {
                this.isLoading = false;
            }
        },
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
                    <div class="mb-3"> <label class="form-label fw-bold">Kargo Firması</label>
                        <div class="input-group"> <select v-model="selectedCargo" class="form-select">
                                <option disabled value="">Seçiniz...</option>
                                <option v-for="opt in cargoOptions" :key="opt.value" :value="opt.value"> {{
                                    opt.label }} </option>
                            </select> <button class="btn btn-outline-primary d-flex align-items-center"
                                @click="changeCargoProvider" :disabled="isLoading"> <span v-if="isLoading"
                                    class="spinner-border spinner-border-sm me-2"></span> Değiştir </button> </div>
                    </div> <!-- Sipariş Bilgileri -->
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <h5>Sipariş Bilgileri</h5>
                            <p><strong>Sipariş No:</strong> {{ order.siparisNumarasi }}</p>
                            <p><strong>Durum:</strong> <span class="badge" :class="statusClass(order.originalStatus)">{{
                                order.durum }}</span> </p>
                            <p><strong>Toplam Tutar:</strong> {{formatMoney(order?.siparisUrunleri?.reduce((sum,
                                item) => sum + (item?.toplam_Fiyat || 0), 0))}} </p>
                            <p><strong>Kargo Firması:</strong> {{ order.cargoProviderName }}</p>
                            <p><strong>Kargo Ücreti:</strong> {{ formatMoney(order.kargoUcreti) }}</p>
                            <p><strong>Kargo Takip No:</strong> {{ order.kargoTakipNumarasi }}</p>
                            <p><strong>Paket No:</strong> {{ order.paketNumarasi }}</p>
                        </div>
                        <div class="col-md-6">
                            <h5>Müşteri Bilgileri</h5>
                            <p><strong>Ad Soyad:</strong> {{ order.musteriAdSoyad }}</p>
                            <p><strong>Adres:</strong></p>
                            <div class="p-2 border rounded" style="max-height: 100px; overflow-y: auto;"> {{
                                order.musteriAdres }} </div>
                            <p><strong>Beden:</strong> {{ order.beden || '-' }}</p>
                            <p><strong>Renk:</strong> {{ order.renk || '-' }}</p>
                        </div>
                    </div>
                    <!-- Ürün Bilgileri -->
                    <h6>Ürün Bilgileri</h6>
                    <table class="table table-sm table-bordered align-middle">
                        <thead class="table-light">
                            <tr>
                                <th>
                                    <input type="checkbox" @change="selectedProducts = $event.target.checked
                                        ? SiparistekiUrunler.map(u => u.id)
                                        : []">
                                </th>
                                <th>Resim</th>
                                <th>Ürün Adı</th>
                                <th>Adet</th>
                                <th>Trendyol Kod</th>
                                <th>Not</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="(urun, index) in SiparistekiUrunler" :key="index">
                                <td><input type="checkbox" v-model="selectedProducts" :value="urun.id" /></td>
                                <td><img :src="urun.image" v-if="urun.image" style="max-width: 80px; max-height: 80px;">
                                </td>
                                <td>{{ urun.ad }}</td>
                                <td>{{ urun.adet }}</td>
                                <td>{{ urun.productCode }}</td>
                                <td>
                                    <input type="text" class="form-control form-control-sm" v-model="urun.siparisNotu"
                                        @blur="saveProductNote(urun)" placeholder="Not ekle..." />
                                </td>
                            </tr>
                        </tbody>
                    </table>

                    <!-- ✅ Paket Bölme Butonu -->
                    <div class="text-end mt-3">
                        <button class="btn btn-outline-warning" @click="splitPackage" :disabled="isLoading">
                            <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                            Seçili Ürünleri Yeni Pakete Taşı
                        </button>
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Kapat</button>
                </div>
            </div>
        </div>
    </div>
</template>
