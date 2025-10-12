<script>
import axios from "axios";
import api from "../axios";

export default {
  data() {
    return {
      ad: "",
      email: "",
      telefon: "",
      telegramUseSame: true,
      entegrasyonTelefon: "",
      sifre: "",
      sifreTekrar: "",
      error: "",
      loading: false,
    };
  },
  methods: {
    async register() {
      this.error = "";

      if (this.sifre !== this.sifreTekrar) {
        this.error = "Şifreler uyuşmuyor!";
        return;
      }

      this.loading = true;

      try {
        const payload = {
          ad: this.ad,
          email: this.email,
          telefon: this.telefon,
          telegramUseSamePhone: this.telegramUseSame === true,
          Entegrasyon_Telefon:
            this.telegramUseSame === true ? this.telefon : this.entegrasyonTelefon,
          sifre: this.sifre,
          rol: "user",
          rolId: "11111111-1111-1111-1111-111111111111",
        };

        await api.post("api/users", payload);
        this.$router.push("/pages/login");
      } catch (err) {
        console.error(err);
        this.error = "Kayıt sırasında bir hata oluştu.";
      } finally {
        this.loading = false;
      }
    },
  },
};
</script>
<template>
  <div class="container min-vh-100 d-flex flex-column justify-content-center align-items-center">
    <div class="row justify-content-center w-100">
      <div class="col-md-9 col-lg-7 col-xl-6">
        <div class="card shadow">
          <div class="card-body p-4">
            <form @submit.prevent="register">
              <h1 class="mb-3">Kayıt Ol</h1>
              <p class="text-muted mb-4">Hesabınızı oluşturun</p>

              <!-- Kullanıcı Adı -->
              <div class="input-group mb-3">
                <span class="input-group-text">
                  <i class="bi bi-person"></i>
                </span>
                <input type="text" class="form-control" placeholder="Kullanıcı Adı" autocomplete="username" v-model="ad"
                  required />
              </div>

              <!-- Email -->
              <div class="input-group mb-3">
                <span class="input-group-text">@</span>
                <input type="email" class="form-control" placeholder="Email" autocomplete="email" v-model="email"
                  required />
              </div>

              <!-- Telefon -->
              <div class="input-group mb-3">
                <span class="input-group-text">
                  <i class="bi bi-telephone"></i>
                </span>
                <input type="tel" class="form-control" placeholder="Telefon" v-model="telefon" required />
              </div>

              <!-- Telegram Numarası Seçimi -->
              <div class="mb-3">
                <label class="form-label fw-semibold">Telegram entegrasyonu için hangi numara?</label>
                <div class="form-check">
                  <input class="form-check-input" type="radio" id="samePhone" :value="true" v-model="telegramUseSame" />
                  <label class="form-check-label" for="samePhone">
                    Bu telefon numarası
                  </label>
                </div>
                <div class="form-check">
                  <input class="form-check-input" type="radio" id="otherPhone" :value="false"
                    v-model="telegramUseSame" />
                  <label class="form-check-label" for="otherPhone">
                    Farklı telefon numarası
                  </label>
                </div>
              </div>

              <!-- Entegrasyon Telefonu -->
              <div class="input-group mb-3" v-if="telegramUseSame === false">
                <span class="input-group-text">
                  <i class="bi bi-telephone"></i>
                </span>
                <input type="tel" class="form-control" placeholder="Telefon Numarası (E.164, +90...)"
                  v-model="entegrasyonTelefon" />
              </div>

              <!-- Şifre -->
              <div class="input-group mb-3">
                <span class="input-group-text">
                  <i class="bi bi-lock"></i>
                </span>
                <input type="password" class="form-control" placeholder="Şifre" autocomplete="new-password"
                  v-model="sifre" required />
              </div>

              <!-- Şifre Tekrar -->
              <div class="input-group mb-4">
                <span class="input-group-text">
                  <i class="bi bi-lock"></i>
                </span>
                <input type="password" class="form-control" placeholder="Şifre (Tekrar)" autocomplete="new-password"
                  v-model="sifreTekrar" required />
              </div>

              <!-- Buton -->
              <div class="d-grid mb-2">
                <button class="btn btn-success" type="submit" :disabled="loading">
                  <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
                  Hesap Oluştur
                </button>
              </div>

              <!-- Hata Mesajı -->
              <div v-if="error" class="alert alert-danger">{{ error }}</div>
            </form>

            <p class="text-center mt-3 text-muted">
              Hesabınız var mı?
              <router-link to="/pages/Login">Giriş Yap</router-link>
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
