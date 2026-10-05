namespace WorkOrderTracker.Api.Models;

public class Building
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
