namespace E2KService.Model;

using System.Collections.Concurrent;

public class MovementHistoryItem
{
	public string Obid => this.obid;
	public string Td => this.td;
    public string Postfix { get => postfix; set => postfix = value; }
    public string TrainType { get => trainType; set => trainType = value; }

    public ActionTime OccurredTime => this.occurredTime; // UTC!
	public string EdgeId => this.edgeId;
	public long AdditionalPosition => this.additionalPosition;
	public bool Terminated => this.terminated;
	//public uint Offset => this.offset;
	//public string FromVertexId => this.fromVertexId;
	//public string AdditionalName => this.additionalName;

	string obid = "";
	string td = "";
	string postfix = "";
	string trainType = "";
	ActionTime occurredTime = new();		// UTC!
	string edgeId = "";
	long additionalPosition = 0;   // Normally millimeters, can be negative
	bool terminated = false;
	//uint offset = 0;
	//string fromVertexId = "";
	//string additionalName = "";

	////////////////////////////////////////////////////////////////////////////////

	public MovementHistoryItem()
	{
	}

	public MovementHistoryItem(string obid, string td, ActionTime occurredTime, string edgeId, long additionalPosition, string additionalName = "")
	{
		this.obid = obid;
		this.td = td;
		this.occurredTime = occurredTime;
		this.edgeId = edgeId;
		this.additionalPosition = additionalPosition;
		//this.additionalName = additionalName;
	}

	public MovementHistoryItem(string obid, string td, ActionTime occurredTime, string edgeId, uint offset, string fromVertexId, long additionalPosition)
	{
		this.obid = obid;
		this.td = td;
        this.occurredTime = occurredTime;
		this.edgeId = edgeId;
		//this.offset = offset;
		//this.fromVertexId = fromVertexId;
		this.additionalPosition = additionalPosition;
	}

	public MovementHistoryItem(string obid, string td, ActionTime occurredTime)
	{
		this.obid = obid;
		this.td = td;
        this.occurredTime = occurredTime;
		this.terminated = true;
	}

	public bool IsValid()
	{
		return this.edgeId != "" || this.terminated;
	}

	public string ToJsonString()
	{
		int secondOffset = this.occurredTime.DateTime.Minute * 60 + this.occurredTime.DateTime.Second;
		
		using var stream = new MemoryStream();
		using var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions() { Indented = false });

		writer.WriteStartObject();
		writer.WriteStartObject(secondOffset.ToString());
		writer.WriteString("td", Td);
		writer.WriteString("pf", Postfix);
		writer.WriteString("tt", TrainType);
		writer.WriteString("t", Terminated.ToString());
		writer.WriteString("e", Terminated ? "" : EdgeId);
		writer.WriteString("p", (Terminated ? 0 : AdditionalPosition).ToString());
		writer.WriteEndObject();
		writer.WriteEndObject();
		writer.Flush();

		return System.Text.Encoding.UTF8.GetString(stream.ToArray());
	}

	public override string ToString()
	{
        return string.Format($"obid = '{obid}', td = '{td}', postfix = '{postfix}', trainType = '{trainType}', time = '{occurredTime}', edgeId = '{edgeId}', addPos = {additionalPosition}, terminated = {terminated}");
	}
}

////////////////////////////////////////////////////////////////////////////////
// Collections (concurrent ones for automatic thread safety)

public class TrainMovements : ConcurrentDictionary<ulong /*UTC time as milliseconds from epoch*/, MovementHistoryItem>
{
    public TrainMovements(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
public class TrainMovementHistory : ConcurrentDictionary<string /*obid*/, TrainMovements>
{
    public TrainMovementHistory(int concurrencyLevel, int capacity) : base(concurrencyLevel, capacity)
    {
    }
}
