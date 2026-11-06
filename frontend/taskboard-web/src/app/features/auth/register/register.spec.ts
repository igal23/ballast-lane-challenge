import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { User } from '../../../core/auth/auth.models';
import { RegisterComponent } from './register';
import { vi } from 'vitest';

class AuthStub {
  registerResult: Observable<User> = of({ id: '1', email: 'user@example.com' });
  loginResult: Observable<User> = of({ id: '1', email: 'user@example.com' });
  register(email: string, password: string) { return this.registerResult; }
  login(email: string, password: string) { return this.loginResult; }
}

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let auth: AuthStub;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useClass: AuthStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    auth = TestBed.inject(AuthService) as unknown as AuthStub;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('has accessible labels and password length guidance', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('label[for="register-email"]')).not.toBeNull();
    expect(element.querySelector('label[for="register-password"]')).not.toBeNull();
    expect(element.textContent).toContain('15');
    expect(element.textContent).toContain('128');
  });

  it('shows a generic registration conflict', () => {
    auth.registerResult = throwError(() => new HttpErrorResponse({ status: 409 }));
    component.form.setValue({ email: 'user@example.com', password: 'a sufficiently long password' });
    component.submit();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Registration could not be completed');
  });

  it('logs in and navigates to tasks after registration', () => {
    component.form.setValue({ email: 'user@example.com', password: 'a sufficiently long password' });
    component.submit();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/tasks');
  });
});
