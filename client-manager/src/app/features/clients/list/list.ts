import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ClientService } from '../../../core/services/client.service';
import { Client } from '../../../core/models/client.models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-clients-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatTableModule, MatPaginatorModule, MatButtonModule,
    MatIconModule, MatInputModule, MatFormFieldModule,
    MatProgressSpinnerModule, MatTooltipModule,
    MatSnackBarModule, MatDialogModule
  ],
  templateUrl: './list.html',
  styleUrl: './list.scss'
})
export class ClientsListComponent implements OnInit {
  displayedColumns = ['id', 'name', 'email', 'phone', 'createdAt', 'actions'];
  clients = signal<Client[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  downloadingPdf = signal(false);

  page = signal(1);
  pageSize = signal(10);
  searchControl = new FormControl('');

  constructor(
    private clientService: ClientService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadClients();
    this.searchControl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.page.set(1);
      this.loadClients();
    });
  }

  loadClients(): void {
    this.loading.set(true);
    this.clientService.getClients(
      this.page(),
      this.pageSize(),
      this.searchControl.value ?? ''
    ).subscribe({
      next: result => {
        this.clients.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load clients.', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadClients();
  }

  deleteClient(client: Client): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { message: `Delete ${client.firstName} ${client.lastName}?` }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.clientService.deleteClient(client.id).subscribe({
        next: () => {
          this.snackBar.open('Client deleted.', 'Close', { duration: 3000 });
          this.loadClients();
        },
        error: () => this.snackBar.open('Failed to delete client.', 'Close', { duration: 3000 })
      });
    });
  }

  downloadPdf(): void {
    this.downloadingPdf.set(true);
    this.clientService.downloadPdfReport().subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `clients-report-${new Date().toISOString().slice(0, 10)}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.downloadingPdf.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to generate report.', 'Close', { duration: 3000 });
        this.downloadingPdf.set(false);
      }
    });
  }
}
