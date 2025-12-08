<script>
import api from '@/views/axios'

export default {
  name: 'AdminSiparisler',
  data() {
    return {
      users: [],
      orders: {},
      loading: true,
      error: null,
      searchQuery: '',
      selectedUserId: null,
      currentPage: {},
      pageSize: 10
    }
  },
  computed: {
    isAdmin() {
      return localStorage.getItem('rol') === 'Admin'
    },
    filteredUsers() {
      if (!this.searchQuery) return this.users
      const query = this.searchQuery.toLowerCase()
      return this.users.filter(u =>
        u.ad?.toLowerCase().includes(query) ||
        u.email?.toLowerCase().includes(query)
      )
    }
  },
  async mounted() {
    if (!this.isAdmin) {
      this.$router.push('/anasayfa')
      return
    }
    await this.fetchUsers()
  },
  methods: {
    async fetchUsers() {
      this.loading = true
      this.error = null
      try {
        const res = await api.get('api/users')
        this.users = res.data || []
        // Her kullanıcı için sayfa numarasını başlat (Vue 3: this.$set yok)
        this.users.forEach(user => {
          this.currentPage = {
            ...this.currentPage,
            [user.id]: 0
          }
        })
      } catch (err) {
        console.error('Kullanıcılar yüklenemedi:', err)
        this.error = 'Kullanıcılar yüklenirken hata oluştu.'
      } finally {
        this.loading = false
      }
    },
    async fetchUserOrders(userId) {
      try {
        const page = this.currentPage[userId] || 0
        // Backend'de tüm siparişleri getiren endpoint olmayabilir
        // Şimdilik kullanıcı bazlı endpoint kullanıyoruz
        const res = await api.get(`api/siparisler/kullanici/${userId}?page=${page}&size=${this.pageSize}`)
        console.log('Siparişler yüklendi for user',res.data.data)
       
        this.orders = {
          ...this.orders,
          [userId]: res.data.data|| []
        }
      } catch (err) {
        // Alternatif endpoint deneyelim
        try {
          const res = await api.get(`api/siparisler/by-user/${userId}`)
          this.orders = {
            ...this.orders,
            [userId]: Array.isArray(res.data) ? res.data : []
          }
        } catch (err2) {
          console.error('Siparişler yüklenemedi:', err2)
          this.orders = {
            ...this.orders,
            [user.id]: []
          }
        }
      }
    },
    async loadOrdersForUser(userId) {
      if (!this.orders[userId]) {
        await this.fetchUserOrders(userId)
      }
    },
    formatDate(dateStr) {
      if (!dateStr) return '-'
      const d = new Date(dateStr)
      return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
    },
    formatCurrency(amount) {
      if (amount == null || isNaN(amount)) return '0,00 ₺'
      return new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY'
      }).format(amount)
    },
    getUserOrders(userId) {
      return this.orders[userId] || []
    },
    getTotalOrders(userId) {
      return this.getUserOrders(userId).length
    }
  }
}
</script>

<template>
  <div class="container-fluid py-4">
    <div v-if="!isAdmin" class="alert alert-danger">
      Bu sayfaya sadece Admin rolündeki kullanıcılar erişebilir.
    </div>

    <div v-else>
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>Sipariş Yönetimi</h2>
      </div>

      <!-- Arama -->
      <div class="card mb-3">
        <div class="card-body">
          <input type="text" class="form-control" v-model="searchQuery"
            placeholder="Kullanıcı adı veya email ile ara..." />
        </div>
      </div>

      <!-- Kullanıcı ve Sipariş Listesi -->
      <div class="card">
        <div class="card-body">
          <div v-if="loading" class="text-center py-5">
            <div class="spinner-border" role="status">
              <span class="visually-hidden">Yükleniyor...</span>
            </div>
          </div>

          <div v-else-if="error" class="alert alert-danger">{{ error }}</div>

          <div v-else-if="filteredUsers.length === 0" class="text-center py-5 text-muted">
            Kullanıcı bulunamadı.
          </div>

          <div v-else>
            <div v-for="user in filteredUsers" :key="user.id" class="mb-4">
              <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                  <div>
                    <h5 class="mb-0">{{ user.ad || user.email }}</h5>
                    <small class="text-muted">{{ user.email }}</small>
                  </div>
                  <button class="btn btn-sm btn-outline-primary" @click="loadOrdersForUser(user.id)">
                    <span class="material-icons me-1" style="font-size: 18px; vertical-align: middle;">refresh</span>
                    Siparişleri Yükle
                  </button>
                </div>
                <div class="card-body">
                  <div v-if="!orders[user.id]" class="text-center text-muted py-3">
                    Siparişler yüklenmedi. "Siparişleri Yükle" butonuna tıklayın.
                  </div>
                  <div v-else-if="getUserOrders(user.id).length === 0" class="text-center text-muted py-3">
                    Bu kullanıcının siparişi bulunmamaktadır.
                  </div>
                  <div v-else class="table-responsive">
                    <table class="table table-sm table-hover">
                      <thead>
                        <tr>
                          <th>Sipariş No</th>
                          <th>Müşteri</th>
                          <th>Tutar</th>
                          <th>Durum</th>
                          <th>Tarih</th>
                          <th>Telegram</th>
                          <th>Tedarik Satın Alım</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="order in getUserOrders(user.id)" :key="order.id">
                          <td>{{ order.siparisNumarasi || '-' }}</td>
                          <td>{{ order.musteriAdSoyad || '-' }}</td>
                          <td>{{ formatCurrency(order.toplamTutar) }}</td>
                          <td>
                            <span :class="{
                              'badge bg-success': order.durum === 'SHIPPED' || order.durum === 'DELIVERED',
                              'badge bg-warning': order.durum === 'PICKING' || order.durum === 'INVOICED',
                              'badge bg-danger': order.durum === 'CANCELLED',
                              'badge bg-secondary': !order.durum
                            }">
                              {{ order.durum || '-' }}
                            </span>
                          </td>
                          <td>{{ formatDate(order.createdAt) }}</td>
                          <td>
                            <span v-if="order.telegramSent" class="badge bg-success">
                              <span class="material-icons" style="font-size: 14px; vertical-align: middle;">check</span>
                            </span>
                            <span v-else class="badge bg-danger">
                              <span class="material-icons" style="font-size: 14px; vertical-align: middle;">close</span>
                            </span>
                          </td>
                          <td>{{order.eslestirmeDurumu}}</td>
                        </tr>
                      </tbody>
                    </table>
                    <div class="mt-2 text-muted">
                      Toplam: {{ getTotalOrders(user.id) }} sipariş
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.table-responsive {
  max-height: 400px;
  overflow-y: auto;
}
</style>
