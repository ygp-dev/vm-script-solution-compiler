/**
 * Single point of contact with the Pi coding agent package.
 *
 * Pi does not export these modules through its package `exports` map, so they have to be reached by
 * deep path into `dist/`. Keeping every such path in this one file means a Pi upgrade — or a package
 * manager whose hoisting makes `../node_modules` resolve differently — breaks here and nowhere else.
 * The pinned version lives in `agent/package.json`; update both together.
 *
 * Do not import `@earendil-works/pi-coding-agent/dist/**` anywhere else under `agent/src`.
 */
export {
  type AgentSession,
  type AgentSessionEvent,
} from "../node_modules/@earendil-works/pi-coding-agent/dist/core/agent-session.js";

export { DefaultResourceLoader } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/resource-loader.js";

export {
  SessionManager,
  type SessionInfo,
} from "../node_modules/@earendil-works/pi-coding-agent/dist/core/session-manager.js";

export { createAgentSession } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/sdk.js";

export { ModelRuntime } from "../node_modules/@earendil-works/pi-coding-agent/dist/core/model-runtime.js";

export {
  defineTool,
  type ToolDefinition,
} from "../node_modules/@earendil-works/pi-coding-agent/dist/core/extensions/types.js";
