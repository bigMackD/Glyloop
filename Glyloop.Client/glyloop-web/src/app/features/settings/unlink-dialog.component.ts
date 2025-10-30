import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';

/**
 * Unlink confirmation dialog component
 * Confirmation modal to unlink Dexcom; explains retention; no purge toggle in MVP
 */
@Component({
  selector: 'app-unlink-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule],
  templateUrl: './unlink-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UnlinkDialogComponent {
  readonly open = input.required<boolean>();
  readonly confirm = output<void>();
  readonly cancel = output<void>();

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
