<script>
import { Modal } from "bootstrap";
import api from "../../axios";

export default {

    data() {
        return {
            brands: [],
            subCategories: [],
            selectedAttributes: {},
            successMessage: "",
            categories: [],
            categoryPath: [{ id: null }],
            categoryAttributes: [],
            mainProduct: {
                title: "",
                productMainId: "",
                brandId: null,
                categoryId: null,
                description: "Ürün açıklaması buraya gelecek",
            },
            variants: [
                {
                    barcode: "",
                    stockCode: "",
                    quantity: 1,
                    listPrice: 99.99,
                    salePrice: 199.99,
                    image: "",
                    attributes: [{ attributeId: null, attributeValueId: null }],
                },
            ],
        };
    },
    async mounted() {
        this.loadBrands();
        this.loadCategories();
    },
    methods: {
        addVariant() {
            this.variants.push({
                barcode: "",
                stockCode: "",
                quantity: 1,
                listPrice: 0,
                salePrice: 0,
                image: "",
                attributes: [{ attributeId: null, attributeValueId: null }],
            });
        },
        removeVariant(index) {
            this.variants.splice(index, 1);
        },
        addAttribute(index) {
            this.variants[index].attributes.push({
                attributeId: null,
                attributeValueId: null,
            });
        },

        async sendProduct() {
            const kullanici_id = localStorage.getItem("kullanici_id");
            if (!kullanici_id) {
                alert("Kullanıcı bilgisi bulunamadı!");
                return;
            }
            const missingRequired = this.categoryAttributes.filter(
                (a) => a.required && !this.selectedAttributes[a.attribute?.id]
            );

            if (missingRequired.length > 0) {
                const list = missingRequired.map((a) => `- ${a.attribute?.name}`).join("\n");
                alert("Lütfen şu zorunlu özellikleri doldurun:\n\n" + list);
                return;
            }
            const variantAttributes = this.variants.map((variant, index) => {
                const attrs = this.categoryAttributes.map(attr => {
                    const val = this.selectedAttributes[attr.attribute?.id];
                    if (!val) return null; // zorunlu kontroller zaten yapıldı

                    return {
                        attributeId: Number(attr.attribute.id),
                        attributeValueId: attr.attributeValues?.length ? Number(val) : null,
                        customAttributeValue: !attr.attributeValues?.length ? val : null
                    };
                }).filter(a => a !== null);

                return {
                    barcode: variant.barcode,
                    stockCode: variant.stockCode,
                    salePrice: variant.salePrice,
                    stock: variant.quantity,
                    description: this.mainProduct.description,
                    imageUrls: [variant.image],
                    attributes: attrs
                };
            });

            // Formdan gelen selectedAttributes'i Trendyol formatına çevir
            const attributes = Object.entries(this.selectedAttributes).map(([attrId, value]) => {
                if (typeof value === "number") {
                    return { attributeId: Number(attrId), attributeValueId: value };
                } else if (typeof value === "string" && value.trim() !== "") {
                    return { attributeId: Number(attrId), customAttributeValue: value.trim() };
                }
                return null;
            }).filter(a => a !== null);
            const payload = {
                kullaniciId: localStorage.getItem("kullanici_id"),
                title: this.mainProduct.title,
                categoryName: this.getSelectedCategoryName(),
                categoryId: this.mainProduct.categoryId,
                brandId: this.mainProduct.brandId,
                productMainId: this.mainProduct.productMainId,
                description: this.mainProduct.description,
                variants: variantAttributes

            };


            console.log("Gönderilen Payload:", payload);

            try {
                const response = await api.post("/api/Urunler/trendyol-add", payload);

                console.log("Trendyol Yanıtı:", response.data);
                alert("Ürün başarıyla gönderildi!");
            } catch (error) {
                console.error("Gönderim Hatası:", error.response?.data || error);
                alert("Ürün gönderilirken hata oluştu.");
            }
        },
        openVariantModal() {
            const el = document.getElementById("addVariantProductModal");
            if (el) new Modal(el).show();
            else console.warn("Modal bulunamadı: #addVariantProductModal");
        },

        async loadBrands() {
            try {
                const res = await api.get("/api/Trendyol/brands");
                this.brands = res.data?.brands ?? [];
            } catch (err) {
                console.error("Markalar alınamadı:", err);
            }
        },
        async loadCategories() {
            try {
                const res = await api.get("/api/trendyol/categories");
                this.categories = res.data.categories ?? [];
            } catch (err) {
                console.error("Kategoriler alınamadı:", err);
            }
        },
        getSubCategories(level) {
            if (level === 0) return this.categories;
            let parentCats = this.categories;
            for (let i = 0; i < level; i++) {
                const selectedId = this.categoryPath[i].id;
                const found = parentCats.find(c => c.id === selectedId);
                if (!found) return [];
                parentCats = found.subCategories || [];
            }
            return parentCats;
        },
        handleCategoryChange(level) {
            if (!this.categoryPath[level]) this.categoryPath[level] = { id: null, subCategories: [] };

            const selectedCatId = this.categoryPath[level]?.id;

            // sonraki seviyeleri sıfırla
            this.categoryPath = this.categoryPath.slice(0, level + 1);

            const subCats = this.getSubCategories(level);
            const currentCat = subCats.find(c => c.id === selectedCatId);

            if (currentCat?.subCategories?.length) {
                this.categoryPath[level + 1] = { id: null, subCategories: currentCat.subCategories };
                this.mainProduct.categoryId = null;
                this.categoryAttributes = [];
            } else if (selectedCatId) {
                this.mainProduct.categoryId = selectedCatId;
                this.$nextTick(() => {
                    this.loadCategoryAttributes();
                });
            }

        }
        ,

        async loadCategoryAttributes() {
            const categoryId = this.mainProduct?.categoryId;
            if (!categoryId) {
                this.categoryAttributes = [];
                return;
            }
            try {
                const res = await api.get(`/api/trendyol/category-attributes/${categoryId}`);
                this.categoryAttributes = res.data.categoryAttributes ?? [];
                // selectedAttributes'i resetle
                this.selectedAttributes = {};
            } catch (err) {
                this.categoryAttributes = [];
                console.error("Kategori özellikleri alınamadı:", err);
            }
        },
        getSelectedCategoryName() {
            // Son seçilen kategori id'sine göre ismi bulur
            const findName = (cats, id) => {
                for (const cat of cats) {
                    if (cat.id === id) return cat.name;
                    if (cat.subCategories?.length) {
                        const found = findName(cat.subCategories, id);
                        if (found) return found;
                    }
                }
                return "";
            };
            return findName(this.categories, this.mainProduct.categoryId);
        },
    },
};
</script>

