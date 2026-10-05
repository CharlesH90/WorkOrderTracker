import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Building, STATUSES, STATUS_LABELS, WorkOrder, WorkOrderStatus } from '../models/work-order';
import { WorkOrderService } from '../services/work-order.service';

@Component({
  selector: 'app-work-order-list',
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
    <h1>Work Orders</h1>

    <div class="toolbar">
      <label>Status
        <select [(ngModel)]="status" (ngModelChange)="load()">
          <option value="">All</option>
          @for (s of statuses; track s) { <option [value]="s">{{ statusLabels[s] }}</option> }
        </select>
      </label>
      <label>Building
        <select [(ngModel)]="buildingId" (ngModelChange)="load()">
          <option [ngValue]="null">All</option>
          @for (b of buildings(); track b.id) { <option [ngValue]="b.id">{{ b.name }}</option> }
        </select>
      </label>
      <span class="spacer"></span>
      <a class="btn btn-primary" routerLink="/work-orders/new">+ New work order</a>
    </div>

    @if (error()) { <p class="error">{{ error() }}</p> }

    <table>
      <thead>
        <tr>
          <th>Title</th><th>Building</th><th>Priority</th><th>Status</th>
          <th>Technician</th><th>Due</th><th></th>
        </tr>
      </thead>
      <tbody>
        @for (w of workOrders(); track w.id) {
          <tr>
            <td>{{ w.title }}<br><span class="muted">{{ w.location }}</span></td>
            <td>{{ w.buildingName }}</td>
            <td><span class="badge" [class]="w.priority">{{ w.priority }}</span></td>
            <td><span class="badge" [class]="w.status">{{ statusLabels[w.status] }}</span></td>
            <td>{{ w.technicianName ?? '—' }}</td>
            <td>{{ w.dueDate ? (w.dueDate | date: 'mediumDate') : '—' }}</td>
            <td>
              <a class="btn" [routerLink]="['/work-orders', w.id, 'edit']">Edit</a>
              <button class="btn-danger" (click)="remove(w)">Delete</button>
            </td>
          </tr>
        } @empty {
          <tr><td colspan="7" class="muted">{{ loading() ? 'Loading…' : 'No work orders found.' }}</td></tr>
        }
      </tbody>
    </table>
  `,
})
export class WorkOrderListComponent implements OnInit {
  private api = inject(WorkOrderService);

  readonly statuses = STATUSES;
  readonly statusLabels = STATUS_LABELS;
  workOrders = signal<WorkOrder[]>([]);
  buildings = signal<Building[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  status: WorkOrderStatus | '' = '';
  buildingId: number | null = null;

  ngOnInit(): void {
    this.api.getBuildings().subscribe(b => this.buildings.set(b));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getWorkOrders({ status: this.status, buildingId: this.buildingId }).subscribe({
      next: items => { this.workOrders.set(items); this.loading.set(false); this.error.set(null); },
      error: () => { this.loading.set(false); this.error.set('Could not load work orders. Is the API running?'); },
    });
  }

  remove(w: WorkOrder): void {
    if (!confirm(`Delete "${w.title}"?`)) return;
    this.api.deleteWorkOrder(w.id).subscribe(() => this.load());
  }
}
