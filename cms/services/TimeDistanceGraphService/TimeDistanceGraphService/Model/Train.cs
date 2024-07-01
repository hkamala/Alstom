namespace E2KService.Model;

public class Train : IEquatable<Train?>
{
	public string Obid => this.obid;
	public string Guid => this.guid;
	public string CtcId => this.ctcId;
	public string Td => this.td;
    public string Postfix { get => postfix; set => postfix = value; }
    public string TrainType { get => trainType; set => trainType = value; }

    private readonly string obid = "";
	private readonly string guid = "";
	private string ctcId = "";
	private string td = "";
    private string postfix = "";
    private string trainType = "";

    public Train(string obid, string guid, string ctcId, string td)
	{
		this.obid = obid;
		this.guid = guid;
		this.ctcId = ctcId;
		this.td = td;
	}

	public void UpdateBaseInfo(string? ctcId, string? td)
	{
		if (ctcId != null)
			this.ctcId = ctcId;
		if (td != null)
			this.td = td;
	}

	public bool IsValid()
    {
		return this.obid != "" && this.ctcId != "";
    }

    public override string ToString()
    {
        return "Obid=" + this.obid + ", Guid=" + this.guid + ", CtcId=" + this.ctcId + ", Td=" + this.td + ", Postfix=" + this.postfix + ", TrainType=" + this.trainType;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Train);
    }

    public bool Equals(Train? other)
    {
        return other is not null &&
               Obid == other.Obid &&
               Guid == other.Guid &&
               CtcId == other.CtcId &&
               Td == other.Td;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Obid, Guid, CtcId, Td);
    }

    public static bool operator ==(Train? left, Train? right)
    {
        return EqualityComparer<Train>.Default.Equals(left, right);
    }

    public static bool operator !=(Train? left, Train? right)
    {
        return !(left == right);
    }
}
