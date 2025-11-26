<script>

import api from '../../axios';

export default {
    props: {
        product: { type: Object, required: true },
        price: { type: Number, required: true },
        stats: { type: Object, default: () => ({ totalSold: 0, orderCount: 0 }) }
    },
    data() {
        return {
            selectedImage: this.product?.images?.[0]?.url || '',
            editableProduct: { ...this.product },

            salePriceInput: 0,
            listPriceInput: 0,
            quantityInput: 0,

            isSaving: false,
            successMessage: '',
            formData: {}
        };
    },

    watch: {
        product: {

            handler(newVal) {
                if (newVal) {
                    this.loadFullProductInfo();


                }
            },

            immediate: true
        }
    },
    methods: {
        async loadFullProductInfo() {
            const kullaniciId = localStorage.getItem("kullanici_id");
            const barcode = this.product?.barcode;

            if (!barcode) return;

            try {
                const res = await api.get(`/api/urunler/trendyol/${kullaniciId}/product-by-barcode`, {
                    params: { barcode }
                });

                if (res?.data) {
                    this.formData = { ...res.data };
                }
            } catch (err) {
                console.error("Ürün detayları alınamadı:", err);
            }
        },
        async updateProductInfo() {
            const kullaniciId = localStorage.getItem("kullanici_id");
            if (!kullaniciId) return alert("Kullanıcı bilgisi bulunamadı.");

            this.isSaving = true;

            try {
                const item = {
                    barcode: this.formData.barcode ?? this.product.barcode,
                    productMainId: this.formData.productMainId || this.product.productMainId || this.formData.stockCode || this.product.stockCode, title: this.formData.title ?? this.product.title,
                    description: this.formData.description ?? this.product.description,

                    categoryId: Number(this.formData.categoryId ?? this.product.categoryId),
                    brandId: Number(this.formData.brandId ?? this.product.brandId),

                    stockCode: this.formData.stockCode ?? "",
                    dimensionalWeight: Number(this.formData.dimensionalWeight ?? 1),
                    vatRate: Number(this.formData.vatRate ?? 20),

                    // !!! 0 OLAMAZ !!!
                    cargoCompanyId: Number(this.formData.cargoCompanyId || this.product.cargoCompanyId || 1),
                    shipmentAddressId: Number(this.formData.shipmentAddressId || this.product.shipmentAddressId),
                    returningAddressId: Number(this.formData.returningAddressId || this.product.returningAddressId),

                    // 0 GEÇERSİZ → Min 1
                    deliveryDuration: Number(this.formData.deliveryDuration || this.product.deliveryDuration || 1),

                    currencyType: "TRY",

                    images: (this.formData.images && this.formData.images.length)
                        ? this.formData.images.map(i => ({ url: i.url }))
                        : this.product.images.map(i => ({ url: i.url })),

                    attributes: this.formData.attributes?.length
                        ? this.formData.attributes
                        : this.product.attributes,

                    lotNumber: null,
                    locationBasedDelivery: null,
                    deliveryOption: null
                };



                const payload = { items: [item] };

                const res = await api.put(
                    `/api/urunler/trendyol/${kullaniciId}/update-product`,
                    item // payload yerine direkt item nesnesini gönderin
                );

                console.log("Update response:", res.data); // ✔️ burada göreceksin artık

                if (res?.data?.success) {
                    this.successMessage = "Ürün bilgisi başarıyla güncellendi.";
                    setTimeout(() => this.successMessage = '', 3000);
                } else {
                    alert(res?.data?.message || "Güncelleme başarısız.");
                }

            } catch (err) {
                console.error(err);
                alert("Güncelleme sırasında hata oluştu.");
            } finally {
                this.isSaving = false;
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
        }

    },
    created() {
    }

};
</script>
<template>
    <div class="modal fade" id="productDetailModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">{{ product?.title || 'Ürün Detayı' }}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>

                <div class="modal-body">
                    <div class="row g-3">
                        <!-- Ürün Görseli -->
                        <div class="col-md-5 text-center">
                            <img :src="selectedImage || product?.images?.[0]?.url || product?.productUrl"
                                class="img-fluid rounded mb-3" alt="Ürün Resmi" />
                            <div v-if="product?.images?.length > 1"
                                class="d-flex flex-wrap gap-2 justify-content-center mt-2">
                                <img v-for="(img, idx) in product.images" :key="idx" :src="img.url"
                                    class="img-thumbnail"
                                    style="width: 60px; height: 60px; object-fit: cover; cursor: pointer;"
                                    @click="selectedImage = img.url" />
                            </div>
                        </div>

                        <!-- Sekmeler -->
                        <div class="col-md-7">
                            <div class="tab-content">
                                <!-- Düzenlenebilir Genel Bilgiler -->
                                <div class="mb-2">
                                    <label><strong>Başlık (title)</strong></label>
                                    <input type="text" class="form-control form-control-sm" v-model="formData.title" />
                                </div>

                                <div class="mb-2">
                                    <label><strong>Açıklama (description)</strong></label>
                                    <textarea class="form-control form-control-sm" rows="3"
                                        v-model="formData.description"></textarea>
                                </div>
                                <div class="row g-2">
                                    <div class="col-md-12">
                                        <label><strong>Kategori</strong></label>
                                        <input disabled type="text" class="form-control form-control-sm"
                                            v-model="formData.category" />
                                    </div>

                                    <div class="col-md-6">
                                        <label><strong>Marka</strong></label>
                                        <input disabled type="text" class="form-control form-control-sm"
                                            v-model="formData.brand" />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <label><strong>KDV Oranı</strong></label>
                                    <input type="number" class="form-control form-control-sm"
                                        v-model.number="formData.vatRate" />
                                </div>
                                <div class="row g-2 mt-2">


                                    <div class="col-md-6">
                                        <label><strong>Desi</strong></label>
                                        <input type="number" class="form-control form-control-sm"
                                            v-model.number="formData.dimensionalWeight" />
                                    </div>
                                </div>

                                <div class="row g-2 mt-2">
                                    <div class="col-md-6">
                                        <label><strong>Stok Kodu</strong></label>
                                        <input type="text" class="form-control form-control-sm"
                                            v-model="formData.stockCode" />
                                    </div>


                                </div>

                                <div class="row g-2 mt-2">
                                    <div class="col-md-6">
                                        <label><strong>Sevkiyat Süresi (deliveryDuration)</strong></label>
                                        <input type="number" class="form-control form-control-sm"
                                            v-model.number="formData.deliveryDuration" />
                                    </div>

                                    <div class="col-md-6">
                                        <label><strong>Kargo Firması ID (cargoCompanyId)</strong></label>
                                        <input type="number" class="form-control form-control-sm"
                                            v-model.number="formData.cargoCompanyId" />
                                    </div>
                                </div>

                                <div class="row g-2 mt-2">
                                    <div class="col-md-6">
                                        <label><strong>KATEGORİ İD</strong></label>
                                        <input type="number" class="form-control form-control-sm"
                                            v-model.number="formData.categoryId" />
                                    </div>
                                </div>
                                {{ formData }}




                            </div>

                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <div class="me-auto" v-if="successMessage"><span class="text-success">{{ successMessage
                    }}</span>
                    </div>
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Kapat</button>
                    <button type="button" class="btn btn-primary" @click="saveChanges" :disabled="isSaving">
                        <span v-if="isSaving" class="spinner-border spinner-border-sm me-2" role="status"
                            aria-hidden="true"></span>
                        Kaydet
                    </button>
                </div>
            </div>
        </div>
    </div>

</template>
<style scoped>
.img-thumbnail {
    transition: all 0.2s ease;
}

.img-thumbnail:hover {
    transform: scale(1.05);
}
</style>
