namespace ConflictManagementService.Model;

using System;
using System.Collections.Generic;
using static E2KService.ServiceImp;

public class Train : IEquatable<Train?>
{
    public enum CtcTrainType { Sequence, Train };

    public string Obid => obid;
    public string Guid => guid;
    public string CtcId => ctcId;
    public string Td => td;
    public uint Sysid => sysid;
    public CtcTrainType CtcType => ctcType;
    public string Postfix { get => postfix; set => postfix = value; }
    public string AllocatedTrainGuid { get => allocatedTrainGuid; set => allocatedTrainGuid = value == null ? "" : value; }
    public string TrainType { get => trainType; set => trainType = value; } // Returns proper value only when train is allocated to timetable, because train type comes from timetable!
    public int DefaultLength { get => GetDefaultLength(); }   // Returns proper value only when train is allocated to timetable, because train type comes from timetable!

    private readonly string obid = "";
    private string guid = "";
    private string ctcId = "";
    private string td = "";
    private uint sysid = 0;
    private CtcTrainType ctcType = CtcTrainType.Sequence;
    private string postfix = "";
    private string allocatedTrainGuid = "";
    private string trainType = "";

    public Train(string obid, string guid, string ctcId, string td, uint sysid, CtcTrainType ctcType)
    {
        this.obid = obid;
        this.guid = guid;
        this.ctcId = ctcId;
        this.td = td;
        this.sysid = sysid;
        this.ctcType = ctcType;
    }

    public void UpdateBaseInfo(string? guid, string? ctcId, string? td, CtcTrainType? ctcType)
    {
        if (guid != null)
            this.guid = guid;
        if (ctcId != null)
            this.ctcId = ctcId;
        if (td != null)
            this.td = td;
        if (ctcType != null)
            this.ctcType = (CtcTrainType)ctcType;
    }

    public bool IsValid()
    {
        return obid != "" && ctcId != "" && sysid != 0;
    }

    private int GetDefaultLength()
    {
        return Service.DataHandler.GetTrainDefaultLength(this.trainType);
    }

    public override string ToString()
    {
        return $"[Obid={obid}, Guid={guid}, CtcId={ctcId}], Td={td}, SysId={sysid}, CtcType={ctcType}, Postfix={postfix}, AllocatedTrainGuid={allocatedTrainGuid}";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Train);
    }

    public bool Equals(Train? other)
    {
        return other != null &&
                obid == other.obid &&
                guid == other.guid &&
                ctcId == other.ctcId &&
                sysid == other.sysid &&
                ctcType == other.ctcType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(obid, guid, ctcId);
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
