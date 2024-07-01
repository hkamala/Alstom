namespace E2KService.Model;

public class EdgePosition
{
    public string EdgeId => edgeId;
    public uint Offset { get; }
    public string FromVertexId => fromVertexId;
    public long AdditionalPos { get; }
    public string AdditionalName => additionalName;

    private readonly string edgeId = "";
    private readonly string fromVertexId = "";
    private readonly string additionalName = "";

    public EdgePosition()
    {
    }

    public EdgePosition(string edgeId, uint offset, string fromVertexId, long additionalPos = 0, string additionalName = "")
    {
        this.edgeId = edgeId;
        Offset = offset;
        this.fromVertexId = fromVertexId;
        AdditionalPos = additionalPos;
        this.additionalName = additionalName;
    }

    public bool IsValid()
    {
        return EdgeId != null;
    }

    public override string ToString()
    {
        return string.Format("EdgeId = '{0}', Offset = {1}, FromVertexId = '{2}', AdditionalPos = {3}, AdditionalName = '{4}'", EdgeId, Offset, FromVertexId, AdditionalPos, AdditionalName);
    }

    public string GetEdgePosIdentifier()
    {
        return string.Format("{0}({1})", EdgeId, AdditionalPos);
    }
}

////////////////////////////////////////////////////////////////////////////////

public class EdgeExtension
{
	public EdgePosition StartPos => this.startPos;
	public EdgePosition EndPos => this.endPos;
	public List<string> Edges => this.edges;

	private EdgePosition startPos = new();
	private EdgePosition endPos = new();
	private List<string> edges = new();

	public EdgeExtension()
	{
	}
	
	public EdgeExtension(EdgePosition startPos, EdgePosition endPos, List<string> edges)
	{
		this.startPos = startPos;
		this.endPos = endPos;
		this.edges = edges;
		
		if (!IsValid())
			throw new Exception(string.Format("Invalid edge extension: {0}", ToString()));
	}

	public bool IsValid()
	{
		return StartPos.IsValid() && EndPos.IsValid() && Edges.Count != 0 && Edges.First() == StartPos.EdgeId && Edges.Last() == EndPos.EdgeId;
	}

	public override string ToString()
	{
		string s = string.Format("Start: [{0}] End: [{1}] Edges:", StartPos.ToString(), EndPos.ToString());
		foreach (var edge in Edges)
			s += " " + edge;
		return s;
	}
}
