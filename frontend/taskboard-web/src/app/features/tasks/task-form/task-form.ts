import { Component, input, output } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { TaskStatus, TaskWriteRequest } from '../../../core/tasks/task.models';

function utcDateString(): string {
  return new Date().toISOString().slice(0, 10);
}

function notBefore(minimum: string) {
  return (control: AbstractControl<string>): ValidationErrors | null =>
    control.value && control.value < minimum ? { beforeToday: true } : null;
}

@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule],
  templateUrl: './task-form.html',
  styleUrl: './task-form.scss',
})
export class TaskFormComponent {
  readonly serverErrors = input<Record<string, string>>({});
  readonly submitting = input(false);
  readonly submitted = output<TaskWriteRequest>();
  readonly utcToday = utcDateString();
  readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
    status: new FormControl<TaskStatus>('Pending', { nonNullable: true }),
    dueDate: new FormControl('', {
      nonNullable: true,
      validators: [notBefore(utcDateString())],
    }),
  });

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting()) return;
    const value = this.form.getRawValue();
    this.submitted.emit({
      title: value.title,
      description: value.description.trim() || null,
      status: value.status,
      dueDate: value.dueDate || null,
    });
  }

  reset(): void {
    this.form.reset({ title: '', description: '', status: 'Pending', dueDate: '' });
  }
}
