import { Component } from '@angular/core';

@Component({
  selector: 'app-tasks-placeholder',
  template: '<main class="placeholder"><h1>Your tasks</h1><p>The task board is loading.</p></main>',
  styles: ':host{display:block;padding:2rem}.placeholder{max-width:72rem;margin:auto}',
})
export class TasksPlaceholderComponent {}
