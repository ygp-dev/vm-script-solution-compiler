export type VmTaskMode = "create" | "patch" | "unknown";

export type VmTaskPhase =
  | "idle"
  | "inspecting"
  | "drafting"
  | "requirement-validating"
  | "planned"
  | "building"
  | "built"
  | "offline-validating"
  | "offline-validated"
  | "user-validated"
  | "blocked";

export interface DomainError {
  code: string;
  message: string;
  timestampUtc: string;
  recoverability: "automatic" | "requires-user-choice" | "blocked";
}

export interface CapabilityEvidence {
  kind: "environment" | "solution" | "capability" | "validation" | "report";
  summary: string;
  data?: unknown;
  timestampUtc: string;
}

export interface VmArtifact {
  kind: "solution" | "report" | "task-directory" | "generated-source" | "dependency" | "other";
  path: string;
  timestampUtc: string;
}

export interface VmCompletionState {
  requirementValid: boolean;
  solutionBuilt: boolean;
  offlineValidationPassed: boolean;
  vmRuntimeValidationRequired: boolean;
  userValidated: boolean;
}

export interface RequirementRetryState {
  turnValidationAttempts: number;
  totalValidationAttempts: number;
  consecutiveSameFailures: number;
  lastFailureSignature?: string;
  blocked: boolean;
}

export interface VmTaskState {
  version: 1;
  intent: VmTaskMode;
  baseSolution?: string;
  outputDirectory?: string;
  targetVmVersion: string;
  phase: VmTaskPhase;
  requirementRevision: number;
  requirementRetry: RequirementRetryState;
  requirement?: Record<string, unknown>;
  confirmedFacts: string[];
  unresolvedQuestions: string[];
  capabilityEvidence: CapabilityEvidence[];
  artifacts: VmArtifact[];
  lastCompilerError?: DomainError;
  lastTool?: {
    name: string;
    ok: boolean;
    timestampUtc: string;
  };
  completion: VmCompletionState;
  createdUtc: string;
  updatedUtc: string;
}

export interface TaskContextInput {
  mode?: VmTaskMode;
  baseSolution?: string;
  outputDirectory?: string;
}
