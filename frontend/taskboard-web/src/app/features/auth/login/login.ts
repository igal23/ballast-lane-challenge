import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(15), Validators.maxLength(128)],
    }),
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) return;
    this.errorMessage.set(null);
    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).pipe(
      finalize(() => this.submitting.set(false)),
    ).subscribe({
      next: () => void this.router.navigateByUrl('/tasks'),
      error: (error: HttpErrorResponse) => this.errorMessage.set(
        error.status === 401 ? 'Invalid email or password.' : 'Unable to sign in. Please try again.'),
    });
  }
}
