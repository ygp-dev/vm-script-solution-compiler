import { InMemoryCredentialStore, type Model } from "@earendil-works/pi-ai";
import { ModelRuntime } from "@earendil-works/pi-coding-agent";
import type { AgentConfiguration } from "./config.js";

const PROVIDER_ID = "vm-configured-provider";

export interface ConfiguredModel {
  runtime: ModelRuntime;
  model: Model<any>;
}

export async function configureModel(config: AgentConfiguration): Promise<ConfiguredModel> {
  if (!config.model.trim()) {
    throw new Error("未配置模型。请在桌面配置中填写模型 ID。");
  }
  if (!config.apiKey.trim()) {
    throw new Error("未配置 API Key。请在桌面配置中填写 API Key。");
  }

  const credentials = new InMemoryCredentialStore();
  const runtime = await ModelRuntime.create({
    credentials,
    modelsPath: null,
    allowModelNetwork: false,
  });
  const api = config.provider === "openai-compatible"
    ? "openai-completions"
    : "openai-responses";
  runtime.registerProvider(PROVIDER_ID, {
    name: "VM Script Agent Provider",
    baseUrl: config.endpoint,
    api,
    authHeader: true,
    models: [{
      id: config.model,
      name: config.model,
      api,
      reasoning: config.provider === "openai-responses",
      input: ["text", "image"],
      cost: { input: 0, output: 0, cacheRead: 0, cacheWrite: 0 },
      contextWindow: config.contextWindow,
      maxTokens: config.maxTokens,
      compat: api === "openai-responses"
        ? {
            supportsDeveloperRole: true,
            supportsStrictMode: true,
          }
        : undefined,
    }],
  });
  await runtime.setRuntimeApiKey(PROVIDER_ID, config.apiKey);
  const model = runtime.getModel(PROVIDER_ID, config.model);
  if (!model) throw new Error(`Pi 未能注册模型：${config.model}`);
  return { runtime, model };
}

export const ConfiguredProviderId = PROVIDER_ID;
