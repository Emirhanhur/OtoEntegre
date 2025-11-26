<script>
import { formatCurrency } from '../../../utils/format'
import SiparisDetayModal from './siparisDetayModal.vue';
import OtostickerLoginModal from './OtostickerLoginModal.vue';
import { nextTick } from 'vue';
import api from "../../axios";

export default {
    components: {
        SiparisDetayModal,
        OtostickerLoginModal
    },
    data() {
        return {
            isBulkSendingTelegram: false,
            orders: [],
            isPdfLoading: false, // PDF yükleniyor durumu için
            selectedStatus: null,
            startDate: '',
            endDate: '',
            transactionType: 'Sale',
            cariData: [],
            isCariLoading: false,
            currentPage: 1,
            pageSize: 10,
            isLoading: false,
            isGlobalLoading: false,
            gonderilmeyenler: false,
            sendingTelegramId: null, // 🔄 Telegram gönderimi yapılan siparişin id'si
            isSendingTelegram: false, // yeni state
            sendingOrderId: null, // hangi sipariş gönderiliyor

            selectedOrders: [],
            selectedOrder: null,
            searchQuery: "", // 🔍 Arama inputu için
            showOtostickerModal: false,
            selectedOrderForOtosticker: null,
            selectedShippingCompany: "",
            successMessage: "", // ✅ alert için eklendi

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
                { key: 'PICKING', label: 'İşleme Alındı', count: 0 },
                { key: 'SHIPPED', label: 'Taşıma Durumunda', count: 0 },
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
            return Math.ceil(this.filteredOrders.length / this.pageSize) || 1;
        },
        filteredOrders() {
            let list = this.orders;

            // Statüye göre filtre
            if (this.selectedStatus) {
                list = list.filter(order => order.originalStatus === this.selectedStatus);
            }

            // 🔍 Arama filtresi
            if (this.searchQuery.trim() !== "") {
                const q = this.searchQuery.toLowerCase();
                list = list.filter(order =>
                    (order.siparisNumarasi && order.siparisNumarasi.toString().toLowerCase().includes(q)) ||
                    (order.musteriAdSoyad && order.musteriAdSoyad.toLowerCase().includes(q)) ||
                    (order.urunAdi && order.urunAdi.toLowerCase().includes(q)) // API’den ürün adı geliyorsa
                );
            }
            if (this.gonderilmeyenler) {
                list = list.filter(order => !order.telegramSent && order.durum !== 'İptal Edildi');
            }


            return list;
        },
        paginatedOrders() {
            const start = (this.currentPage - 1) * this.pageSize;
            return this.filteredOrders.slice(start, start + this.pageSize);
        },
    },
    watch: {
        selectedStatus() {
            this.currentPage = 1;
        },
        searchQuery() {
            this.currentPage = 1; // Arama yapıldığında ilk sayfaya dön
        },
        pageSize() {
            this.currentPage = 1;
        }

    },
    async mounted() {
        this.loadOrders();
        const orderNumber = this.$route.query.orderNumber;
        if (orderNumber) {
            this.openOrderModalByNumber(orderNumber);
        }
    },
    beforeUnmount() {
        clearInterval(this.pollingInterval);
    },
    methods: {
        handleModalClose() {
            this.selectedOrder = null;

            // 🔙 URL'den orderNumber parametresini kaldır
            this.$router.push({
                path: this.$route.path, // yani /trendyol-entegrasyon
                query: {}               // boş query -> ?orderNumber kaldırılır
            });
        },
        async openOrderModalByNumber(orderNumber) {
            try {
                const res = await api.get(`/api/Siparisler/by-order-number/${orderNumber}`);
                this.selectedOrder = res.data;

                await nextTick(); // props güncellensin

                // order artık null değil → modal aç
                if (this.$refs.orderModal?.showModal) {
                    this.$refs.orderModal.showModal();
                }

            } catch (err) {
                console.error("Sipariş detayı alınamadı:", err);
                alert("Sipariş detayı alınamadı.");
            }
        },


        formatDate(timestamp) {
            return new Date(timestamp).toLocaleString("tr-TR");
        },
        async setPicking(order) {
            console.log("İşleme alınıyor:", order);
            if (!order.sellerId || !order.paketNumarasi) {
                alert("SellerId veya PackageId eksik vue.");
                return;
            }

            const payload = {
                lines: order.siparisUrunleri.map(item => ({
                    lineId: item.lineId,   // backend'den gelen lineId
                    quantity: item.Adet
                })),
                params: {},
                status: "Picking"
            };

            try {
                this.isGlobalLoading = true;

                const res = await api.put(`/api/Siparisler/trendyol/picking/${order.id}`, payload);
                if (res.data.success) {
                    alert("Sipariş Trendyol’da İşleme Alındı!");
                    this.loadOrders(this.selectedStatus);
                } else {
                    alert("Hata oluştu: " + (res.data.error || "Bilinmeyen hata"));
                }
            } catch (err) {
                console.error(err);
                alert("Hata oluştu.");
            } finally {
                this.isGlobalLoading = false;
            }
        },
        async loadOrders(durum = null) {
            try {
                this.isLoading = true;
                let url = `/api/Siparisler/kullanici/${localStorage.getItem("kullanici_id")}?sort=desc`;
                if (durum !== null) url += `?durum=${durum}`;
                const res = await api.get(url);
                console.log("siparişler ===", res);
                this.orders = res.data.data;
                this.orders.forEach(element => {
                    element.originalStatus = element.durum?.toUpperCase() || '';

                    const statusMap = {
                        CREATED: "Oluşturuldu",
                        PICKING: "İşleme Alındı",
                        SHIPPED: "Taşıma Durumunda",
                        DELIVERED: "Teslim Edildi",
                        INVOICED: "Faturalandı",
                        CANCELLED: "İptal Edildi",
                        UNDELIVERED: "Teslim Edilemedi",
                        RETURNED: "İade Edildi",
                        UNSUPPLIED: "Temin Edilmemiş",
                        AWAITING: "Bekleniyor",
                        UNPACKED: "Pakete Çıktı",
                        AT_COLLECTION_POINT: "Teslimat Noktasında",
                        VERIFIED: "Doğrulandı"
                    };

                    element.durum = statusMap[element.originalStatus] || element.originalStatus;
                });

                this.updateStatusCounts();
            } catch (err) {
                console.error("Siparişler yüklenemedi", err);
            } finally {
                this.isLoading = false;
            }
        },
        updateStatusCounts() {
            this.orderStatuses.forEach(status => status.count = 0);
            this.orderStatuses[0].count = this.orders.length;

            this.orders.forEach(order => {
                const statusObj = this.orderStatuses.find(s => s.key === order.originalStatus);
                if (statusObj) statusObj.count++;
            });
        },
        async updateShippingCompany() {
            if (!this.selectedShippingCompany || this.selectedOrders.length === 0) {
                alert("Lütfen en az bir sipariş seçin ve kargo firması belirleyin.");
                return;
            }

            try {
                const payload = {
                    OrderIds: this.selectedOrders.map(id => id.toString()),
                    ShippingCompany: this.selectedShippingCompany
                };

                const res = await api.post('/api/siparisler/toplu-kargo-degistir', payload);

                if (res.data.success) {
                    this.successMessage = "Kargo firması başarıyla güncellendi!";
                    this.selectedOrders = [];
                    this.selectedShippingCompany = null;
                    this.loadOrders(this.selectedStatus);

                    // ✅ 3 saniye sonra mesajı otomatik kaldır
                    setTimeout(() => {
                        this.successMessage = "";
                    }, 3000);
                } else {
                    alert("Güncelleme başarısız!");
                }
            } catch (err) {
                console.error("Toplu kargo güncelleme hatası:", err);
                alert("Hata oluştu!");
            }
        }

        ,
        async loadReturnedOrders() {
            try {
                this.isLoading = true;
                const userId = localStorage.getItem("kullanici_id");
                // backend endpoint durum parametresiyle iade siparişlerini çekiyor
                const res = await api.get(`/api/Siparisler/kullanici/${userId}?durum=RETURNED`);
                this.orders = res.data.data;

                // Durumları eşle
                this.orders.forEach(order => {
                    order.originalStatus = order.durum?.toUpperCase() || '';
                    const statusMap = {
                        RETURNED: "İade Edildi",
                        // diğer statüler
                        CREATED: "Oluşturuldu",
                        PICKING: "İşleme Alındı",
                        SHIPPED: "Taşıma Durumunda",
                        DELIVERED: "Teslim Edildi",
                        INVOICED: "Faturalandı",
                        CANCELLED: "İptal Edildi",
                        UNDELIVERED: "Teslim Edilemedi"
                    };
                    order.durum = statusMap[order.originalStatus] || order.originalStatus;
                });

                this.updateStatusCounts();
            } catch (err) {
                console.error("İade siparişler yüklenemedi", err);
            } finally {
                this.isLoading = false;
            }
        }
        ,
        async selectStatus(statusKey) {
            this.selectedStatus = statusKey;
            if (statusKey === 'RETURNED') {
                await this.loadReturnedOrders(); // iade siparişleri yükle
            } else {
                await this.loadOrders(statusKey); // normal siparişler
            }
        }
        ,
        async iptalTelegram(orderId) {
            try {
                const res = await api.post(`/api/entegrasyonlar/send-iptal-telegram/${orderId}`);

                if (res.data.sent) {
                    alert("İptal bilgisi Telegram'a gönderildi.");
                } else {
                    alert("Gönderilemedi.");
                }
            } catch (err) {
                console.error("İptal telegram hatası:", err);
                alert("Hata oluştu.");
            }
        },

        async sendTelegram(orderId) {
            if (this.isSendingTelegram) return; // zaten gönderim varsa engelle

            this.isSendingTelegram = true;
            this.sendingOrderId = orderId;
            try {

                const res = await api.post(`/api/entegrasyonlar/send-siparis-telegram/${orderId}`);
                if (res.data.sent) {
                    const toastEl = document.getElementById('successToast');
                    if (toastEl) {
                        const toast = new bootstrap.Toast(toastEl);
                        toast.show();
                    }
                    this.loadOrders(this.selectedStatus);
                } else {
                    alert("Gönderilemedi.");
                }
            } catch (err) {
                console.error(err);
                alert("Hata oluştu.");
            }
            finally {
                this.isSendingTelegram = false;
                this.sendingOrderId = null;
            }
        },

        formatMoney(amount, currency) {
            return formatCurrency(amount, currency);
        },

        formatOrderDate(val) {
            const d = new Date(val);
            d.setHours(d.getHours() - 3);
            return d.toLocaleString('tr-TR', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        },

        async openDetailModal(order) {
            // URL’ye orderNumber parametresini ekle
            this.$router.push({
                path: this.$route.path,
                query: { orderNumber: order.siparisNumarasi }
            });

            // Modal’ı aç
            this.selectedOrder = order;
            await nextTick();
            if (this.$refs.orderModal && typeof this.$refs.orderModal.showModal === 'function') {
                this.$refs.orderModal.showModal();
            }
        },
        openOtostickerModal(order) {
            this.selectedOrderForOtosticker = order;
            this.showOtostickerModal = true;
        },
        toggleSelectAll(event) {
            if (event.target.checked) {
                this.selectedOrders = this.paginatedOrders.map(o => o.id);
            } else {
                this.selectedOrders = [];
            }
        },
        getDelayStatus(order) {
            const shippedStatuses = ["SHIPPED", "DELIVERED", "CANCELLED", "RETURNED", "INVOICED"];
            if (shippedStatuses.includes(order.originalStatus)) return null;

            const deliveryDate = order.estimatedDeliveryDate
                ? new Date(order.estimatedDeliveryDate)
                : new Date(new Date(order.createdAt).getTime() + 3 * 24 * 60 * 60 * 1000);

            const now = new Date();
            const diffMs = deliveryDate - now;

            if (diffMs < 0) {
                const diffDays = Math.floor(Math.abs(diffMs) / (1000 * 60 * 60 * 24));
                return { text: `Gecikti (${diffDays} gün)`, class: "badge bg-danger" };
            } else {
                const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
                const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
                return { text: `${diffDays} gün ${diffHours} saat ${diffMinutes} dakika kaldı`, class: "badge bg-success" };
            }
        },
        async printSelectedOrders() {
            if (this.selectedOrders.length === 0) return;
            if (this.isSendingTelegram) return; // zaten gönderim varsa engelle

            // 1. Kullanıcıya sor: Telegrama gitsin mi?
            const sendToTelegram = confirm("Oluşturulan toplu PDF Telegram'a gönderilsin mi?");

            this.isPdfLoading = true;
            try {
                // 2. Backend'e 'sendTelegram' parametresini de ekleyerek gönderiyoruz
                const response = await api.post('/api/Entegrasyonlar/toplu-pdf', {
                    orderIds: this.selectedOrders,
                    SendToTelegram: sendToTelegram // büyük S!
                }, {
                    responseType: 'blob'
                });


                const file = new Blob([response.data], { type: 'application/pdf' });
                const fileURL = URL.createObjectURL(file);
                window.open(fileURL, '_blank');

                this.isBulkSendingTelegram = true;
                // 4. Eğer telegrama gönderildiyse kullanıcıya bilgi ver
                if (sendToTelegram) {
                    // İsteğe bağlı olarak toast mesajı da tetikleyebilirsin
                    const toastEl = document.getElementById('successToast');
                    if (toastEl) {
                        const toast = new bootstrap.Toast(toastEl);
                        toast.show();
                    }
                }

            } catch (err) {
                console.error("PDF Hatası:", err);
                alert("PDF işlemi sırasında bir hata meydana geldi.");
            } finally {
                this.isPdfLoading = false;
                this.isBulkSendingTelegram = false;   // toplu işlem bittikten sonra kapat
                this.sendingOrderId = null;       // tekli sipariş gönderimi için de sıfırlama
                this.selectedOrders.forEach(id => {
                    const order = this.orders.find(o => o.id === id);
                    if (order) order.telegramSent = true;
                });
                this.selectedOrders = []
            }
        },




    }
};
</script>

<template>
    <div>
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h2>Trendyol Siparişleri</h2>
            <!-- 🔍 Arama Inputu -->

        </div>

        <div class="d-flex  gap-2 mb-3 justify-content-start align-items-center flex-wrap responsive-controls">
            <input v-model="searchQuery" type="text" class=" form-control w-100"
                placeholder="Sipariş no, müşteri adı veya ürün adı ara..." />
            <select class="form-select w-auto" v-model="selectedShippingCompany">
                <option disabled value="">Kargo Seç</option>
                <option v-for="company in cargoOptions" :key="company.value" :value="company.value">
                    {{ company.label }}
                </option>
            </select>

            <button class="btn btn-success" :disabled="!selectedShippingCompany || selectedOrders.length === 0"
                @click="updateShippingCompany">
                Kargo Firmasını Güncelle
            </button>
            <button class="btn btn-dark text-white ms-2" :disabled="selectedOrders.length === 0"
                @click="printSelectedOrders">
                <span v-if="isPdfLoading" class="spinner-border spinner-border-sm me-1"></span>
                <span v-else class="material-icons align-middle me-1">print</span>
                {{ isPdfLoading ? 'Hazırlanıyor...' : 'Seçilenleri Yazdır (PDF)' }}
            </button>
            <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" id="gonderilmeyenler" v-model="gonderilmeyenler" />
                <label class="form-check-label" for="gonderilmeyenler">Sadece Gönderilmeyenler</label>
            </div>
        </div>


        <!-- Durum Tabları -->
        <div class="d-flex flex-wrap gap-2 mb-3">
            <button v-for="status in orderStatuses" :key="status.key" class="btn d-flex align-items-center gap-2"
                :class="{
                    'btn-primary': selectedStatus === status.key,
                    'btn-outline-secondary': selectedStatus !== status.key
                }" @click="selectStatus(status.key)">
                <span>{{ status.label }}</span>
                <span class="badge ms-1" :class="{
                    'bg-danger': status.count === 0,
                    'bg-success': status.count > 0 && selectedStatus !== status.key
                }">
                    {{ status.count }}
                </span>
            </button>
        </div>
        <div class="d-flex justify-content-between">
            <div v-if="selectedOrders" class="mt-2">
                <b>Seçilen Sipariş Sayısı:</b>
                <span class="badge bg-primary">{{ selectedOrders.length }}</span>
            </div>
            <div class="d-flex justify-content-end mb-2 align-items-center gap-2">
                <b>
                    Listelenecek Ürün Sayısı:
                </b>

                <select v-model="pageSize" class="form-select form-select-sm" style="width:120px">
                    <option :value="10">10</option>
                    <option :value="20">20</option>
                    <option :value="50">50</option>
                    <option :value="100">100</option>
                </select>
            </div>
        </div>

        <div class="position-fixed top-0 end-0 p-3" style="z-index: 9999;">
            <div id="successToast" class="toast align-items-center text-bg-success border-0" role="alert"
                aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        ✅ Sipariş Telegrama gönderildi
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"
                        aria-label="Close"></button>
                </div>
            </div>
        </div>

        <!-- Loading -->
        <div v-if="isLoading" class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2 text-secondary">Siparişler yükleniyor...</p>
        </div>

        <!-- Tablo -->
        <div class="table-responsive">
            <table class="table dark:table-dark table-bordered table-hover">
                <thead class="table-light">
                    <tr>
                        <th>
                            <input type="checkbox" @change="toggleSelectAll($event)">
                        </th>
                        <th>Ürün</th>
                        <th>Sipariş No</th>
                        <th>Müşteri Adı</th>
                        <th>Kargo Firması</th>
                        <th>Durum</th>
                        <th>Tarih</th>
                        <!-- <th>Gecikme Durumu</th> -->

                        <th>Toplam</th>
                        <th class="text-center">Mesaj Durumu</th>
                        <th class="text-center">#</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="(order) in paginatedOrders" :key="order.id">
                        <td>
                            <input type="checkbox" :value="order.id" v-model="selectedOrders">
                        </td>
                        <td class="text-center">
                            <div v-if="order.siparisUrunleri && order.siparisUrunleri.length > 0"
                                class="d-flex flex-wrap justify-content-center align-items-start gap-2">
                                <div v-for="(urunItem, i) in order.siparisUrunleri" :key="i"
                                    class="d-flex flex-column align-items-center position-relative"
                                    style="width:100px;">

                                    <!-- 📌 Adet Badge (sağ üst köşe) -->
                                    <span v-if="urunItem.adet"
                                        class="position-absolute top-0 end-0 translate-middle badge rounded-pill bg-primary"
                                        style="font-size:0.7rem; padding:4px 6px; z-index:10;">
                                        {{ urunItem.adet }}
                                    </span>

                                    <!-- Ürün görseli -->
                                    <img :src="urunItem.urun?.image" alt="Ürün Resmi"
                                        style="height:90px; object-fit:contain; border-radius:6px; border:1px solid #ddd;">

                                    <!-- Ürün adı -->
                                    <div class="mt-1 text-truncate" style="font-size:0.75rem; width:100px;">
                                        {{ urunItem.urun?.ad }}
                                    </div>
                                </div>

                            </div>
                            <div v-else>—</div>
                        </td>

                        <td>{{ order.siparisNumarasi }}</td>
                        <td>{{ order.musteriAdSoyad }}</td>
                        <td>
                            {{
                                cargoOptions.find(c => c.value === order.cargoProviderName)?.label ||
                                order.cargoProviderName ||
                                '—'
                            }}
                        </td>
                        <td>
                            <span class="badge" :class="{
                                'bg-success': order.originalStatus === 'DELIVERED',
                                'bg-primary': order.originalStatus === 'SHIPPED',
                                'bg-warning text-dark': ['CREATED', 'AWAITING'].includes(order.originalStatus),
                                'bg-danger': order.originalStatus === 'CANCELLED',
                                'bg-secondary': !['DELIVERED', 'SHIPPED', 'CREATED', 'AWAITING', 'CANCELLED'].includes(order.originalStatus)
                            }">
                                {{ order.durum }}
                            </span>
                        </td>
                        <td>{{ formatOrderDate(order.createdAt) }}</td>
                        <!-- <td>
                            <span v-if="getDelayStatus(order)" :class="getDelayStatus(order).class">
                                {{ getDelayStatus(order).text }}
                            </span>
                            <span v-else>—</span> 
                        </td> -->
                        <td>
                            {{order.siparisUrunleri.reduce((toplam, urun) => toplam + urun.toplam_Fiyat, 0).toFixed(2)}}
                        </td>



                        <td class="text-center">
                            <span v-if="order.durum === 'İptal Edildi'" class="text-danger">
                                <span class="material-icons align-middle">cancel</span>
                            </span>

                            <span v-else-if="order.telegramSent" class="text-success">
                                <span class="material-icons align-middle">check_circle</span>
                            </span>

                            <button v-else
                                class="btn btn-primary btn-sm d-flex align-items-center justify-content-center gap-2"
                                @click="sendTelegram(order.id)" :disabled="isSendingTelegram">
                                <span v-if="sendingOrderId === order.id" class="spinner-border spinner-border-sm"
                                    role="status" aria-hidden="true"></span>
                                <span>{{ sendingOrderId === order.id ? "Gönderiliyor..." : "Gönder" }}</span>
                            </button>

                            <button v-if="order.durum === 'İptal Edildi'"
                                class="btn btn-primary btn-sm d-flex align-items-center justify-content-center gap-2"
                                @click="iptalTelegram(order.id)">
                                İptal Edildi gönder
                            </button>


                        </td>

                        <td class="text-center">
                            <div class="d-flex flex-column gap-4 justify-content-center align-items-center h-100 w-100">
                                <button class="btn btn-outline-primary btn-sm w-100" @click="openDetailModal(order)">
                                    <span class="material-icons align-middle">visibility</span> Detay
                                </button>

                                <!-- İşleme Al butonu -->
                                <button v-if="order.durum" class="btn btn-sm w-100"
                                    :class="{ 'btn-warning': order.originalStatus !== 'PICKING', 'btn-success': order.originalStatus === 'PICKING' }"
                                    @click="setPicking(order)"
                                    :disabled="order.originalStatus === 'PICKING' || isGlobalLoading">
                                    <span class="material-icons align-middle">autorenew</span>
                                    {{ order.originalStatus === 'PICKING' ? 'İşleme Alındı' : 'İşleme Al' }}
                                </button>

                                <!-- Tam ekran loading overlay -->
                                <div v-if="isGlobalLoading"
                                    class="position-fixed top-0 start-0 w-100 h-100 d-flex justify-content-center align-items-center"
                                    style="z-index: 2000; background-color: rgba(220, 220, 220, 0.7);opacity: 0.05;">
                                    <div class="text-center text-dark">
                                        <div class="spinner-border text-success" role="status"></div>
                                    </div>
                                </div>

                            </div>
                        </td>

                        <!--  <button class="btn btn-success" @click="showOtostickerModal = true">
                                Otosticker Login
                            </button>-->




                    </tr>
                </tbody>
                <tfoot v-if="paginatedOrders.length < 1">
                    <tr>
                        <td colspan="8" class="text-center py-4 text-secondary">
                            <span class="material-icons fs-1 mb-2 align-middle">inbox</span>
                            <p>{{ selectedStatus ? 'Bu durumda sipariş bulunmuyor' : 'Henüz sipariş yok' }}</p>
                        </td>
                    </tr>
                </tfoot>
            </table>
        </div>

        <!-- Pagination -->
        <div v-if="!isLoading && paginatedOrders.length > 0" class="d-flex justify-content-center mt-3 gap-2">
            <button class="btn btn-outline-secondary" :disabled="currentPage === 1" @click="currentPage--">
                <span class="material-icons align-middle me-1">chevron_left</span> Önceki
            </button>
            <span class="align-self-center">
                Sayfa {{ currentPage }} / {{ totalPages }} ({{ filteredOrders.length }} sipariş)
            </span>
            <button class="btn btn-outline-secondary" :disabled="currentPage === totalPages" @click="currentPage++">
                Sonraki <span class="material-icons align-middle ms-1">chevron_right</span>
            </button>
        </div>
        <SiparisDetayModal ref="orderModal" :order="selectedOrder" @close="handleModalClose" />


        <OtostickerLoginModal :show="showOtostickerModal" @close="showOtostickerModal = false" />
    </div>
</template>
<style>
/* Açık mod (varsayılan) */
.btn-outline-primary {
    color: #1e1e1e;
    border-color: #1e1e1e;
}

.btn-outline-primary .material-icons {
    color: #1e1e1e;
}

/* Karanlık mod */
html[data-coreui-theme='dark'] .btn-outline-primary {
    color: #1e1e1e;
    border-color: #1e1e1e;
}

html[data-coreui-theme='dark'] .btn-outline-primary .material-icons {
    color: #1e1e1e;
}
</style>