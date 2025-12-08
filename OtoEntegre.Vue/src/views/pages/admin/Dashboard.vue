<script>
import api from '@/views/axios'

export default {
    name: 'AdminDashboard',
    data() {
        return {
            stats: {
                totalUsers: 0,
                totalOrders: 0,
                totalCredits: 0,
                totalIntegrations: 0,
                activeUsers: 0,
                pendingOrders: 0
                ,
                sales: {
                    daily: { count: 0, amount: 0 },
                    weekly: { count: 0, amount: 0 },
                    monthly: { count: 0, amount: 0 }
                }
            },
            loading: true,
            error: null,
            // per-user sales
            salesByUser: [],
            salesPeriod: 'monthly',
            topByCount: null,
            topByAmount: null
        }
    },
    computed: {
        isAdmin() {
            return localStorage.getItem('rol') === 'Admin'
        }
    },
    async mounted() {
        if (!this.isAdmin) {
            this.$router.push('/anasayfa')
            return
        }
        await this.fetchStats()
    },
    methods: {
        async fetchSalesByUser() {
            try {
                const res = await api.get(`api/admin/sales-by-user?period=${this.salesPeriod}`)
                const data = res.data || {}
                console.log('Per-user sales data:', data)
                this.salesByUser = (data.users || []).map(u => ({
                    userId: u.userId,
                    name: u.name || (u.userId || '-'),
                    count: u.count || 0,
                    amount: Number(u.amount || 0)
                }))
                this.topByCount = data.topByCount || null
                this.topByAmount = data.topByAmount || null
            } catch (err) {
                console.error('Per-user satışlar alınamadı', err)
                this.salesByUser = []
                this.topByCount = null
                this.topByAmount = null
            }
        },
        async fetchStats() {
            this.loading = true
            this.error = null
            try {
                // Kullanıcı sayısı
                const usersRes = await api.get('api/users')
                this.stats.totalUsers = usersRes.data?.length || 0

                // Aktif kullanıcılar (son 3 gün)
                const activeUsers = usersRes.data?.filter(u => {
                    if (!u.lastLogin) return false
                    const lastLogin = new Date(u.lastLogin)
                    const threeDaysAgo = new Date()
                    threeDaysAgo.setDate(threeDaysAgo.getDate() - 3)
                    return lastLogin >= threeDaysAgo
                }) || []
                this.stats.activeUsers = activeUsers.length

                // Toplam kredi
                let totalCredits = 0
                for (const user of usersRes.data || []) {
                    try {
                        const creditRes = await api.get(`api/krediler/${user.id}`)
                        totalCredits += creditRes.data?.kalanKredi || 0
                    } catch (err) {
                        // Kredi bilgisi yoksa atla
                    }
                }
                this.stats.totalCredits = totalCredits

                // Entegrasyon sayısı (tahmini - tüm kullanıcılar için entegrasyon kontrolü yapılabilir)
                // Şimdilik basit bir tahmin yapıyoruz
                 const integrationsRes = await api.get('api/entegrasyonlar')
                this.stats.totalIntegrations = integrationsRes.data?.length || 0

                // Sales stats (daily/weekly/monthly)
                try {
                    const salesRes = await api.get('api/admin/sales-stats')
                    const s = salesRes.data || {}
                    this.stats.sales.daily.count = s.daily?.count || 0
                    this.stats.sales.daily.amount = s.daily?.amount || 0
                    this.stats.sales.weekly.count = s.weekly?.count || 0
                    this.stats.sales.weekly.amount = s.weekly?.amount || 0
                    this.stats.sales.monthly.count = s.monthly?.count || 0
                    this.stats.sales.monthly.amount = s.monthly?.amount || 0
                } catch (err) {
                    // ignore sales errors for dashboard
                }

                // fetch per-user sales list
                try {
                    await this.fetchSalesByUser()
                } catch (e) {
                    // ignore
                }

                // Sipariş sayısı ve bekleyen siparişler
                // Bu endpoint'ler backend'de olmayabilir, şimdilik placeholder
                this.stats.totalOrders = 0
                this.stats.pendingOrders = 0

            } catch (err) {
                console.error('İstatistikler yüklenemedi:', err)
                this.error = 'İstatistikler yüklenirken hata oluştu.'
            } finally {
                this.loading = false
            }
        },
          formatCurrency(amount) {
      if (amount == null || isNaN(amount)) return '0,00 ₺'
      return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
      }).format(amount)
    },
    }
}
</script>

