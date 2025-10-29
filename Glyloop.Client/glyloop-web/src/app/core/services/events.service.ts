import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import {
  EventListItemDto,
  EventResponseDto,
  EventOutcomeResponseDto,
  PagedResponseDto,
  HistoryFilters,
  CreateFoodEventRequestDto,
  CreateInsulinEventRequestDto,
  CreateExerciseEventRequestDto,
  CreateNoteEventRequestDto
} from '../models/dashboard.types';

/**
 * Service for managing events (Food, Insulin, Exercise, Notes).
 * Provides CRUD operations and maintains an ephemeral cache for the current history page.
 */
@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(API_CONFIG);

  // Ephemeral cache for current history page
  private readonly _historyPage$ = new BehaviorSubject<PagedResponseDto<EventListItemDto> | null>(
    null
  );
  readonly historyPage$ = this._historyPage$.asObservable();

  private buildUrl(endpoint: string): string {
    return `${this.apiConfig.baseUrl}${endpoint}`;
  }

  /**
   * Lists events with optional filters
   */
  list(filters: HistoryFilters): Observable<PagedResponseDto<EventListItemDto>> {
    let params = new HttpParams()
      .set('page', filters.page.toString())
      .set('pageSize', Math.min(filters.pageSize, 100).toString()); // Cap at 100

    if (filters.fromDateUtc) {
      params = params.set('fromDate', filters.fromDateUtc);
    }

    if (filters.toDateUtc) {
      params = params.set('toDate', filters.toDateUtc);
    }

    if (filters.types && filters.types.length > 0) {
      // If API supports multiple types, send as comma-separated or multiple params
      // Assuming API accepts eventType multiple times
      filters.types.forEach((type) => {
        params = params.append('eventType', type);
      });
    }

    return this.http
      .get<PagedResponseDto<EventListItemDto>>(this.buildUrl('/api/events'), {
        params,
        withCredentials: true
      })
      .pipe(
        tap((response) => {
          // Update cache
          this._historyPage$.next(response);
        })
      );
  }

  /**
   * Gets a single event by ID
   */
  get(id: string): Observable<EventResponseDto> {
    return this.http.get<EventResponseDto>(this.buildUrl(`/api/events/${id}`), {
      withCredentials: true
    });
  }

  /**
   * Gets the outcome (2-hour post-meal glucose) for a Food event
   */
  getOutcome(id: string): Observable<EventOutcomeResponseDto> {
    return this.http.get<EventOutcomeResponseDto>(this.buildUrl(`/api/events/${id}/outcome`), {
      withCredentials: true
    });
  }

  /**
   * Creates a Food event
   */
  createFood(body: CreateFoodEventRequestDto): Observable<EventResponseDto> {
    return this.http.post<EventResponseDto>(this.buildUrl('/api/events/food'), body, {
      withCredentials: true
    });
  }

  /**
   * Creates an Insulin event
   */
  createInsulin(body: CreateInsulinEventRequestDto): Observable<EventResponseDto> {
    return this.http.post<EventResponseDto>(this.buildUrl('/api/events/insulin'), body, {
      withCredentials: true
    });
  }

  /**
   * Creates an Exercise event
   */
  createExercise(body: CreateExerciseEventRequestDto): Observable<EventResponseDto> {
    return this.http.post<EventResponseDto>(this.buildUrl('/api/events/exercise'), body, {
      withCredentials: true
    });
  }

  /**
   * Creates a Note event
   */
  createNote(body: CreateNoteEventRequestDto): Observable<EventResponseDto> {
    return this.http.post<EventResponseDto>(this.buildUrl('/api/events/note'), body, {
      withCredentials: true
    });
  }

  /**
   * Clears the cached history page
   */
  clearCache(): void {
    this._historyPage$.next(null);
  }
}
