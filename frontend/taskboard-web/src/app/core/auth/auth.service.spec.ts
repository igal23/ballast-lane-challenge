import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('registers with the exact public contract and no owner id', () => {
    service.register('user@example.com', 'a sufficiently long password').subscribe();

    const request = http.expectOne('/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'user@example.com',
      password: 'a sufficiently long password',
    });
    expect(request.request.body.ownerId).toBeUndefined();
    request.flush({ id: '11111111-1111-1111-1111-111111111111', email: 'user@example.com' });
  });

  it('stores a token only after successful login and restores the user via me', () => {
    service.login('user@example.com', 'a sufficiently long password').subscribe();
    const login = http.expectOne('/api/auth/login');
    expect(login.request.method).toBe('POST');
    expect(sessionStorage.getItem('taskboard.accessToken')).toBeNull();

    login.flush({ accessToken: 'jwt-token', expiresAt: '2026-11-06T15:00:00Z' });
    expect(sessionStorage.getItem('taskboard.accessToken')).toBe('jwt-token');
    const me = http.expectOne('/api/auth/me');
    me.flush({ id: '11111111-1111-1111-1111-111111111111', email: 'user@example.com' });

    expect(service.currentUser()?.email).toBe('user@example.com');
    expect(service.isAuthenticated()).toBe(true);
    expect(localStorage.length).toBe(0);
    expect(JSON.stringify(sessionStorage)).not.toContain('a sufficiently long password');
  });

  it('does not persist anything when login fails and logout clears session state', () => {
    service.login('user@example.com', 'wrong password value').subscribe({ error: () => undefined });
    http.expectOne('/api/auth/login').flush(
      { code: 'auth.invalid_credentials' },
      { status: 401, statusText: 'Unauthorized' },
    );
    expect(sessionStorage.getItem('taskboard.accessToken')).toBeNull();

    sessionStorage.setItem('taskboard.accessToken', 'token');
    service.logout();
    expect(sessionStorage.getItem('taskboard.accessToken')).toBeNull();
    expect(service.currentUser()).toBeNull();
  });
});
