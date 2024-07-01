using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConflictManagementLibrary.Model.Conflict
{
    [Serializable]
    public class ConflictResolution
    {
        //[JsonProperty(PropertyName = "MyTypeOfResolution")]
        public TypeOfResolution MyTypeOfResolution { get; set; }

        public static ConflictResolution CreateInstance(TypeOfResolution theTypeOfResolution)
        {
            return new ConflictResolution(theTypeOfResolution);
        }

        [JsonConstructor]
        public ConflictResolution()
        {
            
        }
        private ConflictResolution(TypeOfResolution theTypeOfResolution)
        {
            MyTypeOfResolution = theTypeOfResolution;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeOfResolution
        {
            RouteAround,
            HoldThisTrain,
            HoldOtherTrain,
            Manual
        }
    }
}
