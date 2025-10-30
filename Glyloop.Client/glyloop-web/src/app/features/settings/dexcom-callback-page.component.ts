import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DexcomStore } from '../../core/stores/dexcom.store';

type CallbackState = 'processing' | 'success' | 'error';

/**
 * Dexcom OAuth callback page component
 * Captures code from query, calls POST /api/dexcom/link, shows progress and redirects
 */
@Component({
  selector: 'app-dexcom-callback-page',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, MatSnackBarModule],
  templateUrl: './dexcom-callback-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DexcomCallbackPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(DexcomStore);
  private readonly snackBar = inject(MatSnackBar);

  readonly state = signal<CallbackState>('processing');
  readonly errorMessage = signal<string | null>(null);

  // Localized strings
  readonly linkSuccessMessage = $localize`:@@dexcom.linkSuccess:Dexcom account linked successfully`;

  ngOnInit(): void {
    this.handleCallback();
  }

  /**
   * Handles the OAuth callback by extracting the code and linking the account
   */
  private async handleCallback(): Promise<void> {
    // Extract the authorization code from query params
    const code = this.route.snapshot.queryParamMap.get('code');

    if (!code) {
      this.state.set('error');
      this.errorMessage.set($localize`:@@dexcom.callback.missingCode:Authorization code is missing. Please try linking your account again.`);
      return;
    }

    // Attempt to link the account
    const result = await this.store.link(code);

    if (result.success) {
      this.state.set('success');

      // Show success toast
      this.snackBar.open(this.linkSuccessMessage, $localize`:@@common.close:Close`, {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'bottom',
        panelClass: 'snackbar-success'
      });

      // Redirect to data sources page after a short delay
      setTimeout(() => {
        this.router.navigate(['/settings/data-sources']);
      }, 2000);
    } else {
      this.state.set('error');
      this.errorMessage.set(result.error || $localize`:@@dexcom.callback.genericError:An error occurred while linking your account.`);
    }
  }

  /**
   * Navigates back to the data sources page
   */
  goToDataSources(): void {
    this.router.navigate(['/settings/data-sources']);
  }

  /**
   * Retries the OAuth flow
   */
  retry(): void {
    this.store.startLinkFlow();
  }
}
