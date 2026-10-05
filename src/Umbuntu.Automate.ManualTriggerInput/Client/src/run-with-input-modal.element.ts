import type {
  UmbuntuRunWithInputModalData,
  UmbuntuRunWithInputModalValue,
} from "./run-with-input-modal.token.js";
import { automationFetch } from "./automation.api.js";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { umbFocus } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { formatBytes } from "@umbraco-cms/backoffice/utils";
import type { UUIButtonState, UUITextareaElement } from "@umbraco-cms/backoffice/external/uui";

const FORM_ID = "RunWithInputForm";

// The built-in "Run now" action (UmbracoAutomate.EntityAction.Automation.RunNow) posts to
// {id}/trigger with no body, so a Manual Trigger's ${trigger.output.*} fields can never be
// populated from it. This modal collects those fields and posts them to the sibling
// {id}/trigger-with-input endpoint (RunAutomationWithInputController) instead.
//
// Layout follows the core form modals (e.g. umb-rename-modal): a small sidebar with
// umb-body-layout, the form inside a uui-box, and a submit button bound to the form so
// native required-field validation runs before anything is posted.
@customElement("umbuntu-run-with-input-modal")
export class UmbuntuRunWithInputModalElement extends UmbModalBaseElement<
  UmbuntuRunWithInputModalData,
  UmbuntuRunWithInputModalValue
