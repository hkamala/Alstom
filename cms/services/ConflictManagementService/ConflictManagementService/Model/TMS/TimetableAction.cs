using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlInclude(typeof(RunTripAction))]
    [XmlInclude(typeof(TrainFormationAction))]
    [XmlInclude(typeof(GlueAction))]
    [XmlInclude(typeof(MovingAction))]
    [XmlInclude(typeof(StopAction))]
    [XmlInclude(typeof(PassAction))]

    public abstract class TimetableAction
    {
        public enum MainActionType
        {
            [XmlEnum("0")]
            UNKNOWN = 0,
            [XmlEnum("1")]
            RUN_TRIP,
            [XmlEnum("2")]
            PASSENGER_STOP,
            [XmlEnum("3")]
            CARGO_STOP,
            [XmlEnum("4")]
            SCHEDULING_STOP,
            [XmlEnum("5")]
            TRAIN_MOVING,
            [XmlEnum("6")]
            TRAIN_FORMATION,
            [XmlEnum("7")]
            TRAIN,
            [XmlEnum("8")]
            CREW,
            [XmlEnum("9")]
            PASS_OBJECT,
            [XmlEnum("10")]
            GLUE,
            [XmlEnum("11")]
            CUSTOMER_INFO,
            [XmlEnum("12")]
            MAINTENANCE,
            [XmlEnum("13")]
            BOOK_DUTY,
            [XmlEnum("14")]
            FULL_TRIP_DRIVE_DUTY,
            [XmlEnum("15")]
            PARTIAL_TRIP_DRIVE_DUTY,
            [XmlEnum("16")]
            MANDATORY_BREAK_DUTY,
            [XmlEnum("17")]
            OPTIONAL_BREAK_DUTY,
            [XmlEnum("18")]
            ROUTE_ACTION,
            [XmlEnum("19")]
            CMD_ACTION,
            [XmlEnum("20")]
            START_AUTOMATON_ACTION,
            [XmlEnum("21")]
            STOP_AUTOMATON_ACTION,
            [XmlEnum("22")]
            SHUNTING_ROUTE_ACTION,
            [XmlEnum("23")]
            MOVEMENT_TRIP_ACTION,
            [XmlEnum("24")]
            MOVEMENT_CONTROL
        };
        public enum MappingStatus
        {
            NA,
            CLEAR,
            ERROR,
            MAPPED,
            ERUN,
        };
        [XmlAttribute("AId")]
        public int actionId;
        [XmlAttribute("ATId")]
        public int actionTypeId;
        [XmlAttribute("PSecs")]
        public int plannedSecs;
        [XmlAttribute("MSecs")]
        public int minSecs;
        [XmlAttribute("TSecs")]
        public int endTimeFromTripStartSecs;
        [XmlAttribute("TTName")]
        public string? timetableName;
        [XmlAttribute("NodeID")]
        public int nodeId;
        [XmlAttribute("Sub")]
        public bool wasSubstituted;
        [XmlAttribute("cur")]
        public MappingStatus currentMappingStatus;
    }
}
