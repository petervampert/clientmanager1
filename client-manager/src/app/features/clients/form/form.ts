import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ClientService } from '../../../core/services/client.service';

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './form.html',
  styleUrl: './form.scss'
})
export class ClientFormComponent implements OnInit {
  form: FormGroup;
  isEditMode = signal(false);
  clientId = signal<number | null>(null);
  loading = signal(false);
  saving = signal(false);

  constructor(
    private fb: FormBuilder,
    private clientService: ClientService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar
  ) {
    this.form = this.fb.nonNullable.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName:  ['', [Validators.required, Validators.maxLength(100)]],
      email:     ['', [Validators.required, Validators.email]],
      phone:     [''],
      address:   ['']
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.clientId.set(+id);
      this.loadClient(+id);
    }
  }

  loadClient(id: number): void {
    this.loading.set(true);
    this.clientService.getClient(id).subscribe({
      next: client => {
        this.form.patchValue(client);
        this.loading.set(false);
      },
      error: () => {
        this.snackBar.open('Client not found.', 'Close', { duration: 3000 });
        this.router.navigate(['/clients']);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);

    const request = this.form.getRawValue();
    const op = this.isEditMode()
      ? this.clientService.updateClient(this.clientId()!, request)
      : this.clientService.createClient(request);

    op.subscribe({
      next: client => {
        this.snackBar.open(
          this.isEditMode() ? 'Client updated.' : 'Client created.',
          'Close', { duration: 3000 }
        );
        this.router.navigate(['/clients', client.id]);
      },
      error: err => {
        this.snackBar.open(
          err.error?.message ?? 'Save failed. Please try again.',
          'Close', { duration: 4000 }
        );
        this.saving.set(false);
      }
    });
  }

  get firstNameCtrl() { return this.form.get('firstName')!; }
  get lastNameCtrl()  { return this.form.get('lastName')!; }
  get emailCtrl()     { return this.form.get('email')!; }

  get title(): string {
    return this.isEditMode() ? 'Edit Client' : 'New Client';
  }
}
