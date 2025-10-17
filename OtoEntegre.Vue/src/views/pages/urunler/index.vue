<script>
import { formatCurrency } from '../../../utils/format'
import { Modal } from 'bootstrap';
import { nextTick } from 'vue';
import api from "../../axios";

export default {
    data() {
        return {
            products: [], // Trendyol ürünleri
            productsTotal: 0,
            productsPage: 0,
            productsSize: 20,
            orders: [],
            selectedStatus: null,
            currentPage: 1,
            pageSize: 10,
            isLoading: false,
            selectedOrders: [],
            selectedOrder: null,
            searchQuery: "", // 🔍 Arama inputu için
            selectedShippingCompany: "",
            successMessage: "", // ✅ alert için eklendi

            // Modal state
            modalProduct: null,
            modalPrice: 0,
            modalStats: null,
            _bsModalInstance: null,

            // Category filter
            selectedCategory: '',
            categories: [],

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
            return Math.ceil(this.orders.length / this.pageSize) || 1;
        },
        filteredProducts() {
            // Apply search and category filtering
            const q = this.searchQuery.trim().toLowerCase();

            return this.products.filter(p => {
                const matchesSearch = !q || ((p.productCode && p.productCode.toString().toLowerCase().includes(q)) || (p.title && p.title.toLowerCase().includes(q)));
                const matchesCategory = !this.selectedCategory || (p.category && p.category === this.selectedCategory);
                return matchesSearch && matchesCategory;
            });
        },
        paginatedOrders() {
            const start = (this.currentPage - 1) * this.pageSize;
            return this.orders.slice(start, start + this.pageSize);
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
        this.loadTrendyolProducts();
    },
    beforeUnmount() {
        clearInterval(this.pollingInterval);
    },
    methods: {
        async loadTrendyolProducts(page = 0) {
            this.isLoading = true;
            const kullaniciId = localStorage.getItem("kullanici_id");
            const query = this.searchQuery ? `&search=${encodeURIComponent(this.searchQuery)}` : "";

            const res = await api.get(`/api/urunler/trendyol/${kullaniciId}?page=${page}&size=${this.productsSize}${query}`);

            this.products = res.data.data ?? [];
            // populate unique categories from returned products
            this.categories = Array.from(new Set(this.products.map(p => p.category).filter(c => c && c.length > 0)));
            this.productsTotal = res.data.total;
            this.productsPage = res.data.page;
            this.isLoading = false;
        },

        nextProductsPage() {
            if ((this.productsPage + 1) * this.productsSize >= this.productsTotal) return;
            this.productsPage++;
            this.loadTrendyolProducts(this.productsPage);
        },

        prevProductsPage() {
            if (this.productsPage === 0) return;
            this.productsPage--;
            this.loadTrendyolProducts(this.productsPage);
        },
        formatMoney(amount, currency) {
            return formatCurrency(amount, currency);
        },
        // Product modal related methods
        async openProductModal(p) {
            this.modalProduct = p;
            this.modalPrice = p.salePrice || 0;
            this.modalStats = null;
            try {
                const kullaniciId = localStorage.getItem('kullanici_id') || '';
                const q = kullaniciId ? `?kullaniciId=${kullaniciId}` : '';
                const res = await api.get(`api/urunler/stats/${p.productCode}${q}`);
                this.modalStats = res.data;
            } catch (err) {
                console.error('stats fetch error', err);
                this.modalStats = { totalSold: 0, orderCount: 0 };
            }

            await nextTick();
            const modalEl = document.getElementById('productDetailModal');
            if (modalEl) {
                const bsModal = new Modal(modalEl);
                bsModal.show();
                this._bsModalInstance = bsModal;
            }
        },
        async closeProductModal() {
            if (this._bsModalInstance) {
                this._bsModalInstance.hide();
                this._bsModalInstance = null;
            }
            this.modalProduct = null;
            this.modalStats = null;
        },
        async savePrice() {
            if (!this.modalProduct) return;
            try {
                const payload = { kullaniciId: localStorage.getItem('kullanici_id'), price: this.modalPrice };
                await api.post(`api/urunler/${this.modalProduct.productCode}/update-price`, payload);
                this.successMessage = 'Fiyat güncelleme isteği başarıyla gönderildi.';
                // update local view
                this.modalProduct.salePrice = this.modalPrice;
            } catch (err) {
                console.error('update price error', err);
                this.successMessage = 'Fiyat güncelleme isteği başarısız.';
            }
        }
    }
};
</script>

<template>
    <div>
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h2>Trendyol Ürünlerim</h2>
            <div class="d-flex gap-2 align-items-center">
                <!-- Category filter -->
                <select v-model="selectedCategory" class="form-select">
                    <option value="">Tümü</option>
                    <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
                </select>
                <!-- 🔍 Arama Inputu -->
                <input v-model="searchQuery" type="text" class="form-control" style="width:300px"
                    placeholder="Sipariş no, müşteri adı veya ürün adı ara..." />
            </div>
        </div>

        <!-- Trendyol Ürün Listesi -->
        <div class="card mb-3">
            <div class="card-body">
                <h5 class="card-title">Trendyol Ürünleri</h5>

                <div v-if="isLoading" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>

                <div v-else>
                    <div class="row g-3">
                        <div class="col-6 col-md-3" v-for="p in filteredProducts" :key="p.productCode">
                            <div class="card h-100">
                                <img :src="(p.images && p.images.length > 0) ? p.images[0].url : p.productUrl"
                                    class="card-img-top" style="height:140px;object-fit:contain;" />
                                <div class="card-body p-2">
                                    <h6 class="card-title" style="font-size:0.85rem;height:2.6rem;overflow:hidden;cursor:pointer"
                                        @click="openProductModal(p)">
                                        {{ p.title.length > 60 ? p.title.substring(0, 60) + '...' : p.title }}
                                    </h6>

                                    <p class="mb-0" style="font-size:0.9rem;font-weight:600">{{ formatMoney(p.salePrice,
                                        'TRY') }}</p>
                                    <p class="text-muted small mb-0">SKU: {{ p.productCode }}<br />Barkod: {{ p.barcode
                                        || '—' }}</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="d-flex justify-content-center gap-2 mt-3">
                        <button class="btn btn-outline-secondary" :disabled="productsPage === 0"
                            @click="prevProductsPage">Önceki</button>
                        <span class="align-self-center">Sayfa {{ productsPage + 1 }} / {{ Math.ceil(productsTotal /
                            productsSize) || 1 }} ({{ productsTotal }})</span>
                        <button class="btn btn-outline-secondary"
                            :disabled="(productsPage + 1) * productsSize >= productsTotal"
                            @click="nextProductsPage">Sonraki</button>
                    </div>
                </div>
            </div>
        </div>


    </div>
                <!-- Product Detail Modal -->
                <div class="modal fade" id="productDetailModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Ürün Detayları</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body">
                                <div v-if="modalProduct">
                                    <div class="row">
                                        <div class="col-md-4">
                                            <img :src="(modalProduct.images && modalProduct.images.length>0)?modalProduct.images[0].url:modalProduct.productUrl"
                                                class="img-fluid" />
                                        </div>
                                        <div class="col-md-8">
                                            <h5>{{ modalProduct.title }}</h5>
                                            <p>SKU: {{ modalProduct.productCode }}</p>
                                            <p>Satış Fiyatı: <strong>{{ formatMoney(modalProduct.salePrice,'TRY') }}</strong></p>
                                            <div class="mb-3">
                                                <label class="form-label">Yeni Fiyat</label>
                                                <input type="number" step="0.01" class="form-control" v-model.number="modalPrice" />
                                            </div>
                                            <div v-if="modalStats">
                                                <p>Toplam satılan adet: <strong>{{ modalStats.totalSold }}</strong></p>
                                                <p>Geçen sipariş sayısı: <strong>{{ modalStats.orderCount }}</strong></p>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Kapat</button>
                                <button type="button" class="btn btn-primary" @click="savePrice">Fiyatı Kaydet</button>
                            </div>
                        </div>
                    </div>
                </div>
</template>
<style scoped>
/* Açık mod (varsayılan) */
.btn-outline-primary {
    color: #1e1e1e;
    border-color: #1e1e1e;
}

.btn-outline-primary .bi {
    color: #1e1e1e;
}

/* Karanlık mod */
html[data-coreui-theme='dark'] .btn-outline-primary {
    color: #1e1e1e;
    border-color: #1e1e1e;
}

html[data-coreui-theme='dark'] .btn-outline-primary .bi {
    color: #1e1e1e;
}
</style>