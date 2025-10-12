<script>
export default {
    name: "OrderDetailModal",
    props: {
        show: { type: Boolean, required: true },
        order: { type: Object, default: null },
    },
    emits: ["close"],
    methods: {
        formatDate(dateStr) {
            if (!dateStr) return "";
            const d = new Date(dateStr);
            return (
                d.toLocaleDateString("tr-TR", {
                    year: "numeric",
                    month: "2-digit",
                    day: "2-digit",
                }) +
                " " +
                d.toLocaleTimeString("tr-TR", {
                    hour: "2-digit",
                    minute: "2-digit",
                })
            );
        },
        formatCurrency(amount) {
            if (amount == null || isNaN(amount)) return "0,00 ₺";
            return new Intl.NumberFormat("tr-TR", {
                style: "currency",
                currency: "TRY",
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
            }).format(amount);
        },

        getOrderStatusText(status) {
            switch (status) {
                case 1:
                    return "Yeni siparişler";
                case 2:
                    return "Hazırlanan siparişler";
                case 3:
                    return "Kargolanan siparişler";
                case 4:
                    return "İptal/değişim/iade Siparişleri";
                case 5:
                    return "İptal edilen Siparişler";
                default:
                    return "Bilinmeyen durum";
            }
        }
    },
};
</script>

<template>
    <div v-if="show" class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0, 0, 0, 0.5);">
        <div class="modal-dialog modal-xl modal-dialog-centered">
            <div class="modal-content shadow-lg border-0 rounded-4">
                <div class="modal-header bg-primary text-white rounded-top-4">
                    <h5 class="modal-title fw-bold">
                        <i class="bi bi-receipt me-2"></i> Sipariş Detayı
                    </h5>
                    <button type="button" class="btn-close btn-close-white" @click="$emit('close')"></button>
                </div>

                <div class="modal-body" v-if="order">
                    <!-- Sipariş Genel Bilgileri -->
                    <div class="p-3 mb-3 bg-light rounded shadow-sm">
                        <div class="row">
                            <div class="col-md-3">
                                <small class="text-muted d-block">Sipariş No</small>
                                <span class="fw-bold">{{ order.order.code }}</span>
                            </div>
                            <div class="col-md-3">
                                <small class="text-muted d-block">Tarih</small>
                                <span>{{ formatDate(order.order.createdAt) }}</span>
                            </div>
                            <div class="col-md-3">
                                <small class="text-muted d-block">Durum</small>
                                <span class="badge bg-info text-dark">
                                    {{ getOrderStatusText(order.order.status) }}
                                </span>
                            </div>

                            <div class="col-md-3">
                                <small class="text-muted d-block">Toplam</small>
                                <span class="badge bg-success fs-6">
                                    {{ formatCurrency(order.summary.overall) }}
                                </span>
                            </div>
                        </div>
                    </div>

                    <!-- Müşteri Bilgileri -->
                    <div class="p-3 mb-3 rounded border">
                        <h6 class="fw-bold mb-2"><i class="bi bi-person me-1"></i> Müşteri Bilgileri</h6>
                        <div class="row">
                            <div class="col-md-4">
                                <small class="text-muted d-block">Ad Soyad</small>
                                <span>{{ order.customer.name }}</span>
                            </div>
                            <div class="col-md-4">
                                <small class="text-muted d-block">E-posta</small>
                                <span>{{ order.customer.email }}</span>
                            </div>
                            <div class="col-md-4">
                                <small class="text-muted d-block">Telefon</small>
                                <span>{{ order.customer.phone }}</span>
                            </div>
                        </div>
                    </div>

                    <!-- Adresler -->
                    <div class="p-3 mb-3 rounded border bg-white">
                        <h6 class="fw-bold mb-2"><i class="bi bi-geo-alt me-1"></i> Teslimat Adresi</h6>
                        <p class="mb-0">
                            {{ order.customer.delivery.address }},
                            {{ order.customer.delivery.district }},
                            {{ order.customer.delivery.city }}
                        </p>
                        <hr class="my-2">
                        <h6 class="fw-bold mb-2"><i class="bi bi-receipt me-1"></i> Fatura Adresi</h6>
                        <p class="mb-0">
                            {{ order.customer.invoice.address }},
                            {{ order.customer.invoice.district }},
                            {{ order.customer.invoice.city }}
                        </p>
                    </div>

                    <!-- Ürün Tablosu -->
                    <h6 class="fw-bold mb-2"><i class="bi bi-box-seam me-1"></i> Ürünler</h6>
                    <div class="table-responsive shadow-sm rounded">
                        <table class="table table-bordered table-hover align-middle">
                            <thead class="table-primary">
                                <tr>
                                    <th>Kod</th>
                                    <th>Ürün Adı</th>
                                    <th>Miktar</th>
                                    <th>Birim Fiyat</th>
                                    <th>Toplam</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="product in order.products" :key="product.code">
                                    <td><code>{{ product.code }}</code></td>
                                    <td class="fw-semibold">{{ product.name }}</td>
                                    <td>x{{ product.quantity }}</td>
                                    <td>{{ formatCurrency(product.price + product.price / 10) }}</td>
                                    <!-- Burada product.totalPrice yerine summary.overall -->
                                    <td class="fw-bold text-end">
                                        {{ formatCurrency(Math.ceil((product.price + product.price / 10) *
                                            product.quantity)) }}
                                    </td>

                                </tr>
                            </tbody>
                            <tfoot>
                                <tr class="table-light">
                                    <td colspan="4" class="text-end fw-bold">Genel Toplam:</td>
                                    <td class="fw-bold text-end">
                                        {{ formatCurrency(order.summary.overall) }}
                                    </td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>

                <div class="modal-footer bg-light rounded-bottom-4">
                    <button class="btn btn-outline-secondary" @click="$emit('close')">
                        Kapat
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.modal-content {
    animation: fadeInScale 0.25s ease-in-out;
}

@keyframes fadeInScale {
    0% {
        transform: scale(0.95);
        opacity: 0;
    }

    100% {
        transform: scale(1);
        opacity: 1;
    }
}
</style>
