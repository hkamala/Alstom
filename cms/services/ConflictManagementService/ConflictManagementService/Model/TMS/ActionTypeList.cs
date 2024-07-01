using System.Xml.Serialization;

namespace ConflictManagementService.Model.TMS
{
	[XmlType("ActionTypeList")]

	public class ActionTypeList
	{
		[XmlArray("ActMainTypes")]
		public ActMainType[]? actionmaintypes;
		[XmlArray("TrainTypes")]
		public TrainTypeDetails[]? traintypes;
		[XmlArray("ServiceTypes")]
		public ServiceType[]? serviceTypes;
		[XmlArray("TripTypes")]
		public TripType[]? tripTypes;
		[XmlArray("DutyTypes")]
		public TripType[]? dutytypes;
		[XmlArray("AreaObjects")]
		public AreaObject[]? areaobjects;
		[XmlArray("TrainProperties")]
		public TrainPropertyType[]? trainProperties;
		[XmlArray("TripUserTypes")]
		public TripUserType[]? tripusertypes;
		[XmlArray("ConsistStates")]
		public ConsistState[]? trainStates;
	}

	[XmlType("ActMainType")]
	public class ActMainType
	{
		[XmlElement("ActionType")]
		public int ActionType { get; set; }
		[XmlElement("Name")]
		public string? Name { get; set; }
		[XmlArray("ActionTypes")]
		public ActionType[]? actiontypes;
	}

	[XmlType("ActionType")]
	public class ActionType
	{
		[XmlElement("ID")]
		public int ID { get; set; }
		[XmlElement("Desc")]
		public string? Description { get; set; }
		[XmlElement("MainType")]
		public int ActionMainType;
		[XmlElement("SubType")]
		public int ActionSubType;
		[XmlElement("MSec")]
		public uint MinSecs;
		[XmlElement("PSec")]
		public uint PlanSecs;
	}

	[XmlType("TrainType")]
	public class TrainTypeDetails
	{
		[XmlElement("ID")]
		public int ID { get; set; }
		[XmlElement("Description")]
		public string? Description { get; set; }
		[XmlElement("DefaultLength")]
		public int DefaultLength { get; set; }
		[XmlElement("DerivedFrom")]
		public uint DerivedFrom { get; set; }
		[XmlElement("ColorLine")]
		public string? ColorLine { get; set; }
		[XmlElement("ColorRoutedLine")]
		public string? ColorRoutedLine { get; set; }
		[XmlElement("ColorStop")]
		public string? ColorStop { get; set; }
		[XmlElement("CanBeConsist")]
		public bool CanBeConsist { get; set; }
		[XmlElement("UseAsVehicle")]
		public bool UseAsVehicle { get; set; }
		[XmlElement("CanBeChild")]
		public bool CanBeChild { get; set; }
		[XmlElement("UIImage")]
		public string? UIImage { get; set; }
	}

	public class ConsistState
	{
		[XmlElement("ID")]
		public uint ID { get; set; }

		[XmlElement("Username")]
		public string? Username { get; set; }

		[XmlElement("UseAsDeleted")]
		public bool UseAsDeleted { get; set; }

		public string? LocalisedName { get; set; }
	}
	public enum TripMainType
	{
		[XmlEnum("0")]
		TRIPTYPE_UNKNOWN,
		[XmlEnum("1")]
		COMMERCIAL_TEMPLATE,
		[XmlEnum("2")]
		NONCOMMERCIAL_TEMPLATE,
		[XmlEnum("3")]
		OPPORTUNITY_TEMPLATE,
		[XmlEnum("4")]
		COMMERCIAL,
		[XmlEnum("5")]
		NONCOMMERCIAL,
		[XmlEnum("6")]
		OPPORTUNITY,
		[XmlEnum("7")]
		SERVICE_TEMPLATE,
		[XmlEnum("8")]
		SERVICE,
		[XmlEnum("9")]
		NORMAL_DUTY,
		[XmlEnum("10")]
		SPARE_DUTY,
		[XmlEnum("11")]
		MOVEMENT_TRIP_TEMPLATE,
		[XmlEnum("12")]
		MOVEMENT_TRIP,
		[XmlEnum("13")]
		MOVEMENT_TRIP_GROUP
	};
	public class TripType
	{
		[XmlElement("ttypeid")]
		public int ID { get; set; }
		[XmlElement("mt")]
		TripMainType? mainType;
		[XmlElement("tpl")]
		public bool IsTemplate;
		[XmlElement("Desc")]
		public string? Description { get; set; }
	}
	public enum ServiceSubType
	{
		[XmlEnum("0")]
		SERVICE_NO_SUBTYPE,
		[XmlEnum("1")]
		NORMAL_SERVICE,
		[XmlEnum("2")]
		DEGRADED_SERVICE,
		[XmlEnum("3")]
		DEGRADED_SHUTTLE_SERVICE,
		[XmlEnum("4")]
		SINGLE_TRIP_SERVICE, // added for GR_DEMO
		[XmlEnum("5")]
		LOOP_TRIP_SERVICE, // added for GR_DEMO
	}
	[XmlType("ServiceType")]
	public class ServiceType
	{
		[XmlElement("txtKey")]
		public string? txtKey;
		[XmlElement("mt")]
		public TripMainType mainType;
		[XmlElement("st")]
		public ServiceSubType serviceSubType;
		[XmlElement("typeID")]
		public int typeID;
		[XmlElement("deg")]
		public bool IsDegradedService;
	}
	[XmlType("PropType")]
	public class TrainPropertyType
	{
		[XmlElement("Id")]
		public int TypeId { get; set; }
		[XmlElement("Desc")]
		public string? Description { get; set; }
	}

	public class TripUserType
	{
		[XmlElement("ID")]
		public uint ID { get; set; }
		[XmlElement("Desc")]
		public string? Description { get; set; }
	}
	public class AreaObject
	{
		[XmlElement("ID")]
		public int ID { get; set; }
		[XmlElement("Desc")]
		public string? Description { get; set; }
	}
}
