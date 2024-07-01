using System;

namespace ConflictManagementService.Model;

public class CtcEvent
{
    public string Key { get; }
    public string Str1 { set; get; }    // If this is set/left to "", it is generated automatically from train ID, if event is sent to train
    public string Str2 { set; get; }
    public uint Uint1 { set; get; }
    public uint Uint2 { set; get; }
    public uint Sysid1 { set; get; }
    public uint Sysid2 { set; get; }
    public bool Success { set; get; }
    public uint LogId { set; get; }    // If this is set/left to 0, it is generated automatically from train information, if event is sent to train
    public uint ActorId { set; get; }

    public CtcEvent(string key, uint logId = 0, string str1 = "", string str2 = "", uint uint1 = 0, uint uint2 = 0, uint sysid1 = 0, uint sysid2 = 0, bool success = true, uint actorId = 0)
    {
        if (key == null)
            throw new ArgumentNullException("key");

        Key = key;
        this.Success = success;
        this.LogId = logId;
        this.Str1 = str1;
        this.Str2 = str2;
        this.Uint1 = uint1;
        this.Uint2 = uint2;
        this.Sysid1 = sysid1;
        this.Sysid2 = sysid2;
        this.ActorId = actorId;
    }
}