using ConflictManagementService.Model.TMS;
using System.Collections.Concurrent;

namespace ConflictManagementService.Model;

public class Movement
{
    public string? Description { get => description; set => description = value; }
    public int FromStationId { get => fromStationId; set => fromStationId = value; }
    public string? FromStationName { get => fromStationName; set => fromStationName = value; }
    public int FromId { get => fromId; set => fromId = value; }
    public string? FromName { get => fromName; set => fromName = value; }
    public int ToStationId { get => toStationId; set => toStationId = value; }
    public string? ToStationName { get => toStationName; set => toStationName = value; }
    public int ToId { get => toId; set => toId = value; }
    public string? ToName { get => toName; set => toName = value; }

    // The IDs in here are TMS DB IDs, not CTC IDs!
    private int id = 0;
    private string? description = "";
    private int fromStationId = 0;
    private string? fromStationName = "";
    private int fromId = 0;
    private string? fromName = "";
    private int toStationId = 0;
    private string? toStationName = "";
    private int toId = 0;
    private string? toName = "";

    public Movement(MovementTemplate movementTemplate)
    {
        this.id = movementTemplate.movementTemplateID;
        this.Description = movementTemplate.movementTemplateDescription;
        this.FromStationId = movementTemplate.fromStationID;
        this.FromStationName = movementTemplate.fromStationName;
        this.FromId = movementTemplate.fromID;
        this.FromName = movementTemplate.fromName;
        this.ToStationId = movementTemplate.toStationID;
        this.ToStationName = movementTemplate.toStationName;
        this.ToId = movementTemplate.toID;
        this.ToName = movementTemplate.toName;
    }

    public override string ToString()
    {
        return $"Movement ID={id} {Description}: {FromStationName}/{FromName} -> {ToStationName}/{ToName}";
    }

    public bool IsFromTo(string fromStationName, string fromName, string toStationName)
    {
        return fromStationName == this.FromStationName && fromName == this.FromName && toStationName == this.ToStationName && this.ToName != null && this.ToName != "";
    }
}

public class Movements : ConcurrentDictionary<int, Movement>
{
    public Movements(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}