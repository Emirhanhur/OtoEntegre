<template>
	<div class="container py-4">
		<div class="card">
			<div class="card-header">
				<h4>Kredi Yükle (Admin)</h4>
			</div>
			<div class="card-body">
				<div v-if="!isAdmin" class="alert alert-danger">Bu sayfaya sadece Admin rolündeki kullanıcılar erişebilir.</div>

				<div v-else>
					<div class="mb-3">
						<label class="form-label">Kullanıcı seç</label>
						<select class="form-select" v-model="selectedUserId">
							<option value="">-- Kullanıcı seçin --</option>
							<option v-for="u in users" :key="u.id" :value="u.id">{{ u.ad }} ({{ u.email }})</option>
						</select>
					</div>

					<div class="mb-3">
						<label class="form-label">Kredi miktarı</label>
						<input type="number" class="form-control" v-model.number="amount" min="1" />
					</div>

					<div class="d-flex gap-2">
						<button class="btn btn-primary" :disabled="loading || !selectedUserId || !amount" @click="submit">Kredi Ekle</button>
						<button class="btn btn-secondary" @click="reset">Sıfırla</button>
					</div>

					<div v-if="success" class="alert alert-success mt-3">Kredi başarıyla eklendi.</div>
					<div v-if="error" class="alert alert-danger mt-3">{{ error }}</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script>
import api from '@/views/axios'

export default {
	name: 'KrediYukle',
	data() {
		return {
			users: [],
			selectedUserId: '',
			amount: 1,
			loading: false,
			success: false,
			error: null,
		}
	},
	computed: {
		isAdmin() {
			const rol = localStorage.getItem('rol')
			return rol === 'Admin'
		},
	},
	async mounted() {
		if (!this.isAdmin) return
		try {
			const res = await api.get('api/users')
			// Expect API to return array of users
			this.users = res.data || []
		} catch (err) {
			console.error(err)
			this.error = 'Kullanıcılar yüklenemedi.'
		}
	},
	methods: {
		async submit() {
			if (!this.selectedUserId || !this.amount) return
			this.loading = true
			this.success = false
			this.error = null
			try {
				await api.post(`api/krediler/${this.selectedUserId}/add?amount=${this.amount}`)
				this.success = true
			} catch (err) {
				console.error(err)
				this.error = err.response?.data?.error || 'Kredi eklenirken hata oluştu.'
			} finally {
				this.loading = false
			}
		},
		reset() {
			this.selectedUserId = ''
			this.amount = 1
			this.success = false
			this.error = null
		}
	}
}
</script>

<style scoped>
.gap-2 { gap: .5rem; }
</style>
