import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Client, ClientRequest, PagedResult } from '../models/client.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly apiUrl = `${environment.apiUrl}/clients`;

  constructor(private http: HttpClient) {}

  getClients(page = 1, pageSize = 10, search = ''): Observable<PagedResult<Client>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResult<Client>>(this.apiUrl, { params });
  }

  getClient(id: number): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${id}`);
  }

  createClient(request: ClientRequest): Observable<Client> {
    return this.http.post<Client>(this.apiUrl, request);
  }

  updateClient(id: number, request: ClientRequest): Observable<Client> {
    return this.http.put<Client>(`${this.apiUrl}/${id}`, request);
  }

  deleteClient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  downloadPdfReport(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/report/pdf`, { responseType: 'blob' });
  }
}