<template>
    <div class="modal fade" id="addVariantProductModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content border-0 shadow-lg rounded-4">
                <div class="modal-header bg-primary text-white rounded-top-4">
                    <h5 class="modal-title fw-semibold"><i class="bi bi-box-seam me-2"></i>Yeni Ürün Ekle</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body bg-light">
                    <form @submit.prevent="sendProduct" class="p-2">

                        <div class="row g-2">
                            <div class="col-md-6">
                                <label class="form-label">Ürün Adı</label>
                                <input v-model="mainProduct.title" class="form-control" />
                            </div>

                            <div class="col-md-6">
                                <label class="form-label">Ana Ürün Kodu (productMainId)</label>
                                <input v-model="mainProduct.productMainId" class="form-control" />
                            </div>

                            <div v-for="(cat, level) in categoryPath" :key="level" class="mb-3">
                                <label class="form-label">
                                    Kategori Seviye {{ level + 1 }} <span class="text-danger">*</span>
                                </label>
                                <select class="form-select shadow-sm" v-model="categoryPath[level].id"
                                    @change="handleCategoryChange(level)" required>
                                    <option value="">Seçiniz</option>
                                    <option v-for="sub in getSubCategories(level) || []" :key="sub.id" :value="sub.id">
                                        {{ sub.name }}
                                    </option>
                                </select>
                            </div>

                            <div class="mb-3">
                                <label class="form-label">Marka <span class="text-danger">*</span></label>
                                <select v-model="mainProduct.brandId" class="form-select shadow-sm" required>
                                    <option value="">Marka Seçiniz</option>
                                    <option v-for="brand in brands" :key="brand.id" :value="brand.id">{{ brand.name }}
                                    </option>
                                </select>
                            </div>
                        </div>

                        <hr />

                        <!-- Varyantlar -->
                        <div>
                            <h6>Varyantlar</h6>

                            <div v-for="(variant, index) in variants" :key="index" class="border rounded p-3 mb-3">
                                <h6>Varyant {{ index + 1 }}</h6>

                                <div class="row g-2">
                                    <div class="col-md-6">
                                        <label class="form-label">Barkod</label>
                                        <input v-model="variant.barcode" class="form-control" />
                                    </div>

                                    <div class="col-md-6">
                                        <label class="form-label">Stok Kodu</label>
                                        <input v-model="variant.stockCode" class="form-control" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Adet</label>
                                        <input v-model.number="variant.quantity" class="form-control" type="number" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Liste Fiyatı</label>
                                        <input v-model.number="variant.listPrice" class="form-control" type="number" />
                                    </div>

                                    <div class="col-md-4">
                                        <label class="form-label">Satış Fiyatı</label>
                                        <input v-model.number="variant.salePrice" class="form-control" type="number" />
                                    </div>

                                    <div class="col-md-12">
                                        <label class="form-label">Görsel URL</label>
                                        <input v-model="variant.image" class="form-control" />
                                    </div>

                                    <div class="attributes-section mt-4 mb-4">
                                        <h4>Ürün Özellikleri</h4>

                                        <!-- Zorunlu Alanlar -->
                                        <div v-if="categoryAttributes.filter(a => a.required).length" class="mb-3">
                                            <h5 class="fw-semibold">Zorunlu Alanlar</h5>
                                            <div v-for="attr in categoryAttributes.filter(a => a.required)"
                                                :key="attr.attribute?.id" class="mb-3">
                                                <label class="form-label">{{ attr.attribute?.name }} <span
                                                        class="text-danger">*</span></label>

                                                <select v-if="attr.attributeValues?.length"
                                                    class="form-select shadow-sm"
                                                    v-model="selectedAttributes[attr.attribute?.id]" required>
                                                    <option value="">Seçiniz</option>
                                                    <option v-for="val in attr.attributeValues" :key="val.id"
                                                        :value="val.id">{{
                                                            val.name }}</option>
                                                </select>

                                                <input v-else-if="attr.allowCustom" type="text"
                                                    class="form-control shadow-sm"
                                                    v-model="selectedAttributes[attr.attribute?.id]" required />

                                                <input v-else type="text" class="form-control shadow-sm"
                                                    value="Seçenek yok" disabled />
                                            </div>
                                        </div>

                                        <!-- Opsiyonel Alanlar -->
                                        <div v-if="categoryAttributes.filter(a => !a.required).length" class="mb-3">
                                            <h5 class="fw-semibold">Opsiyonel Alanlar</h5>
                                            <div v-for="attr in categoryAttributes.filter(a => !a.required)"
                                                :key="attr.attribute?.id" class="mb-3">
                                                <label class="form-label">{{ attr.attribute?.name }}</label>

                                                <select v-if="attr.attributeValues?.length"
                                                    class="form-select shadow-sm"
                                                    v-model="selectedAttributes[attr.attribute?.id]">
                                                    <option value="">Seçiniz</option>
                                                    <option v-for="val in attr.attributeValues" :key="val.id"
                                                        :value="val.id">{{
                                                            val.name }}</option>
                                                </select>

                                                <input v-else-if="attr.allowCustom" type="text"
                                                    class="form-control shadow-sm"
                                                    v-model="selectedAttributes[attr.attribute?.id]"
                                                    placeholder="İsteğe bağlı" />

                                                <input v-else type="text" class="form-control shadow-sm"
                                                    value="Seçenek yok" disabled />
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <button class="btn btn-outline-danger btn-sm mt-3" @click="removeVariant(index)">
                                    Varyantı Kaldır
                                </button>
                            </div>

                            <button class="btn btn-outline-success" @click="addVariant">
                                + Yeni Varyant Ekle
                            </button>
                        </div>

                        <hr />

                        <button class="btn btn-primary" type="submit">Ürünü Trendyol’a Gönder</button>
                    </form>
                </div>
            </div>
        </div>
    </div>

</template>



<style scoped>
.container {
    max-width: 800px;
}
</style>
