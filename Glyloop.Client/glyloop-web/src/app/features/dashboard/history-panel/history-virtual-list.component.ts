import { Component, ChangeDetectionStrategy, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { EventListItemDto } from '../../../core/models/dashboard.types';

/**
 * Virtual scrolling list component for event history.
 * Uses Angular CDK Virtual Scroll for performance with large lists.
 */
@Component({
  selector: 'app-history-virtual-list',
  standalone: true,
  imports: [CommonModule, ScrollingModule, MatListModule, MatIconModule],
  templateUrl: './history-virtual-list.component.html',
  styleUrl: './history-virtual-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryVirtualListComponent {
  // Inputs
  readonly items = input.required<EventListItemDto[]>();
  readonly selectedEventId = input<string | undefined>(undefined);

  // Output
  readonly rowActivate = output<string>();

  // Item height for virtual scroll
  readonly itemHeight = 72; // pixels

  // Track focused index for keyboard navigation
  private readonly focusedIndex = signal<number>(-1);

  // Localized strings
  readonly noItemsMessage = $localize`:@@dashboard.history.list.noItems:No events found`;
  readonly eventTypeAriaPrefix = $localize`:@@dashboard.history.list.eventType:Event type:`;

  constructor() {
    // Reset focused index when items change
    effect(() => {
      const itemList = this.items();
      if (itemList.length === 0) {
        this.focusedIndex.set(-1);
      }
    });
  }

  /**
   * Handles row click
   */
  onRowClick(event: EventListItemDto): void {
    this.rowActivate.emit(event.eventId);
  }

  /**
   * Handles keyboard navigation
   */
  onKeyDown(event: KeyboardEvent, index: number): void {
    const itemList = this.items();
    const maxIndex = itemList.length - 1;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (index < maxIndex) {
          this.focusedIndex.set(index + 1);
          this.focusItemAtIndex(index + 1);
        }
        break;

      case 'ArrowUp':
        event.preventDefault();
        if (index > 0) {
          this.focusedIndex.set(index - 1);
          this.focusItemAtIndex(index - 1);
        }
        break;

      case 'Enter':
      case ' ':
        event.preventDefault();
        this.rowActivate.emit(itemList[index].eventId);
        break;

      case 'Home':
        event.preventDefault();
        this.focusedIndex.set(0);
        this.focusItemAtIndex(0);
        break;

      case 'End':
        event.preventDefault();
        this.focusedIndex.set(maxIndex);
        this.focusItemAtIndex(maxIndex);
        break;
    }
  }

  /**
   * Focuses an item at a specific index
   */
  private focusItemAtIndex(index: number): void {
    setTimeout(() => {
      const element = document.querySelector(
        `.history-list-item[data-index="${index}"]`
      ) as HTMLElement;
      if (element) {
        element.focus();
      }
    }, 0);
  }

  /**
   * Checks if an item is selected
   */
  isSelected(eventId: string): boolean {
    return this.selectedEventId() === eventId;
  }

  /**
   * Gets the icon for an event type
   */
  getEventTypeIcon(type: EventListItemDto['eventType']): string {
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

  /**
   * Gets the color class for an event type
   */
  getEventTypeColorClass(type: EventListItemDto['eventType']): string {
    switch (type) {
      case 'Food':
        return 'event-type-food';
      case 'Insulin':
        return 'event-type-insulin';
      case 'Exercise':
        return 'event-type-exercise';
      case 'Note':
        return 'event-type-note';
      default:
        return '';
    }
  }

  /**
   * Formats timestamp for display
   */
  formatTimestamp(timestampUtc: string): string {
    const date = new Date(timestampUtc);
    return date.toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Tracks items by ID for performance
   */
  trackByEventId(index: number, item: EventListItemDto): string {
    return item.eventId;
  }
}
