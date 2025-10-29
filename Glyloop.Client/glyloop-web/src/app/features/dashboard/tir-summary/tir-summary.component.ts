import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimeInRangeResponseDto } from '../../../core/models/dashboard.types';

/**
 * Time-in-Range summary component.
 * Displays TIR percentage, breakdown counts, and target range.
 */
@Component({
  selector: 'app-tir-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tir-summary.component.html',
  styleUrl: './tir-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TirSummaryComponent {
  // Input
  readonly tir = input.required<TimeInRangeResponseDto | null>();

  // Computed values
  readonly tirPercentage = computed(() => {
    const data = this.tir();
    return data ? Math.round(data.timeInRangePercentage) : 0;
  });

  readonly belowPercentage = computed(() => {
    const data = this.tir();
    if (!data || data.totalReadings === 0) return 0;
    return Math.round((data.readingsBelowRange / data.totalReadings) * 100);
  });

  readonly abovePercentage = computed(() => {
    const data = this.tir();
    if (!data || data.totalReadings === 0) return 0;
    return Math.round((data.readingsAboveRange / data.totalReadings) * 100);
  });

  readonly targetRangeText = computed(() => {
    const data = this.tir();
    if (!data) return '';
    return `${data.targetLowMgdl}-${data.targetHighMgdl} mg/dL`;
  });

  // Localized strings
  readonly title = $localize`:@@dashboard.tir.title:Time in Range`;
  readonly inRangeLabel = $localize`:@@dashboard.tir.inRange:In Range`;
  readonly belowRangeLabel = $localize`:@@dashboard.tir.belowRange:Below Range`;
  readonly aboveRangeLabel = $localize`:@@dashboard.tir.aboveRange:Above Range`;
  readonly targetRangeLabel = $localize`:@@dashboard.tir.targetRange:Target Range`;
  readonly totalReadingsLabel = $localize`:@@dashboard.tir.totalReadings:Total Readings`;
  readonly noDataMessage = $localize`:@@dashboard.tir.noData:No data available`;

  /**
   * Gets the CSS class for TIR percentage badge based on value
   */
  getTirBadgeClass(): string {
    const percentage = this.tirPercentage();

    if (percentage >= 70) {
      return 'tir-good'; // Green
    } else if (percentage >= 50) {
      return 'tir-moderate'; // Yellow
    } else {
      return 'tir-poor'; // Red
    }
  }

  /**
   * Gets tooltip text explaining TIR calculation
   */
  getTirTooltip(): string {
    const data = this.tir();
    if (!data) return this.noDataMessage;

    return $localize`:@@dashboard.tir.tooltip:Time in Range is calculated from ${data.readingsInRange}:inRange: of ${data.totalReadings}:total: readings within ${this.targetRangeText()}:range:.`;
  }

  /**
   * Gets the color for the circular progress indicator
   */
  getProgressColor(): string {
    const percentage = this.tirPercentage();
    
    if (percentage >= 70) {
      return '#10b981'; // Green
    } else if (percentage >= 50) {
      return '#f59e0b'; // Yellow/Orange
    } else {
      return '#ef4444'; // Red
    }
  }

  /**
   * Gets the stroke-dasharray for the circular progress (circumference)
   */
  getProgressDashArray(): string {
    const radius = 70;
    const circumference = 2 * Math.PI * radius;
    return `${circumference} ${circumference}`;
  }

  /**
   * Gets the stroke-dashoffset for the circular progress
   */
  getProgressDashOffset(): number {
    const radius = 70;
    const circumference = 2 * Math.PI * radius;
    const percentage = this.tirPercentage();
    const offset = circumference - (percentage / 100) * circumference;
    return offset;
  }
}
