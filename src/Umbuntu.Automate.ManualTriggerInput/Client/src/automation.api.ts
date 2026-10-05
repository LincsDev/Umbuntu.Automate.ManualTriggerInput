import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import type { UmbClassInterface } from "@umbraco-cms/backoffice/class-api";

const AUTOMATIONS_API = "/umbraco/automate/management/api/v1/automations";

// There's no generated OpenAPI client for Automate's management API (it isn't published as an
// npm package), so auth is wired by hand per the documented manual-fetch pattern on
// UmbAuthContext.getOpenApiConfiguration(). Path segments are encoded individually, e.g.
// [automationId, "trigger-with-input"].
export async function automationFetch(
  host: UmbClassInterface,
  segments: ReadonlyArray<string>,
  init: RequestInit = {},
): Promise<Response | undefined> {
  const authContext = await host.getContext(UMB_AUTH_CONTEXT);
  if (!authContext) return undefined;
  const config = authContext.getOpenApiConfiguration();

  const path = segments.map(encodeURIComponent).join("/");

  return fetch(`${config.base}${AUTOMATIONS_API}/${path}`, {
    ...init,
    credentials: config.credentials,
    headers: {
      ...init.headers,
      Authorization: `Bearer ${await config.token()}`,
    },
  });
}
