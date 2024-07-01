using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
    [XmlType("TmsPTI")]
    public class TmsPTI
    {
        public TmsPTI()
        {
        }
        //      public const string ONBOARD_GUID_COL = "obguid";
        //      public const string TRAIN_GUID_COL = "tguid";
        //      public const string TRIP_GUID_COL = "trid";
        //      public const string TEMPLATE_TRIP_GUID_COL = "ttrid";
        //      public const string TRIP_TYPEID_COL = "ttypeid";
        // 
        //      public const string SERVICE_GUID_COL = "serid";
        //      public const string TRACKED_TRAIN_GUID_COL = "trguid";
        //      public const string SCHEDULED_DAY_CODE_COL = "dc";
        //      public const string DUTY_GUID_COL = "dtid";

        [XmlAttribute("wbydc")]
        public string? lastUpdatedByDC;
        [XmlAttribute("trguid")]
        public string? trackedTrainGUID;

        // Physical train
        [XmlAttribute("obguid")]
        public string? onBoardGUID;
        [XmlAttribute("typeid")]
        public int trainTypeID;
        [XmlAttribute("cid")]
        public int trainConsistID;

        // Service info...
        [XmlAttribute("tguid")]
        public string? trainGUID;
        [XmlAttribute("serid")]
        public string? serviceGUID;
        [XmlAttribute("dc")]
        public int serviceDayCode;
        [XmlAttribute("serdbid")]
        public int serviceDBID;

        // MockTrip info
        [XmlAttribute("trid")]
        public string? tripGUID;
        [XmlAttribute("ttypeid")]
        public int tripTypeID;
        [XmlAttribute("trdbid")]
        public int tripDBID;
    }
}
