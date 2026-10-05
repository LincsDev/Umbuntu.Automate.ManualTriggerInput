import { UMBUNTU_RUN_WITH_INPUT_MODAL } from "./run-with-input-modal.token.js";
import { UmbEntityActionBase, type MetaEntityAction } from "@umbraco-cms/backoffice/entity-action";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";

export class UmbuntuRunWithInputEntityAction extends UmbEntityActionBase<MetaEntityAction> {
  override async execute() {
    const automationId = this.args.unique;
    if (!automationId) return;

    const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
    if (!modalManager) return;

    const modalHandle = modalManager.open(this, UMBUNTU_RUN_WITH_INPUT_MODAL, {
      data: { automationId },
    });

    try {
      await modalHandle.onSubmit();
    } catch {
      // Closed without running — nothing to do.
    }
  }
}

export default UmbuntuRunWithInputEntityAction;
