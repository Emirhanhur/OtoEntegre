<template>
  <div v-if="show" class="modal-backdrop fade show" @click.self="close">
    <div class="modal-dialog modal-xl modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">Otosticker Girişi</h5>
          <button type="button" class="btn-close" @click="close"></button>
        </div>
        <div class="modal-body" style="height:600px; overflow:hidden;">
          <iframe src="https://www.otosticker.com.tr/uye-giris" width="100%" height="100%"></iframe>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  props: ['show'],
  emits: ['close'],
  methods: {
    close() {
      this.$emit('close');
    }
  }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top:0; left:0; right:0; bottom:0;
  background: rgba(0,0,0,0.5);
  display:flex;
  justify-content:center;
  align-items:center;
  z-index: 1050;
}
.modal-dialog {
  max-width: 90%;
}
</style>
