<script setup>
import IconsImg from '@/assets/images/icons.webp'
</script>

<template>
  <div class="bg-warning bg-opacity-10 border border-2 border-warning rounded mb-4">
    <div class="row d-flex align-items-center p-3 px-xl-4 flex-xl-nowrap">
      <div class="col-xl-auto col-12 d-none d-xl-block p-0">
        <img class="img-fluid" :src="IconsImg" width="160px" height="160px" alt="CoreUI Simgeleri" />
      </div>
      <div class="col-md col-12 px-lg-4">
        CoreUI Icons paketi, SVG, PNG ve Webfont olmak üzere birden fazla formatta 1500'den fazla simge ile birlikte gelir.
        CoreUI Icons, yaygın eylemler ve öğeler için özenle hazırlanmış simgelerdir.
        Bu simgeleri web veya mobil uygulamalarınızda dijital ürünlerinizde kullanabilirsiniz.
        Daha fazla bilgi için lütfen dokümantasyonumuzu ziyaret edin.
      </div>
      <div class="col-md-auto col-12 mt-3 mt-lg-0">
        <a
          class="btn btn-warning text-nowrap text-white"
          href="https://coreui.io/vue/docs/components/icon.html"
          target="_blank"
          rel="noopener noreferrer"
        >
          Dokümantasyonu Keşfedin
        </a>
      </div>
    </div>
  </div>
</template>
