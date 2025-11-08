import { Component, ChangeDetectionStrategy, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ChartToolbarComponent } from './chart-toolbar/chart-toolbar.component';
import { CgmChartComponent } from './cgm-chart/cgm-chart.component';
import { TirSummaryComponent } from './tir-summary/tir-summary.component';
import { HistoryPanelComponent } from './history-panel/history-panel.component';
import { AddEventModalComponent } from './add-event-modal/add-event-modal.component';
import { DashboardStateService } from '../../core/services/dashboard-state.service';
import { ChartDataService } from '../../core/services/chart-data.service';
import { EventsService } from '../../core/services/events.service';
import {
  ChartRange,
  HistoryFilters
} from '../../core/models/dashboard.types';
import { catchError, of } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

/**
 * Dashboard page component - main orchestrator for the dashboard view.
 * Coordinates chart, TIR summary, history panel, and event creation.
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    MatSnackBarModule,
    ChartToolbarComponent,
    CgmChartComponent,
    TirSummaryComponent,
    HistoryPanelComponent,
    AddEventModalComponent
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  private readonly dashboardState = inject(DashboardStateService);
  private readonly chartDataService = inject(ChartDataService);
  private readonly eventsService = inject(EventsService);
  private readonly snackBar = inject(MatSnackBar);

  // State from services as signals
  readonly activeRange = this.dashboardState.activeRange;
  readonly pollState = this.dashboardState.pollState;
  readonly selectedEventId = this.dashboardState.selectedEventId;

  // Convert observables to signals
  readonly chartData = toSignal(this.chartDataService.chartData$, { initialValue: null });
  readonly tir = toSignal(this.chartDataService.tir$, { initialValue: null });

  // Local state
  readonly addEventModalOpen = signal<boolean>(false);
  readonly historyFilters = signal<HistoryFilters>({
    page: 1,
    pageSize: 50
  });

  // Localized strings
  readonly pageTitle = $localize`:@@dashboard.title:Dashboard`;
  readonly addEventButton = $localize`:@@dashboard.addEvent:Add Event`;
  readonly eventCreatedMessage = $localize`:@@dashboard.eventCreated:Event created successfully`;
  readonly chartErrorMessage = $localize`:@@dashboard.chartError:Failed to load chart data`;
  readonly historyTitle = $localize`:@@dashboard.historyTitle:Event History`;

  ngOnInit(): void {
    // Start polling for chart data
    this.startPolling();

    // Set poll state to ok initially
    this.dashboardState.setPollState({ status: 'ok', lastFetchedAt: new Date() });
  }

  ngOnDestroy(): void {
    // Stop polling when component is destroyed
    this.chartDataService.stop();
    this.dashboardState.reset();
  }

  /**
   * Starts polling for chart data
   */
  private startPolling(): void {
    const range = this.activeRange();
    this.chartDataService.start(range);
  }

  /**
   * Handles range change from toolbar
   */
  onRangeChange(range: ChartRange): void {
    this.dashboardState.setRange(range);

    // Fetch new data for the selected range
    this.dashboardState.setPollState({ status: 'ok', lastFetchedAt: new Date() });

    this.chartDataService
      .fetchChartData(range)
      .pipe(
        catchError((err) => {
          this.handleChartError(err);
          return of(null);
        })
      )
      .subscribe();

    this.chartDataService
      .fetchTir(range)
      .pipe(
        catchError((err) => {
          console.error('Failed to fetch TIR:', err);
          return of(null);
        })
      )
      .subscribe();

    // Restart polling with new range
    this.chartDataService.start(range, { skipInitialFetch: true });
  }

  /**
   * Handles manual refresh request
   */
  onManualRefresh(): void {
    const range = this.activeRange();
    this.dashboardState.setPollState({ status: 'ok', lastFetchedAt: new Date() });

    this.chartDataService
      .fetchChartData(range)
      .pipe(
        catchError((err) => {
          this.handleChartError(err);
          return of(null);
        })
      )
      .subscribe(() => {
        this.showSnackbar($localize`:@@dashboard.refreshed:Data refreshed`);
      });

    this.chartDataService
      .fetchTir(range)
      .pipe(
        catchError((err) => {
          console.error('Failed to fetch TIR:', err);
          return of(null);
        })
      )
      .subscribe();
  }

  /**
   * Handles event selection from chart or history
   */
  onEventSelect(eventId: string): void {
    this.dashboardState.selectEvent(eventId);
  }

  /**
   * Handles crosshair movement on chart
   */
  onCrosshairMove(timestampUtc: string): void {
    // Could be used for accessibility announcements or other features
    // For now, we just log it
    console.debug('Crosshair at:', timestampUtc);
  }

  /**
   * Handles history filter changes
   */
  onHistoryFiltersChange(filters: HistoryFilters): void {
    this.historyFilters.set(filters);
  }

  /**
   * Handles event selection from history panel
   * Recenters chart to ±30 minutes around the event and highlights it
   */
  onHistorySelect(eventId: string): void {
    this.dashboardState.selectEvent(eventId);

    // Find the event in chart data to get its timestamp
    const data = this.chartData();
    if (data) {
      const eventMarker = data.eventOverlays.find((overlay) => overlay.eventId === eventId);
      if (eventMarker) {
        // TODO: Implement chart recentering logic
        // For MVP, we just highlight the event marker
        console.log('Selected event at:', eventMarker.timestamp);
      }
    }
  }

  /**
   * Opens the Add Event modal
   */
  openAddEventModal(): void {
    this.addEventModalOpen.set(true);
  }

  /**
   * Closes the Add Event modal
   */
  closeAddEventModal(): void {
    this.addEventModalOpen.set(false);
  }

  /**
   * Handles successful event creation
   */
  onEventCreated(): void {
    this.showSnackbar(this.eventCreatedMessage);

    // Refresh chart data to include the new event
    const range = this.activeRange();
    this.chartDataService.fetchChartData(range).subscribe();
    this.chartDataService.fetchTir(range).subscribe();

    // Refresh history (maintain current filters but refetch)
    // The HistoryPanelComponent will handle this through its service subscription
    this.eventsService.list(this.historyFilters()).subscribe();
  }

  /**
   * Handles chart errors
   */
  private handleChartError(err: { status?: number }): void {
    console.error('Chart data error:', err);

    if (err.status === 429 || err.status >= 500) {
      // Rate limit or server error - trigger backoff
      const nextRetryAt = new Date(Date.now() + 60000); // Retry in 1 minute
      this.dashboardState.setPollState({
        status: 'backoff',
        nextRetryAt,
        attempt: 1
      });
    } else {
      this.dashboardState.setPollState({
        status: 'error',
        message: this.chartErrorMessage
      });
    }

    this.showSnackbar(this.chartErrorMessage, 'error');
  }

  /**
   * Shows a snackbar notification
   */
  private showSnackbar(message: string, type: 'success' | 'error' = 'success'): void {
    this.snackBar.open(message, $localize`:@@common.close:Close`, {
      duration: type === 'success' ? 3000 : 5000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error'
    });
  }
}
