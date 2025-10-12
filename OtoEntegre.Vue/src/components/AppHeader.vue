<script>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { useColorModes } from '@coreui/vue'
import AppBreadcrumb from '@/components/AppBreadcrumb.vue'
import AppHeaderDropdownAccnt from '@/components/AppHeaderDropdownAccnt.vue'
import { useSidebarStore } from '@/stores/sidebar.js'
import * as signalR from "@microsoft/signalr";
import { useRouter } from 'vue-router';
import { emitter } from '@/main'

export default {
  name: 'AppHeader',
  components: {
    AppBreadcrumb,
    AppHeaderDropdownAccnt
  },
  data() {
    return {
      headerClassNames: 'mb-4 p-0',
      notifications: [],
      showNotifications: false,
      pollerId: null,
      connection: null,
      colorMode: 'light',
      sidebar: useSidebarStore()
    }
  },
  methods: {
    toggleNotifications(event) {
      if (event && event.preventDefault) event.preventDefault();
      this.showNotifications = !this.showNotifications;
    },
    closeNotifications() {
      this.showNotifications = false;
    },
    formatDate(date) {
      return date instanceof Date && !isNaN(date)
        ? date.toLocaleString("tr-TR")
        : "Tarih yok";
    },
    goToOrder(orderId) {
      this.closeNotifications();
      emitter.emit('highlight-order', orderId);
      this.$router.push({ path: '/trendyol-entegrasyon' });
    },
    async initSignalR() {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5079/orderHub")
        .withAutomaticReconnect()
        .build();

      try {
        await this.connection.start();
        console.log("SignalR connected");

        this.connection.on("ReceiveOrderNotification", (order) => {
          order.date = order.date ? new Date(order.date) : new Date();
          order.type = order.type || 'info'; // info, success, warning, danger vb.

          // Sadece notifications array'ine ekle, otomatik açma yok
          this.notifications.push(order);

          // showNotifications = true satırını kaldırdık
          // this.showNotifications = true;
        });

      } catch (err) {
        console.error(err);
      }
    },
    setColorMode(mode) {
      const { setColorMode } = useColorModes('coreui-free-vue-admin-template-theme');
      setColorMode(mode);
      this.colorMode = mode;
    }
  },
  mounted() {
    this.initSignalR();
  },
  beforeUnmount() {
    if (this.pollerId) {
      clearInterval(this.pollerId);
      this.pollerId = null;
    }
  },
  watch: {
    notifications(newVal) {
      // Eğer özel bir şey yapmak istersen buraya ekleyebilirsin
      console.log("Yeni bildirim sayısı:", newVal.length);
    }
  }
}
</script>


<template>
  <CHeader position="sticky" :class="headerClassNames">
    <CContainer class="border-bottom px-4" fluid>
      <CHeaderToggler @click="sidebar.toggleVisible()" style="margin-inline-start: -14px">
        <CIcon icon="cil-menu" size="lg" />
      </CHeaderToggler>
      <CHeaderNav class="d-none d-md-flex">
        <CNavItem>
          <CNavLink href="/anasayfa"> Anasayfa </CNavLink>
        </CNavItem>
      </CHeaderNav>
      <CHeaderNav class="ms-auto">
        <CNavItem style="position: relative;">
          <CNavLink href="#" @click="toggleNotifications">
            <CIcon icon="cil-bell" size="lg" />
            <span v-if="notifications.length"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
              {{ notifications.length }}
            </span>
          </CNavLink>


          <div v-if="showNotifications" class="dropdown-menu dropdown-menu-end show p-2"
            style="position: absolute; right: 0; top: 100%; min-width:300px; max-width:350px; max-height:400px; overflow:auto;">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <span class="fw-bold">Bildirimler</span>
              <button class="btn btn-sm btn-link" @click="closeNotifications">Kapat</button>
            </div>
            <div v-if="!notifications.length" class="text-muted small">Yeni bildirim yok</div>
            <ul class="list-unstyled mb-0">
              <li v-for="n in notifications" :key="n.id" class="mb-2 border-bottom pb-2" :class="{
                'bg-light': n.type === 'info',
                'bg-success text-white': n.type === 'success',
                'bg-warning': n.type === 'warning',
                'bg-danger text-white': n.type === 'danger'
              }" @click="goToOrder(n.id)" style="cursor: pointer">
                <div class="fw-semibold">{{ n.message }}
                </div>
                <div class="text-muted small">{{ formatDate(n.date) }}</div>
              </li>

            </ul>

          </div>
        </CNavItem>
        <CNavItem>
          <CNavLink href="#">
            <CIcon icon="cil-list" size="lg" />
          </CNavLink>
        </CNavItem>
        <CNavItem>
          <CNavLink href="#">
            <CIcon icon="cil-envelope-open" size="lg" />
          </CNavLink>
        </CNavItem>
      </CHeaderNav>
      <CHeaderNav>
        <li class="nav-item py-1">
          <div class="vr h-100 mx-2 text-body text-opacity-75"></div>
        </li>
        <CDropdown variant="nav-item" placement="bottom-end">
          <CDropdownToggle :caret="false">
            <CIcon v-if="colorMode === 'dark'" icon="cil-moon" size="lg" />
            <CIcon v-else-if="colorMode === 'light'" icon="cil-sun" size="lg" />
            <CIcon v-else icon="cil-contrast" size="lg" />
          </CDropdownToggle>
          <CDropdownMenu>
            <CDropdownItem :active="colorMode === 'light'" class="d-flex align-items-center" component="button"
              type="button" @click="setColorMode('light')">
              <CIcon class="me-2" icon="cil-sun" size="lg" /> Gündüz
            </CDropdownItem>
            <CDropdownItem :active="colorMode === 'dark'" class="d-flex align-items-center" component="button"
              type="button" @click="setColorMode('dark')">
              <CIcon class="me-2" icon="cil-moon" size="lg" /> Gece
            </CDropdownItem>
            <CDropdownItem :active="colorMode === 'auto'" class="d-flex align-items-center" component="button"
              type="button" @click="setColorMode('auto')">
              <CIcon class="me-2" icon="cil-contrast" size="lg" /> Otomatik
            </CDropdownItem>
          </CDropdownMenu>
        </CDropdown>
        <li class="nav-item py-1">
          <div class="vr h-100 mx-2 text-body text-opacity-75"></div>
        </li>
        <AppHeaderDropdownAccnt />
      </CHeaderNav>
    </CContainer>
    <CContainer class="px-4" fluid>
      <AppBreadcrumb />
    </CContainer>
  </CHeader>
</template>
