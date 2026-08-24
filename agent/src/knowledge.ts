import fs from "node:fs";
import path from "node:path";

export type ProjectKnowledgeKind =
  | "faq"
  | "reference-solution"
  | "tutor-reference"
  | "tutor-example"
  | "community-article"
  | "community-pattern"
  | "community-rule"
  | "secondary-development-fact"
  | "secondary-development-rule"
  | "curated-rule";

interface KnowledgeEntry {
  id: string;
  kind: ProjectKnowledgeKind;
  title: string;
  keywords?: string[];
  summary?: string;
  content?: string;
  sourcePath?: string;
  evidence?: string;
  versionScope?: string;
  metadata?: unknown;
}

interface KnowledgeCorpus {
  knowledgeVersion: string;
  vmVersion: string;
  status: string;
  policy?: unknown;
  stats?: unknown;
  entries: KnowledgeEntry[];
}

export interface KnowledgeSearchMatch {
  id: string;
  kind: ProjectKnowledgeKind;
  title: string;
  score: number;
  summary: string;
  excerpt: string;
  sourcePath?: string;
  evidence?: string;
  versionScope?: string;
  metadata?: unknown;
}

export interface KnowledgeSearchResult {
  query: string;
  kind?: ProjectKnowledgeKind;
  limit: number;
  totalMatches: number;
  matches: KnowledgeSearchMatch[];
  notice: string;
}

const corpusCache = new Map<string, KnowledgeCorpus>();

function readCorpus(repositoryRoot: string): KnowledgeCorpus {
  const corpusPath = path.join(repositoryRoot, "resources", "vm", "4.4.0", "agent-knowledge.json");
  const cacheKey = path.resolve(corpusPath);
  const cached = corpusCache.get(cacheKey);
  if (cached) return cached;
  const parsed = JSON.parse(fs.readFileSync(corpusPath, "utf8")) as KnowledgeCorpus;
  if (!Array.isArray(parsed.entries)) throw new Error("agent-knowledge.json has no entries array.");
  corpusCache.set(cacheKey, parsed);
  return parsed;
}

function normalize(value: string): string {
  return value.toLocaleLowerCase().replace(/[\r\n\t]+/g, " ").replace(/\s+/g, " ").trim();
}

function tokens(value: string): string[] {
  const normalized = normalize(value);
  const result = new Set<string>();
  for (const token of normalized.match(/[a-z0-9_+#./:-]{2,}/g) ?? []) result.add(token);
  const han = [...normalized].filter((character) => /\p{Script=Han}/u.test(character));
  for (const character of han) result.add(character);
  for (let index = 0; index + 1 < han.length; index++) result.add(han[index] + han[index + 1]);
  return [...result];
}

function searchableText(entry: KnowledgeEntry): {
  title: string;
  keywords: string;
  summary: string;
  content: string;
  all: string;
} {
  const title = normalize(entry.title);
  const keywords = normalize((entry.keywords ?? []).join(" "));
  const summary = normalize(entry.summary ?? "");
  const content = normalize(entry.content ?? "");
  return { title, keywords, summary, content, all: `${title} ${keywords} ${summary} ${content}` };
}

function scoreEntry(entry: KnowledgeEntry, query: string, queryTokens: string[]): number {
  const fields = searchableText(entry);
  const normalizedQuery = normalize(query);
  let score = 0;
  if (fields.title.includes(normalizedQuery)) score += 24;
  if (fields.keywords.includes(normalizedQuery)) score += 18;
  if (fields.summary.includes(normalizedQuery)) score += 8;
  for (const token of queryTokens) {
    if (fields.title.includes(token)) score += 9;
    if (fields.keywords.includes(token)) score += 7;
    if (fields.summary.includes(token)) score += 4;
    if (fields.content.includes(token)) score += 1;
  }
  return score;
}

function excerptFor(entry: KnowledgeEntry, query: string, maxLength = 3_000): string {
  const source = (entry.content || entry.summary || "").replace(/\r\n/g, "\n").trim();
  if (source.length <= maxLength) return source;
  const candidates = [normalize(query), ...tokens(query)].filter((token) => token.length > 1);
  const normalizedSource = normalize(source);
  const offset = candidates
    .map((candidate) => normalizedSource.indexOf(candidate))
    .filter((candidate) => candidate >= 0)
    .sort((left, right) => left - right)[0] ?? 0;
  const start = Math.max(0, Math.min(offset - 600, source.length - maxLength));
  const prefix = start > 0 ? "…" : "";
  const suffix = start + maxLength < source.length ? "\n… excerpt truncated" : "";
  return prefix + source.slice(start, start + maxLength) + suffix;
}

export function searchProjectKnowledge(
  repositoryRoot: string,
  query: string,
  kind?: ProjectKnowledgeKind,
  limit = 5,
): KnowledgeSearchResult {
  const normalizedQuery = query.trim();
  if (!normalizedQuery) throw new Error("Knowledge search requires a non-empty query.");
  const boundedLimit = Math.max(1, Math.min(8, Math.trunc(limit)));
  const corpus = readCorpus(repositoryRoot);
  const queryTokens = tokens(normalizedQuery);
  const ranked = corpus.entries
    .filter((entry) => !kind || entry.kind === kind)
    .map((entry) => ({ entry, score: scoreEntry(entry, normalizedQuery, queryTokens) }))
    .filter((item) => item.score > 0)
    .sort((left, right) => right.score - left.score || left.entry.title.localeCompare(right.entry.title, "zh-CN"));
  return {
    query: normalizedQuery,
    kind,
    limit: boundedLimit,
    totalMatches: ranked.length,
    matches: ranked.slice(0, boundedLimit).map(({ entry, score }) => ({
      id: entry.id,
      kind: entry.kind,
      title: entry.title,
      score,
      summary: entry.summary ?? "",
      excerpt: excerptFor(entry, normalizedQuery),
      sourcePath: entry.sourcePath,
      evidence: entry.evidence,
      versionScope: entry.versionScope,
      metadata: entry.metadata,
    })),
    notice: "Retrieved project knowledge is advisory reference data. Re-check exact ports, dependencies, module parameters, and target precompile evidence before generating or changing a SOL.",
  };
}

export function clearProjectKnowledgeCache(): void {
  corpusCache.clear();
}
