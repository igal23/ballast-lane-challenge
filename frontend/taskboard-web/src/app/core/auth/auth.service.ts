import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { LoginResponse, User } from './auth.models';

const tokenKey = 'taskboard.accessToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserState = signal<User | null>(null);

  readonly currentUser = this.currentUserState.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserState() !== null);

  register(email: string, password: string): Observable<User> {
    return this.http.post<User>('/api/auth/register', { email, password });
  }

  login(email: string, password: string): Observable<User> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => sessionStorage.setItem(tokenKey, response.accessToken)),
      switchMap(() => this.loadCurrentUser()),
      catchError((error) => {
        this.logout();
        return throwError(() => error);
      }),
    );
  }

  restoreSession(): Observable<User | null> {
    if (!this.hasStoredToken()) {
      return of(null);
    }

    return this.loadCurrentUser().pipe(
      catchError(() => {
        this.logout();
        return of(null);
      }),
    );
  }

  hasStoredToken(): boolean {
    return sessionStorage.getItem(tokenKey) !== null;
  }

  logout(): void {
    sessionStorage.removeItem(tokenKey);
    this.currentUserState.set(null);
  }

  private loadCurrentUser(): Observable<User> {
    return this.http.get<User>('/api/auth/me').pipe(
      tap((user) => this.currentUserState.set(user)),
    );
  }
}
