<script>
import api from "../axios";

export default {
    name: 'Profil',
    data() {
        return {
            user: null,
            entegrasyonlar: [],
            editAd: '',
            editEmail: '',
            editTelefon: '',
            editEntegrasyonTelefon: '',
            oldPassword: '',
            newPassword: '',
            confirmNewPassword: '',
            savingPassword: false,
            passwordSuccess: false,
            passwordError: '',
            saving: false,
            saveSuccess: false,
            saveError: '',

            showOldPassword: false,
            showNewPassword: false,
            showConfirmPassword: false,
            changingPassword: false,
            changeSuccess: false,
            changeError: ''
            // 🔑 Şifre değiştirme alanları
        };
    },
    async mounted() {
        const kullaniciId = localStorage.getItem('kullanici_id');
        if (!kullaniciId) return;
        try {
            const userRes = await api.get(`api/users/${kullaniciId}`);
            this.user = userRes.data;
            this.editAd = userRes.data.ad || '';
            this.editEmail = userRes.data.email || '';
            this.editTelefon = userRes.data.telefon || '';
            this.editEntegrasyonTelefon = userRes.data.entegrasyon_Telefon || '';
        } catch (e) {
            this.user = null;
        }
        try {
            const entRes = await api.get(`api/entegrasyonlar/by-user/${kullaniciId}`);
            this.entegrasyonlar = Array.isArray(entRes.data) ? entRes.data : (entRes.data ? [entRes.data] : []);
        } catch (e) {
            this.entegrasyonlar = [];
        }
    },
    methods: {
        maskKey(value) {
            if (!value) return '';
            const visible = value.slice(0, 6);
            const masked = '*'.repeat(Math.max(value.length - 6, 0));
            return visible + masked;
        },
        async updateUser() {
            this.saving = true;
            this.saveSuccess = false;
            this.saveError = '';
            const kullaniciId = localStorage.getItem('kullanici_id');
            try {
                let rolId = this.user.roller?.[0]?.rolId;
                const payload = {
                    ad: this.editAd,
                    email: this.editEmail,
                    telefon: this.editTelefon,
                    entegrasyon_Telefon: this.editEntegrasyonTelefon,
                    telegramUseSamePhone: false,
                    ...(rolId && { rolId })
                };
                await api.put(`api/users/${kullaniciId}`, payload);
                this.user = { ...this.user, ...payload };
                this.saveSuccess = true;
            } catch (e) {
                console.error(e);
                this.saveError = 'Güncelleme başarısız.';
            } finally {
                this.saving = false;
                setTimeout(() => { this.saveSuccess = false; }, 2000);
            }
        },

        // 🔑 Yeni: Şifre değiştirme fonksiyonu
        async changePassword() {
            if (!this.oldPassword || !this.newPassword || !this.confirmNewPassword) {
                alert("Lütfen tüm alanları doldurun!");
                return;
            }

            if (this.newPassword !== this.confirmNewPassword) {
                alert("Yeni şifre ve tekrar şifre uyuşmuyor!");
                return;
            }

            this.changingPassword = true;
            this.changeSuccess = false;
            this.changeError = '';

            const kullaniciId = localStorage.getItem('kullanici_id');

            try {
                const res = await api.post(`/api/users/change-password/${kullaniciId}`, {
                    oldPassword: this.oldPassword,
                    newPassword: this.newPassword,
                    confirmPassword: this.confirmNewPassword
                });

                alert("Şifre başarıyla değiştirildi!");
                this.oldPassword = '';
                this.newPassword = '';
                this.confirmNewPassword = '';
                this.changeSuccess = true;
            } catch (err) {
                console.error(err);
                this.changeError = err.response?.data?.title || 'Şifre değiştirilemedi!';
                alert(this.changeError);
            } finally {
                this.changingPassword = false;
            }
        }


    }
};
</script>

