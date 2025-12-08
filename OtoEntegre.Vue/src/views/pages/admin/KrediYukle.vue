<script>
import api from '@/views/axios'
import { useKrediStore } from '@/utils/format'

export default {
  name: 'KrediYukle',
  setup() {
    const krediStore = useKrediStore()
    return { krediStore }
  },
  data() {
    return {
      users: [],
      selectedUserId: '',
      userSearch: '',
      dropdownVisible: false,
      amount: 1,
      loading: false,
      success: false,
      error: null,
    }
  },
  computed: {
    isAdmin() {
      return localStorage.getItem('rol') === 'Admin'
    },
    filteredUsers() {
      const search = this.userSearch.toLowerCase()
      return this.users.filter(u =>
        u.ad.toLowerCase().includes(search) || u.email.toLowerCase().includes(search)
      )
    }
  },
  async mounted() {
    if (!this.isAdmin) return
    try {
      const res = await api.get('api/users')
      this.users = res.data || []
    } catch (err) {
      console.error(err)
      this.error = 'Kullanıcılar yüklenemedi.'
    }
  },
  methods: {
    selectUser(user) {
      this.selectedUserId = user.id
      this.userSearch = `${user.ad} (${user.email})`
      this.dropdownVisible = false
    },
    handleBlur() {
      // küçük gecikme ile seçimi yakala
      setTimeout(() => this.dropdownVisible = false, 200)
    },
    async submit() {
      if (!this.selectedUserId || !this.amount) return
      this.loading = true
      this.success = false
      this.error = null
      try {
        await api.post(`api/krediler/${this.selectedUserId}/add?amount=${this.amount}`)
        this.success = true

        // Eğer yüklenen kullanıcı mevcut kullanıcıysa, kredi bilgisini güncelle
        const currentUserId = localStorage.getItem('kullanici_id')
        if (currentUserId && this.selectedUserId === currentUserId) {
          await this.krediStore.fetchKredi()
        }
      } catch (err) {
        console.error(err)
        this.error = err.response?.data?.error || 'Kredi eklenirken hata oluştu.'
      } finally {
        this.loading = false
      }
    },
    reset() {
      this.selectedUserId = ''
      this.userSearch = ''
      this.amount = 1
      this.success = false
      this.error = null
    }
  }
}
</script>

<style scoped>
.gap-2 {
  gap: .5rem;
}

.position-relative {
  position: relative;
}

.zindex-dropdown {
  z-index: 1000;
}

.list-group-item {
  cursor: pointer;
}

.list-group-item:hover {
  background-color: #f0f0f0;
}
</style>

<template>
  <div class="container py-4">
    <div class="card">
      <div class="card-header">
        <h4>Kredi Yükle (Admin)</h4>
      </div>
      <div class="card-body">
        <div v-if="!isAdmin" class="alert alert-danger">Bu sayfaya sadece Admin rolündeki kullanıcılar erişebilir.</div>

        <div v-else>
          <!-- Kullanıcı seç / autocomplete -->
          <div class="mb-3 position-relative">
            <label class="form-label">Kullanıcı seç</label>
            <input type="text" class="form-control" v-model="userSearch" placeholder="Ad veya e-posta ile ara"
              @focus="dropdownVisible = true" @blur="handleBlur" />
            <ul v-if="dropdownVisible && filteredUsers.length"
              class="list-group position-absolute w-100 zindex-dropdown" style="max-height: 200px; overflow-y: auto;">
              <li v-for="u in filteredUsers" :key="u.id" class="list-group-item list-group-item-action"
                @mousedown.prevent="selectUser(u)">
                {{ u.ad }} ({{ u.email }})
              </li>
            </ul>
          </div>

          <div class="mb-3">
            <label class="form-label">Kredi miktarı</label>
            <input type="number" class="form-control" v-model.number="amount" min="1" />
          </div>

          <div class="d-flex gap-2">
            <button class="btn btn-primary" :disabled="loading || !selectedUserId || !amount" @click="submit">Kredi
              Ekle</button>
            <button class="btn btn-secondary" @click="reset">Sıfırla</button>
          </div>

          <div v-if="success" class="alert alert-success mt-3">Kredi başarıyla eklendi.</div>
          <div v-if="error" class="alert alert-danger mt-3">{{ error }}</div>
        </div>
      </div>
    </div>
  </div>
</template>
