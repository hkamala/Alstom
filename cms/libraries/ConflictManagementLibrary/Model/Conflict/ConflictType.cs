using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public class ConflictType
    {
        //[JsonProperty(PropertyName = "MyConflictType")]
        public TypeOfConflict MyConflictType { get; set; }

        public static ConflictType CreateType(TypeOfConflict theConflictType)
        {
            return new ConflictType(theConflictType);
        }
        private ConflictType(TypeOfConflict theConflictType)
        {
            MyConflictType = theConflictType;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeOfConflict
        {
            Train,
            Station,
            Track,
            Point
        }
    }
}
