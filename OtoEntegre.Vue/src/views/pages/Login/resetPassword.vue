<script>
import api from "../../axios";

export default {
  data() {
    return {
      email: "",
      newPassword: "",
      message: "",
      success: false,
      loading: false
    };
  },
  methods: {
    async resetPassword() {
      this.message = "";
      this.loading = true;
      try {
        const response = await api.post("api/auth/reset-password", {
          email: this.email,
          newPassword: this.newPassword
        });
        this.success = true;
        this.message = response.data.message;
      } catch (err) {
        this.success = false;
        this.message = err.response?.data?.message || "Sunucu hatası, tekrar deneyin.";
      } finally {
        this.loading = false;
      }
    }
  }
};
</script>

<template>
  <div class="d-flex justify-content-center align-items-center vh-100 bg-light">
    <div class="card p-4 shadow-sm" style="width: 400px;">
      <h3 class="card-title text-center mb-4">Şifre Sıfırlama</h3>

      <div v-if="message" :class="{'alert alert-success': success, 'alert alert-danger': !success}">
        {{ message }}
      </div>

      <form @submit.prevent="resetPassword">
        <div class="mb-3">
          <label for="email" class="form-label">Email</label>
          <input type="email" id="email" class="form-control" v-model="email" required />
        </div>

        <div class="mb-3">
          <label for="newPassword" class="form-label">Yeni Şifre</label>
          <input type="password" id="newPassword" class="form-control" v-model="newPassword" required />
        </div>

        <button type="submit" class="btn btn-primary w-100" :disabled="loading">
          <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
          Şifreyi Sıfırla
        </button>
      </form>

      <p class="text-center mt-3 text-muted">
        <router-link to="/login">Girişe Dön</router-link>
      </p>
    </div>
  </div>
</template>