> {
  @state()
  private _automationName?: string;

  @state()
  private _triggerInput = "";

  @state()
  private _inputBytes = 0;

  // Configured server-side (Umbuntu:ManualTriggerInput:MaxInputBytes); undefined until loaded,
  // in which case the server still enforces it on submit.
  @state()
  private _maxInputBytes?: number;

  @state()
  private _runState?: UUIButtonState;

  #encoder = new TextEncoder();

  override connectedCallback() {
    super.connectedCallback();
    void this.#loadAutomationName();
    void this.#loadSettings();
  }

  override firstUpdated() {
    // Hook the size limit into UUI's own validation so it surfaces like any other field error
    // (message under the field, submit blocked) rather than as a bespoke banner.
    // The check reads the textarea's own value: UUI runs validators while setting it, before
    // our @input handler has updated _inputBytes.
    const textarea = this.shadowRoot?.querySelector<UUITextareaElement>("#triggerInput");
    textarea?.addValidator(
      "tooLong",
      () => this.localize.term("umbuntuRunWithInput_inputTooLarge", this.#format(this._maxInputBytes ?? 0)),
      () => this.#isTooLarge(this.#byteLength(String(textarea.value ?? ""))),
    );
  }

  #byteLength(value: string) {
    return this.#encoder.encode(value).length;
  }

  #isTooLarge(bytes: number) {
    return this._maxInputBytes !== undefined && bytes > this._maxInputBytes;
  }

  #format(bytes: number) {
    return formatBytes(bytes, { decimals: 1, culture: this.localize.lang() });
  }

  async #loadSettings() {
    try {
      const response = await automationFetch(this, ["trigger-with-input", "settings"]);
      if (!response?.ok) return;
      const settings = await response.json();
      if (typeof settings?.maxInputBytes === "number") this._maxInputBytes = settings.maxInputBytes;
    } catch {
      // The server enforces the limit regardless; this only drives the counter.
    }
  }

  async #loadAutomationName() {
    const automationId = this.data?.automationId;
    if (!automationId) return;

    try {
      const response = await automationFetch(this, [automationId]);
      if (!response?.ok) return;
      const automation = await response.json();
      this._automationName = automation?.name ?? undefined;
    } catch {
      // Purely cosmetic — the box falls back to a generic headline.
    }
  }

  async #onSubmit(event: SubmitEvent) {
    event.preventDefault();

    const automationId = this.data?.automationId;
    if (!automationId || this._runState === "waiting") return;

    this._runState = "waiting";

    try {
      const response = await automationFetch(this, [automationId, "trigger-with-input"], {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ triggerInput: this._triggerInput }),
      });

      if (!response?.ok) {
        const problem = await response?.json().catch(() => undefined);
        this._runState = "failed";
        await this.#notify("danger", {
          headline: problem?.title ?? this.localize.term("umbuntuRunWithInput_failedHeadline"),
          message:
            problem?.detail ??
            this.localize.term("umbuntuRunWithInput_failedMessage", response?.status ?? "—"),
        });
        return;
      }

      this._runState = "success";
      await this.#notify("positive", {
        headline: this.localize.term("umbuntuRunWithInput_startedHeadline"),
        message: this.localize.term("umbuntuRunWithInput_startedMessage"),
      });

      this.value = { submitted: true };
      this._submitModal();
    } catch {
      this._runState = "failed";
      await this.#notify("danger", {
        headline: this.localize.term("umbuntuRunWithInput_failedHeadline"),
        message: this.localize.term("umbuntuRunWithInput_networkError"),
      });
    }
  }

  async #notify(color: "positive" | "danger", data: { headline: string; message: string }) {
    const notificationContext = await this.getContext(UMB_NOTIFICATION_CONTEXT);
    notificationContext?.peek(color, { data });
  }

  #onInput(event: InputEvent) {
    this._triggerInput = (event.target as HTMLTextAreaElement).value;
    this._inputBytes = this.#byteLength(this._triggerInput);
    if (this._runState === "failed") this._runState = undefined;
  }

  // Enter adds a new line in the textarea, so offer Ctrl/Cmd+Enter to run.
  #onKeyDown(event: KeyboardEvent) {
    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      this.shadowRoot?.querySelector<HTMLFormElement>(`#${FORM_ID}`)?.requestSubmit();
    }
  }

  override render() {
    const waiting = this._runState === "waiting";

    return html`
      <umb-body-layout headline=${this.localize.term("umbuntuRunWithInput_headline")}>
        <uui-box headline=${this._automationName ?? this.localize.term("umbuntuRunWithInput_automation")}>
          <uui-form>
            <form id=${FORM_ID} @submit=${this.#onSubmit}>
              <uui-form-layout-item>
                <uui-label id="triggerInputLabel" for="triggerInput" slot="label" required>
                  <umb-localize key="umbuntuRunWithInput_inputLabel">Trigger input</umb-localize>
                </uui-label>
                <span slot="description">
                  <umb-localize key="umbuntuRunWithInput_inputDescription">
                    Passed to the automation's steps as
                  </umb-localize>
                  <code>\${trigger.triggerInput}</code>
                </span>
                <uui-textarea
                  id="triggerInput"
                  name="triggerInput"
                  label=${this.localize.term("umbuntuRunWithInput_inputLabel")}
                  rows="8"
                  auto-height
                  required
                  required-message=${this.localize.term("umbuntuRunWithInput_inputRequired")}
                  .value=${this._triggerInput}
                  ?readonly=${waiting}
                  @input=${this.#onInput}
                  @keydown=${this.#onKeyDown}
                  ${umbFocus()}
                ></uui-textarea>
                ${this._maxInputBytes !== undefined
                  ? html`<div id="size" class=${this.#isTooLarge(this._inputBytes) ? "over" : ""}>
                      ${this.localize.term(
                        "umbuntuRunWithInput_inputSize",
                        this.#format(this._inputBytes),
                        this.#format(this._maxInputBytes),
                      )}
                    </div>`
                  : ""}
              </uui-form-layout-item>
            </form>
          </uui-form>
        </uui-box>

        <uui-button
          slot="actions"
          label=${this.localize.term("general_cancel")}
          ?disabled=${waiting}
          @click=${this._rejectModal}
        ></uui-button>
        <uui-button
          slot="actions"
          type="submit"
          form=${FORM_ID}
          look="primary"
          color="positive"
          label=${this.localize.term("umbuntuRunWithInput_run")}
          .state=${this._runState}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override readonly styles = [
    UmbTextStyles,
    css`
      uui-form-layout-item {
        margin: 0;
      }

      #triggerInput {
        width: 100%;
        --uui-textarea-min-height: 160px;
      }

      #size {
        margin-top: var(--uui-size-space-2);
        text-align: right;
        font-size: var(--uui-type-small-size);
        color: var(--uui-color-text-alt);
      }

      #size.over {
        color: var(--uui-color-danger);
      }
    `,
  ];
}

export default UmbuntuRunWithInputModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "umbuntu-run-with-input-modal": UmbuntuRunWithInputModalElement;
  }
}
