<!-- BulkUploadModal.vue -->
<script>
import { Modal } from 'bootstrap';
import api from '../../axios';

export default {
    name: 'BulkUploadModal',
    data() {
        return {
            successMessage: "", // ✅ alert için eklendi
            brands: [],
            subCategories: [],
            selectedAttributes: {},
            successMessage: "",
            categories: [],
            errorMessage: "",          // ✅ ekleyin
            selectedFile: null,        // ✅ ekleyin
            isUploading: false,        // yükleme spinner için
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
    computed: {
        selectedCategoryName() {
            const findCategoryName = (categories, targetId) => {
                for (const cat of categories) {
                    if (cat.id === targetId) return cat.name;
                    if (cat.subCategories) {
                        const found = findCategoryName(cat.subCategories, targetId);
                        if (found) return found;
                    }
                }
                return null;
            };

            return this.newProduct.categoryId ?
                findCategoryName(this.categories, this.newProduct.categoryId) :
                null;
        }
    },
    async mounted() {
        this.loadCategories();
    },
    methods: {


        async loadCategories() {
            try {
                const res = await api.get("/api/trendyol/categories");
                this.categories = res.data.categories ?? [];
            } catch (err) {
                console.error("Kategoriler alınamadı:", err);
            }
        },

        handleFileChange(event) {
            this.selectedFile = event.target.files[0];
            this.errorMessage = '';
            this.successMessage = '';
        },

        async downloadTemplate() {
            if (!this.newProduct.categoryId) {
                this.errorMessage = 'Lütfen önce kategori seçiniz';
                return;
            }

            try {
                const requestBody = {
                    CategoryId: this.newProduct.categoryId,
                    Columns: this.categoryAttributes.map(attr => ({
                        Header: attr.required ? `${attr.name} (*)` : attr.name
                    }))
                };

                const response = await api.post(
                    `/api/Urunler/trendyol/download-template/${this.newProduct.categoryId}`,
                    requestBody,
                    { responseType: 'blob' }
                );

                const url = window.URL.createObjectURL(new Blob([response.data]));
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', `trendyol-urun-sablonu-${this.newProduct.categoryId}.csv`);
                document.body.appendChild(link);
                link.click();
                link.remove();
            } catch (error) {
                console.error('Şablon indirilirken hata:', error);
                this.errorMessage = 'Şablon indirilemedi';
            }
        }

        ,

        async uploadProducts() {
            if (!this.newProduct.categoryId) {
                this.errorMessage = 'Lütfen kategori seçiniz';
                return;
            }

            if (!this.selectedFile) {
                this.errorMessage = 'Lütfen bir Excel dosyası seçiniz';
                return;
            }

            this.isUploading = true;
            this.errorMessage = '';
            this.successMessage = '';

            const formData = new FormData();
            formData.append('file', this.selectedFile);
            formData.append('categoryId', this.newProduct.categoryId);
            formData.append('supplierId', localStorage.getItem('kullanici_id'));

            try {
                const response = await api.post('/api/trendyol/products/bulk-upload', formData, {
                    headers: {
                        'Content-Type': 'multipart/form-data'
                    }
                });

                this.successMessage = `${response.data.count} ürün başarıyla yüklendi`;
                this.$emit('upload-complete');
                setTimeout(() => {
                    this.closeModal();
                }, 2000);
            } catch (error) {
                console.error('Ürünler yüklenirken hata:', error);
                if (error.response?.data?.errors) {
                    // Trendyol API'den gelen hataları göster
                    this.errorMessage = error.response.data.errors
                        .map(err => `Satır ${err.line}: ${err.message}`)
                        .join('\n');
                } else {
                    this.errorMessage = 'Ürünler yüklenirken bir hata oluştu';
                }
            } finally {
                this.isUploading = false;
            }
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

        openModal() {
            if (!this._modalInstance) {
                const modalEl = document.getElementById('bulkUploadModal');
                this._modalInstance = new Modal(modalEl);
            }
            this._modalInstance.show();
            this.loadCategories();
        },

        closeModal() {
            if (this._modalInstance) {
                this._modalInstance.hide();
            }
            // Reset form
            this.selectedCategoryId = '';
            this.selectedAttributes = {};
            this.selectedFile = null;
            this.errorMessage = '';
            this.successMessage = '';
            if (this.$refs.fileInput) {
                this.$refs.fileInput.value = '';
            }
        }
    }
};
</script>

<template>
    <div class="modal fade" id="bulkUploadModal" tabindex="-1" aria-labelledby="bulkUploadModalLabel"
        aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="bulkUploadModalLabel">Toplu Ürün Yükleme</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button>
                </div>
                <div class="modal-body">
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

                    <!-- Kategori seçildiğinde gösterilecek alan -->
                    <div v-if="newProduct.categoryId" class="mt-4">
                        <div class="alert alert-info">
                            <span class="material-icons me-2 align-middle">info</span>
                            Seçilen kategoriye ait ürün şablonunu indirerek Excel dosyasını hazırlayabilirsiniz.
                        </div>

                        <div class="d-flex justify-content-center gap-4 py-3">
                            <button @click="downloadTemplate" class="btn btn-primary">
                                <span class="material-icons me-2 align-middle">download</span>
                                Ürün Şablonunu İndir
                            </button>
                        </div>
                    </div>

                    <!-- Excel Yükleme - sadece kategori seçiliyse göster -->
                    <div v-if="newProduct.categoryId" class="mt-4">
                        <hr>
                        <h6 class="mb-3">Excel Dosyası Yükle</h6>
                        <input type="file" ref="fileInput" class="form-control" accept=".xlsx,.xls"
                            @change="handleFileChange">
                        <small class="text-muted d-block mt-2">
                            Not: İndirdiğiniz şablona uygun hazırlanmış Excel dosyasını yükleyiniz.
                        </small>
                    </div>

                    <!-- Hata ve Başarı Mesajları -->
                    <div v-if="errorMessage" class="alert alert-danger mt-3">
                        {{ errorMessage }}
                    </div>
                    <div v-if="successMessage" class="alert alert-success mt-3">
                        {{ successMessage }}
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Kapat</button>
                    <button v-if="selectedFile" type="button" class="btn btn-primary" @click="uploadProducts"
                        :disabled="isUploading">
                        <span v-if="isUploading" class="spinner-border spinner-border-sm me-2"></span>
                        Ürünleri Yükle
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>


<style scoped>
.modal-content {
    border-radius: 1rem;
}

.alert {
    border-radius: 0.5rem;
    padding: 1rem;
    margin-bottom: 1rem;
}
</style>