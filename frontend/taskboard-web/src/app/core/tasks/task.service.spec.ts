import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TaskService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists tasks from the exact API route without converting date-only values', () => {
    let dueDate: string | null | undefined;
    service.list().subscribe((tasks) => (dueDate = tasks[0].dueDate));

    const request = http.expectOne('/api/tasks');
    expect(request.request.method).toBe('GET');
    request.flush([
      {
        id: '11111111-1111-1111-1111-111111111111',
        title: 'Prepare demo',
        description: null,
        status: 'Wip',
        dueDate: '2026-11-07',
      },
    ]);

    expect(dueDate).toBe('2026-11-07');
  });

  it('creates a task with exact status and date strings and never sends an owner id', () => {
    service
      .create({
        title: 'Ship MVP',
        description: 'Verify the local demo',
        status: 'Pending',
        dueDate: '2026-11-08',
      })
      .subscribe();

    const request = http.expectOne('/api/tasks');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      title: 'Ship MVP',
      description: 'Verify the local demo',
      status: 'Pending',
      dueDate: '2026-11-08',
    });
    expect(request.request.body.ownerId).toBeUndefined();
    request.flush({
      id: '22222222-2222-2222-2222-222222222222',
      ...request.request.body,
    });
  });
});
