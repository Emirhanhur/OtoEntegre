<script>
import api from '@/views/axios'

export default {
  name: 'AdminKrediler',
  data() {
    return {
      users: [],
      userCredits: {},
      loading: true,
      error: null,
      searchQuery: '',
      selectedUserId: null,
      creditAmount: 0,
      showAddModal: false
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
    await this.fetchCreditsSummary()
  },
  methods: {
    async fetchCreditsSummary() {
      this.loading = true
      this.error = null
      try {
        // Yeni endpoint tüm kullanıcıları ve kredilerini tek seferde döner
        const res = await api.get('api/krediler')
        const list = res.data || []

        // Kullanıcıları ve kredi bilgilerini ayır
        this.users = list.map(u => ({ id: u.kullaniciId, ad: u.ad, email: u.email }))

        const creditsMap = {}
        list.forEach(u => {
          creditsMap[u.kullaniciId] = {
            kalanKredi: u.kalanKredi || 0,
            sonSatinAlim: u.sonSatinAlim || null
          }
        })
        this.userCredits = creditsMap
      } catch (err) {
        console.error('Kredi özeti yüklenemedi:', err)
        this.error = 'Kredi bilgileri yüklenirken hata oluştu.'
      } finally {
        this.loading = false
      }
    },
  async fetchUserCredit(userId) {
    try {
      const res = await api.get(`api/krediler/${userId}`)
      this.userCredits = {
        ...this.userCredits,
        [userId]: {
          kalanKredi: res.data?.kalanKredi || 0,
          sonSatinAlim: res.data?.sonSatinAlim || null
        }
      }
    } catch (err) {
      this.userCredits = {
        ...this.userCredits,
        [userId]: {
          kalanKredi: 0,
          sonSatinAlim: null
        }
      }
    }
  },
  openAddModal(user) {
    this.selectedUserId = user.id
    this.creditAmount = 0
    this.showAddModal = true
  },
  closeAddModal() {
    this.showAddModal = false
    this.selectedUserId = null
    this.creditAmount = 0
  },
  async addCredit() {
    if (!this.selectedUserId || !this.creditAmount || this.creditAmount <= 0) {
      alert('Lütfen geçerli bir kredi miktarı girin.')
      return
    }
    try {
      await api.post(`api/krediler/${this.selectedUserId}/add?amount=${this.creditAmount}`)
      await this.fetchUserCredit(this.selectedUserId)
      this.closeAddModal()
      alert('Kredi başarıyla eklendi.')
    } catch (err) {
      alert(err.response?.data?.error || 'Kredi eklenirken hata oluştu.')
    }
  },
  formatDate(dateStr) {
    if (!dateStr) return '-'
    const d = new Date(dateStr)
    return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
  },
  getCreditInfo(userId) {
    return this.userCredits[userId] || { kalanKredi: 0, sonSatinAlim: null }
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
        <h2>Kredi Yönetimi</h2>
      </div>

      <!-- Arama -->
      <div class="card mb-3">
        <div class="card-body">
          <input type="text" class="form-control" v-model="searchQuery"
            placeholder="Kullanıcı adı veya email ile ara..." />
        </div>
      </div>

      <!-- Kullanıcı Kredi Listesi -->
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

          <div v-else class="table-responsive">
            <table class="table table-hover">
              <thead>
                <tr>
                  <th>Kullanıcı Adı</th>
                  <th>Email</th>
                  <th>Kalan Kredi</th>
                  <th>Son Satın Alım</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="user in filteredUsers" :key="user.id">
                  <td>{{ user.ad || '-' }}</td>
                  <td>{{ user.email || '-' }}</td>
                  <td>
                    <span :class="{
                      'badge bg-success': getCreditInfo(user.id).kalanKredi > 0,
                      'badge bg-danger': getCreditInfo(user.id).kalanKredi === 0
                    }">
                      {{ getCreditInfo(user.id).kalanKredi }}
                    </span>
                  </td>
                  <td>{{ formatDate(getCreditInfo(user.id).sonSatinAlim) }}</td>
                  <td>
                    <button class="btn btn-sm btn-primary" @click="openAddModal(user)">
                      <span class="material-icons me-1" style="font-size: 18px; vertical-align: middle;">add</span>
                      Kredi Ekle
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Kredi Ekle Modal -->
      <div v-if="showAddModal" class="modal fade show" style="display: block;" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Kredi Ekle</h5>
              <button type="button" class="btn-close" @click="closeAddModal"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Kullanıcı</label>
                <input type="text" class="form-control"
                  :value="users.find(u => u.id === selectedUserId)?.ad + ' (' + users.find(u => u.id === selectedUserId)?.email + ')'"
                  disabled />
              </div>
              <div class="mb-3">
                <label class="form-label">Mevcut Kredi</label>
                <input type="text" class="form-control" :value="getCreditInfo(selectedUserId).kalanKredi" disabled />
              </div>
              <div class="mb-3">
                <label class="form-label">Eklenecek Kredi Miktarı *</label>
                <input type="number" class="form-control" v-model.number="creditAmount" min="1" required />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeAddModal">İptal</button>
              <button type="button" class="btn btn-primary" @click="addCredit">Kredi Ekle</button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="showAddModal" class="modal-backdrop fade show"></div>
    </div>
  </div>
</template>

<style scoped>
.table-responsive {
  max-height: 600px;
  overflow-y: auto;
}
</style>
