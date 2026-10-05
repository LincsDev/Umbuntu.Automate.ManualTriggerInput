import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { defineConfig, type Plugin } from "vite";

const OUT_DIR = "../wwwroot/App_Plugins/UmbuntuAutomateManualTriggerInput";

// Stamps the package version into the copied umbraco-package.json so the backoffice reports the
// same version as the NuGet package. MSBuild passes PACKAGE_VERSION (resolved by MinVer from git
// tags); plain `npm run build` keeps the placeholder from public/. The editor-only $schema link
// is relative to Client/, so it's dropped from the shipped copy.
function stampPackageVersion(): Plugin {
  return {
    name: "umbuntu-stamp-package-version",
    apply: "build",
    writeBundle() {
      const manifestPath = resolve(__dirname, OUT_DIR, "umbraco-package.json");
      const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
      delete manifest.$schema;
      if (process.env.PACKAGE_VERSION) manifest.version = process.env.PACKAGE_VERSION;
      writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);
    },
  };
}

export default defineConfig(({ mode }) => ({
  plugins: [stampPackageVersion()],
  build: {
    lib: {
      entry: "src/bundle.manifests.ts",
      formats: ["es"],
      fileName: "umbuntu-automate-manual-trigger-input",
    },
    outDir: OUT_DIR,
    emptyOutDir: true,
    // Source maps only for `npm run watch` (development mode); production builds ship without them.
    sourcemap: mode === "development",
    rollupOptions: {
      external: [/^@umbraco/],
      output: {
        chunkFileNames: "[name].js",
        assetFileNames: "[name].[ext]",
      },
    },
  },
}));
