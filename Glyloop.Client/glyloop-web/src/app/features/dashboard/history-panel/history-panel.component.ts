import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HistoryFilterBarComponent } from './history-filter-bar.component';
import { HistoryVirtualListComponent } from './history-virtual-list.component';
import { EventDetailsDrawerComponent } from './event-details-drawer.component';
import {
  HistoryFilters,
  EventListItemDto,
  EventResponseDto,
  PagedResponseDto
} from '../../../core/models/dashboard.types';
import { EventsService } from '../../../core/services/events.service';
import { catchError, of } from 'rxjs';

/**
 * History panel container component.
 * Coordinates filter bar, virtual list, and event details drawer.
 */
@Component({
  selector: 'app-history-panel',
  standalone: true,
  imports: [
    CommonModule,
    HistoryFilterBarComponent,
    HistoryVirtualListComponent,
    EventDetailsDrawerComponent
  ],
  templateUrl: './history-panel.component.html',
  styleUrl: './history-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryPanelComponent {
  private readonly eventsService = inject(EventsService);

  // Inputs
  readonly initialFilters = input.required<HistoryFilters>();
  readonly selectedEventId = input<string | undefined>(undefined);

  // Outputs
  readonly select = output<string>();
  readonly filtersChange = output<HistoryFilters>();

  // State
  readonly currentFilters = signal<HistoryFilters>({ page: 1, pageSize: 50 });
  readonly events = signal<EventListItemDto[]>([]);
  readonly selectedEvent = signal<EventResponseDto | undefined>(undefined);
  readonly loading = signal<boolean>(false);
  readonly error = signal<string | undefined>(undefined);

  // Pagination state
  readonly totalPages = signal<number>(0);
  readonly totalItems = signal<number>(0);

  // Localized strings
  readonly title = $localize`:@@dashboard.history.title:Event History`;
  readonly loadingMessage = $localize`:@@dashboard.history.loading:Loading events...`;
  readonly errorMessage = $localize`:@@dashboard.history.error:Failed to load events`;

  constructor() {
    // Initialize with input filters
    effect(() => {
      const filters = this.initialFilters();
      this.currentFilters.set(filters);
      this.fetchEvents(filters);
    });

    // Fetch selected event details when selection changes
    effect(() => {
      const eventId = this.selectedEventId();
      if (eventId) {
        this.fetchEventDetails(eventId);
      } else {
        this.selectedEvent.set(undefined);
      }
    });
  }

  /**
   * Fetches events list with filters
   */
  private fetchEvents(filters: HistoryFilters): void {
    this.loading.set(true);
    this.error.set(undefined);

    this.eventsService
      .list(filters)
      .pipe(
        catchError((err) => {
          this.error.set(this.errorMessage);
          return of(null);
        })
      )
      .subscribe((response) => {
        this.loading.set(false);
        if (response) {
          this.events.set(response.items);
          this.totalPages.set(response.totalPages);
          this.totalItems.set(response.totalItems);
        }
      });
  }

  /**
   * Fetches event details by ID
   */
  private fetchEventDetails(eventId: string): void {
    this.eventsService
      .get(eventId)
      .pipe(
        catchError((err) => {
          console.error('Failed to fetch event details:', err);
          return of(undefined);
        })
      )
      .subscribe((event) => {
        this.selectedEvent.set(event);
      });
  }

  /**
   * Handles filter changes
   */
  onFiltersChange(filters: HistoryFilters): void {
    this.currentFilters.set(filters);
    this.fetchEvents(filters);
    this.filtersChange.emit(filters);
  }

  /**
   * Handles filter reset
   */
  onFiltersReset(): void {
    const defaultFilters: HistoryFilters = {
      page: 1,
      pageSize: 50
    };
    this.currentFilters.set(defaultFilters);
    this.fetchEvents(defaultFilters);
    this.filtersChange.emit(defaultFilters);
  }

  /**
   * Handles row selection
   */
  onRowSelect(eventId: string): void {
    this.select.emit(eventId);
  }

  /**
   * Handles details drawer close
   */
  onDetailsClose(): void {
    this.select.emit(undefined!);
  }

  /**
   * Refreshes the current view
   */
  refresh(): void {
    this.fetchEvents(this.currentFilters());
  }
}
