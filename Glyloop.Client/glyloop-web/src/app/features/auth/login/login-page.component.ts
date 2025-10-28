import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4">
      <div class="max-w-md w-full">
        <h1 class="text-center text-3xl font-extrabold text-white mb-8">
          Login
        </h1>
        
        @if (registeredSuccessfully()) {
          <div class="rounded-md bg-green-50 dark:bg-green-900/20 p-4 border border-green-200 dark:border-green-800 mb-6" role="alert">
            <div class="text-sm text-green-800 dark:text-green-200">
              <strong>Account created successfully!</strong> Please log in to continue.
            </div>
          </div>
        }
        
        <div class="bg-white dark:bg-gray-800 p-8 rounded-lg shadow">
          <p class="text-gray-700 dark:text-gray-300">
            Login page placeholder. The registration flow has been implemented successfully!
          </p>
          <a href="/register" class="mt-4 inline-block text-blue-600 hover:text-blue-500 dark:text-blue-400">
            ← Back to Register
          </a>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly registeredSuccessfully = signal(false);

  ngOnInit(): void {
    // Check if user was redirected from successful registration
    this.route.queryParams.subscribe(params => {
      if (params['registered'] === 'true') {
        this.registeredSuccessfully.set(true);
      }
    });
  }
}

