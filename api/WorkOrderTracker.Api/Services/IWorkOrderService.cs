using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Services;

public interface IWorkOrderService
{
    Task<IReadOnlyList<WorkOrderDto>> GetAllAsync(WorkOrderStatus? status, int? buildingId);
    Task<WorkOrderDto?> GetAsync(int id);
    Task<ServiceResult<WorkOrderDto>> CreateAsync(WorkOrderRequest request);
    Task<ServiceResult> UpdateAsync(int id, WorkOrderRequest request);
    Task<bool> DeleteAsync(int id);
}
