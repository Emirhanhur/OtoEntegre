<template>
    <div v-if="show" class="modal-backdrop">
        <div class="modal-dialog modal-lg">
            <div class="modal-content p-3">
                <div class="modal-header">
                    <h5 class="modal-title">Otosticker Sipariş</h5>
                    <button type="button" class="btn-close" @click="$emit('close')"></button>
                </div>

                <div class="modal-body">
                    <div class="mb-3">
                        <label>Email</label>
                        <input v-model="form.email" type="email" class="form-control" placeholder="Otosticker Email" />
                    </div>

                    <div class="mb-3">
                        <label>Şifre</label>
                        <input v-model="form.password" type="password" class="form-control"
                            placeholder="Otosticker Şifre" />
                    </div>

                    <div class="mb-3">
                        <label>Ürün Adı</label>
                        <input v-model="form.productName" type="text" class="form-control"
                            placeholder="Sipariş edilecek ürün adı" />
                    </div>

                    <button class="btn btn-primary" @click="searchProduct" :disabled="loading">
                        {{ loading ? "Aranıyor..." : "Ürün Ara" }}
                    </button>

                    <div v-if="products.length" class="mt-4">
                        <h6>Bulunan Ürünler:</h6>
                        <ul class="list-group">
                            <li v-for="p in products" :key="p.id"
                                class="list-group-item d-flex justify-content-between align-items-center">
                                <div>
                                    <strong>{{ p.name }}</strong><br />
                                    <small>{{ p.price }} ₺</small>
                                </div>
                                <button class="btn btn-sm btn-success" @click="orderProduct(p)">Sipariş Et</button>
                            </li>
                        </ul>
                    </div>

                </div>

                <div class="modal-footer">
                    <button class="btn btn-secondary" @click="$emit('close')">Kapat</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import api from "../../axios";

export default {
    name: "OtostickerLoginModal",
    props: {
        show: { type: Boolean, required: true }
    },
    data() {
        return {
            form: { email: "", password: "", productName: "" },
            products: [],
            loading: false
        };
    },
    methods: {
        async searchProduct() {
    try {
        const payload = {
            email: this.form.email,
            password: this.form.password,
            productName: this.form.productName
        };
        const response = await api.post("/api/Siparisler/search", payload, {
            headers: { "Content-Type": "application/json" }
        });
        console.log("Products:", response.data);
        this.products = response.data;
    } catch (error) {
        console.error(error);
        alert(error.response?.data || error.message);
    }
}
,
        async orderProduct(product) {
            if (!confirm(`${product.name} sipariş edilsin mi?`)) return;
            try {
                const res = await api.post("/api/Siparisler/fast-sale", {
                    email: this.form.email,
                    password: this.form.password,
                    productId: product.id
                });
                console.log(res.data);
                if (res.data.success) {
                    alert("✅ Sipariş oluşturuldu! Kod: " + res.data.code);
                } else {
                    alert("❌ Sipariş oluşturulamadı: " + res.data.message);
                }
            } catch (err) {
                console.error(err);
                alert("Sipariş sırasında hata oluştu!");
            }
        }
    }
};
</script>

<style scoped>
.modal-backdrop {
    display: flex;
    justify-content: center;
    align-items: center;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.5);
    z-index: 1050;
    overflow-y: auto;
}

.modal-dialog {
    margin: 0;
}

.modal-content {
    background: #fff;
    border-radius: 0.5rem;
    width: 100%;
    max-width: 700px;
}
</style>
