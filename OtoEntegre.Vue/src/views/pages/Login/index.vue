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

                const decoded = jwtDecode(token);

                // Tek rol veya birden fazla rol olabilir
                let roles = decoded.role;
                if (!roles) {
                    console.warn("JWT içinde rol bulunamadı!");
                } else if (Array.isArray(roles)) {
                    roles = roles[0]; // İlk rolü al
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
                            <i :class="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
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
        </div>
    </div>
</template>
