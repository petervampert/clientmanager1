import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';
import { Router } from '@angular/router';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;

  const setup = (token: string | null) => {
    if (token) {
      localStorage.setItem('cm_token', token);
      localStorage.setItem('cm_expires', new Date(Date.now() + 8 * 3600 * 1000).toISOString());
    }

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: () => {} } },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  };

  afterEach(() => {
    httpMock?.verify();
  });

  it('does not add Authorization header when no token is stored', () => {
    setup(null);

    httpClient.get('/api/test').subscribe();

    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('adds Bearer Authorization header when token is in localStorage', () => {
    setup('test.jwt.token');

    httpClient.get('/api/clients').subscribe();

    const req = httpMock.expectOne('/api/clients');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test.jwt.token');
    req.flush([]);
  });

  it('passes request through unchanged when no token', () => {
    setup(null);

    httpClient.get('/api/clients').subscribe();

    const req = httpMock.expectOne('/api/clients');
    expect(req.request.url).toBe('/api/clients');
    req.flush([]);
  });

  it('preserves existing headers alongside Authorization', () => {
    setup('my.token');

    httpClient.get('/api/data', { headers: { 'X-Custom': 'value' } }).subscribe();

    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my.token');
    expect(req.request.headers.get('X-Custom')).toBe('value');
    req.flush({});
  });
});
