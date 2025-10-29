// ============================================================================
// Dashboard Types and DTOs
// Based on dashboard-view-implementation-plan.md section 5
// ============================================================================

// Shared
export type ChartRange = 1 | 3 | 5 | 8 | 12 | 24;

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
  eventType: EventType;
  eventTime: string;
  summary: string;
}

export interface EventResponseDto {
  eventId: string;
  eventType: EventType;
  eventTime: string;
  createdAt: string;
  note?: string;
  carbohydratesGrams?: number;
  mealTagId?: number;
  absorptionHint?: string;
  insulinType?: string;
  insulinUnits?: number;
  preparation?: string;
  delivery?: string;
  timing?: string;
  exerciseTypeId?: number;
  durationMinutes?: number;
  intensity?: string;
  noteText?: string;
}

export interface EventOutcomeResponseDto {
  eventId: string;
  eventTime: string;
  outcomeTime: string;
  glucoseValue?: number;
  isApproximate: boolean;
  message?: string;
}

// Create Event payloads
export interface CreateFoodEventRequestDto {
  eventTime: string;
  carbohydratesGrams: number;
  mealTagId?: number;
  absorptionHint?: string;
  note?: string;
}

export interface CreateInsulinEventRequestDto {
  eventTime: string;
  insulinType: string;
  insulinUnits: number;
  preparation?: string;
  delivery?: string;
  timing?: string;
  note?: string;
}

export interface CreateExerciseEventRequestDto {
  eventTime: string;
  exerciseTypeId: number;
  durationMinutes: number;
  intensity?: string;
  note?: string;
}

export interface CreateNoteEventRequestDto {
  eventTime: string;
  noteText: string;
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
