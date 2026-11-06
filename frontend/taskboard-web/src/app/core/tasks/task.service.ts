import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TaskItem, TaskWriteRequest } from './task.models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  list(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>('/api/tasks');
  }

  create(request: TaskWriteRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>('/api/tasks', request);
  }

  get(id: string): Observable<TaskItem> {
    return this.http.get<TaskItem>(`/api/tasks/${id}`);
  }

  replace(id: string, request: TaskWriteRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`/api/tasks/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${id}`);
  }
}
