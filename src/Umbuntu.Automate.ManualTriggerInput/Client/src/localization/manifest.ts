export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "localization",
    alias: "Umbuntu.Automate.ManualTriggerInput.Localization.En",
    name: "Umbuntu Automate Manual Trigger Input English",
    meta: {
      culture: "en",
    },
    js: () => import("./en.js"),
  },
];
