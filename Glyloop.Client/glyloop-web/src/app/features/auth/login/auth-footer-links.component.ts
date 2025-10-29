import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-auth-footer-links',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './auth-footer-links.component.html',
  styleUrl: './auth-footer-links.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthFooterLinksComponent {
}
