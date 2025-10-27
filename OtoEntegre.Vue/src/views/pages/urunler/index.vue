<script>
import { formatCurrency } from '../../../utils/format'
import { Modal } from 'bootstrap';
import { nextTick } from 'vue';
import api from "../../axios";
import AddProductModal from './addProductModal.vue';
import AddVariantProductModal from './addVariantProductModal.vue';
import BulkUploadModal from './BulkUploadModal.vue';

export default {
    components: {
        AddProductModal,
        AddVariantProductModal,
        BulkUploadModal
    },
    data() {
        return {
            products: [], // Trendyol ürünleri
            selectedCategory: null, // artık ID tutacak
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
            newImageUrl: "",

            newProduct: {
                images: [],
                title: "",
                categoryName: "",
                categoryId: null,
                brandId: null,
                productMainId: null,
                barcode: "",
                salePrice: 0,
                stock: 0,
                description: "",
                attributes: [
                    { attributeId: 47, customAttributeValue: "Renk" },
                    { attributeId: 348, attributeValueId: 686230 },
                    { attributeId: 1155, attributeValueId: 1225104 },
                    { attributeId: 346, attributeValueId: 4293 },
                    { attributeId: 279, attributeValueId: 1256866 },
                    { attributeId: 1156, attributeValueId: 1225110 },
                    { attributeId: 91, attributeValueId: 10576981 },
                    { attributeId: 343, attributeValueId: 4296 },
                    { attributeId: 1192, attributeValueId: 10633877 },
                    { attributeId: 767, attributeValueId: 10591493 }
                ]
            },
            brands: [],
            subCategories: [],
            selectedAttributes: {},
            successMessage: "",
            _addModalInstance: null,
            // Category filter
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
            ],

            categories: [],       // tüm kategori ağacı
            categoryPath: [{ id: null }], // kategori seçim yolu
            newProduct: {
                categoryId: null, // son seçilen kategori (alt kategori)
                title: "",
                brandId: null,
                salePrice: 0,
                stock: 0,
                description: "",
                imageUrl: "",
            },
            categoryAttributes: [],
            selectedAttributes: {},
            successMessage: "",
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
            console.log('Opening product modal for', p);
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




    }
};
</script>

<template>
    <div class="container-fluid trendyol-page">
        <!-- Header -->
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h3 class="fw-semibold mb-0">
                <i class="bi bi-bag-check-fill text-primary me-2"></i> Trendyol Ürünlerim
            </h3>

            <div class="d-flex flex-wrap gap-2 align-items-center">
                <button class="btn btn-primary" @click="$refs.addProductModal.openModal()">Yeni Ürün</button>
                <button class="btn btn-primary" @click="$refs.addVariantProductModal.openVariantModal()">
                    Yeni Varyant Ürün
                </button>
                <button class="btn btn-success" @click="$refs.bulkUploadModal.openModal()">
                    <i class="bi bi-file-earmark-excel me-1"></i> Excel ile Toplu Yükle
                </button>


                <select v-model="selectedCategory" class="form-select form-select-sm category-filter">
                    <option value="">Tüm Kategoriler</option>
                    <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
                </select>

                <div class="search-box">
                    <i class="bi bi-search"></i>
                    <input v-model="searchQuery" type="text" class="form-control shadow-sm"
                        placeholder="Ürün adı veya SKU ara..." />
                </div>
            </div>
        </div>

        <!-- Ürün Listesi -->
        <div class="card shadow-sm border-0 rounded-4">
            <div class="card-body">
                <div v-if="isLoading" class="text-center py-5">
                    <div class="spinner-border text-primary" role="status"></div>
                    <p class="mt-2 text-muted small">Ürünler yükleniyor...</p>
                </div>

                <div v-else>
                    <div class="row g-4">
                        <div class="col-6 col-md-3 col-lg-2" v-for="p in filteredProducts" :key="p.productCode">
                            <div class="product-card" @click="openProductModal(p)">
                                <div class="img-wrapper">
                                    <img :src="(p.images?.length ? p.images[0].url : p.productUrl)" alt="product" />
                                </div>
                                <div class="p-2">
                                    <h6 class="title">{{ p.title }}</h6>
                                    <p class="price">{{ formatMoney(p.salePrice, 'TRY') }}</p>
                                    <p class="small text-muted mb-0">SKU: {{ p.productCode }}</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Sayfalandırma -->
                    <div class="d-flex justify-content-center align-items-center gap-3 mt-4">
                        <button class="btn btn-outline-secondary btn-sm" :disabled="productsPage === 0"
                            @click="prevProductsPage">
                            <i class="bi bi-chevron-left"></i> Önceki
                        </button>
                        <span class="fw-medium small">
                            Sayfa {{ productsPage + 1 }} / {{ Math.ceil(productsTotal / productsSize) || 1 }}
                        </span>
                        <button class="btn btn-outline-secondary btn-sm"
                            :disabled="(productsPage + 1) * productsSize >= productsTotal" @click="nextProductsPage">
                            Sonraki <i class="bi bi-chevron-right"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Modals -->
        <AddProductModal ref="addProductModal" />
        <AddVariantProductModal ref="addVariantProductModal" />
        <BulkUploadModal ref="bulkUploadModal" @upload-complete="loadTrendyolProducts" />

    </div>
</template>

<style scoped>
.trendyol-page {
    padding: 1rem 2rem;
    background: #f8f9fb;
    min-height: 100vh;
}

/* Search box */
.search-box {
    position: relative;
}

.search-box i {
    position: absolute;
    top: 50%;
    left: 12px;
    transform: translateY(-50%);
    color: #888;
}

.search-box input {
    padding-left: 36px;
    min-width: 220px;
    border-radius: 10px;
}

/* Category filter */
.category-filter {
    min-width: 180px;
    border-radius: 10px;
}

/* Product card */
.product-card {
    border: 1px solid #eaeaea;
    border-radius: 14px;
    overflow: hidden;
    transition: all 0.2s ease;
    cursor: pointer;
    background: #fff;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.04);
}

.product-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 6px 14px rgba(0, 0, 0, 0.08);
}

.product-card .img-wrapper {
    height: 140px;
    display: flex;
    justify-content: center;
    align-items: center;
    background: #f9f9f9;
}

.product-card img {
    max-height: 100%;
    width: auto;
    object-fit: contain;
}

.product-card .title {
    font-size: 0.85rem;
    height: 2.6rem;
    overflow: hidden;
}

.product-card .price {
    font-weight: 600;
    color: #ff7b00;
    margin-bottom: 0;
}

/* Alert */
.alert {
    border-left: 4px solid #28a745;
    font-weight: 500;
}

/* Fade animation */
.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}
</style>
