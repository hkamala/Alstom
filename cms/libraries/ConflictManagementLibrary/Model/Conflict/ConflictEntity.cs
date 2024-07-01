using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public class ConflictEntity
    {
        public EntityType MyEntityType { get; set; }
        public string? MyDescription { get; set; }
        public string? MyName { get; set; }
        public string? MyUid { get; set; }
        public int MySubTypeIndex { get; set; }

        public static ConflictEntity CreateInstance(EntityType theEntityType, string? theDescription, string  theName ="", string? theUid ="", int theSubtypeIndex = 0)
        {
            return new ConflictEntity(theEntityType, theDescription, theName, theUid);
        }
        private ConflictEntity(EntityType theEntityType, string? theDescription, string? theName, string? theUid, int theSubtypeIndex = 0)
        {
            MyEntityType = theEntityType;
            MyDescription = theDescription;
            MyName = theName;
            MyUid = theUid;
            MySubTypeIndex = theSubtypeIndex;
        }

        [JsonConstructor]
        public ConflictEntity()
        {
            
        }
       
        [JsonConverter(typeof(StringEnumConverter))]
        public enum EntityType
        {
            Train,
            Station,
            Possession,
            Track,
            Point
        }
    }
}
