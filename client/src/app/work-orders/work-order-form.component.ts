import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import {
  Building, PRIORITIES, STATUSES, STATUS_LABELS, Technician, WorkOrderPriority, WorkOrderRequest, WorkOrderStatus,
} from '../models/work-order';
import { WorkOrderService } from '../services/work-order.service';

@Component({
  selector: 'app-work-order-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <h1>{{ id() ? 'Edit' : 'New' }} Work Order</h1>

    <form [formGroup]="form" (ngSubmit)="save()">
      <div class="field">
        <label for="title">Title *</label>
        <input id="title" formControlName="title">
        @if (form.controls.title.touched && form.controls.title.invalid) {
          <span class="error">Title is required (3–150 characters).</span>
        }
      </div>

      <div class="field">
        <label for="description">Description</label>
        <textarea id="description" rows="3" formControlName="description"></textarea>
      </div>

      <div class="row">
        <div class="field">
          <label for="building">Building *</label>
          <select id="building" formControlName="buildingId">
            <option [ngValue]="null" disabled>Select a building</option>
            @for (b of buildings(); track b.id) { <option [ngValue]="b.id">{{ b.name }}</option> }
          </select>
          @if (form.controls.buildingId.touched && form.controls.buildingId.invalid) {
            <span class="error">Building is required.</span>
          }
        </div>
        <div class="field">
          <label for="location">Location</label>
          <input id="location" formControlName="location" placeholder="e.g. Unit 4B">
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label for="priority">Priority</label>
          <select id="priority" formControlName="priority">
            @for (p of priorities; track p) { <option [value]="p">{{ p }}</option> }
          </select>
        </div>
        <div class="field">
          <label for="status">Status</label>
          <select id="status" formControlName="status">
            @for (s of statuses; track s) { <option [value]="s">{{ statusLabels[s] }}</option> }
          </select>
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label for="technician">Technician</label>
          <select id="technician" formControlName="technicianId">
            <option [ngValue]="null">Unassigned</option>
            @for (t of technicians(); track t.id) {
              <option [ngValue]="t.id">{{ t.name }} ({{ t.trade }})</option>
            }
          </select>
        </div>
        <div class="field">
          <label for="dueDate">Due date</label>
          <input id="dueDate" type="date" formControlName="dueDate">
        </div>
      </div>

      @for (msg of serverErrors(); track msg) { <p class="error">{{ msg }}</p> }

      <div class="toolbar">
        <button class="btn-primary" type="submit" [disabled]="saving()">Save</button>
        <a class="btn" routerLink="/work-orders">Cancel</a>
      </div>
    </form>
  `,
})
export class WorkOrderFormComponent implements OnInit {
  private api = inject(WorkOrderService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  /** Bound from the :id route param via withComponentInputBinding(). */
  id = input<string>();

  readonly priorities = PRIORITIES;
  readonly statuses = STATUSES;
  readonly statusLabels = STATUS_LABELS;
  buildings = signal<Building[]>([]);
  technicians = signal<Technician[]>([]);
  serverErrors = signal<string[]>([]);
  saving = signal(false);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    description: [''],
    location: [''],
    priority: ['Medium' as WorkOrderPriority, Validators.required],
    status: ['Open' as WorkOrderStatus, Validators.required],
    dueDate: [''],
    buildingId: [null as number | null, Validators.required],
    technicianId: [null as number | null],
  });

  ngOnInit(): void {
    forkJoin([this.api.getBuildings(), this.api.getTechnicians()]).subscribe(([b, t]) => {
      this.buildings.set(b);
      this.technicians.set(t);
    });

    const id = this.id();
    if (id) {
      this.api.getWorkOrder(+id).subscribe(w => this.form.patchValue({
        ...w,
        description: w.description ?? '',
        location: w.location ?? '',
        dueDate: w.dueDate ? w.dueDate.substring(0, 10) : '',
      }));
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const body: WorkOrderRequest = {
      title: v.title!,
      description: v.description || null,
      location: v.location || null,
      priority: v.priority!,
      status: v.status!,
      dueDate: v.dueDate || null,
      buildingId: v.buildingId!,
      technicianId: v.technicianId,
    };

    const id = this.id();
    const request$: Observable<unknown> = id ? this.api.updateWorkOrder(+id, body) : this.api.createWorkOrder(body);

    this.saving.set(true);
    this.serverErrors.set([]);
    request$.subscribe({
      next: () => this.router.navigate(['/work-orders']),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        // ASP.NET returns ValidationProblemDetails: { errors: { Field: [messages] } }
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        this.serverErrors.set(errors ? Object.values(errors).flat() : ['Save failed. Please try again.']);
      },
    });
  }
}
