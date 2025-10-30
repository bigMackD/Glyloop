import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DexcomStatusCardComponent } from './dexcom-status-card.component';
import { UnlinkDialogComponent } from './unlink-dialog.component';
import { DexcomStore } from '../../core/stores/dexcom.store';

/**
 * Data Sources section component
 * Shows Dexcom link status and provides Link/Unlink actions
 */
@Component({
  selector: 'app-data-sources-section',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    DexcomStatusCardComponent,
    UnlinkDialogComponent
  ],
  templateUrl: './data-sources-section.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DataSourcesSectionComponent implements OnInit {
  private readonly store = inject(DexcomStore);
  private readonly snackBar = inject(MatSnackBar);

  // Expose store signals to template
  readonly state = this.store.state;
  readonly isLinked = this.store.isLinked;
  readonly loading = this.store.loading;
  readonly linking = this.store.linking;
  readonly unlinking = this.store.unlinking;
  readonly error = this.store.error;

  // Local UI state
  readonly unlinkDialogOpen = signal<boolean>(false);

  // Localized strings
  readonly unlinkSuccessMessage = $localize`:@@dexcom.unlinkSuccess:Dexcom account unlinked successfully`;
  readonly unlinkErrorMessage = $localize`:@@dexcom.unlinkError:Failed to unlink Dexcom account`;
  readonly refreshSuccessMessage = $localize`:@@dexcom.refreshSuccess:Status refreshed`;

  ngOnInit(): void {
    this.store.loadStatus();
  }

  /**
   * Initiates the Dexcom OAuth link flow
   */
  onLink(): void {
    this.store.startLinkFlow();
  }

  /**
   * Opens the unlink confirmation dialog
   */
  openUnlinkDialog(): void {
    this.unlinkDialogOpen.set(true);
  }

  /**
   * Closes the unlink confirmation dialog
   */
  closeUnlinkDialog(): void {
    this.unlinkDialogOpen.set(false);
  }

  /**
   * Handles the unlink confirmation
   */
  async onUnlinkConfirm(): Promise<void> {
    this.closeUnlinkDialog();

    const result = await this.store.unlink();

    if (result.success) {
      this.showSnackbar(this.unlinkSuccessMessage, 'success');
    } else {
      this.showSnackbar(result.error || this.unlinkErrorMessage, 'error');
    }
  }

  /**
   * Refreshes the Dexcom status
   */
  onRefresh(): void {
    this.store.loadStatus();
    this.showSnackbar(this.refreshSuccessMessage, 'success');
  }

  /**
   * Shows a snackbar notification
   */
  private showSnackbar(message: string, type: 'success' | 'error'): void {
    this.snackBar.open(message, $localize`:@@common.close:Close`, {
      duration: type === 'success' ? 3000 : 5000,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass: type === 'success' ? 'snackbar-success' : 'snackbar-error'
    });
  }
}
