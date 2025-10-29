// ============================================================================
// Dashboard Types and DTOs
// Based on dashboard-view-implementation-plan.md section 5
// ============================================================================

// Shared
export type ChartRange = '1h' | '3h' | '5h' | '8h' | '12h' | '24h';

export type EventType = 'Food' | 'Insulin' | 'Exercise' | 'Note';

export interface PagedResponseDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// Chart
export interface GlucosePointDto {
  timestampUtc: string; // ISO 8601 UTC
  mgdl: number | null; // null to create a gap/break
}

export interface OverlayEventMarkerDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  icon: string; // name or URL (frontend maps types to icons/colors)
  color: string; // hex
}

export interface ChartDataResponseDto {
  glucose: GlucosePointDto[];
  overlays: OverlayEventMarkerDto[];
  rangeSeconds: number; // echoed by server or computed client
}

export interface TimeInRangeResponseDto {
  timeInRangePercentage: number; // 0..100
  totalReadings: number;
  readingsInRange: number;
  readingsBelowRange: number;
  readingsAboveRange: number;
  targetLowMgdl: number; // e.g., 70
  targetHighMgdl: number; // e.g., 180
}

// Events (list and details)
export interface EventListItemDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  summary: string; // e.g., "Food: 45g" or "Insulin: 3U (fast)"
  tags?: string[];
}

export interface EventResponseDto {
  eventId: string;
  type: EventType;
  timestampUtc: string;
  // Food
  carbs_g?: number;
  meal_tag?: string;
  absorption_hint?: string;
  note?: string;
  // Insulin
  insulin_units?: number; // 0..100 in 0.5 steps
  preparation?: string;
  delivery?: string;
  timing?: string;
  // Exercise
  exercise_type?: string;
  duration_min?: number; // 1..300
  intensity?: string;
  start_time_utc?: string;
}

export interface EventOutcomeResponseDto {
  eventId: string;
  available: boolean; // true if glucose exists within ±5 min of +120 min
  outcomeTimestampUtc?: string;
  glucoseMgdl?: number; // present only if available
}

// Create Event payloads
export interface CreateFoodEventRequestDto {
  carbs_g: number;
  meal_tag?: string;
  absorption_hint?: string;
  note?: string; // ≤500
}

export interface CreateInsulinEventRequestDto {
  insulin_units: number; // 0..100, step 0.5
  preparation?: string;
  delivery?: string;
  timing?: string;
  note?: string; // ≤500
}

export interface CreateExerciseEventRequestDto {
  type: string;
  duration_min: number; // 1..300
  intensity?: string;
  start_time_utc?: string; // not in future
}

export interface CreateNoteEventRequestDto {
  note: string; // ≤500
}

// View models
export interface HistoryFilters {
  fromDateUtc?: string; // ISO, start inclusive
  toDateUtc?: string; // ISO, end inclusive
  types?: EventType[];
  page: number;
  pageSize: number; // ≤100
}

export type PollState =
  | { status: 'idle' }
  | { status: 'ok'; lastFetchedAt: Date }
  | { status: 'paused' }
  | { status: 'backoff'; nextRetryAt: Date; attempt: number }
  | { status: 'error'; message: string };
