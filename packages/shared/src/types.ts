export type ScanStatus = 'Pending' | 'Running' | 'Completed' | 'Failed';
export type ScanStatusEnum = 0 | 1 | 2 | 3; // Backend sends numeric enum

export interface VersionCount {
  version: string;
  count: number;
}

export interface TechnologyAggregate {
  name: string;
  total: number;
  outdatedCount: number;
  versions: VersionCount[];
}

export interface TechnologyFinding {
  name: string;
  version?: string;
  sourceFile?: string;
  detector?: string;
  isOutdated?: boolean;
  latestVersion?: string;
}

export interface ScanHistoryItem {
  scanId: string;
  startedAt: string;
  finishedAt?: string;
  status: ScanStatus;
}

export interface ProjectListItem {
  id: string;
  name: string;
  path: string;
  lastScannedAt: string;
  scanCount: number;
  technologyCount: number;
  lastStatus?: string | null;
}

export interface ProjectDetail {
  id: string;
  name: string;
  path: string;
  lastScannedAt: string;
  aiInsights?: string | null;
  technologyFindings: TechnologyFinding[];
  scanHistory: ScanHistoryItem[];
}

export interface ScanStatusResponse {
  scanId: string;
  status: ScanStatus | ScanStatusEnum; // Can be string or number
  startedAt: string;
  finishedAt?: string;
  progress?: number | null;
  error?: string | null;
}

export interface StartScanRequest {
  projectName: string;
  path: string;
}
