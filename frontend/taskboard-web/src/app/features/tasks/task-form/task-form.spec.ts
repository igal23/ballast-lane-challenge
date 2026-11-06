import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TaskFormComponent } from './task-form';
import { TaskWriteRequest } from '../../../core/tasks/task.models';
import { vi } from 'vitest';

describe('TaskFormComponent', () => {
  let fixture: ComponentFixture<TaskFormComponent>;
  let component: TaskFormComponent;

  beforeEach(async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-11-06T23:30:00Z'));
    await TestBed.configureTestingModule({ imports: [TaskFormComponent] }).compileComponents();
    fixture = TestBed.createComponent(TaskFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => vi.useRealTimers());

  it('uses UTC today as date guidance and emits only valid owner-free input', () => {
    const emitted: TaskWriteRequest[] = [];
    component.submitted.subscribe((value) => emitted.push(value));
    const date = (fixture.nativeElement as HTMLElement).querySelector('#task-due-date') as HTMLInputElement;
    expect(date.min).toBe('2026-11-06');

    component.form.setValue({
      title: 'Prepare release',
      description: '',
      status: 'Wip',
      dueDate: '2026-11-05',
    });
    component.submit();
    expect(emitted).toHaveLength(0);

    component.form.patchValue({ dueDate: '2026-11-06' });
    component.submit();
    expect(emitted).toEqual([{
      title: 'Prepare release',
      description: null,
      status: 'Wip',
      dueDate: '2026-11-06',
    }]);
    expect((emitted[0] as TaskWriteRequest & { ownerId?: string }).ownerId).toBeUndefined();
  });

  it('shows server field errors and can reset after a successful create', () => {
    fixture.componentRef.setInput('serverErrors', {
      title: 'Title is required.',
      dueDate: 'Due date must be today or later.',
    });
    component.form.patchValue({ title: 'Keep this value' });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Title is required.');
    expect(component.form.controls.title.value).toBe('Keep this value');

    component.reset();
    expect(component.form.getRawValue()).toEqual({
      title: '', description: '', status: 'Pending', dueDate: '',
    });
  });

  it('prefills edits, allows an unchanged overdue date, and rejects a changed past date', () => {
    const emitted: TaskWriteRequest[] = [];
    component.submitted.subscribe((value) => emitted.push(value));
    fixture.componentRef.setInput('task', {
      id: '11111111-1111-1111-1111-111111111111',
      title: 'Overdue task',
      description: 'Keep the original due date',
      status: 'Pending',
      dueDate: '2026-11-01',
    });
    fixture.detectChanges();

    expect(component.form.getRawValue()).toEqual({
      title: 'Overdue task',
      description: 'Keep the original due date',
      status: 'Pending',
      dueDate: '2026-11-01',
    });
    component.form.patchValue({ status: 'Completed' });
    component.submit();
    expect(emitted[0]).toMatchObject({ status: 'Completed', dueDate: '2026-11-01' });

    component.form.patchValue({ dueDate: '2026-11-02' });
    component.submit();
    expect(emitted).toHaveLength(1);
  });
});
