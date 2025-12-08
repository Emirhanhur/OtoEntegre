import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

import CoreuiVue from '@coreui/vue'
import CIcon from '@coreui/icons-vue'
import { iconsSet as icons } from '@/assets/icons'
import DocsComponents from '@/components/DocsComponents'
import DocsIcons from '@/components/DocsIcons'

import '@coreui/coreui/dist/css/coreui.min.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'

// ✅ EKLE ----
import * as bootstrap from 'bootstrap'
window.bootstrap = bootstrap
// ✅ ----------

import './assets/main.css'

// Mitt Event Bus
import mitt from 'mitt'
const emitter = mitt()

const app = createApp(App)
app.config.globalProperties.emitter = emitter

export { emitter }

app.use(createPinia())
app.use(router)
app.use(CoreuiVue)
app.provide('icons', icons)
app.component('CIcon', CIcon)
app.component('DocsComponents', DocsComponents)
app.component('DocsIcons', DocsIcons)


// Session timeout: auto-logout after 1 hour of inactivity
const INACTIVITY_TIMEOUT = 60 * 60 * 1000; // 1 hour in milliseconds
let inactivityTimer;

const resetInactivityTimer = () => {
	// Clear existing timer
	if (inactivityTimer) {
		clearTimeout(inactivityTimer);
	}

	// Only set timer if user is logged in
	const token = localStorage.getItem('token');
	if (!token) return;

	// Set new timer
	inactivityTimer = setTimeout(() => {
		// Logout the user
		localStorage.removeItem('token');
		localStorage.removeItem('rol');
		localStorage.removeItem('userId');
		localStorage.removeItem('bayi_id');
		window.location.href = '/login'; // Redirect to login
	}, INACTIVITY_TIMEOUT);
};

// Activity events that reset the inactivity timer
const activityEvents = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click'];

activityEvents.forEach(event => {
	document.addEventListener(event, resetInactivityTimer, true);
});

// Initialize timer on app load
resetInactivityTimer();

app.mount('#app')
