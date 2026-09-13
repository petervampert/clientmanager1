import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { noAuthGuard } from './core/guards/no-auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'clients', pathMatch: 'full' },

  // Auth routes (no shell layout)
  {
    path: 'auth',
    canActivate: [noAuthGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register').then(m => m.RegisterComponent)
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  // Protected routes (inside shell layout)
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/components/layout/shell').then(m => m.ShellComponent),
    children: [
      {
        path: 'clients',
        loadComponent: () =>
          import('./features/clients/list/list').then(m => m.ClientsListComponent)
      },
      {
        path: 'clients/new',
        loadComponent: () =>
          import('./features/clients/form/form').then(m => m.ClientFormComponent)
      },
      {
        path: 'clients/:id',
        loadComponent: () =>
          import('./features/clients/detail/detail').then(m => m.ClientDetailComponent)
      },
      {
        path: 'clients/:id/edit',
        loadComponent: () =>
          import('./features/clients/form/form').then(m => m.ClientFormComponent)
      }
    ]
  },

  { path: '**', redirectTo: 'clients' }
];
