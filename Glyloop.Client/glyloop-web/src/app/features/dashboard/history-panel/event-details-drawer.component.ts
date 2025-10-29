import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { EventResponseDto, EventOutcomeResponseDto } from '../../../core/models/dashboard.types';
import { EventsService } from '../../../core/services/events.service';
import { catchError, of } from 'rxjs';

/**
 * Event details drawer component.
 * Shows full event details; for Food events, fetches and displays 2-hour outcome.
 */
@Component({
  selector: 'app-event-details-drawer',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './event-details-drawer.component.html',
  styleUrl: './event-details-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventDetailsDrawerComponent {
  private readonly eventsService = inject(EventsService);

  // Input
  readonly event = input<EventResponseDto | undefined>(undefined);

  // Output
  readonly close = output<void>();

  // Outcome state
  readonly outcome = signal<EventOutcomeResponseDto | null>(null);
  readonly outcomeLoading = signal<boolean>(false);
  readonly outcomeError = signal<string | undefined>(undefined);

  // Localized strings
  readonly closeLabel = $localize`:@@dashboard.eventDetails.close:Close`;
  readonly titleLabel = $localize`:@@dashboard.eventDetails.title:Event Details`;
  readonly timestampLabel = $localize`:@@dashboard.eventDetails.timestamp:Time`;
  readonly typeLabel = $localize`:@@dashboard.eventDetails.type:Type`;
  readonly outcomeLabel = $localize`:@@dashboard.eventDetails.outcome:2-Hour Outcome`;
  readonly outcomeNotAvailable = $localize`:@@dashboard.eventDetails.outcomeNotAvailable:Not available`;
  readonly outcomeTooltip = $localize`:@@dashboard.eventDetails.outcomeTooltip:Glucose reading approximately 2 hours after the event (±5 minutes)`;
  readonly retryLabel = $localize`:@@dashboard.eventDetails.retry:Retry`;

  // Food event labels
  readonly carbsLabel = $localize`:@@dashboard.eventDetails.carbs:Carbohydrates`;
  readonly mealTagLabel = $localize`:@@dashboard.eventDetails.mealTag:Meal Tag`;
  readonly absorptionHintLabel = $localize`:@@dashboard.eventDetails.absorptionHint:Absorption`;
  readonly noteLabel = $localize`:@@dashboard.eventDetails.note:Note`;

  // Insulin event labels
  readonly insulinUnitsLabel = $localize`:@@dashboard.eventDetails.insulinUnits:Units`;
  readonly preparationLabel = $localize`:@@dashboard.eventDetails.preparation:Preparation`;
  readonly deliveryLabel = $localize`:@@dashboard.eventDetails.delivery:Delivery`;
  readonly timingLabel = $localize`:@@dashboard.eventDetails.timing:Timing`;

  // Exercise event labels
  readonly exerciseTypeLabel = $localize`:@@dashboard.eventDetails.exerciseType:Exercise Type`;
  readonly durationLabel = $localize`:@@dashboard.eventDetails.duration:Duration`;
  readonly intensityLabel = $localize`:@@dashboard.eventDetails.intensity:Intensity`;
  readonly startTimeLabel = $localize`:@@dashboard.eventDetails.startTime:Start Time`;

  constructor() {
    // Fetch outcome when event changes (if Food)
    effect(() => {
      const currentEvent = this.event();
      if (currentEvent && currentEvent.type === 'Food') {
        this.fetchOutcome(currentEvent.eventId);
      } else {
        this.outcome.set(null);
        this.outcomeError.set(undefined);
      }
    });
  }

  /**
   * Fetches outcome for a Food event
   */
  private fetchOutcome(eventId: string): void {
    this.outcomeLoading.set(true);
    this.outcomeError.set(undefined);

    this.eventsService
      .getOutcome(eventId)
      .pipe(
        catchError((err) => {
          // 404 is expected if outcome not available yet
          if (err.status === 404) {
            this.outcome.set({
              eventId,
              available: false
            });
          } else {
            this.outcomeError.set($localize`:@@dashboard.eventDetails.outcomeError:Failed to load outcome`);
          }
          return of(null);
        })
      )
      .subscribe((result) => {
        this.outcomeLoading.set(false);
        if (result) {
          this.outcome.set(result);
        }
      });
  }

  /**
   * Handles retry button click
   */
  onRetry(): void {
    const currentEvent = this.event();
    if (currentEvent) {
      this.fetchOutcome(currentEvent.eventId);
    }
  }

  /**
   * Closes the drawer
   */
  onClose(): void {
    this.close.emit();
  }

  /**
   * Checks if drawer is open
   */
  isOpen(): boolean {
    return !!this.event();
  }

  /**
   * Formats timestamp for display
   */
  formatTimestamp(timestampUtc: string): string {
    const date = new Date(timestampUtc);
    return date.toLocaleString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Gets icon for event type
   */
  getEventTypeIcon(type: string): string {
    switch (type) {
      case 'Food':
        return 'restaurant';
      case 'Insulin':
        return 'vaccines';
      case 'Exercise':
        return 'fitness_center';
      case 'Note':
        return 'note';
      default:
        return 'event';
    }
  }
}
