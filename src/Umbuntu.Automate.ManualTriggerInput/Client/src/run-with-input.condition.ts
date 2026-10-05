import { UMB_ENTITY_CONTEXT } from "@umbraco-cms/backoffice/entity";
import { automationFetch } from "./automation.api.js";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type { UmbConditionConfigBase } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

// Mirrors RunWithInputTrigger.TriggerAlias in Triggers/RunWithInputTrigger.cs.
export const RUN_WITH_INPUT_TRIGGER_ALIAS = "umbuntu.manualWithInput";

// Successful lookups are reused briefly so a burst of evaluations (tree items, action menus)
// shares one request, but expire quickly enough that changing an automation's trigger or
// publishing it shows up without a page reload. Failures are never cached, so a transient
// error doesn't hide the action for the rest of the session.
const CACHE_TTL_MS = 30 * 1000;

interface AutomationSummary {
  triggerAlias: string | null;
  status: string | null;
}

const cache = new Map<string, { value: AutomationSummary; expires: number }>();

export interface UmbuntuHasRunWithInputTriggerConditionConfig extends UmbConditionConfigBase {}

// Modelled on Automate's own "can run now" condition: consume UMB_ENTITY_CONTEXT for the
// automation's id, fetch the automation, and permit only when it's published and uses our trigger.
export class UmbuntuHasRunWithInputTriggerCondition extends UmbConditionBase<UmbuntuHasRunWithInputTriggerConditionConfig> {
  constructor(
    host: UmbControllerHost,
    args: {
      config: UmbuntuHasRunWithInputTriggerConditionConfig;
      onChange: (permitted: boolean) => void;
    },
  ) {
    super(host, args);

    this.consumeContext(UMB_ENTITY_CONTEXT, (entityContext) => {
      this.observe(
        entityContext?.unique,
        async (unique) => {
          this.permitted = await this.#canRunWithInput(unique ?? null);
        },
        "umbuntuEntityUnique",
      );
    });
  }

  async #canRunWithInput(automationId: string | null): Promise<boolean> {
    if (!automationId) return false;

    const automation = await this.#getAutomation(automationId);
    return automation?.status === "Published" && automation.triggerAlias === RUN_WITH_INPUT_TRIGGER_ALIAS;
  }

  async #getAutomation(automationId: string): Promise<AutomationSummary | undefined> {
    const cached = cache.get(automationId);
    if (cached && cached.expires > Date.now()) return cached.value;

    try {
      const response = await automationFetch(this, [automationId]);
      if (!response?.ok) return undefined;

      const data = await response.json();
      const value: AutomationSummary = {
        triggerAlias: data?.trigger?.triggerAlias ?? null,
        status: data?.status ?? null,
      };
      cache.set(automationId, { value, expires: Date.now() + CACHE_TTL_MS });
      return value;
    } catch {
      return undefined;
    }
  }
}

export default UmbuntuHasRunWithInputTriggerCondition;
