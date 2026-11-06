import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { finalize } from 'rxjs';
import { TaskItem, TaskWriteRequest } from '../../../core/tasks/task.models';
import { TaskService } from '../../../core/tasks/task.service';
import { TaskFormComponent } from '../task-form/task-form';

interface ApiFieldError { code?: string; field?: string; }

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
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly submitting = signal(false);
  readonly createError = signal<string | null>(null);
  readonly fieldErrors = signal<Record<string, string>>({});

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.tasksApi.list().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (tasks) => this.tasks.set(tasks),
      error: () => this.loadError.set(true),
    });
  }

  create(request: TaskWriteRequest): void {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.createError.set(null);
    this.fieldErrors.set({});
    this.tasksApi.create(request).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (task) => {
        this.tasks.update((tasks) => [task, ...tasks]);
        this.form().reset();
      },
      error: (error: HttpErrorResponse) => this.handleCreateError(error),
    });
  }

  private handleCreateError(error: HttpErrorResponse): void {
    if (error.status !== 400) {
      this.createError.set('Unable to add task. Please try again.');
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
