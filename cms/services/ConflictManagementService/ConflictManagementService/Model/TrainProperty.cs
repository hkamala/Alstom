namespace ConflictManagementService.Model;

public class TrainProperty
{
    public enum EPropertyType { ptUnknown, ptTmsTtObject, ptTmsTripObject, ptInteger, ptBoolean, ptString, ptReal, ptBitVector, ptTimestamp };
    public enum EAlarmLevel { atNoAlarm, atEvent, atNotify, atWarning, atAlarm, atCriticalAlarm };

    public string Name { get; }
    public bool Valid { get; set; }
    public string Value { get; set;  }
    public EPropertyType Type { get; set; }
    public EAlarmLevel? AlarmLevel { get; set; }

    public TrainProperty(string propertyName, string propertyValue, EPropertyType propertyType, EAlarmLevel? alarmLevel = null) 
    {
        this.Name = propertyName;
        this.Value = propertyValue;
        this.Type = propertyType;
        this.AlarmLevel = alarmLevel;
        this.Valid = true;
    }
    public TrainProperty(string propertyName)
    {
        this.Name = propertyName;
        this.Value = "";
        this.Type = EPropertyType.ptUnknown;
        this.AlarmLevel = null;
        this.Valid = false;
    }
}