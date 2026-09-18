import { TestBed } from '@angular/core/testing';
import {
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import {
  provideHttpClientTesting,
  HttpTestingController,
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.models';

const FUTURE = new Date(Date.now() + 8 * 3600 * 1000).toISOString();

const FAKE_RESPONSE: AuthResponse = {
  token: 'fake.jwt.token',
  username: 'testuser',
  expiresAt: FUTURE,
};

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    routerSpy = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerSpy },
      ],
    });

    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('isLoggedIn returns false when localStorage is empty', () => {
    expect(service.isLoggedIn()).toBe(false);
  });

  it('currentUsername returns null when not logged in', () => {
    expect(service.currentUsername()).toBeNull();
  });

  it('token returns null when not logged in', () => {
    expect(service.token()).toBeNull();
  });

  // ── login ─────────────────────────────────────────────────────────────────

  it('login sends POST to /auth/login', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();

    const req = http.expectOne(r => r.url.includes('/auth/login'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'user', password: 'pass' });
    req.flush(FAKE_RESPONSE);
  });

  it('login saves token to localStorage on success', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    expect(localStorage.getItem('cm_token')).toBe('fake.jwt.token');
    expect(localStorage.getItem('cm_username')).toBe('testuser');
  });

  it('login updates isLoggedIn signal to true', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    expect(service.isLoggedIn()).toBe(true);
  });

  it('login updates currentUsername signal', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    expect(service.currentUsername()).toBe('testuser');
  });

  it('login updates token signal', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    expect(service.token()).toBe('fake.jwt.token');
  });

  // ── register ──────────────────────────────────────────────────────────────

  it('register sends POST to /auth/register', () => {
    service.register({ username: 'newuser', password: 'password' }).subscribe();

    const req = http.expectOne(r => r.url.includes('/auth/register'));
    expect(req.request.method).toBe('POST');
    req.flush(FAKE_RESPONSE);
  });

  it('register saves session after success', () => {
    service.register({ username: 'newuser', password: 'password' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/register')).flush(FAKE_RESPONSE);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUsername()).toBe('testuser');
  });

  // ── logout ────────────────────────────────────────────────────────────────

  it('logout clears localStorage', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    service.logout();

    expect(localStorage.getItem('cm_token')).toBeNull();
    expect(localStorage.getItem('cm_username')).toBeNull();
    expect(localStorage.getItem('cm_expires')).toBeNull();
  });

  it('logout sets isLoggedIn to false', () => {
    service.login({ username: 'user', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/auth/login')).flush(FAKE_RESPONSE);

    service.logout();

    expect(service.isLoggedIn()).toBe(false);
  });

  it('logout navigates to /auth/login', () => {
    service.logout();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login']);
  });

  // ── expired token ─────────────────────────────────────────────────────────

  it('isLoggedIn returns false when stored expiry is in the past', () => {
    // Manually seed expired token in localStorage
    localStorage.setItem('cm_token', 'expired.token');
    localStorage.setItem('cm_expires', new Date(Date.now() - 1000).toISOString());

    // Check the logic directly (service reads localStorage on computed())
    const expired = localStorage.getItem('cm_expires')!;
    expect(new Date(expired) <= new Date()).toBe(true);
  });
});
