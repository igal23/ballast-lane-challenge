import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { User } from '../../../core/auth/auth.models';
import { LoginComponent } from './login';
import { vi } from 'vitest';

class AuthStub {
  loginResult: Observable<User> = of({ id: '1', email: 'user@example.com' });
  login(email: string, password: string) { return this.loginResult; }
}

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let auth: AuthStub;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useClass: AuthStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    auth = TestBed.inject(AuthService) as unknown as AuthStub;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('has accessible fields, password guidance, and disables invalid or in-flight submit', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('label[for="login-email"]')?.textContent).toContain('Email');
    expect(element.querySelector('label[for="login-password"]')?.textContent).toContain('Password');
    expect(element.textContent).toContain('15');
    expect((element.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);

    const pending = new Subject<User>();
    auth.loginResult = pending;
    component.form.setValue({ email: 'user@example.com', password: 'a sufficiently long password' });
    component.submit();
    fixture.detectChanges();
    expect((element.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows a generic credential error without echoing credentials', () => {
    auth.loginResult = throwError(() => new HttpErrorResponse({ status: 401 }));
    component.form.setValue({ email: 'user@example.com', password: 'wrong password value' });
    component.submit();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Invalid email or password');
    expect(text).not.toContain('wrong password value');
  });

  it('navigates to tasks after success', () => {
    component.form.setValue({ email: 'user@example.com', password: 'a sufficiently long password' });
    component.submit();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/tasks');
  });
});
