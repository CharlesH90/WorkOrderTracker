namespace WorkOrderTracker.Api.Services;

/// <summary>Settings that describe where the business operates.</summary>
public class BusinessOptions
{
    /// <summary>
    /// IANA time zone used to decide what "today" means for date-only rules such as
    /// "due date cannot be in the past". The server clock is UTC, so without this an
    /// evening request in New York would already be "tomorrow".
    /// </summary>
    public string TimeZoneId { get; set; } = "America/New_York";
}