<template>
    <div class="container-fluid py-4">
        <div v-if="!isAdmin" class="alert alert-danger">
            Bu sayfaya sadece Admin rolündeki kullanıcılar erişebilir.
        </div>

        <div v-else>
            <h2 class="mb-4">Admin Dashboard</h2>

            <div v-if="loading" class="text-center py-5">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Yükleniyor...</span>
                </div>
            </div>

            <div v-else-if="error" class="alert alert-danger">{{ error }}</div>

            <div v-else>
                <!-- İstatistik Kartları -->
                <div class="row g-3 mb-4">
                    <div class="col-md-4">
                        <div class="card text-white bg-primary">
                            <div class="card-body">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div>
                                        <h5 class="card-title">Toplam Kullanıcı</h5>
                                        <h2 class="mb-0">{{ stats.totalUsers }}</h2>
                                    </div>
                                    <span class="material-icons" style="font-size: 48px; opacity: 0.3">people</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="col-md-4">
                        <div class="card text-white bg-success">
                            <div class="card-body">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div>
                                        <h5 class="card-title">Aktif Kullanıcılar</h5>
                                        <h2 class="mb-0">{{ stats.activeUsers }}</h2>
                                        <small>(Son 3 gün)</small>
                                    </div>
                                    <span class="material-icons" style="font-size: 48px; opacity: 0.3">person</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card text-white bg-warning">
                            <div class="card-body">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div>
                                        <h5 class="card-title">Toplam Entegrasyon</h5>
                                        <h2 class="mb-0">{{ stats.totalIntegrations }}</h2>
                                    </div>
                                    <span class="material-icons" style="font-size: 48px; opacity: 0.3">settings</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Satış Özetleri -->
                <div class="row g-3 mb-4">
                    <div class="col-md-4">
                        <div class="card text-white bg-info">
                            <div class="card-body">
                                <h6 class="card-title">Günlük Satış (Adet)</h6>
                                <h3 class="mb-0">{{ stats.sales.daily.count }}</h3>
                                <small>Toplam Tutar: {{ formatCurrency(stats.sales.daily.amount) }}</small>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card text-white bg-secondary">
                            <div class="card-body">
                                <h6 class="card-title">Haftalık Satış (Adet)</h6>
                                <h3 class="mb-0">{{ stats.sales.weekly.count }}</h3>
                                <small>Toplam Tutar: {{ formatCurrency(stats.sales.weekly.amount) }}</small>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card text-white bg-dark">
                            <div class="card-body">
                                <h6 class="card-title">Aylık Satış (Adet)</h6>
                                <h3 class="mb-0">{{ stats.sales.monthly.count }}</h3>
                                <small>Toplam Tutar: {{ formatCurrency(stats.sales.monthly.amount) }}</small>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Per-user Sales (Top users) -->
                <div class="card mb-3">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h5>En Çok Satış Yapan Kullanıcılar</h5>
                            <div style="width:220px">
                                <select class="form-select" v-model="salesPeriod" @change="fetchSalesByUser">
                                    <option value="daily">Günlük</option>
                                    <option value="weekly">Haftalık</option>
                                    <option value="monthly">Aylık</option>
                                    <option value="all">Tümü</option>
                                </select>
                            </div>
                        </div>

                        <div class="row mb-3">
                            <div class="col-md-6">
                                <div class="card border-success">
                                    <div class="card-body">
                                        <h6 class="card-title">En Çok Adet (Kullanıcı)</h6>
                                        <p class="h4">{{ topByCount ? (topByCount.name || topByCount.userId) + ' (' + topByCount.count + ')' : '-' }}</p>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="card border-primary">
                                    <div class="card-body">
                                        <h6 class="card-title">En Çok Tutar (Kullanıcı)</h6>
                                        <p class="h4">{{  formatCurrency(topByAmount ? topByAmount.amount : 0) }}</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="table-responsive">
                            <table class="table table-sm table-hover">
                                <thead>
                                    <tr>
                                        <th>Kullanıcı</th>
                                        <th>Adet</th>
                                        <th>Tutar</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="u in salesByUser" :key="u.userId">
                                        <td>{{ u.name || u.userId }}</td>
                                        <td>{{ u.count }}</td>
                                        <td>{{ formatCurrency(u.amount) }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <!-- Hızlı Erişim -->
                <div class="card">
                    <div class="card-header">
                        <h5>Hızlı Erişim</h5>
                    </div>
                    <div class="card-body">
                        <div class="row g-3">
                            <div class="col-md-3">
                                <router-link to="/admin/kullanicilar" class="btn btn-outline-primary w-100">
                                    <span class="material-icons me-2">people</span>
                                    Kullanıcı Yönetimi
                                </router-link>
                            </div>
                            <div class="col-md-3">
                                <router-link to="/admin/krediler" class="btn btn-outline-success w-100">
                                    <span class="material-icons me-2">account_balance_wallet</span>
                                    Kredi Yönetimi
                                </router-link>
                            </div>
                            <div class="col-md-3">
                                <router-link to="/admin/entegrasyonlar" class="btn btn-outline-info w-100">
                                    <span class="material-icons me-2">settings</span>
                                    Entegrasyon Yönetimi
                                </router-link>
                            </div>
                            <div class="col-md-3">
                                <router-link to="/admin/siparisler" class="btn btn-outline-warning w-100">
                                    <span class="material-icons me-2">shopping_cart</span>
                                    Sipariş Yönetimi
                                </router-link>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.card {
    border: none;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition: transform 0.2s;
}

.card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.material-icons {
    vertical-align: middle;
}
</style>
