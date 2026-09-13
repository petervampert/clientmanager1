import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ClientService } from '../../../core/services/client.service';
import { Client } from '../../../core/models/client.models';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatDividerModule,
    MatSnackBarModule, MatDialogModule
  ],
  templateUrl: './detail.html',
  styleUrl: './detail.scss'
})
export class ClientDetailComponent implements OnInit {
  client = signal<Client | null>(null);
  loading = signal(true);

  constructor(
    private clientService: ClientService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/clients']); return; }

    this.clientService.getClient(+id).subscribe({
      next: c => { this.client.set(c); this.loading.set(false); },
      error: () => {
        this.snackBar.open('Client not found.', 'Close', { duration: 3000 });
        this.router.navigate(['/clients']);
      }
    });
  }

  deleteClient(): void {
    const c = this.client();
    if (!c) return;

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: { message: `Delete ${c.firstName} ${c.lastName}?` }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.clientService.deleteClient(c.id).subscribe({
        next: () => {
          this.snackBar.open('Client deleted.', 'Close', { duration: 3000 });
          this.router.navigate(['/clients']);
        },
        error: () => this.snackBar.open('Failed to delete client.', 'Close', { duration: 3000 })
      });
    });
  }
}
