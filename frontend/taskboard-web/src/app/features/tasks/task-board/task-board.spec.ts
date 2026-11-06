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
  replaceResult: Observable<TaskItem> = of(existingTask);
  deleteResult: Observable<void> = of(undefined);
  listCalls = 0;
  createCalls = 0;
  replaceCalls = 0;
  deleteCalls = 0;
  list() {
    this.listCalls += 1;
    return this.listResult;
  }
  create(request: TaskWriteRequest) {
    this.createCalls += 1;
    return this.createResult;
  }
  replace(id: string, request: TaskWriteRequest) {
    this.replaceCalls += 1;
    return this.replaceResult;
  }
  delete(id: string) {
    this.deleteCalls += 1;
    return this.deleteResult;
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

    fixture.componentInstance.save(request);
    fixture.componentInstance.save(request);
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

    fixture.componentInstance.save(form.form.getRawValue() as TaskWriteRequest);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Title is required');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('today or later');
    expect(form.form.controls.title.value).toBe('Still here');
  });

  it('prefills an edit, allows status changes, and applies a successful replacement', async () => {
    await createFixture();
    service.listResult = of([existingTask]);
    service.replaceResult = of({ ...existingTask, title: 'Updated', status: 'Completed' });
    fixture.detectChanges();

    fixture.componentInstance.edit(existingTask);
    fixture.detectChanges();
    expect(fixture.componentInstance.form().form.controls.title.value).toBe('Existing task');

    fixture.componentInstance.save({
      title: 'Updated', description: null, status: 'Completed', dueDate: null,
    });
    fixture.detectChanges();
    expect(service.replaceCalls).toBe(1);
    expect(fixture.componentInstance.tasks()[0]).toMatchObject({ title: 'Updated', status: 'Completed' });
    expect(fixture.componentInstance.editing()).toBeNull();
  });

  it('requires inline delete confirmation, supports cancel, and removes only after success', async () => {
    await createFixture();
    service.listResult = of([existingTask]);
    const deletion = new Subject<void>();
    service.deleteResult = deletion;
    fixture.detectChanges();

    fixture.componentInstance.requestDelete(existingTask.id);
    expect(fixture.componentInstance.pendingDeleteId()).toBe(existingTask.id);
    fixture.componentInstance.cancelDelete();
    expect(service.deleteCalls).toBe(0);

    fixture.componentInstance.requestDelete(existingTask.id);
    fixture.componentInstance.confirmDelete(existingTask.id);
    fixture.componentInstance.confirmDelete(existingTask.id);
    expect(service.deleteCalls).toBe(1);
    expect(fixture.componentInstance.tasks()).toHaveLength(1);

    deletion.next();
    deletion.complete();
    expect(fixture.componentInstance.tasks()).toHaveLength(0);
  });

  it('refreshes after a replacement returns 404 and reports the recoverable conflict', async () => {
    await createFixture();
    service.listResult = of([existingTask]);
    service.replaceResult = throwError(() => new HttpErrorResponse({ status: 404 }));
    fixture.detectChanges();
    fixture.componentInstance.edit(existingTask);

    fixture.componentInstance.save({
      title: 'Changed elsewhere', description: null, status: 'Wip', dueDate: null,
    });
    fixture.detectChanges();
    expect(service.listCalls).toBe(2);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('no longer exists');
  });

  it('preserves API ordering after creation and a status-changing replacement', async () => {
    await createFixture();
    const completed: TaskItem = {
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      title: 'Completed without date', description: null, status: 'Completed', dueDate: null,
    };
    const pending: TaskItem = {
      id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      title: 'Pending without date', description: null, status: 'Pending', dueDate: null,
    };
    const newCompleted: TaskItem = {
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      title: 'Completed with date', description: null, status: 'Completed', dueDate: '2026-11-08',
    };
    service.listResult = of([pending, completed]);
    service.createResult = of(newCompleted);
    fixture.detectChanges();

    fixture.componentInstance.save({
      title: newCompleted.title, description: null, status: 'Completed', dueDate: '2026-11-08',
    });
    expect(fixture.componentInstance.tasks().map((task) => task.id)).toEqual([
      pending.id, newCompleted.id, completed.id,
    ]);

    service.replaceResult = of({ ...pending, status: 'Completed' });
    fixture.componentInstance.edit(pending);
    fixture.componentInstance.save({
      title: pending.title, description: null, status: 'Completed', dueDate: null,
    });
    expect(fixture.componentInstance.tasks().map((task) => task.id)).toEqual([
      newCompleted.id, pending.id, completed.id,
    ]);
  });
});
