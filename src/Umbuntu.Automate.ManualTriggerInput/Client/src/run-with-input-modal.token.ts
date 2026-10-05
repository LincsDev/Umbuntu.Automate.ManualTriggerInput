import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UmbuntuRunWithInputModalData {
  automationId: string;
}

export interface UmbuntuRunWithInputModalValue {
  submitted: boolean;
}

export const UMBUNTU_RUN_WITH_INPUT_MODAL = new UmbModalToken<
  UmbuntuRunWithInputModalData,
  UmbuntuRunWithInputModalValue
>("Umbuntu.Modal.RunAutomationWithInput", {
  modal: {
    type: "sidebar",
    size: "small",
  },
});
