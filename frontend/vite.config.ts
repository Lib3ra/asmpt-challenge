import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Forward API calls to the ASP.NET Core backend during development.
      '/api': process.env.API_URL ?? 'http://localhost:5080',
    },
  },
})
