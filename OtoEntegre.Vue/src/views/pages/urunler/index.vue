<script>
import { formatCurrency } from '../../../utils/format'
import { Modal } from 'bootstrap';
import { nextTick } from 'vue';
import api from "../../axios";
import AddProductModal from './addProductModal.vue';
import AddVariantProductModal from './addVariantProductModal.vue';
import BulkUploadModal from './BulkUploadModal.vue';
import ProductDetailModal from './ProductDetailModal.vue';

export default {
    components: {
        AddProductModal,
        AddVariantProductModal,
        BulkUploadModal,
        ProductDetailModal
    },
    data() {
        return {
            bayiId: localStorage.getItem("bayi_id"),
            productPrices: {},
            salePriceInput: 0,
            listPriceInput: 0,
            quantityInput: 0,
            approvedFilter: null,
            archivedFilter: null,
            onSaleFilter: null,
            rejectedFilter: null,
            blacklistedFilter: null,
            barcodeFilter: "",
            products: [], // Trendyol ürünleri
            otostickerBarcodes: {}, // Trendyol barkoduna karşılık gelen Otosticker barkodunu tutar
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
            selectedImage: "", // Kullanıcının seçtiği büyük resim

            selectedShippingCompany: "",
            successMessage: "", // ✅ alert için eklendi

            // Modal state
            modalProduct: null,
            modalPrice: 0,
            modalStats: null,
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
            categoryAttributes: [],
        };
    },
    computed: {

        totalPages() {
            return Math.ceil(this.orders.length / this.pageSize) || 1;
        },

        paginatedOrders() {
            const start = (this.currentPage - 1) * this.pageSize;
            return this.orders.slice(start, start + this.pageSize);
        },
        isOtostickerEnabled() {
            return String(this.bayiId) === '55';
        },
    },
    watch: {
        selectedStatus() {
            this.currentPage = 1;
        },

    },
    async mounted() {
        this.loadTrendyolProducts();
    },
    beforeUnmount() {
        clearInterval(this.pollingInterval);
    },
    methods: {
        initInputs() {
            this.salePriceInput = this.product?.salePrice ?? this.price ?? 0;
            this.listPriceInput = this.product?.listPrice ?? this.price ?? 0;
            this.quantityInput = this.product?.stock ?? 0;
        },

        async filterByBarcode() {
            this.productsPage = 0; // sayfa sıfırla
            await this.loadTrendyolProducts(0, this.barcodeFilter);
        },

        async loadTrendyolProducts(page = 0) {
            this.isLoading = true;
            const kullaniciId = localStorage.getItem("kullanici_id");
            const query = new URLSearchParams();
            query.append("page", this.productsPage);
            query.append("size", this.productsSize);

            if (this.barcodeFilter) query.append("barcode", this.barcodeFilter);
            if (this.approvedFilter !== null) query.append("approved", this.approvedFilter);
            if (this.archivedFilter !== null) query.append("archived", this.archivedFilter);
            if (this.onSaleFilter !== null) query.append("onSale", this.onSaleFilter);
            if (this.rejectedFilter !== null) query.append("rejected", this.rejectedFilter);
            if (this.blacklistedFilter !== null) query.append("blacklisted", this.blacklistedFilter);

            const res = await api.get(`/api/urunler/trendyol/${kullaniciId}?${query.toString()}`);
            this.products = res.data.data ?? [];

            for (const product of this.products) {
                // otostickerId set et
                const eslesmeRes = await api.get(`/api/urunler/otosticker/eslesme-kontrol?kullaniciId=${kullaniciId}&productCode=${product.productCode}`);
                product.matched = eslesmeRes.data.matched;
                if (eslesmeRes.data.matched)
                    product.otosticker_id = eslesmeRes.data.data?.urunTedarikBarcode ?? null;

                this.otostickerBarcodes[product.barcode] = product.otosticker_id || "";
                this.productPrices[product.barcode] = {
                    salePrice: product.salePrice || 0,
                    listPrice: product.listPrice || 0,
                    quantity: product.stock || 0
                };
            }

            this.productsTotal = res.data.total;
            this.productsPage = res.data.page;
            this.isLoading = false;
        }

        ,
        async matchOtostickerBarcode(p) {
            const kullaniciId = localStorage.getItem("kullanici_id");
            if (!kullaniciId) return alert("Kullanıcı bulunamadı");

            const trendyolBarcode = p.barcode;
            const otostickerBarcode = this.otostickerBarcodes[trendyolBarcode];
            const productCode = String(p.productCode || ""); // 👈 ProductCode'u String'e dönüştür ve boşsa "" yap            
            if (!otostickerBarcode) {
                return alert("Lütfen bir Otosticker Barkodu girin.");
            }

            const req = {
                TrendyolBarcode: trendyolBarcode,
                OtostickerBarcode: otostickerBarcode,
                ProductCode: productCode,
                KullaniciId: kullaniciId,
                PlatformId: "4cf98531-60ac-49e5-b9d2-08d77d8ce3fb" // Trendyol PlatformId'si

            };

            try {
                // Bu endpoint'i C# Controller'da oluşturacağız
                await api.post(`/api/urunler/match-otosticker-barcode`, req);
                p.otosticker_id = otostickerBarcode; // Yerelde de güncelle
                this.successMessage = `Trendyol Ürünü ${trendyolBarcode} Otosticker Barkodu ${otostickerBarcode} ile eşleştirildi.`;
                setTimeout(() => this.successMessage = "", 3000);
            } catch (err) {
                console.error(err);
                alert("Eşleştirme hatası");
            }
        },
        async updateProduct(p) {
            const kullaniciId = localStorage.getItem("kullanici_id");
            if (!kullaniciId) return alert("Kullanıcı bulunamadı");

            const prices = this.productPrices[p.barcode];

            const req = {
                KullaniciId: kullaniciId,
                Price: Number(prices.salePrice),
                ListPrice: Number(prices.listPrice),
                Quantity: Number(prices.quantity)
            };

            try {
                await api.post(`/api/urunler/${p.barcode}/update-price`, req);
                p.salePrice = prices.salePrice;
                p.listPrice = prices.listPrice;
                p.stock = prices.quantity;

                this.successMessage = "Güncellendi";
                setTimeout(() => this.successMessage = "", 2000);

            } catch (err) {
                console.error(err);
                alert("Güncelleme hatası");
            }
        }
        ,
        async saveChanges() {
            const kullaniciId = localStorage.getItem('kullanici_id');
            if (!kullaniciId) return alert('Kullanıcı bilgisi bulunamadı.');
            const barcode = this.product?.barcode;
            if (!barcode) return alert('Ürün barkodu bulunamadı.');

            this.isSaving = true;

            try {
                // 1️⃣ Fiyat & stok güncellemesi
                const priceReq = {
                    KullaniciId: kullaniciId,
                    Price: Number(this.salePriceInput),
                    ListPrice: Number(this.listPriceInput),
                    Quantity: Number(this.quantityInput)
                };
                const priceRes = await api.post(`/api/urunler/${barcode}/update-price`, priceReq);
                if (!priceRes?.data?.success) {
                    throw new Error(priceRes?.data?.message || 'Fiyat güncellemesi başarısız.');
                }

                // 2️⃣ Ürün bilgisi güncellemesi
                await this.updateProductInfo();

                this.successMessage = 'Ürün ve fiyat bilgileri başarıyla güncellendi.';
                this.$emit('updated', { barcode, salePrice: this.salePriceInput, listPrice: this.listPriceInput });
                setTimeout(() => { this.successMessage = ''; }, 3000);
            } catch (err) {
                console.error(err);
                alert(err.message || 'Güncelleme sırasında hata oluştu.');
            } finally {
                this.isSaving = false;
            }
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
        openProductModal(product) {
            this.modalProduct = product;       // seçilen ürünü atıyoruz
            this.modalPrice = product.salePrice || 0;
            this.modalStats = { totalSold: 0, orderCount: 0 }; // gerekiyorsa doldurun
            console.log(product)

            // Modal açma
            this.$nextTick(() => {
                const el = document.getElementById("productDetailModal");
                if (el) new Modal(el).show();
            });
        },
        changeProductPageSize() {
            this.productsPage = 0; // ilk sayfaya dön
            this.loadTrendyolProducts();
        }

    }
};
</script>

<template>
    <div class="container-fluid trendyol-page">
        <!-- Header -->
        <h3 class="fw-semibold mb-0 text-center text-md-start mb-4">
            <span class="material-icons text-primary me-2 align-middle">inventory_2</span> Trendyol Ürünlerim
        </h3>


        <!-- Filters inside tab -->
        <div class="filters-container mb-4 p-3 bg-white rounded-4 shadow-sm d-flex flex-wrap align-items-center gap-2">
            <select class="form-select form-select-sm filter-select" v-model="approvedFilter"
                @change="loadTrendyolProducts()">
                <option :value="null">Onay Durumu (Tümü)</option>
                <option :value="true">Onaylı</option>
                <option :value="false">Onaysız</option>
            </select>

            <select class="form-select form-select-sm filter-select" v-model="archivedFilter"
                @change="loadTrendyolProducts()">
                <option :value="null">Arşiv Durumu (Tümü)</option>
                <option :value="true">Arşivlenmiş</option>
                <option :value="false">Arşivlenmemiş</option>
            </select>

            <select class="form-select form-select-sm filter-select" v-model="onSaleFilter"
                @change="loadTrendyolProducts()">
                <option :value="null">Satış Durumu (Tümü)</option>
                <option :value="true">Satışta</option>
                <option :value="false">Satışta Değil</option>
            </select>

            <select class="form-select form-select-sm filter-select" v-model="rejectedFilter"
                @change="loadTrendyolProducts()">
                <option :value="null">Reddedilen (Tümü)</option>
                <option :value="true">Reddedilen</option>
                <option :value="false">Reddedilmeyen</option>
            </select>

            <select class="form-select form-select-sm filter-select" v-model="blacklistedFilter"
                @change="loadTrendyolProducts()">
                <option :value="null">Black List (Tümü)</option>
                <option :value="true">Black List</option>
                <option :value="false">Black List Değil</option>
            </select>

            <div class="input-group input-group-sm ms-auto" style="max-width: 200px;">
                <input type="text" class="form-control" placeholder="Barkod ile ara..." v-model="barcodeFilter"
                    @keyup.enter="filterByBarcode">
                <button class="btn btn-outline-secondary" type="button" @click="filterByBarcode">
                    <span class="material-icons" style="font-size:16px;">search</span>
                </button>
            </div>
        </div>

        <div class="d-flex justify-content-end mb-2 align-items-center gap-2">
            Listelenecek Ürün Sayısı:
            <select class="form-select form-select-sm" style="width:120px" v-model.number="productsSize"
                @change="changeProductPageSize">
                <option :value="10">10</option>
                <option :value="20">20</option>
                <option :value="50">50</option>
                <option :value="100">100</option>
            </select>
        </div>

        <!-- Ürün Listesi -->
        <div class="card shadow-sm border-0 rounded-4">
            <div class="card-body p-0">
                <table class="table table-striped table-hover mb-0">
                    <thead class="table-light">
                        <tr>
                            <th>Görsel</th>
                            <th>Ürün</th>
                            <th>Satış Fiyatı</th>
                            <th>Liste Fiyatı</th>
                            <th>Stok</th>
                            <th v-if="isOtostickerEnabled">Otosticker Barkod Eşleştirme</th>
                            <th>Durum</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="p in products" :key="p.productCode">
                            <td>
                                <img :src="p.images?.[0]?.url || p.productUrl" style="height:50px;">
                            </td>

                            <td>
                                <strong>{{ p.title }}</strong><br>
                                Kod: {{ p.productCode }}<br>
                                Barkod: {{ p.barcode }}
                            </td>

                            <td>
                                <input type="number" step="0.01" class="form-control form-control-sm"
                                    v-model.number="productPrices[p.barcode].salePrice" @click.stop
                                    @keyup.enter="updateProduct(p)" />
                            </td>

                            <td>
                                <input type="number" step="0.01" class="form-control form-control-sm"
                                    v-model.number="productPrices[p.barcode].listPrice" @click.stop
                                    @keyup.enter="updateProduct(p)" />
                            </td>




                            <td>
                                <input type="number" class="form-control form-control-sm"
                                    v-model.number="productPrices[p.barcode].quantity" @click.stop
                                    @keyup.enter="updateProduct(p)" />
                            </td>
                            <td v-if="isOtostickerEnabled" @click.stop>

                                <div class="d-flex align-items-center gap-1">
                                    <input type="text" class="form-control form-control  w-auto"
                                        placeholder="Otosticker Barkodu" v-model="otostickerBarcodes[p.barcode]"
                                        @keyup.enter="matchOtostickerBarcode(p)" />
                                    <button class="btn btn-success btn-sm"
                                        :disabled="!otostickerBarcodes[p.barcode] || otostickerBarcodes[p.barcode] === p.otosticker_id"
                                        @click="matchOtostickerBarcode(p)">
                                        <span class="material-icons align-middle" style="font-size: 16px;">link</span>
                                    </button>
                                </div>
                                <small v-if="p.otosticker_id" class="text-success d-block mt-1">
                                    <span class="material-icons align-middle" style="font-size: 20px;">check</span>
                                </small>
                            </td>
                            <td>
                                <button class="btn btn-outline-primary btn-sm" @click.stop="openProductModal(p)">
                                    Detay
                                </button>
                            </td>
                        </tr>

                    </tbody>
                </table>

                <!-- Sayfalandırma -->
                <div class="d-flex justify-content-center align-items-center gap-3 mt-2 p-2">
                    <button class="btn btn-outline-secondary btn-sm" :disabled="productsPage === 0"
                        @click="prevProductsPage">
                        <span class="material-icons align-middle me-1">chevron_left</span> Önceki
                    </button>
                    <span class="fw-medium small">
                        Sayfa {{ productsPage + 1 }} / {{ Math.ceil(productsTotal / productsSize) || 1 }}
                    </span>
                    <button class="btn btn-outline-secondary btn-sm"
                        :disabled="(productsPage + 1) * productsSize >= productsTotal" @click="nextProductsPage">
                        Sonraki <span class="material-icons align-middle ms-1">chevron_right</span>
                    </button>
                </div>
            </div>
        </div>
        <!-- Modals -->
        <AddProductModal ref="addProductModal" />
        <AddVariantProductModal ref="addVariantProductModal" />
        <BulkUploadModal ref="bulkUploadModal" @upload-complete="loadTrendyolProducts" />
        <!-- Product Detail Modal -->
        <ProductDetailModal ref="detailProductModal" :product="modalProduct" :price="modalPrice" :stats="modalStats" />



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




.btn-group .btn.active {
    background-color: #0d6efd;
    color: #fff;
    border-color: #0d6efd;
}




/* Tabs */
.nav-tabs .nav-link {
    border-radius: 12px 12px 0 0;
}

.nav-tabs .nav-link.active {
    background-color: #0d6efd;
    color: #fff;
}

/* Filters inside tab */
.filters-container {
    border-radius: 12px;
}

.filter-select {
    min-width: 150px;
    max-width: 180px;
    border-radius: 8px;
}

.input-group input {
    border-radius: 8px 0 0 8px;
}

.input-group button {
    border-radius: 0 8px 8px 0;
}
</style>
