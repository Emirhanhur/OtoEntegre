<script>
import api from '@/views/axios'

export default {
  name: 'AdminKullanicilar',
  data() {
    return {
      users: [],
      loading: true,
      error: null,
      searchQuery: '',
          statusFilter: 'all',
      showAddModal: false,
      showEditModal: false,
      showDeleteModal: false,
      selectedUser: null,
      newUser: {
        ad: '',
        email: '',
        password: '',
        telefon: '',
        sehir: '',
        ilce: '',
        adres: '',
        rolId: ''
      },
      editUser: {
        id: null,
        ad: '',
        email: '',
        telefon: '',
        sehir: '',
        ilce: '',
        adres: ''
      },
      roles: [] // Roller burada tutulacak
    }
  },
  computed: {
    isAdmin() {
      return localStorage.getItem('rol') === 'Admin'
    },
    filteredUsers() {
      // Start with base array and apply status filter
      let base = this.users || []
      if (this.statusFilter === 'active') {
        base = base.filter(u => !u.deleted)
      } else if (this.statusFilter === 'passive') {
        base = base.filter(u => u.deleted)
      }

      if (!this.searchQuery) return base
      const query = this.searchQuery.toLowerCase()
      return base.filter(u =>
        (u.ad || '').toLowerCase().includes(query) ||
        (u.email || '').toLowerCase().includes(query) ||
        (u.telefon || '').toLowerCase().includes(query)
      )
    }
  },
  async mounted() {
    if (!this.isAdmin) {
      this.$router.push('/anasayfa')
      return
    }
    await this.fetchUsers()
    // Rolleri çek
    try {
      const res = await api.get('api/roller')
      this.roles = res.data || []
    } catch (err) {
      this.roles = []
      // alert('Roller yüklenemedi!')
    }
  },
  methods: {
    async fetchUsers() {
      this.loading = true
      this.error = null
      try {
        // API'den kullanıcı listesini çekerken 'deleted' bilgisinin geldiğinden emin olun.
        const res = await api.get('api/users')
        this.users = res.data || []
      } catch (err) {
        console.error('Kullanıcılar yüklenemedi:', err)
        this.error = 'Kullanıcılar yüklenirken hata oluştu.'
      } finally {
        this.loading = false
      }
    },

    openAddModal() {
      this.newUser = {
        ad: '',
        email: '',
        password: '',
        telefon: '',
        sehir: '',
        ilce: '',
        adres: '',
        rolId: ''
      }
      this.showAddModal = true
    },
    closeAddModal() {
      this.showAddModal = false
    },
    async addUser() {
      try {
        if (!this.newUser.rolId) {
          alert('Lütfen bir rol seçiniz.');
          return;
        }
        const payload = {
          Ad: this.newUser.ad,
          Email: this.newUser.email,
          Sifre: this.newUser.password,
          Telefon: this.newUser.telefon,
          Sehir: this.newUser.sehir,
          Ilce: this.newUser.ilce,
          Adres: this.newUser.adres,
          RolId: this.newUser.rolId
        }
        await api.post('api/users', payload)
        this.closeAddModal()
        await this.fetchUsers()
        alert('Kullanıcı başarıyla eklendi.')
      } catch (err) {
        alert(err.response?.data?.message || 'Kullanıcı eklenirken hata oluştu.')
      }
    },
    openEditModal(user) {
      this.editUser = {
        id: user.id,
        ad: user.ad || '',
        email: user.email || '',
        telefon: user.telefon || '',
        sehir: user.sehir || '',
        ilce: user.ilce || '',
        adres: user.adres || ''
      }
      this.showEditModal = true
    },
    closeEditModal() {
      this.showEditModal = false
    },
    async updateUser() {
      try {
        await api.put(`api/users/${this.editUser.id}`, this.editUser)
        this.closeEditModal()
        await this.fetchUsers()
        alert('Kullanıcı başarıyla güncellendi.')
      } catch (err) {
        alert(err.response?.data?.message || 'Kullanıcı güncellenirken hata oluştu.')
      }
    },
    openDeleteModal(user) {
      this.selectedUser = user
      this.showDeleteModal = true
    },
    closeDeleteModal() {
      this.showDeleteModal = false
      this.selectedUser = null
    },
    async deleteUser() {
      if (!this.selectedUser) return
      try {
        // Bu çağrı, API'deki Soft Delete metodunu tetikler: user.Deleted = true;
        await api.delete(`api/users/${this.selectedUser.id}`)
        this.closeDeleteModal()
        await this.fetchUsers()
        alert('Kullanıcı başarıyla pasifleştirildi (Soft Delete).')
      } catch (err) {
        alert(err.response?.data?.message || 'Kullanıcı pasifleştirilirken hata oluştu.')
      }
    },
    async restoreUser(user) {
      try {
        await api.post(`api/users/restore/${user.id}`);
        await this.fetchUsers();
        alert('Kullanıcı tekrar aktifleştirildi.');
      } catch (err) {
        alert(err.response?.data?.message || 'Kullanıcı aktifleştirilirken hata oluştu.');
      }
    },
    formatDate(dateStr) {
      if (!dateStr) return '-'
      const d = new Date(dateStr)
      return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
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
        <h2>Kullanıcı Yönetimi</h2>
        <button class="btn btn-primary" @click="openAddModal">
          <span class="material-icons me-2" style="vertical-align: middle; font-size: 18px;">add</span>
          Yeni Kullanıcı
        </button>
      </div>

      <!-- Arama -->
      <div class="card mb-3">
        <div class="card-body">
          <div class="d-flex gap-2 flex-column flex-md-row">
            <input type="text" class="form-control" v-model="searchQuery"
              placeholder="Kullanıcı adı, email veya telefon ile ara..." />

            <select class="form-select" v-model="statusFilter" style="max-width: 220px;">
              <option value="all">Tümü</option>
              <option value="active">AKTİF</option>
              <option value="passive">PASİF</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Kullanıcı Listesi -->
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
                  <th>Ad</th>
                  <th>Email</th>
                  <th>Telefon</th>
                  <th>Şehir</th>
                  <th>İlçe</th>
                  <th>Oluşturulma</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="user in filteredUsers" :key="user.id">
                  <td>{{ user.ad || '-' }}</td>
                  <td>{{ user.email || '-' }}</td>
                  <td>{{ user.telefon || '-' }}</td>
                  <td>{{ user.sehir || '-' }}</td>
                  <td>{{ user.ilce || '-' }}</td>
                  <td>{{ formatDate(user.created_At) }}</td>
                  <td>
                    <span :class="['badge', user.deleted ? 'bg-danger' : 'bg-success']">
                      {{ user.deleted ? 'PASİF' : 'AKTİF' }}
                    </span>
                  </td>
                  <td>
                    <button  class="btn btn-sm btn-outline-primary me-2"
                      @click="openEditModal(user)">
                      <span class="material-icons" style="font-size: 18px;">edit</span>
                    </button>

                    <!-- PASİF kullanıcı: AKTİF ET butonu -->
                    <button v-if="user.deleted" class="btn btn-sm btn-outline-success me-2" @click="restoreUser(user)">
                      <span class="material-icons" style="font-size: 18px;">restore</span>
                    </button>

                    <!-- Sadece aktif kullanıcılar için PASİF ET -->
                    <button v-if="!user.deleted" class="btn btn-sm btn-outline-danger" @click="openDeleteModal(user)">
                      <span class="material-icons" style="font-size: 18px;">delete</span>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Yeni Kullanıcı Modal -->
      <div v-if="showAddModal" class="modal fade show" style="display: block;" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Yeni Kullanıcı Ekle</h5>
              <button type="button" class="btn-close" @click="closeAddModal"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Ad *</label>
                <input type="text" class="form-control" v-model="newUser.ad" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Email *</label>
                <input type="email" class="form-control" v-model="newUser.email" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Şifre *</label>
                <input type="password" class="form-control" v-model="newUser.password" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Rol *</label>
                <select v-model="newUser.rolId" class="form-control" required>
                  <option value="" disabled>Rol Seçiniz</option>
                  <option v-for="role in roles" :key="role.id" :value="role.id">{{ role.ad }}</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Telefon</label>
                <input type="text" class="form-control" v-model="newUser.telefon" />
              </div>
              <div class="mb-3">
                <label class="form-label">Şehir</label>
                <input type="text" class="form-control" v-model="newUser.sehir" />
              </div>
              <div class="mb-3">
                <label class="form-label">İlçe</label>
                <input type="text" class="form-control" v-model="newUser.ilce" />
              </div>
              <div class="mb-3">
                <label class="form-label">Adres</label>
                <textarea class="form-control" v-model="newUser.adres" rows="3"></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeAddModal">İptal</button>
              <button type="button" class="btn btn-primary" @click="addUser">Kaydet</button>
            </div>
          </div>
        </div>
      </div>
      <div v-if="showAddModal" class="modal-backdrop fade show"></div>

      <!-- Düzenle Modal -->
      <div v-if="showEditModal" class="modal fade show" style="display: block;" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Kullanıcı Düzenle</h5>
              <button type="button" class="btn-close" @click="closeEditModal"></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Ad *</label>
                <input type="text" class="form-control" v-model="editUser.ad" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Email *</label>
                <input type="email" class="form-control" v-model="editUser.email" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Telefon</label>
                <input type="text" class="form-control" v-model="editUser.telefon" />
              </div>
              <div class="mb-3">
                <label class="form-label">Şehir</label>
                <input type="text" class="form-control" v-model="editUser.sehir" />
              </div>
              <div class="mb-3">
                <label class="form-label">İlçe</label>
                <input type="text" class="form-control" v-model="editUser.ilce" />
              </div>
              <div class="mb-3">
                <label class="form-label">Adres</label>
                <textarea class="form-control" v-model="editUser.adres" rows="3"></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeEditModal">İptal</button>
              <button type="button" class="btn btn-primary" @click="updateUser">Güncelle</button>
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
              <h5 class="modal-title">Kullanıcı Pasifleştir</h5>
              <button type="button" class="btn-close" @click="closeDeleteModal"></button>
            </div>
            <div class="modal-body">
              <p>Bu kullanıcıyı pasife almak istediğinizden emin misiniz?</p>
              <p><strong>{{ selectedUser?.ad }} ({{ selectedUser?.email }})</strong></p>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeDeleteModal">İptal</button>
              <button type="button" class="btn btn-danger" @click="deleteUser">Sil</button>
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
