import { manifests as localization } from "./localization/manifest.js";

// Entity type alias for automations, as registered by Umbraco.Automate.Web. Automate's client
// isn't published as an npm package, so it's duplicated here as a literal.
const AUTOMATION_ENTITY_TYPE = "ua:automation";

const HAS_RUN_WITH_INPUT_TRIGGER_CONDITION_ALIAS = "Umbuntu.Condition.Entity.Automation.HasRunWithInputTrigger";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "modal",
    alias: "Umbuntu.Modal.RunAutomationWithInput",
    name: "Run Automation With Input Modal",
    element: () => import("./run-with-input-modal.element.js"),
  },
  {
    type: "condition",
    alias: HAS_RUN_WITH_INPUT_TRIGGER_CONDITION_ALIAS,
    name: "Entity Automation Has Run-With-Input Trigger Condition",
    api: () => import("./run-with-input.condition.js"),
  },
  {
    type: "entityAction",
    kind: "default",
    alias: "Umbuntu.EntityAction.Automation.RunWithInput",
    name: "Run Automation With Input Entity Action",
    weight: 290,
    api: () => import("./run-with-input.action.js"),
    forEntityTypes: [AUTOMATION_ENTITY_TYPE],
    meta: {
      icon: "icon-terminal",
      label: "#umbuntuRunWithInput_actionLabel",
    },
    conditions: [{ alias: HAS_RUN_WITH_INPUT_TRIGGER_CONDITION_ALIAS }],
  },
  ...localization,
];
