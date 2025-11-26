<script>
import api from "../../axios";
import { jwtDecode } from "jwt-decode";

export default {
  data() {
    return {
      email: "",
      password: "",
      error: "",
      loading: false,
      showPassword: false,
      
      // Reset password modal
      showResetModal: false,
      resetEmail: "",
      newPassword: "",
      resetMessage: "",
      resetSuccess: false,
      resetLoading: false,
    };
  },
  methods: {
    async login() {
      this.error = "";
      this.loading = true;
      try {
        const response = await api.post("api/auth/login", {
          email: this.email,
          password: this.password,
        });

        const token = response.data.token;
        localStorage.setItem("token", token);
        localStorage.setItem("bayi_id", response.data.user.tedarik_Kullanici_Id);
        localStorage.setItem("kullanici_id", response.data.user.id);
        localStorage.setItem("telegram_chat", response.data.user.telegram_Chat);
        localStorage.setItem("telegram_token", response.data.user.telegram_Token);
        localStorage.setItem("bayi_email", response.data.user.email);

        const decoded = jwtDecode(token);

        let roles = decoded.role;
        if (!roles) {
          console.warn("JWT içinde rol bulunamadı!");
        } else if (Array.isArray(roles)) {
          roles = roles[0];
        }

        localStorage.setItem("rol", roles);

        this.$router.push("/anasayfa");
      } catch (err) {
        if (err.response && err.response.status === 401) {
          this.error = "Email veya şifre yanlış!";
        } else {
          this.error = "Sunucu hatası, lütfen tekrar deneyin.";
        }
      } finally {
        this.loading = false;
      }
    },

    // Reset password modal method
    async resetPassword() {
      this.resetMessage = "";
      this.resetLoading = true;
      try {
        const response = await api.post("api/auth/reset-password", {
          email: this.resetEmail,
          newPassword: this.newPassword
        });
        this.resetSuccess = true;
        this.resetMessage = response.data.message;
      } catch (err) {
        this.resetSuccess = false;
        this.resetMessage = err.response?.data?.message || "Sunucu hatası, tekrar deneyin.";
      } finally {
        this.resetLoading = false;
      }
    },

    openResetModal() {
      this.resetEmail = "";
      this.newPassword = "";
      this.resetMessage = "";
      this.resetSuccess = false;
      this.showResetModal = true;
    },

    closeResetModal() {
      this.showResetModal = false;
    }
  },
};
</script>

<template>
  <div class="d-flex justify-content-center align-items-center vh-100 bg-light">
    <div class="card p-4 shadow-sm" style="width: 400px;">
      <h3 class="card-title text-center mb-4">Giriş Yap</h3>

      <div v-if="error" class="alert alert-danger">{{ error }}</div>

      <form @submit.prevent="login">
        <div class="mb-3">
          <label for="email" class="form-label">Email</label>
          <input type="email" id="email" class="form-control" v-model="email" required />
        </div>

        <div class="mb-3">
          <label for="password" class="form-label">Şifre</label>
          <div class="input-group">
            <input :type="showPassword ? 'text' : 'password'" id="password" class="form-control"
                   v-model="password" required />
            <button class="btn btn-outline-secondary" type="button" @click="showPassword = !showPassword">
              <span v-if="showPassword" class="material-icons">visibility_off</span>
              <span v-else class="material-icons">visibility</span>
            </button>
          </div>
        </div>

        <button type="submit" class="btn btn-primary w-100" :disabled="loading">
          <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
          Giriş
        </button>
      </form>

      <p class="text-center mt-3 text-muted">
        Hesabınız yok mu?
        <router-link to="/pages/register">Kayıt Ol</router-link>
      </p>

      <p class="text-center mt-2 text-muted">
        <a href="#" @click.prevent="openResetModal">Şifrenizi mi unuttunuz?</a>
      </p>
    </div>
  </div>

  <!-- Reset Password Modal -->
  <div class="modal fade" tabindex="-1" :class="{ show: showResetModal }" style="display: block;" v-if="showResetModal">
    <div class="modal-dialog modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">Şifre Sıfırlama</h5>
          <button type="button" class="btn-close" @click="closeResetModal"></button>
        </div>
        <div class="modal-body">
          <div v-if="resetMessage" :class="{'alert alert-success': resetSuccess, 'alert alert-danger': !resetSuccess}">
            {{ resetMessage }}
          </div>
          <div class="mb-3">
            <label>Email</label>
            <input type="email" class="form-control" v-model="resetEmail" required />
          </div>
          <div class="mb-3">
            <label>Yeni Şifre</label>
            <input type="password" class="form-control" v-model="newPassword" required />
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="closeResetModal">Kapat</button>
          <button type="button" class="btn btn-primary" @click="resetPassword" :disabled="resetLoading">
            <span v-if="resetLoading" class="spinner-border spinner-border-sm me-2"></span>
            Şifreyi Sıfırla
          </button>
        </div>
      </div>
    </div>
  </div>
  <div v-if="showResetModal" class="modal-backdrop fade show"></div>
</template>
