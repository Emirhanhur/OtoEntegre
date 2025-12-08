<script>
import api from '@/views/axios'

export default {
  name: 'AdminEntegrasyonlar',
  data() {
    return {
      users: [],
          platforms: [],
      integrations: {},
      loading: true,
      error: null,
      searchQuery: '',
      showEditModal: false,
      showDeleteModal: false,
      selectedIntegration: null,
      editIntegration: {
        id: null,
        kullaniciId: null,
        platformId: null,
        apiKey: '',
        apiSecret: '',
        sellerId: null
      }
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
    await this.fetchPlatforms()
    await this.fetchUsersWithIntegrations()
  },
  methods: {
    async fetchPlatforms() {
      try {
        const res = await api.get('api/platformlar')
        this.platforms = res.data || []
      } catch (err) {
        console.error('Platformlar yüklenemedi', err)
        this.platforms = []
      }
    },
    async fetchUsersWithIntegrations() {
      this.loading = true
      this.error = null
      try {
        const res = await api.get('api/entegrasyonlar/with-users')
        const list = res.data || []

        // map users and integrations
        this.users = list.map(x => ({ id: x.kullaniciId, ad: x.ad, email: x.email }))
        const map = {}
        list.forEach(x => {
          map[x.kullaniciId] = x.entegrasyon || null
        })
        this.integrations = map
      } catch (err) {
        console.error('Entegrasyonlar yüklenemedi:', err)
        this.error = 'Entegrasyon bilgileri yüklenirken hata oluştu.'
      } finally {
        this.loading = false
      }
    },
    async fetchAllIntegrations() {
      for (const user of this.users) {
        try {
          const res = await api.get(`api/entegrasyonlar/by-user/${user.id}`)
          console.log('Entegrasyon yüklendi for user', user.id, res.data)
          this.integrations = {
            ...this.integrations,
            [user.id]: res.data
          }
        } catch (err) {
          // Entegrasyon yoksa null olarak ayarla
          this.integrations = {
            ...this.integrations,
            [user.id]: null
          }
        }
      }
    },
    async fetchUserIntegration(userId) {
      try {
        const res = await api.get(`api/entegrasyonlar/by-user/${userId}`)
        this.integrations = {
          ...this.integrations,
          [userId]: res.data
        }
      } catch (err) {
        this.integrations = {
          ...this.integrations,
          [userId]: null
        }
      }
    },
    openEditModal(integration, user) {
      this.selectedIntegration = integration
      const userId = (integration && integration.kullaniciId) || (user && user.id) || null
      this.editIntegration = {
        id: (integration && integration.id) || null,
        kullaniciId: userId,
        platformId: (integration && integration.platformId) || null,
        apiKey: (integration && integration.apiKey) || '',
        apiSecret: (integration && integration.apiSecret) || '',
        sellerId: (integration && integration.sellerId) || null
      }
      this.showEditModal = true
    },
    closeEditModal() {
      this.showEditModal = false
      this.selectedIntegration = null
    },
    async updateIntegration() {
      try {
        const payload = {
          kullanici_Id: this.editIntegration.kullaniciId,
          platform_Id: this.editIntegration.platformId,
          api_Key: this.editIntegration.apiKey,
          api_Secret: this.editIntegration.apiSecret,
          seller_Id: this.editIntegration.sellerId,
          kullanici_Adi: this.users.find(u => u.id === this.editIntegration.kullaniciId)?.ad || ''
        }

        // Only include secret if user provided a new one (avoid showing/storing encrypted secret)
        

        if (this.editIntegration.id) {
          console.log('Güncelleniyor entegrasyon', this.editIntegration.id, payload)
          await api.put(`api/entegrasyonlar/${this.editIntegration.id}`, payload)
          await this.fetchUserIntegration(this.editIntegration.kullaniciId)
          this.closeEditModal()
          alert('Entegrasyon başarıyla güncellendi.')
        } else {
          await api.post('api/entegrasyonlar', payload)
          // refresh the just-created integration for the user
          await this.fetchUserIntegration(this.editIntegration.kullaniciId)
          this.closeEditModal()
          alert('Entegrasyon başarıyla eklendi.')
        }
      } catch (err) {
        alert(err.response?.data?.message || 'Entegrasyon kaydedilirken hata oluştu.')
      }
    },
    openDeleteModal(integration) {
      this.selectedIntegration = integration
      this.showDeleteModal = true
    },
    closeDeleteModal() {
      this.showDeleteModal = false
      this.selectedIntegration = null
    },
    async deleteIntegration() {
      if (!this.selectedIntegration) return
      try {
        await api.delete(`api/entegrasyonlar/${this.selectedIntegration.id}`)
        await this.fetchUserIntegration(this.selectedIntegration.kullaniciId)
        this.closeDeleteModal()
        alert('Entegrasyon başarıyla silindi.')
      } catch (err) {
        alert(err.response?.data?.message || 'Entegrasyon silinirken hata oluştu.')
      }
    },
    maskSecret(secret) {
      if (!secret) return '-'
      if (secret.length <= 4) return '****'
      return secret.substring(0, 4) + '****' + secret.substring(secret.length - 4)
    },
    getIntegration(userId) {
      return this.integrations[userId] || null
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
        <h2>Entegrasyon Yönetimi</h2>
      </div>

      <!-- Arama -->
      <div class="card mb-3">
        <div class="card-body">
          <input
            type="text"
            class="form-control"
            v-model="searchQuery"
            placeholder="Kullanıcı adı veya email ile ara..."
          />
        </div>
      </div>

      <!-- Entegrasyon Listesi -->
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
                  <th>Kullanıcı</th>
                  <th>Email</th>
                  <th>Platform</th>
                  <th>API Key</th>
                  <th>API Secret</th>
                  <th>Seller ID</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="user in filteredUsers" :key="user.id">
                  <td>{{ user.ad || '-' }}</td>
                  <td>{{ user.email || '-' }}</td>
                  <td>
                    <span v-if="getIntegration(user.id)" class="badge bg-success">
                      {{ getIntegration(user.id).platformAdi || 'Trendyol' }}
                    </span>
                    <span v-else class="badge bg-secondary">Yok</span>
                  </td>
                  <td>

                    <span v-if="getIntegration(user.id)">
                      {{ getIntegration(user.id).apiKey ? maskSecret(getIntegration(user.id).apiKey) : '-' }}
                    </span>
                    <span v-else>-</span>
                  </td>
                  <td>
                    <span v-if="getIntegration(user.id)">
                      {{ getIntegration(user.id).apiSecret ? maskSecret(getIntegration(user.id).apiSecret) : '-' }}
                    </span>
                    <span v-else>-</span>
                  </td>
                  <td>
                    <span v-if="getIntegration(user.id)">
                      {{ getIntegration(user.id).sellerId || '-' }}
                    </span>
                    <span v-else>-</span>
                  </td>
                  <td>
                    <button
                      v-if="getIntegration(user.id)"
                      class="btn btn-sm btn-outline-primary me-2"
                      @click="openEditModal(getIntegration(user.id), user)"
                    >
                      <span class="material-icons" style="font-size: 18px;">edit</span>
                    </button>
                    <button
                      v-if="getIntegration(user.id)"
                      class="btn btn-sm btn-outline-danger"
                      @click="openDeleteModal(getIntegration(user.id))"
                    >
                      <span class="material-icons" style="font-size: 18px;">delete</span>
                    </button>

                    <!-- If the user has no integration, allow adding one -->
                    <button
                      v-else
                      class="btn btn-sm btn-outline-success"
                      @click="openEditModal(null, user)"
                      title="Entegrasyon ekle"
                    >
                      <span class="material-icons" style="font-size: 18px;">add</span>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Düzenle Modal -->
      <div v-if="showEditModal" class="modal fade show" style="display: block;" tabindex="-1">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Entegrasyon Düzenle</h5>
              <button type="button" class="btn-close" @click="closeEditModal"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Kullanıcı</label>
                <input
                  type="text"
                  class="form-control"
                  :value="users.find(u => u.id === editIntegration.kullaniciId)?.ad + ' (' + users.find(u => u.id === editIntegration.kullaniciId)?.email + ')'"
                  disabled
                />
              </div>
              <div class="mb-3">
                <label class="form-label">Platform</label>
                <select class="form-select" v-model="editIntegration.platformId">
                  <option :value="null">-- Platform seçin --</option>
                  <option v-for="p in platforms" :key="p.id" :value="p.id">{{ p.ad }}</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">API Key *</label>
                <input type="text" class="form-control" v-model="editIntegration.apiKey" required />
              </div>
              <div class="mb-3">
                <label class="form-label">API Secret *</label>
                <input
                  class="form-control"
                  v-model="editIntegration.apiSecret"
                />
              </div>
              <div class="mb-3">
                <label class="form-label">Seller ID</label>
                <input type="number" class="form-control" v-model.number="editIntegration.sellerId" />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeEditModal">İptal</button>
              <button type="button" class="btn btn-primary" @click="updateIntegration">
                {{ editIntegration.id ? 'Güncelle' : 'Kaydet' }}
              </button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="showEditModal" class="modal-backdrop fade show"></div>

      <!-- Silme Onay Modal -->
      <div v-if="showDeleteModal" class="modal fade show" style="display: block;" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Entegrasyon Sil</h5>
              <button type="button" class="btn-close" @click="closeDeleteModal"></button>
            </div>
            <div class="modal-body">
              <p>Bu entegrasyonu silmek istediğinizden emin misiniz?</p>
              <p class="text-danger">Bu işlem geri alınamaz!</p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeDeleteModal">İptal</button>
              <button type="button" class="btn btn-danger" @click="deleteIntegration">Sil</button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="showDeleteModal" class="modal-backdrop fade show"></div>
    </div>
  </div>
</template>

<style scoped>
.table-responsive {
  max-height: 600px;
  overflow-y: auto;
}
</style>

