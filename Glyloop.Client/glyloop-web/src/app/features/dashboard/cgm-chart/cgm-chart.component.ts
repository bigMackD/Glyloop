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
  ChartEvent,
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
import { ChartDataResponseDto, ChartRange } from '../../../core/models/dashboard.types';

interface NormalizedGlucosePoint {
  iso: string;
  epochMs: number;
  value: number | null;
}

interface NormalizedOverlayPoint {
  eventId?: string;
  eventType?: string;
  iso: string;
  epochMs: number;
  icon?: string | null;
  color?: string | null;
  summary?: string | null;
}

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
  readonly upperBoundary = input<number>(180);
  readonly lowerBoundary = input<number>(70);

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

  // Overlay points (kept in component state to avoid typing issues with Chart.js datasets)
  private overlayPointsState: NormalizedOverlayPoint[] = [];
  // Overlay points grouped per dataset (aligned with scatter datasets order)
  private overlayDatasetMeta: NormalizedOverlayPoint[][] = [];

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
        maintainAspectRatio: true,
        interaction: {
          mode: 'nearest',
          axis: 'x',
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
                // Prefer the hovered scatter point (event) if present
                const preferred =
                  context.find(
                    (c) => (c.dataset as { type?: string }).type === 'scatter'
                  ) ?? context[0];
                const xValue = preferred?.parsed?.x;
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
    const glucosePoints = this.buildGlucosePoints(data).sort((a, b) => a.epochMs - b.epochMs);
    const overlayPoints = this.buildOverlayPoints(data).sort((a, b) => a.epochMs - b.epochMs);
    // Keep overlay points in sync with dataset order
    this.overlayPointsState = overlayPoints;

    // Glucose line dataset
    const glucoseData = glucosePoints.map((point) => ({
      x: point.epochMs,
      y: point.value
    }));

    // Event overlay scatter datasets grouped by event type to get distinct legend entries/colors
    const eventTypeColors: Record<string, string> = {
      Food: '#fb923c',
      Insulin: '#5B8DEF',
      Exercise: '#10b981',
      Note: '#a855f7'
    };
    const overlayByType = new Map<string, NormalizedOverlayPoint[]>();
    for (const point of overlayPoints) {
      const type = point.eventType ?? 'Other';
      if (!overlayByType.has(type)) {
        overlayByType.set(type, []);
      }
      overlayByType.get(type)!.push(point);
    }

    const eventTypesOrder = ['Food', 'Insulin', 'Exercise', 'Note'];
    const sortedTypes = Array.from(overlayByType.keys()).sort((a, b) => {
      const ia = eventTypesOrder.indexOf(a);
      const ib = eventTypesOrder.indexOf(b);
      if (ia === -1 && ib === -1) return a.localeCompare(b);
      if (ia === -1) return 1;
      if (ib === -1) return -1;
      return ia - ib;
    });

    const overlayDatasets: ChartData['datasets'] = [];
    this.overlayDatasetMeta = [];
    for (const type of sortedTypes) {
      const pointsForType = overlayByType.get(type)!;
      this.overlayDatasetMeta.push(pointsForType);
      const datasetData = pointsForType.map((overlay) => {
        const yValue = this.getGlucoseValueAt(overlay.epochMs, glucosePoints) ?? 200;
        return { x: overlay.epochMs, y: yValue };
      });
      const color = eventTypeColors[type] ?? '#ff6384';
      overlayDatasets.push({
        type: 'scatter',
        label: type,
        data: datasetData,
        backgroundColor: color,
        pointRadius: 8,
        pointHoverRadius: 10,
        pointStyle: 'circle'
      } as never);
    }

    const thresholdDatasets = this.buildThresholdDatasets(glucosePoints, data);

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
        ...overlayDatasets,
        ...thresholdDatasets
      ]
    };
  }

  /**
   * Gets glucose value at a specific time (or nearest)
   */
  private getGlucoseValueAt(
    targetEpochMs: number,
    points: NormalizedGlucosePoint[]
  ): number | null {
    let closest: NormalizedGlucosePoint | null = null;
    let minDiff = Infinity;

    for (const point of points) {
      const diff = Math.abs(point.epochMs - targetEpochMs);

      if (diff < minDiff && point.value !== null && point.value !== undefined) {
        minDiff = diff;
        closest = point;
      }
    }

    return closest?.value ?? null;
  }

  /**
   * Extracts glucose points from raw API response (supports legacy and new shapes)
   */
  private buildGlucosePoints(data: ChartDataResponseDto): NormalizedGlucosePoint[] {
    return (data.glucoseData ?? [])
      .map((point) => ({
        iso: point.timestamp,
        epochMs: Date.parse(point.timestamp),
        value: point.value ?? null
      }))
      .filter((point) => Number.isFinite(point.epochMs));
  }

  /**
   * Normalizes overlay markers with millisecond timestamps
   */
  private buildOverlayPoints(data: ChartDataResponseDto): NormalizedOverlayPoint[] {
    return (data.eventOverlays ?? [])
      .map((overlay) => ({
        eventId: overlay.eventId,
        eventType: overlay.eventType?.toString(),
        iso: overlay.timestamp,
        epochMs: Date.parse(overlay.timestamp),
        icon: overlay.icon ?? null,
        color: overlay.color ?? null,
        summary: overlay.summary ?? null
      }))
      .filter((overlay) => Number.isFinite(overlay.epochMs));
  }

  private buildThresholdDatasets(
    glucosePoints: NormalizedGlucosePoint[],
    data: ChartDataResponseDto
  ): ChartData['datasets'] {
    const upper = this.upperBoundary();
    const lower = this.lowerBoundary();

    const startEpoch = this.resolveStartEpoch(glucosePoints, data);
    const endEpoch = this.resolveEndEpoch(glucosePoints, data);

    if (startEpoch === null || endEpoch === null || startEpoch === endEpoch) {
      return [];
    }

    const minEpoch = Math.min(startEpoch, endEpoch);
    const maxEpoch = Math.max(startEpoch, endEpoch);

    const datasets: ChartData['datasets'] = [];

    if (Number.isFinite(upper)) {
      datasets.push(this.createThresholdDataset(minEpoch, maxEpoch, upper, 'Upper Range'));
    }

    if (Number.isFinite(lower)) {
      datasets.push(this.createThresholdDataset(minEpoch, maxEpoch, lower, 'Lower Range'));
    }

    return datasets;
  }

  private resolveStartEpoch(
    glucosePoints: NormalizedGlucosePoint[],
    data: ChartDataResponseDto
  ): number | null {
    const parsedStart = Date.parse(data.startTime);
    if (Number.isFinite(parsedStart)) {
      return parsedStart;
    }

    return glucosePoints[0]?.epochMs ?? null;
  }

  private resolveEndEpoch(
    glucosePoints: NormalizedGlucosePoint[],
    data: ChartDataResponseDto
  ): number | null {
    const parsedEnd = Date.parse(data.endTime);
    if (Number.isFinite(parsedEnd)) {
      return parsedEnd;
    }

    return glucosePoints.at(-1)?.epochMs ?? null;
  }

  private createThresholdDataset(
    minEpoch: number,
    maxEpoch: number,
    value: number,
    label: string
  ): ChartData['datasets'][number] {
    return {
      type: 'line',
      label,
      data: [
        { x: minEpoch, y: value },
        { x: maxEpoch, y: value }
      ],
      borderColor: 'rgba(220, 53, 69, 0.85)',
      borderWidth: 1,
      borderDash: [6, 6],
      pointRadius: 0,
      pointHoverRadius: 0,
      fill: false,
      tension: 0
    } as never;
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

    // Recompute point radii per overlay dataset based on selected event id
    for (let i = 0; i < this.overlayDatasetMeta.length; i++) {
      const meta = this.overlayDatasetMeta[i];
      const radii = meta.map((point) => (point.eventId === eventId ? 12 : 8));
      const datasetIndex = 1 + i; // after glucose dataset
      const dataset = this.chart.data.datasets[datasetIndex] as unknown as {
        pointRadius: number | number[];
      };
      dataset.pointRadius = radii;
    }

    this.chart.update('none');
  }

  /**
   * Handles chart click events (for overlay markers)
   */
  private handleChartClick(event: unknown, elements: { datasetIndex: number; index: number }[]): void {
    if (elements.length === 0) return;

    const element = elements[0];
    const datasetIndex = element.datasetIndex;

    // Check if clicked on overlay (events) datasets (which come right after the glucose dataset)
    const firstOverlayDatasetIndex = 1;
    const lastOverlayDatasetIndex = firstOverlayDatasetIndex + this.overlayDatasetMeta.length - 1;
    if (datasetIndex >= firstOverlayDatasetIndex && datasetIndex <= lastOverlayDatasetIndex) {
      const overlayGroupIndex = datasetIndex - firstOverlayDatasetIndex;
      const dataIndex = element.index;
      const meta = this.overlayDatasetMeta[overlayGroupIndex]?.[dataIndex];
      if (meta?.eventId) this.eventSelect.emit(meta.eventId);
    }
  }

  /**
   * Handles chart hover for crosshair
   */
  private handleChartHover(event: ChartEvent): void {
    if (!this.chart) return;
    // Ensure we have a mouse event on a canvas
    const nativeEvt = (event as unknown as { native?: Event }).native ?? null;
    if (!(nativeEvt instanceof MouseEvent)) return;
    const target = nativeEvt.target;
    if (!(target instanceof HTMLCanvasElement)) return;

    // Get relative position manually
    const rect = target.getBoundingClientRect();
    const x = nativeEvt.clientX - rect.left;
    const y = nativeEvt.clientY - rect.top;
    this.crosshairPosition = { x, y };

    // Find nearest point
    const nearestIndex = this.findNearestDataPoint(x);
    if (nearestIndex >= 0) {
      this.currentCrosshairIndex.set(nearestIndex);
      const data = this.chart.data.datasets[0].data[nearestIndex] as { x?: number };
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

    dataset.data.forEach((point: unknown, index) => {
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
        const data = this.chart.data.datasets[0].data[newIndex] as { x?: number };
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

    const glucosePoint = this.chart.data.datasets[0].data[currentIndex] as { x?: number };
    if (!glucosePoint?.x) return;

    const currentTime = glucosePoint.x;

    // Find event marker at or near current time
    for (const point of this.overlayPointsState) {
      if (Math.abs(point.epochMs - currentTime) < 60000) {
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
    if (!data) {
      return false;
    }

    return data.glucoseData.length > 0;
  }
}