<template>
    <div class="container py-4">
        <h2 class="text-center mb-4 fw-bold">Profilim</h2>

        <div v-if="user" class="row g-4">

            <!-- Kullanıcı Bilgileri -->
            <div class="col-md-6 d-flex">
                <div class="card shadow-sm w-100 h-100">
                    <div class="card-header bg-primary text-white">
                        <h5 class="mb-0">Kullanıcı Bilgileri</h5>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <label class="form-label"><b>Ad</b></label>
                            <input v-model="editAd" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label"><b>Email</b></label>
                            <input v-model="editEmail" type="email" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label"><b>Telefon</b></label>
                            <input v-model="editTelefon" class="form-control" />
                        </div>
                        <div class="mb-3">
                            <label class="form-label"><b>Entegrasyon Telefon</b></label>
                            <input v-model="editEntegrasyonTelefon" class="form-control"
                                placeholder="Telefon numarası girin" />
                            <small class="text-muted">
                                Sipariş ve Telegram entegrasyonunda kullanılacak telefon.
                            </small>
                        </div>
                        <button class="btn btn-success w-100" @click="updateUser" :disabled="saving">
                            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
                        </button>
                        <div v-if="saveSuccess" class="text-success mt-2">✅ Bilgiler güncellendi!</div>
                        <div v-if="saveError" class="text-danger mt-2">{{ saveError }}</div>
                    </div>
                </div>
            </div>

            <!-- 🔑 Şifre Değiştirme Alanı -->
            <!-- Şifre Değiştirme -->
            <div class="col-md-6 d-flex mt-4">
                <div class="card shadow-sm w-100">
                    <div class="card-header bg-warning text-white">
                        <h5 class="mb-0">Şifre Değiştir</h5>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <label class="form-label">Eski Şifre</label>
                            <div class="input-group">
                                <input :type="showOldPassword ? 'text' : 'password'" v-model="oldPassword"
                                    class="form-control" />
                                <button class="btn btn-outline-secondary" type="button"
                                    @click="showOldPassword = !showOldPassword">
                                    <i :class="showOldPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                </button>
                            </div>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Yeni Şifre</label>
                            <div class="input-group">
                                <input :type="showNewPassword ? 'text' : 'password'" v-model="newPassword"
                                    class="form-control" />
                                <button class="btn btn-outline-secondary" type="button"
                                    @click="showNewPassword = !showNewPassword">
                                    <i :class="showNewPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                </button>
                            </div>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Yeni Şifre (Tekrar)</label>
                            <div class="input-group">
                                <input :type="showConfirmPassword ? 'text' : 'password'" v-model="confirmNewPassword"
                                    class="form-control" />
                                <button class="btn btn-outline-secondary" type="button"
                                    @click="showConfirmPassword = !showConfirmPassword">
                                    <i :class="showConfirmPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
                                </button>
                            </div>
                        </div>

                        <button class="btn btn-warning w-100" @click="changePassword" :disabled="savingPassword">
                            {{ savingPassword ? 'Kaydediliyor...' : 'Şifreyi Güncelle' }}
                        </button>
                        <div v-if="passwordSuccess" class="text-success mt-2">✅ Şifre başarıyla güncellendi!</div>
                        <div v-if="passwordError" class="text-danger mt-2">{{ passwordError }}</div>
                    </div>
                </div>
            </div>


            <!-- Entegrasyonlar -->
            <div class="col-12">
                <div class="card shadow-sm w-100 h-100">
                    <div class="card-header bg-secondary text-white">
                        <h5 class="mb-0">Entegrasyonlarım</h5>
                    </div>
                    <div class="card-body">
                        <div v-if="entegrasyonlar && entegrasyonlar.length">
                            <ul class="list-group list-group-flush">
                                <li v-for="ent in entegrasyonlar" :key="ent.id" class="list-group-item">
                                    <h6 class="fw-bold mb-2">{{ ent.platformAdi || ent.platform_Adi || 'Platform' }}
                                    </h6>
                                    <p class="mb-1"><b>API Key:</b> <code>{{ maskKey(ent.api_Key) }}</code></p>
                                    <p class="mb-1"><b>API Secret:</b> <code>{{ maskKey(ent.api_Secret) }}</code></p>
                                    <p class="mb-0"><b>Satıcı ID:</b> {{ ent.seller_Id }}</p>
                                </li>
                            </ul>
                        </div>
                        <div v-else class="alert alert-info text-center">
                            Entegrasyon bulunamadı.
                        </div>
                    </div>
                </div>
            </div>

        </div>

        <div v-else class="alert alert-warning text-center shadow-sm">
            Kullanıcı bilgisi yükleniyor...
        </div>
    </div>
</template>

<style scoped>
.card {
    border-radius: 0.75rem;
}
</style>
