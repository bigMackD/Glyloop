import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
  Observable,
  BehaviorSubject,
  timer,
  throwError,
  EMPTY,
  Subject,
  retryWhen,
  mergeMap,
  tap,
  catchError,
  takeUntil,
  share
} from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import {
  ChartRange,
  ChartDataResponseDto,
  TimeInRangeResponseDto
} from '../models/dashboard.types';

/**
 * Service for fetching chart data and managing polling with exponential backoff.
 * Implements 5-minute polling interval with pause on page visibility hidden.
 */
@Injectable({ providedIn: 'root' })
export class ChartDataService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(API_CONFIG);

  // Streams
  private readonly _chartData$ = new BehaviorSubject<ChartDataResponseDto | null>(null);
  private readonly _tir$ = new BehaviorSubject<TimeInRangeResponseDto | null>(null);
  private readonly _stopPolling$ = new Subject<void>();

  readonly chartData$ = this._chartData$.asObservable();
  readonly tir$ = this._tir$.asObservable();

  // ETag cache for conditional requests (optional optimization)
  private chartDataETag: string | null = null;
  private tirETag: string | null = null;

  // Polling configuration
  private readonly POLL_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes
  private readonly MAX_BACKOFF_MS = 30 * 60 * 1000; // 30 minutes
  private isPolling = false;
  private currentRange: ChartRange | null = null;
  private visibilityListenerSetup = false;

  constructor() {
    // Set up visibility listener once
    this.setupVisibilityListener();
  }

  private buildUrl(endpoint: string): string {
    return `${this.apiConfig.baseUrl}${endpoint}`;
  }

  /**
   * Fetches chart data for the specified range
   */
  fetchChartData(range: ChartRange): Observable<ChartDataResponseDto> {
    const url = this.buildUrl(`/api/chart/data?range=${range}`);
    const headers: { [key: string]: string } = {};

    if (this.chartDataETag) {
      headers['If-None-Match'] = this.chartDataETag;
    }

    return this.http
      .get<ChartDataResponseDto>(url, {
        headers,
        observe: 'response',
        withCredentials: true
      })
      .pipe(
        tap((response) => {
          // Store ETag if provided
          const etag = response.headers.get('ETag');
          if (etag) {
            this.chartDataETag = etag;
          }

          // Handle 304 Not Modified
          if (response.status === 304) {
            // Keep existing data
            return;
          }

          // Update stream with new data
          if (response.body) {
            this._chartData$.next(response.body);
          }
        }),
        mergeMap((response) => {
          if (response.body) {
            return [response.body];
          }
          return EMPTY;
        })
      );
  }

  /**
   * Fetches time-in-range data for the specified range
   */
  fetchTir(range: ChartRange): Observable<TimeInRangeResponseDto> {
    const url = this.buildUrl(`/api/chart/tir?range=${range}`);
    const headers: { [key: string]: string } = {};

    if (this.tirETag) {
      headers['If-None-Match'] = this.tirETag;
    }

    return this.http
      .get<TimeInRangeResponseDto>(url, {
        headers,
        observe: 'response',
        withCredentials: true
      })
      .pipe(
        tap((response) => {
          const etag = response.headers.get('ETag');
          if (etag) {
            this.tirETag = etag;
          }

          if (response.status === 304) {
            return;
          }

          if (response.body) {
            this._tir$.next(response.body);
          }
        }),
        mergeMap((response) => {
          if (response.body) {
            return [response.body];
          }
          return EMPTY;
        })
      );
  }

  /**
   * Starts polling for chart data and TIR at the specified range.
   * Automatically pauses when page is hidden and resumes when visible.
   */
  start(range: ChartRange, options?: { skipInitialFetch?: boolean }): void {
    if (this.isPolling && this.currentRange === range) {
      return; // Already polling this range
    }

    this.stop(); // Stop any existing polling
    this.isPolling = true;
    this.currentRange = range;

    // Initial fetch
    if (!options?.skipInitialFetch) {
      this.fetchBoth(range).subscribe();
    }

    // Set up polling with exponential backoff
    timer(this.POLL_INTERVAL_MS, this.POLL_INTERVAL_MS)
      .pipe(
        // Stop when requested
        takeUntil(this._stopPolling$),
        // Skip polls when page is hidden
        mergeMap(() => {
          if (document.hidden) {
            return EMPTY; // Skip this poll
          }
          return this.fetchBoth(range);
        }),
        // Retry with exponential backoff on errors
        retryWhen((errors) =>
          errors.pipe(
            mergeMap((error, attempt) => {
              // Calculate backoff: 2^attempt seconds with jitter, capped at MAX_BACKOFF
              const baseDelay = Math.min(
                Math.pow(2, attempt) * 1000,
                this.MAX_BACKOFF_MS
              );
              const jitter = Math.random() * 1000; // 0-1 second jitter
              const delay = baseDelay + jitter;

              console.warn(`Chart polling error, retry in ${delay}ms:`, error);
              return timer(delay);
            })
          )
        )
      )
      .subscribe({
        error: (err) => console.error('Chart polling stopped due to error:', err)
      });
  }

  /**
   * Stops polling
   */
  stop(): void {
    this.isPolling = false;
    this.currentRange = null;
    this._stopPolling$.next();
  }

  /**
   * Fetches both chart data and TIR in parallel
   */
  private fetchBoth(range: ChartRange): Observable<unknown> {
    return new Observable((observer) => {
      let chartComplete = false;
      let tirComplete = false;
      let hasError = false;

      const checkComplete = () => {
        if (chartComplete && tirComplete) {
          observer.complete();
        }
      };

      this.fetchChartData(range).subscribe({
        next: () => {
          chartComplete = true;
          checkComplete();
        },
        error: (err) => {
          if (!hasError) {
            hasError = true;
            observer.error(err);
          }
        }
      });

      this.fetchTir(range).subscribe({
        next: () => {
          tirComplete = true;
          checkComplete();
        },
        error: (err) => {
          if (!hasError) {
            hasError = true;
            observer.error(err);
          }
        }
      });
    }).pipe(share());
  }

  /**
   * Sets up page visibility listener to pause/resume polling (only once)
   */
  private setupVisibilityListener(): void {
    if (this.visibilityListenerSetup) return;

    this.visibilityListenerSetup = true;
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        console.log('Page hidden, polling paused');
      } else {
        console.log('Page visible, polling resumed');
        // Trigger immediate fetch when page becomes visible
        if (this.isPolling && this.currentRange) {
          this.fetchBoth(this.currentRange).subscribe();
        }
      }
    });
  }

  /**
   * Clears cached data and ETags
   */
  clearCache(): void {
    this._chartData$.next(null);
    this._tir$.next(null);
    this.chartDataETag = null;
    this.tirETag = null;
  }
}
