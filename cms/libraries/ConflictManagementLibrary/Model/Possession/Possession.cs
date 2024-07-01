using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Model.Schedule;

namespace ConflictManagementLibrary.Model.Possession
{
    public class Possession
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public ElementPosition? StartPos { get; set; }
        public ElementPosition? EndPos { get; set; }
        public ActionTime? StartTime { get; set; }
        public ActionTime? EndTime { get; set; }
        public string? State { get; set; }

        [JsonIgnore]
        public DateTime MyStartTime = DateTime.Now;
        [JsonIgnore]
        public DateTime MyEndTime = DateTime.Now;
        
        public void ApplyTimeUpdate()
        {
            if (MyUseLocalTime)
            {
                if (StartTime != null) MyStartTime = StartTime.DateTime.ToLocalTime();
                if (EndTime != null) MyEndTime = EndTime.DateTime.ToLocalTime();
            }
            else
            {
                if (StartTime != null) MyStartTime = StartTime.DateTime;
                if (EndTime != null) MyEndTime = EndTime.DateTime;
            }
        }
    }
    public class ElementPosition : IEquatable<ElementPosition?>
    {
        public string ElementId { get; set; }
        public uint Offset { get; set; }
        public long AdditionalPos { get; set; }
        public string AdditionalName { get; set; }    // Platform, timing point etc.

        public ElementPosition()
        {
            
        }

        public bool IsValid()
        {
            return ElementId != "";
        }

        public override string ToString()
        {
            return string.Format($"ElementId = '{ElementId}', Offset = {Offset}, AdditionalPos = {AdditionalPos}, AdditionalName = '{AdditionalName}'");
        }

        public string GetEdgePosIdentifier()
        {
            return string.Format($"{ElementId}({AdditionalPos})");
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ElementPosition);
        }

        public bool Equals(ElementPosition? other)
        {
            return other is not null &&
                   ElementId == other.ElementId &&
                   Offset == other.Offset &&
                   AdditionalPos == other.AdditionalPos;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ElementId, Offset, AdditionalPos);
        }

        public static bool operator ==(ElementPosition? left, ElementPosition? right)
        {
            return EqualityComparer<ElementPosition>.Default.Equals(left, right);
        }

        public static bool operator !=(ElementPosition? left, ElementPosition? right)
        {
            return !(left == right);
        }
    }

}
