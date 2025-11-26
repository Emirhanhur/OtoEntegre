<template>
  <CDropdown variant="nav-item" placement="bottom-end">
    <CDropdownToggle :caret="false">
      <span class="material-icons">account_circle</span>
    </CDropdownToggle>
    <CDropdownMenu>
      <CDropdownHeader>Hesap</CDropdownHeader>
      <div class="px-3 pb-2">
        <div class="small text-muted">Kalan Kredi :{{ kalanKredi }}</div>
        
      </div>
      <CDropdownItem component="button" @click="goToProfile">
        <span class="material-icons me-2 align-middle">person</span> Profil
      </CDropdownItem>
      <CDropdownItem component="button">
        <span class="material-icons me-2 align-middle">settings</span> Ayarlar
      </CDropdownItem>
      <CDropdownDivider />
      <CDropdownItem component="button" @click="logout">
        <span class="material-icons me-2 align-middle">lock</span> Çıkış
      </CDropdownItem>
    </CDropdownMenu>
  </CDropdown>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import api from '../views/axios'

const router = useRouter()
const kalanKredi = ref(0)
const krediLoading = ref(true)
const avatarUrl = '/img/default-avatar.png'
let poller = null

async function fetchKredi() {
  const kullaniciId = localStorage.getItem('kullanici_id')
  if (!kullaniciId) {
    krediLoading.value = false
    kalanKredi.value = 0
    return
  }
  try {
    krediLoading.value = true
    const res = await api.get(`api/krediler/${kullaniciId}`)
    kalanKredi.value = res.data?.kalanKredi ?? res.data?.KalanKredi ?? 0
  } catch (err) {
    console.error('Kredi alınamadı', err)
    kalanKredi.value = 0
  } finally {
    krediLoading.value = false
  }
}

function logout() {
  localStorage.clear()
  router.push('/pages/login')
}
function goToProfile() {
  router.push('/profil')
}

onMounted(() => {
  fetchKredi()
  // Poll every 30s to keep the credit in sync
  poller = setInterval(fetchKredi, 30000)
  // Listen for changes to kullanici_id in localStorage via storage event
  window.addEventListener('storage', (e) => {
    if (e.key === 'kullanici_id') fetchKredi()
  })
})

onBeforeUnmount(() => {
  if (poller) clearInterval(poller)
})
</script>
