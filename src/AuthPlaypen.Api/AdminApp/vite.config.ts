import { resolve } from "node:path";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import solidPlugin from "vite-plugin-solid";
import tsConfigPaths from "vite-tsconfig-paths";
import devtools from "solid-devtools/vite";

export default defineConfig(() => ({
  plugins: [devtools(), solidPlugin(), tailwindcss(), tsConfigPaths()],
  server: {
    port: 3000,
  },
  build: {
    target: "esnext",
    outDir: resolve(__dirname, "../wwwroot"),
    emptyOutDir: true,
  },
}));
