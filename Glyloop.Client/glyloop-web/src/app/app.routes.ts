import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register-page.component').then(m => m.RegisterPageComponent),
    data: { title: 'Register' }
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login-page.component').then(m => m.LoginPageComponent),
    data: { title: 'Login' }
  },
  {
    path: '',
    redirectTo: '/register',
    pathMatch: 'full'
  }
];
