import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/users": "http://localhost",
      "/movies": "http://localhost",
      "/reviews": "http://localhost",
      "/feed": "http://localhost",
      "/follow": "http://localhost",
      "/history": "http://localhost",
      "/watchlist": "http://localhost",
      "/playlists": "http://localhost",
      "/ws": { target: "ws://localhost", ws: true },
    },
  },
});
