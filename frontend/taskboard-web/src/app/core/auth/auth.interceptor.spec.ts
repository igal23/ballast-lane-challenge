import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('adds bearer only to API requests', () => {
    sessionStorage.setItem('taskboard.accessToken', 'jwt-token');
    client.get('/api/tasks').subscribe();
    const api = http.expectOne('/api/tasks');
    expect(api.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    api.flush([]);

    client.get('https://example.com/data').subscribe();
    const external = http.expectOne('https://example.com/data');
    expect(external.request.headers.has('Authorization')).toBe(false);
    external.flush({});
  });
});
