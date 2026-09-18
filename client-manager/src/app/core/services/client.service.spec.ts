import { TestBed } from '@angular/core/testing';
import {
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import {
  provideHttpClientTesting,
  HttpTestingController,
} from '@angular/common/http/testing';
import { ClientService } from './client.service';
import { Client, ClientRequest, PagedResult } from '../models/client.models';

const FAKE_CLIENT: Client = {
  id: 1,
  firstName: 'Jane',
  lastName: 'Doe',
  email: 'jane@example.com',
  phone: '555-0100',
  address: '123 Main St',
  createdAt: '2026-01-01T00:00:00Z',
};

const FAKE_PAGED: PagedResult<Client> = {
  items: [FAKE_CLIENT],
  totalCount: 1,
  page: 1,
  pageSize: 10,
};

describe('ClientService', () => {
  let service: ClientService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ClientService,
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ClientService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // ── getClients ────────────────────────────────────────────────────────────

  it('getClients sends GET with default pagination params', () => {
    service.getClients().subscribe();

    const req = http.expectOne(r => r.url.includes('/clients') && !r.url.includes('/'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush(FAKE_PAGED);
  });

  it('getClients sends search param when provided', () => {
    service.getClients(1, 10, 'jane').subscribe();

    const req = http.expectOne(r => r.url.includes('/clients'));
    expect(req.request.params.get('search')).toBe('jane');
    req.flush(FAKE_PAGED);
  });

  it('getClients omits search param when empty string', () => {
    service.getClients(1, 10, '').subscribe();

    const req = http.expectOne(r => r.url.includes('/clients'));
    expect(req.request.params.has('search')).toBe(false);
    req.flush(FAKE_PAGED);
  });

  it('getClients omits search param when whitespace only', () => {
    service.getClients(1, 10, '   ').subscribe();

    const req = http.expectOne(r => r.url.includes('/clients'));
    expect(req.request.params.has('search')).toBe(false);
    req.flush(FAKE_PAGED);
  });

  it('getClients returns the paged result', () => {
    let result!: PagedResult<Client>;
    service.getClients().subscribe(r => (result = r));

    http.expectOne(r => r.url.includes('/clients')).flush(FAKE_PAGED);

    expect(result.totalCount).toBe(1);
    expect(result.items[0].firstName).toBe('Jane');
  });

  it('getClients sends correct page and pageSize', () => {
    service.getClients(3, 25).subscribe();

    const req = http.expectOne(r => r.url.includes('/clients'));
    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush(FAKE_PAGED);
  });

  // ── getClient ─────────────────────────────────────────────────────────────

  it('getClient sends GET to /clients/{id}', () => {
    service.getClient(42).subscribe();

    const req = http.expectOne(r => r.url.includes('/clients/42'));
    expect(req.request.method).toBe('GET');
    req.flush(FAKE_CLIENT);
  });

  it('getClient returns the client', () => {
    let result!: Client;
    service.getClient(1).subscribe(c => (result = c));

    http.expectOne(r => r.url.includes('/clients/1')).flush(FAKE_CLIENT);

    expect(result.email).toBe('jane@example.com');
  });

  // ── createClient ──────────────────────────────────────────────────────────

  it('createClient sends POST with body', () => {
    const request: ClientRequest = { firstName: 'John', lastName: 'Smith', email: 'john@example.com' };
    service.createClient(request).subscribe();

    const req = http.expectOne(r => r.method === 'POST' && r.url.includes('/clients'));
    expect(req.request.body).toEqual(request);
    req.flush({ ...FAKE_CLIENT, ...request });
  });

  it('createClient returns the created client', () => {
    let result!: Client;
    const request: ClientRequest = { firstName: 'John', lastName: 'Smith', email: 'john@example.com' };
    service.createClient(request).subscribe(c => (result = c));

    http.expectOne(r => r.method === 'POST' && r.url.includes('/clients'))
        .flush({ ...FAKE_CLIENT, id: 99 });

    expect(result.id).toBe(99);
  });

  // ── updateClient ──────────────────────────────────────────────────────────

  it('updateClient sends PUT to /clients/{id}', () => {
    const request: ClientRequest = { firstName: 'Updated', lastName: 'Name', email: 'upd@example.com' };
    service.updateClient(5, request).subscribe();

    const req = http.expectOne(r => r.url.includes('/clients/5'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ ...FAKE_CLIENT, id: 5 });
  });

  // ── deleteClient ──────────────────────────────────────────────────────────

  it('deleteClient sends DELETE to /clients/{id}', () => {
    service.deleteClient(7).subscribe();

    const req = http.expectOne(r => r.url.includes('/clients/7'));
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  // ── downloadPdfReport ─────────────────────────────────────────────────────

  it('downloadPdfReport sends GET to /clients/report/pdf', () => {
    service.downloadPdfReport().subscribe();

    const req = http.expectOne(r => r.url.includes('/report/pdf'));
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['%PDF'], { type: 'application/pdf' }));
  });

  it('downloadPdfReport returns a Blob', () => {
    let result!: Blob;
    service.downloadPdfReport().subscribe(b => (result = b));

    http.expectOne(r => r.url.includes('/report/pdf'))
        .flush(new Blob(['%PDF'], { type: 'application/pdf' }));

    expect(result).toBeInstanceOf(Blob);
  });
});
