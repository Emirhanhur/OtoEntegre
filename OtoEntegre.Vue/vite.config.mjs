import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'node:path'
import autoprefixer from 'autoprefixer'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
      stream: 'stream-browserify', // sadece @ ile src'yi çöz
    },
    extensions: ['.mjs', '.js', '.ts', '.jsx', '.tsx', '.json', '.vue', '.scss'],
  },
  css: {
    postcss: {
      plugins: [autoprefixer()],
    },
  },
  optimizeDeps: {
    include: ['stream-browserify','@coreui/icons', '@coreui/utils', 'vue-chartjs'],
  },
  server: {
    port: 3000,
  },
})
