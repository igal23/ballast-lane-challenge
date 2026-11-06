import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { TaskItem, TaskWriteRequest } from '../../../core/tasks/task.models';
import { TaskService } from '../../../core/tasks/task.service';
import { TaskBoardComponent } from './task-board';

const existingTask: TaskItem = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Existing task',
  description: null,
  status: 'Pending',
  dueDate: null,
};

class TaskServiceStub {
  listResult: Observable<TaskItem[]> = of([]);
  createResult: Observable<TaskItem> = of(existingTask);
  createCalls = 0;
  list() { return this.listResult; }
  create(request: TaskWriteRequest) {
    this.createCalls += 1;
    return this.createResult;
  }
}

describe('TaskBoardComponent', () => {
  let fixture: ComponentFixture<TaskBoardComponent>;
  let service: TaskServiceStub;

  async function createFixture(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [TaskBoardComponent],
      providers: [{ provide: TaskService, useClass: TaskServiceStub }],
    }).compileComponents();
    service = TestBed.inject(TaskService) as unknown as TaskServiceStub;
    fixture = TestBed.createComponent(TaskBoardComponent);
  }

  it('renders loading, empty, populated, and API error states', async () => {
    await createFixture();
    const pending = new Subject<TaskItem[]>();
    service.listResult = pending;
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading tasks');

    pending.next([]);
    pending.complete();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No tasks yet');

    service.listResult = of([existingTask]);
    fixture.componentInstance.load();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Existing task');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Pending');

    service.listResult = throwError(() => new HttpErrorResponse({ status: 500 }));
    fixture.componentInstance.load();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Unable to load tasks');
  });

  it('inserts a successful create, resets the form, and blocks duplicate submissions', async () => {
    await createFixture();
    const pending = new Subject<TaskItem>();
    service.createResult = pending;
    fixture.detectChanges();
    const request: TaskWriteRequest = {
      title: 'New task', description: null, status: 'Pending', dueDate: null,
    };

    fixture.componentInstance.create(request);
    fixture.componentInstance.create(request);
    expect(service.createCalls).toBe(1);
    expect(fixture.componentInstance.submitting()).toBe(true);

    pending.next({ ...existingTask, title: 'New task' });
    pending.complete();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New task');
    expect(fixture.componentInstance.form().form.controls.title.value).toBe('');
  });

  it('maps server validation to fields and preserves entered values', async () => {
    await createFixture();
    service.createResult = throwError(() => new HttpErrorResponse({
      status: 400,
      error: { errors: [
        { code: 'task.title.required', field: 'title' },
        { code: 'task.due_date.in_past', field: 'dueDate' },
      ] },
    }));
    fixture.detectChanges();
    const form = fixture.componentInstance.form();
    form.form.patchValue({ title: 'Still here', dueDate: '2026-11-01' });

    fixture.componentInstance.create(form.form.getRawValue() as TaskWriteRequest);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Title is required');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('today or later');
    expect(form.form.controls.title.value).toBe('Still here');
  });
});
