namespace WorkOrderTracker.Api.Models;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Trade or specialty, e.g. HVAC, Plumbing, Electrical.</summary>
    public string Trade { get; set; } = string.Empty;

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
