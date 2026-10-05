import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Building, Technician, WorkOrder, WorkOrderRequest, WorkOrderStatus } from '../models/work-order';

@Injectable({ providedIn: 'root' })
export class WorkOrderService {
  private http = inject(HttpClient);
  private baseUrl = '/api'; // proxied to the .NET API by proxy.conf.json

  getWorkOrders(filter: { status?: WorkOrderStatus | ''; buildingId?: number | null } = {}): Observable<WorkOrder[]> {
    let params = new HttpParams();
    if (filter.status) params = params.set('status', filter.status);
    if (filter.buildingId) params = params.set('buildingId', filter.buildingId);
    return this.http.get<WorkOrder[]>(`${this.baseUrl}/workorders`, { params });
  }

  getWorkOrder(id: number): Observable<WorkOrder> {
    return this.http.get<WorkOrder>(`${this.baseUrl}/workorders/${id}`);
  }

  createWorkOrder(body: WorkOrderRequest): Observable<WorkOrder> {
    return this.http.post<WorkOrder>(`${this.baseUrl}/workorders`, body);
  }

  updateWorkOrder(id: number, body: WorkOrderRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/workorders/${id}`, body);
  }

  deleteWorkOrder(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/workorders/${id}`);
  }

  getBuildings(): Observable<Building[]> {
    return this.http.get<Building[]>(`${this.baseUrl}/buildings`);
  }

  getTechnicians(): Observable<Technician[]> {
    return this.http.get<Technician[]>(`${this.baseUrl}/technicians`);
  }
}
