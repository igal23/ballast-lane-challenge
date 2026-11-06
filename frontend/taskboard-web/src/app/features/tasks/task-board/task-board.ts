import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { finalize } from 'rxjs';
import { TaskItem, TaskStatus, TaskWriteRequest } from '../../../core/tasks/task.models';
import { TaskService } from '../../../core/tasks/task.service';
import { TaskFormComponent } from '../task-form/task-form';

interface ApiFieldError { code?: string; field?: string; }

interface TaskStatusGroup {
  status: TaskStatus;
  label: string;
  tasks: readonly TaskItem[];
  emptyMessage: string;
}

function sortTasks(tasks: TaskItem[]): TaskItem[] {
  return [...tasks].sort((left, right) => {
    const completionOrder = Number(left.status === 'Completed') - Number(right.status === 'Completed');
    if (completionOrder !== 0) return completionOrder;
    if (left.dueDate !== right.dueDate) {
      if (left.dueDate === null) return 1;
      if (right.dueDate === null) return -1;
      return left.dueDate.localeCompare(right.dueDate);
    }
    return left.id.localeCompare(right.id);
  });
}

@Component({
  selector: 'app-task-board',
  imports: [TaskFormComponent],
  templateUrl: './task-board.html',
  styleUrl: './task-board.scss',
})
export class TaskBoardComponent implements OnInit {
  private readonly tasksApi = inject(TaskService);
  readonly form = viewChild.required(TaskFormComponent);
  readonly tasks = signal<TaskItem[]>([]);
  readonly taskGroups = computed<readonly TaskStatusGroup[]>(() => {
    const tasks = this.tasks();
    return [
      {
        status: 'Pending',
        label: 'Pending',
        tasks: tasks.filter((task) => task.status === 'Pending'),
        emptyMessage: 'No pending tasks.',
      },
      {
        status: 'Wip',
        label: 'WIP',
        tasks: tasks.filter((task) => task.status === 'Wip'),
        emptyMessage: 'No work in progress tasks.',
      },
      {
        status: 'Completed',
        label: 'Completed',
        tasks: tasks.filter((task) => task.status === 'Completed'),
        emptyMessage: 'No completed tasks.',
      },
    ];
  });
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly submitting = signal(false);
  readonly createError = signal<string | null>(null);
  readonly fieldErrors = signal<Record<string, string>>({});
  readonly editing = signal<TaskItem | null>(null);
  readonly pendingDeleteId = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);
  readonly mutationNotice = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.tasksApi.list().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (tasks) => this.tasks.set(tasks),
      error: () => this.loadError.set(true),
    });
  }

  save(request: TaskWriteRequest): void {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.createError.set(null);
    this.mutationNotice.set(null);
    this.fieldErrors.set({});
    const editing = this.editing();
    const operation = editing
      ? this.tasksApi.replace(editing.id, request)
      : this.tasksApi.create(request);
    operation.pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (task) => {
        this.tasks.update((tasks) => sortTasks(editing
          ? tasks.map((current) => current.id === task.id ? task : current)
          : [task, ...tasks]));
        this.editing.set(null);
        this.form().reset();
      },
      error: (error: HttpErrorResponse) => this.handleSaveError(error),
    });
  }

  edit(task: TaskItem): void {
    this.pendingDeleteId.set(null);
    this.createError.set(null);
    this.fieldErrors.set({});
    this.editing.set(task);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.fieldErrors.set({});
    this.form().reset();
  }

  requestDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  confirmDelete(id: string): void {
    if (this.deletingId()) return;
    this.deletingId.set(id);
    this.mutationNotice.set(null);
    this.tasksApi.delete(id).pipe(finalize(() => this.deletingId.set(null))).subscribe({
      next: () => {
        this.tasks.update((tasks) => tasks.filter((task) => task.id !== id));
        this.pendingDeleteId.set(null);
        if (this.editing()?.id === id) this.cancelEdit();
      },
      error: (error: HttpErrorResponse) => {
        this.pendingDeleteId.set(null);
        if (error.status === 404) {
          this.mutationNotice.set('This task no longer exists. The list was refreshed.');
          this.load();
        } else {
          this.mutationNotice.set('Unable to delete task. Please try again.');
        }
      },
    });
  }

  private handleSaveError(error: HttpErrorResponse): void {
    if (error.status === 404) {
      this.mutationNotice.set('This task no longer exists. The list was refreshed.');
      this.editing.set(null);
      this.form().reset();
      this.load();
      return;
    }
    if (error.status !== 400) {
      this.createError.set('Unable to save task. Please try again.');
      return;
    }
    const errors = (error.error?.errors ?? []) as ApiFieldError[];
    const mapped: Record<string, string> = {};
    for (const item of errors) {
      if (item.field === 'title') mapped['title'] = 'Title is required.';
      if (item.field === 'dueDate') mapped['dueDate'] = 'Due date must be today or later.';
    }
    this.fieldErrors.set(mapped);
  }
}
