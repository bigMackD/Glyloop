import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  viewChild,
  effect,
  ElementRef,
  OnDestroy,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  Chart,
  ChartConfiguration,
  ChartData,
  TimeSeriesScale,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
  Legend,
  Filler,
  ScatterController
} from 'chart.js';
import 'chartjs-adapter-date-fns';
import {
  ChartDataResponseDto,
  ChartRange,
  OverlayEventMarkerDto
} from '../../../core/models/dashboard.types';

// Register Chart.js components
Chart.register(
  TimeSeriesScale,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
  Legend,
  Filler,
  ScatterController
);

/**
 * CGM Chart component using Chart.js v4.
 * Renders glucose timeseries with gaps, event overlays, and interactive crosshair.
 */
@Component({
  selector: 'app-cgm-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cgm-chart.component.html',
  styleUrl: './cgm-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CgmChartComponent implements OnDestroy {
  // Inputs
  readonly chartData = input.required<ChartDataResponseDto | null>();
  readonly highlightEventId = input<string | undefined>(undefined);
  readonly range = input.required<ChartRange>();

  // Outputs
  readonly eventSelect = output<string>();
  readonly crosshairMove = output<string>(); // timestampUtc

  // Template refs
  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('chartCanvas');

  // Chart instance
  private chart: Chart | null = null;
  readonly currentCrosshairIndex = signal<number>(-1);

  // Crosshair state
  private crosshairPosition: { x: number; y: number } | null = null;

  // Localized strings
  readonly glucoseLabel = $localize`:@@dashboard.chart.glucoseLabel:Glucose (mg/dL)`;
  readonly noDataMessage = $localize`:@@dashboard.chart.noData:No glucose data available for this time range`;

  constructor() {
    // React to chart data changes
    effect(() => {
      const data = this.chartData();
      if (data) {
        this.updateChart(data);
      }
    });

    // React to highlight changes
    effect(() => {
      const eventId = this.highlightEventId();
      this.updateHighlight(eventId);
    });
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  /**
   * Initializes or updates the Chart.js instance
   */
  private updateChart(data: ChartDataResponseDto): void {
    const canvas = this.canvasRef()?.nativeElement;
    if (!canvas) return;

    if (!this.chart) {
      this.createChart(canvas, data);
    } else {
      this.updateChartData(data);
    }
  }

  /**
   * Creates a new Chart.js instance
   */
  private createChart(canvas: HTMLCanvasElement, data: ChartDataResponseDto): void {
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const chartData = this.prepareChartData(data);
    const config: ChartConfiguration = {
      type: 'line',
      data: chartData,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          mode: 'index',
          intersect: false
        },
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            enabled: true,
            callbacks: {
              title: (context) => {
                const xValue = context[0].parsed.x;
                if (xValue === null || xValue === undefined) return '';
                const date = new Date(xValue);
                return date.toLocaleString();
              },
              label: (context) => {
                if (context.dataset.label === this.glucoseLabel) {
                  return `${context.dataset.label}: ${context.parsed.y} mg/dL`;
                }
                return context.dataset.label || '';
              }
            }
          }
        },
        scales: {
          x: {
            type: 'timeseries',
            time: {
              displayFormats: {
                hour: 'HH:mm',
                minute: 'HH:mm'
              }
            },
            title: {
              display: true,
              text: 'Time'
            }
          },
          y: {
            type: 'linear',
            suggestedMin: 50,
            suggestedMax: 350,
            title: {
              display: true,
              text: this.glucoseLabel
            },
            ticks: {
              callback: (value) => `${value}`
            }
          }
        },
        onClick: (event, elements) => {
          this.handleChartClick(event, elements);
        },
        onHover: (event) => {
          this.handleChartHover(event);
        }
      }
    };

    this.chart = new Chart(ctx, config);
    this.setupKeyboardNavigation(canvas);
  }

  /**
   * Prepares Chart.js data structure from API response
   */
  private prepareChartData(data: ChartDataResponseDto): ChartData {
    // Glucose line dataset
    const glucoseData = data.glucose.map((point) => ({
      x: new Date(point.timestampUtc).getTime(),
      y: point.mgdl
    }));

    // Event overlay scatter dataset
    const overlayData = data.overlays.map((overlay) => ({
      x: new Date(overlay.timestampUtc).getTime(),
      y: this.getGlucoseAtTime(data, overlay.timestampUtc) || 200, // Default to mid-range if no glucose
      eventId: overlay.eventId,
      type: overlay.type,
      icon: overlay.icon,
      color: overlay.color
    }));

    return {
      datasets: [
        {
          type: 'line',
          label: this.glucoseLabel,
          data: glucoseData,
          borderColor: 'rgb(75, 192, 192)',
          backgroundColor: 'rgba(75, 192, 192, 0.1)',
          borderWidth: 2,
          pointRadius: 2,
          pointHoverRadius: 4,
          tension: 0, // No smoothing
          spanGaps: false, // Show breaks for null values
          fill: true
        },
        {
          type: 'scatter',
          label: 'Events',
          data: overlayData,
          backgroundColor: overlayData.map((d: any) => d.color || '#ff6384'),
          pointRadius: 8,
          pointHoverRadius: 10,
          pointStyle: 'circle'
        } as any
      ]
    };
  }

  /**
   * Gets glucose value at a specific time (or nearest)
   */
  private getGlucoseAtTime(data: ChartDataResponseDto, timestampUtc: string): number | null {
    const targetTime = new Date(timestampUtc).getTime();
    let closest: { time: number; value: number | null } | null = null;
    let minDiff = Infinity;

    for (const point of data.glucose) {
      const pointTime = new Date(point.timestampUtc).getTime();
      const diff = Math.abs(pointTime - targetTime);

      if (diff < minDiff && point.mgdl !== null) {
        minDiff = diff;
        closest = { time: pointTime, value: point.mgdl };
      }
    }

    return closest?.value || null;
  }

  /**
   * Updates existing chart with new data
   */
  private updateChartData(data: ChartDataResponseDto): void {
    if (!this.chart) return;

    const newData = this.prepareChartData(data);
    this.chart.data = newData;
    this.chart.update('none'); // Update without animation for performance
  }

  /**
   * Updates highlight for selected event
   */
  private updateHighlight(eventId: string | undefined): void {
    if (!this.chart) return;

    const overlayDataset = this.chart.data.datasets[1] as any;
    if (!overlayDataset) return;

    // Reset all point radii
    const radii = overlayDataset.data.map((point: any) =>
      point.eventId === eventId ? 12 : 8
    );
    overlayDataset.pointRadius = radii;

    this.chart.update('none');
  }

  /**
   * Handles chart click events (for overlay markers)
   */
  private handleChartClick(event: any, elements: any[]): void {
    if (elements.length === 0) return;

    const element = elements[0];
    const datasetIndex = element.datasetIndex;

    // Check if clicked on overlay (events) dataset
    if (datasetIndex === 1) {
      const dataIndex = element.index;
      const point = this.chart?.data.datasets[1].data[dataIndex] as any;

      if (point?.eventId) {
        this.eventSelect.emit(point.eventId);
      }
    }
  }

  /**
   * Handles chart hover for crosshair
   */
  private handleChartHover(event: any): void {
    if (!this.chart || !event.native) return;

    // Get relative position manually
    const rect = (event.native.target as HTMLCanvasElement).getBoundingClientRect();
    const x = event.native.clientX - rect.left;
    const y = event.native.clientY - rect.top;
    this.crosshairPosition = { x, y };

    // Find nearest point
    const nearestIndex = this.findNearestDataPoint(x);
    if (nearestIndex >= 0) {
      this.currentCrosshairIndex.set(nearestIndex);
      const data = this.chart.data.datasets[0].data[nearestIndex] as any;
      if (data?.x) {
        const timestamp = new Date(data.x).toISOString();
        this.crosshairMove.emit(timestamp);
      }
    }
  }

  /**
   * Finds the nearest data point to the given x position
   */
  private findNearestDataPoint(x: number): number {
    if (!this.chart) return -1;

    const dataset = this.chart.data.datasets[0];
    if (!dataset?.data) return -1;

    let nearestIndex = -1;
    let minDistance = Infinity;

    dataset.data.forEach((point: any, index) => {
      const meta = this.chart!.getDatasetMeta(0);
      const element = meta.data[index];

      if (element) {
        const distance = Math.abs(element.x - x);
        if (distance < minDistance) {
          minDistance = distance;
          nearestIndex = index;
        }
      }
    });

    return nearestIndex;
  }

  /**
   * Sets up keyboard navigation for crosshair
   */
  private setupKeyboardNavigation(canvas: HTMLCanvasElement): void {
    canvas.tabIndex = 0; // Make canvas focusable
    canvas.setAttribute('role', 'img');
    canvas.setAttribute('aria-label', 'Glucose chart with keyboard navigation');

    canvas.addEventListener('keydown', (event) => {
      if (!this.chart) return;

      const currentIndex = this.currentCrosshairIndex();
      const dataLength = this.chart.data.datasets[0].data.length;

      let newIndex = currentIndex;

      switch (event.key) {
        case 'ArrowLeft':
          event.preventDefault();
          newIndex = event.shiftKey
            ? Math.max(0, currentIndex - 5) // Shift+Left: jump 5 points (~5 min)
            : Math.max(0, currentIndex - 1); // Left: step 1 point
          break;

        case 'ArrowRight':
          event.preventDefault();
          newIndex = event.shiftKey
            ? Math.min(dataLength - 1, currentIndex + 5)
            : Math.min(dataLength - 1, currentIndex + 1);
          break;

        case 'Enter':
          event.preventDefault();
          this.handleEnterOnCrosshair();
          return;
      }

      if (newIndex !== currentIndex && newIndex >= 0 && newIndex < dataLength) {
        this.currentCrosshairIndex.set(newIndex);
        const data = this.chart.data.datasets[0].data[newIndex] as any;
        if (data?.x) {
          const timestamp = new Date(data.x).toISOString();
          this.crosshairMove.emit(timestamp);
        }
      }
    });
  }

  /**
   * Handles Enter key on crosshair (check if over an event marker)
   */
  private handleEnterOnCrosshair(): void {
    if (!this.chart) return;

    const currentIndex = this.currentCrosshairIndex();
    if (currentIndex < 0) return;

    const glucosePoint = this.chart.data.datasets[0].data[currentIndex] as any;
    if (!glucosePoint?.x) return;

    const currentTime = glucosePoint.x;
    const overlayDataset = this.chart.data.datasets[1];

    // Find event marker at or near current time
    for (const point of overlayDataset.data as any[]) {
      if (Math.abs(point.x - currentTime) < 60000) {
        // Within 1 minute
        if (point.eventId) {
          this.eventSelect.emit(point.eventId);
          break;
        }
      }
    }
  }

  /**
   * Destroys the chart instance
   */
  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }

  /**
   * Checks if chart has data
   */
  hasData(): boolean {
    const data = this.chartData();
    return (data?.glucose?.length ?? 0) > 0;
  }
}
