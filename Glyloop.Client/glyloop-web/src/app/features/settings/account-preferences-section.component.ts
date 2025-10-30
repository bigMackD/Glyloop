import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TirRangeFormComponent } from './tir-range-form.component';
import { AccountPreferencesStore } from '../../core/stores/account-preferences.store';
import { UpdatePreferencesRequestDto } from '../../core/models/settings.types';

/**
 * Account preferences section component
 * Shows and edits TIR bounds with immediate preview; Save/Cancel
 */
@Component({
  selector: 'app-account-preferences-section',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, MatSnackBarModule, TirRangeFormComponent],
  templateUrl: './account-preferences-section.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccountPreferencesSectionComponent implements OnInit {
  private readonly store = inject(AccountPreferencesStore);
  private readonly snackBar = inject(MatSnackBar);

  // Expose store signals to template
  readonly loading = this.store.loading;
  readonly error = this.store.error;
  readonly preferences = this.store.preferences;

  // Localized strings
  readonly saveSuccessMessage = $localize`:@@settings.account.saveSuccess:Preferences saved successfully`;
  readonly saveErrorMessage = $localize`:@@settings.account.saveError:Failed to save preferences`;

  ngOnInit(): void {
    this.store.load();
  }

  /**
   * Handles save action
   */
  async onSave(data: UpdatePreferencesRequestDto): Promise<void> {
    const result = await this.store.save(data);

    if (result.success) {
      this.showSnackbar(this.saveSuccessMessage, 'success');
      // Focus management: return focus to heading after save
      setTimeout(() => {
        const heading = document.querySelector('h2');
        if (heading instanceof HTMLElement) {
          heading.setAttribute('tabindex', '-1');
          heading.focus();
        }
      }, 100);
    } else {
      this.showSnackbar(result.error || this.saveErrorMessage, 'error');
    }
  }

  /**
   * Handles cancel action
   */
  onCancel(): void {
    this.store.reset();
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
