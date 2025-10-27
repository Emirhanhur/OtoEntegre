<script>
import { Modal } from "bootstrap";
import api from "../../axios";

export default {

    data() {
        return {
            successMessage: "", // ✅ alert için eklendi
            brands: [],
            subCategories: [],
            selectedAttributes: {},
            successMessage: "",
            categories: [],
            newProduct: {
                title: "",
                categoryId: null,
                brandId: null,
                salePrice: 0,
                stock: 0,
                description: "",
                images: []
            },
            categoryPath: [{ id: null }],
            categoryAttributes: [],
            selectedAttributes: {},
            newImageUrl: "",

        };
    },
    async mounted() {
        this.loadBrands();
        this.loadCategories();
    },
    methods: {
        openModal() {
            const el = document.getElementById("addProductModal");
            if (el) new Modal(el).show();
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
                this.newProduct.categoryId = null;
                this.categoryAttributes = [];
            } else if (selectedCatId) {
                this.newProduct.categoryId = selectedCatId;
                this.$nextTick(() => {
                    this.loadCategoryAttributes();
                });
            }

        }
        ,
        addImageUrl(url) {
            if (typeof url === 'string' && url.trim() !== '') {
                if (!this.newProduct.images) this.newProduct.images = [];
                this.newProduct.images.push(url.trim());
                this.newImageUrl = ''; // inputu temizle
            } else {
                console.warn('addImageUrl parametresi string değil veya boş:', url);
            }
        },

        addImage(file) {
            if (!file) return;
            const reader = new FileReader();
            reader.onload = (e) => {
                if (!this.newProduct.images) this.newProduct.images = [];
                this.newProduct.images.push(e.target.result);
            };
            reader.readAsDataURL(file);
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
        generateBarcode(title) {
            return (
                title
                    .toLowerCase()
                    .replace(/[^a-z0-9]+/g, "-")
                    .replace(/(^-|-$)/g, "") +
                "-" +
                Math.floor(Math.random() * 100000)
            );
        },
        async submitNewProduct() {
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

            // Formdan gelen selectedAttributes'i Trendyol formatına çevir
            const attributes = Object.entries(this.selectedAttributes).map(([attrId, value]) => {
                if (typeof value === "number") {
                    return { attributeId: Number(attrId), attributeValueId: value };
                } else if (typeof value === "string" && value.trim() !== "") {
                    return { attributeId: Number(attrId), customAttributeValue: value.trim() };
                }
                return null;
            }).filter(a => a !== null);

            // Trendyol payload
            const payload = {
                kullaniciId: kullanici_id,
                title: this.newProduct.title,
                categoryName: this.getSelectedCategoryName(),
                categoryId: Number(this.newProduct.categoryId),
                brandId: Number(this.newProduct.brandId),
                productMainId: Number(this.newProduct.brandId), // örnek
                barcode: this.generateBarcode(this.newProduct.title),
                salePrice: Number(this.newProduct.salePrice),
                stock: Number(this.newProduct.stock),
                description: this.newProduct.description,
                ImageUrls: this.newProduct.images.slice(0, 5) || "https://cdn.site.com/no-image.jpg", // 🔹 Trendyol en fazla 5 resim destekliyor
                attributes: attributes
            };

            try {
                const res = await api.post("/api/urunler/trendyol-add", payload);
                this.successMessage = res.data?.message || "Ürün başarıyla eklendi.";
            } catch (err) {
                console.error("Ürün eklenemedi:", err);
                this.successMessage =
                    "Ürün eklenemedi: " + (err.response?.data || err.message);
            }
        }
        ,
        removeImage(index) {
            this.newProduct.images.splice(index, 1);
        },

        async loadCategoryAttributes() {
            const categoryId = this.newProduct?.categoryId;
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
        async loadCategories() {
            try {
                const res = await api.get("/api/trendyol/categories");
                this.categories = res.data.categories ?? [];
            } catch (err) {
                console.error("Kategoriler alınamadı:", err);
            }
        },

        addFileImage(file) {
            if (!file) return;
            const reader = new FileReader();
            reader.onload = (e) => {
                if (!this.newProduct.images) this.newProduct.images = [];
                if (e.target.result && e.target.result.trim() !== "") {
                    this.newProduct.images.push(e.target.result);
                }
            };
            reader.readAsDataURL(file);
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
            return findName(this.categories, this.newProduct.categoryId);
        },
        async loadBrands() {
            try {
                const res = await api.get("/api/Trendyol/brands");
                this.brands = res.data?.brands ?? [];
            } catch (err) {
                console.error("Markalar alınamadı:", err);
            }
        },
    }
};
</script>

<template>
    <div class="modal fade" id="addProductModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content border-0 shadow-lg rounded-4">
                <div class="modal-header bg-primary text-white rounded-top-4">
                    <h5 class="modal-title fw-semibold"><i class="bi bi-box-seam me-2"></i>Yeni Ürün Ekle</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body bg-light">
                    <form @submit.prevent="submitNewProduct" class="p-2">
                        <!-- Ürün Bilgileri -->
                        <div class="info mb-4">
                            <h4>Ürün Bilgileri</h4>

                            <div class="mb-3">
                                <label class="form-label">Ürün Adı <span class="text-danger">*</span></label>
                                <input v-model="newProduct.title" class="form-control shadow-sm" required />
                            </div>

                            <!-- Kategori Seçimi -->
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
                                <select v-model="newProduct.brandId" class="form-select shadow-sm" required>
                                    <option value="">Marka Seçiniz</option>
                                    <option v-for="brand in brands" :key="brand.id" :value="brand.id">{{ brand.name }}
                                    </option>
                                </select>
                            </div>
                        </div>

                        <!-- Ürün Özellikleri -->
                        <div class="attributes-section mt-4 mb-4">
                            <h4>Ürün Özellikleri</h4>

                            <!-- Zorunlu Alanlar -->
                            <div v-if="categoryAttributes.filter(a => a.required).length" class="mb-3">
                                <h5 class="fw-semibold">Zorunlu Alanlar</h5>
                                <div v-for="attr in categoryAttributes.filter(a => a.required)"
                                    :key="attr.attribute?.id" class="mb-3">
                                    <label class="form-label">{{ attr.attribute?.name }} <span
                                            class="text-danger">*</span></label>

                                    <select v-if="attr.attributeValues?.length" class="form-select shadow-sm"
                                        v-model="selectedAttributes[attr.attribute?.id]" required>
                                        <option value="">Seçiniz</option>
                                        <option v-for="val in attr.attributeValues" :key="val.id" :value="val.id">{{
                                            val.name }}</option>
                                    </select>

                                    <input v-else-if="attr.allowCustom" type="text" class="form-control shadow-sm"
                                        v-model="selectedAttributes[attr.attribute?.id]" required />

                                    <input v-else type="text" class="form-control shadow-sm" value="Seçenek yok"
                                        disabled />
                                </div>
                            </div>

                            <!-- Opsiyonel Alanlar -->
                            <div v-if="categoryAttributes.filter(a => !a.required).length" class="mb-3">
                                <h5 class="fw-semibold">Opsiyonel Alanlar</h5>
                                <div v-for="attr in categoryAttributes.filter(a => !a.required)"
                                    :key="attr.attribute?.id" class="mb-3">
                                    <label class="form-label">{{ attr.attribute?.name }}</label>

                                    <select v-if="attr.attributeValues?.length" class="form-select shadow-sm"
                                        v-model="selectedAttributes[attr.attribute?.id]">
                                        <option value="">Seçiniz</option>
                                        <option v-for="val in attr.attributeValues" :key="val.id" :value="val.id">{{
                                            val.name }}</option>
                                    </select>

                                    <input v-else-if="attr.allowCustom" type="text" class="form-control shadow-sm"
                                        v-model="selectedAttributes[attr.attribute?.id]" placeholder="İsteğe bağlı" />

                                    <input v-else type="text" class="form-control shadow-sm" value="Seçenek yok"
                                        disabled />
                                </div>
                            </div>
                        </div>

                        <!-- Açıklama -->
                        <div class="mb-3">
                            <label class="form-label">Açıklama <span class="text-danger">*</span></label>
                            <textarea v-model="newProduct.description" class="form-control shadow-sm"
                                rows="2"></textarea>
                        </div>

                        <!-- Satış ve Stok -->
                        <div class="row mb-3">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Satış Fiyatı (₺) <span class="text-danger">*</span></label>
                                <input v-model="newProduct.salePrice" type="number" step="0.01"
                                    class="form-control shadow-sm" required />
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Stok <span class="text-danger">*</span></label>
                                <input v-model="newProduct.stock" type="number" class="form-control shadow-sm"
                                    required />
                            </div>
                        </div>

                        <!-- Resimler -->
                        <div class="mb-3">
                            <label class="form-label">Resimler (İsteğe bağlı)</label>
                            <div class="d-flex flex-wrap gap-2 mb-2">
                                <div v-for="(img, index) in newProduct.images" :key="index" class="position-relative">
                                    <img :src="img" class="img-thumbnail"
                                        style="width: 80px; height: 80px; object-fit: cover;" />
                                    <button type="button" class="btn-close position-absolute top-0 end-0"
                                        @click="removeImage(index)" aria-label="Kaldır"></button>
                                </div>
                            </div>
                            <div class="input-group mb-2">
                                <input type="text" v-model="newImageUrl" class="form-control shadow-sm"
                                    placeholder="https://..." />
                                <button type="button" class="btn btn-outline-secondary"
                                    @click="addImageUrl(newImageUrl)">Ekle</button>

                            </div>
                        </div>

                        <div class="text-end mt-4">
                            <button type="submit" class="btn btn-primary px-4 py-2">
                                <i class="bi bi-save me-1"></i> Kaydet
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>
</template>