export const PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'] as const;
export const STATUSES = ['Open', 'InProgress', 'OnHold', 'Completed', 'Cancelled'] as const;

export type WorkOrderPriority = (typeof PRIORITIES)[number];
export type WorkOrderStatus = (typeof STATUSES)[number];

export const STATUS_LABELS: Record<WorkOrderStatus, string> = {
  Open: 'Open',
  InProgress: 'In Progress',
  OnHold: 'On Hold',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

export interface WorkOrder {
  id: number;
  title: string;
  description: string | null;
  location: string | null;
  priority: WorkOrderPriority;
  status: WorkOrderStatus;
  createdAt: string;
  dueDate: string | null;
  completedAt: string | null;
  buildingId: number;
  buildingName: string;
  technicianId: number | null;
  technicianName: string | null;
}

/** Body for POST/PUT — mirrors WorkOrderRequest on the API. */
export interface WorkOrderRequest {
  title: string;
  description: string | null;
  location: string | null;
  priority: WorkOrderPriority;
  status: WorkOrderStatus;
  dueDate: string | null;
  buildingId: number;
  technicianId: number | null;
}

export interface Building {
  id: number;
  name: string;
  address: string;
  openWorkOrders: number;
}

export interface Technician {
  id: number;
  name: string;
  email: string;
  trade: string;
}
