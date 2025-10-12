<template>
  <CDropdown variant="nav-item" placement="bottom-end">
    <CDropdownToggle :caret="false">
      <i class="bi bi-gear"></i>
    </CDropdownToggle>
    <CDropdownMenu>
      <CDropdownHeader>Hesap</CDropdownHeader>
      <CDropdownItem component="button" @click="goToProfile">
        <i class="cil-user me-2"></i> Profil
      </CDropdownItem>
      <CDropdownItem component="button">
        <i class="cil-settings me-2"></i> Ayarlar
      </CDropdownItem>
      <CDropdownDivider />
      <CDropdownItem component="button" @click="logout">
        <i class="cil-lock-locked me-2"></i> Çıkış
      </CDropdownItem>
    </CDropdownMenu>
  </CDropdown>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()
function logout() {
  localStorage.clear()
  router.push('/login')
}
function goToProfile() {
  router.push('/profil')
}
</script>
