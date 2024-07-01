using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public  class ConflictSubtypes
    {
        [JsonProperty] 
        public static List<SubtypeOfConflict> MySubtypeOfConflicts = new List<SubtypeOfConflict>();

        public static void AddSubTypes()
        {
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Same Station Track with Opposite Direction Train", 0));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Same Station Track with Same Direction Train", 1));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Open Line Conflict with Opposite Direction Train", 2));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Open Line Conflict with Same Direction Train", 3));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Platform Conflict with Opposite Direction Train", 13));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "Platform Conflict with Same Direction Train", 14));

            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Station, "Track To Short", 4));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Station, "Platform Does Not Exist", 5));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Track, "Possession Exists", 6));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Track, "Track Blocked", 7));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Track, "Track Not Controlled", 8));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Track, "Track False Occupancy", 9));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Point, "Point Not Controlled", 10));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Point, "Point False Occupancy", 11));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Point, "Point Blocked Wrong Position", 12));
            MySubtypeOfConflicts.Add(SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Track, "Station Neck", 15));
        }

        public static SubtypeOfConflict GetSubtype(int theIndex)
        {
            AddSubTypes();
            return MySubtypeOfConflicts.FirstOrDefault(sc => sc.MyIndex == theIndex);
        }
    }

    [Serializable]
    public class SubtypeOfConflict
    {
        //[JsonProperty(PropertyName = "MyDescription")]
        public string MyDescription;
        
        //[JsonProperty(PropertyName = "MyIndex")]
        public int MyIndex;
        
        //[JsonProperty(PropertyName = "MyConflictType")]
        public ConflictType.TypeOfConflict MyConflictType;

        [JsonConstructor]
        public SubtypeOfConflict()
        {
            
        }
        private SubtypeOfConflict(ConflictType.TypeOfConflict theConflictType, string theDescription, int theIndex)
        {
            MyConflictType = theConflictType;
            MyDescription = theDescription;
            MyIndex = theIndex;
        }
        public static SubtypeOfConflict CreateSubType(ConflictType.TypeOfConflict theConflictType, string theDescription, int theIndex)
        {
            return new SubtypeOfConflict(theConflictType, theDescription, theIndex);
        }
    }
}
