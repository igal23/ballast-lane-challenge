export type TaskStatus = 'Pending' | 'Wip' | 'Completed';

export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string | null;
}

export interface TaskWriteRequest {
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string | null;
}
