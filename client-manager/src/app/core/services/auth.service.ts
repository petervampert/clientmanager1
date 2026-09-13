import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';

const TOKEN_KEY = 'cm_token';
const USERNAME_KEY = 'cm_username';
const EXPIRES_KEY = 'cm_expires';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api/auth';

  private _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private _username = signal<string | null>(localStorage.getItem(USERNAME_KEY));

  readonly isLoggedIn = computed(() => {
    const token = this._token();
    if (!token) return false;
    const expires = localStorage.getItem(EXPIRES_KEY);
    if (expires && new Date(expires) <= new Date()) {
      this.clearSession();
      return false;
    }
    return true;
  });

  readonly currentUsername = computed(() => this._username());
  readonly token = computed(() => this._token());

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(res => this.saveSession(res))
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(res => this.saveSession(res))
    );
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/auth/login']);
  }

  private saveSession(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USERNAME_KEY, res.username);
    localStorage.setItem(EXPIRES_KEY, res.expiresAt);
    this._token.set(res.token);
    this._username.set(res.username);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    this._token.set(null);
    this._username.set(null);
  }
}
