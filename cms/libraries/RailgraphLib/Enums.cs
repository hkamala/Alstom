using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Enums
{
	public enum EDirection
	{
		dUnknown,
		dNominal,
		dOpposite,
		dBoth
	};

	public enum ECreateExtensionResult
	{
		ceOk,                                           ///< target extension was successfully created
		ceStartNotExist,                    ///< start element is not exist or it invalid class type
		ceEndNotExist,                      ///< end element is not exist or it invalid class type
		csExtensionNotUnique,           ///< multiple results available; check that possible via points are defined correctly
		csDefinitionError,              ///< There are no path between start and end element with given via points
		ceStartDistanceNotValid,    /**< given start distance for start object is not valid
		- length of start element is shorter that given start distance
		- length of element (same start and end) smaller than sum of start and end distances */
		ceEndDistanceNotValid           ///< given end distance for end object is not valid (length of end element is shorter that given end distance)
	};

	// extension conversion result
	/// Possible convertesion results. Used for example TopoConverterIf conversion methods
	public enum EConversionResult
	{
		crOk,                                           ///< conversion was successfully done
		crNoElements,                           ///<No elements in given source extension
		crInternalDefinitionError   /**< - start- or end source element is not exist
		- start- or end target element is not exist
		- multiple results available; check that possible via points are defined correctly
		- There are no path between start and end element with given via points */
	};

	/// Directives used for finding appropriate object in FindObj and FindTrack methods. 
	public enum EFindObjSpecification
	{
		fosOpposite,                    ///< finds the most opposite(from begin) track/point object from the given vector
		fosMiddle,                      ///< same as fosMiddleOrOpposite
		fosNominal,                     ///< finds the most nominal(from end) track/point object from the given vector
		fosMiddleOrOpposite,    ///< finds the most opposite(from begin) track/point object from the given vector
		fosMiddleOrNominal      ///< finds the most nominal(from end) track/point object from the given vector
	};

	public enum ADJ_TYPE
	{
		ADJ_TRACK = 1,   // track element level
		ADJ_SIGNAL = 2,  // signal element level
		ADJ_STATION = 3, // Adjacent stations in Adjacency	 
		ADJ_RESERVED = 4,    // reserved for...
		ADJ_NEW_TRAIN_GENERATION = 5,    // ???
		ADJ_CROSSING_TO_TRACKSECTION = 6,    // crossing to crossing track section
		ADJ_TDQUEUE = 7,
		ADJ_PICTURE_IN_PICTURE = 8,
		ADJ_RESERVED_2 = 9,  // reserved 2 for...
		ADJ_SIDE_PROTECTION_POINT = 10,
		ADJ_POINTLEG_CONNECTION = 12,
		ADJ_EDGE_TO_EDGE_CONNECTION = 20,
		ADJ_TWC_NETWORK = 21,    // ATOIF specific: defines ATO routes
		ADJ_VPT_NODE_VERTEX = 22
	}

	public enum SYSOBJ_TYPE
	{
		TYP_NO_OBJECT = 0,
		TYP_ADJACENT_STATION = 3, /// Adjacent stations in Adjacency
		TYP_ADJACENT_PICTURE = 8,
		TYP_ZERO_POWER_AREA = 12, // Predefined area - Zero Power Zone
		TYP_PRED_AP_HIERACRCHY = 27, /// Object is action point nominal for PRED
		TYP_PLATFORM_TRACK = 29, /// Track is at a platform
		TYP_TRACK_AT_PLATFORM = 30, /// Id of the track connected platform Id
		TYP_TRAIN = 42,
		TYP_PLATFORM_AT_STATION = 47, /// Id of the track connected platform Id
		TYP_MAIN_SIGNAL = 51,
		TYP_SHUNTING_SIGNAL = 52,
		TYP_COMBINED_SIGNAL = 53,
		TYP_DEPARTING_SIGNAL = 54,
		TYP_CONTROL_OBJECT = 55,
		TYP_FICTIVE_SIGNAL = 56,
		TYP_BUFFER_STOP = 58,
		//TODO: COLLISION ON 59
		TYP_ATR_MODE_BUTTON = 59, // ???? ATR mode button object type (mode = degraded,normal,disturbed)
		TYP_LINE_BLOCK = 59, // ????
		TYP_POINT = 60,
		TYP_DERAILER = 61,
		TYP_CROSSING_TS = 62, //Track Section in Crossing
		TYP_LEVEL_CROSSING = 63,
		TYP_TRACK = 64,
		TYP_TRAIN_BERTH = 65,
		TYP_DWELL_LAMP = 66,
		TYP_FIELD_TERMINAL = 67, //Same as Site
		TYP_TSR_AREA = 68,
		TYP_NON_CORE = 73, // class for Ertms non-core type objects
		TYP_PLC = 75,
		TYP_AUTOMATON = 76, // Automaton object
		TYP_AREAS = 77,
		TYP_ZONE = 78,
		TYP_STATIC_OBJECT = 86,
		TYP_TRACK_CIRCUIT = 87,
		TYP_BORDER_SAFETY = 89, // class for Ertms Border Safety type objects
		TYP_VIRTUAL_TRACK = 90,
		TYP_CROSSING = 92,
		// static const UInt16 TYP_DIAMOND_CROSSING = -1,
		TYP_LOCKING_DEVICE = 94, // class for Ertms Locking Devices
		TYP_DETECTOR = 95, // class for Ertms detector type objects
		TYP_CBR_POINT = 96,
		TYP_ANALOG_MEAS = 100, // Analog measurement
		TYP_COUNTER_VAL = 101, // Counter value
		TYP_PULSE_MEAS = 102, // Pulse measurement
		TYP_CALC_REAL = 103, // Calculated real value
		TYP_REAL_static = 104, // Real static constant
		TYP_GRADIENT = 105, // Gradient (rate of change)
		TYP_CALC_PNT = 110, // Derived (calculated) status point
		TYP_MAN_UPD_PNT = 111, // Manually updated status point
		TYP_CONN_STAT = 112, // Connection status
		TYP_PNT_NO_CTRL = 113, // Status point without control
		TYP_PNT_OPEN_CLOSE = 114, // status point with open/close ctrl.
		TYP_PNT_OFF_ON = 115, // Status point with off/on ctrl.
		TYP_PNT_MAN_AUT = 116, // Status point with manual/autom ctrl.
		TYP_PNT_LOWER_RAISE = 117, // Status point with lower/raise ctrl.
		TYP_PNT_MODE_SETTING = 119, // Status Point for Mode setting
		TYP_SUPERVISION_SECTION = 120, // Supervision section object
		TYP_SPEED_AREA = 121, // Predefined area - Temporary/Percentual speed restriction area
		TYP_DECELERATE_AREA = 122, // Predefined area - Reduced deceleration rate area
		TYP_ACCELERATE_AREA = 122, // Predefined area - Reduced acceleration rate area
		TYP_STOP_AREA = 123, // Predefined area - Code amber or Hold
		TYP_DEICING_AREA = 124, // Predefined area - De-Icing
		TYP_AREA_CMD_AREA = 125, // Area object for area commands (eg. InhibitAtoMode cmd (CBI Area)
		TYP_CBR_ALARM = 126, // CBR Alarm or Event
		TYP_CBR = 127, // CBR (Computer based radioblock)
		TYP_SUBSYSTEM = 128, // subsystem (not in use)
		TYP_INTERLOCKING = 129, // name and number of IL
		TYP_LOGICAL_SITE = 130, // logical substation
		TYP_PHYSICAL_RTU = 131, // physical RTU
		TYP_AUTHORITY = 132, // Authority area
		TYP_WORKSTATION = 133, // Work station
		TYP_OPERATOR = 134, // Operator ID
		TYP_ROLE = 135, // Role ID
		TYP_ACTIVITY = 136, //
		TYP_COMMAND = 137, // Comes from PersistentLib (?)
		TYP_CALC_SHEET_GROUP = 139, // Calculation sheet group
		TYP_CALC_SHEET = 140, // Calculation sheet
		TYP_PROCESS = 141, // Processes
		TYP_CYCLIC_ACTIV = 143, // Cyclic activation
		TYP_MENU_ITEM = 144, // Menu item
		TYP_MENU = 145, // Menu
		TYP_PROFILE = 146, // Profile ID
		TYP_GGR = 150, // Guide group
		TYP_PLATFORM = 157, /// Platform. Same to TYP_PLATFORM_AT_STATION?
		TYP_GRAPH_SET = 159,
		TYP_HISTORY_GRAPH_SET = 160,
		TYP_SITE_GROUP = 161, // From VB
		TYP_OBJECT_TYPE = 163, // New definition from UIC?
		TYP_TRAINGRAPH = 169, // Object type number of train graph
		TYP_TRAINGRAPH_LINE = 170, // Object type number of train graph line
		TYP_TRAINGRAPH_STATIONPAIR = 171, // Object type number of train graph station pair
		TYP_EVENTSTYLE = 172, // Object type number of event style
		TYP_TRAFFICSTATION = 175, // Object type number of traffic station
		TYP_AP_NOMINAL_OBJECT = 177, /// Object is Master object for nominal AP

		// Following object types recerved for CDH for R4 alarms
		// Used in definitions in CrosRef table
		// 179 Alarm_SPU, TMS, ILA
		// 180 Alarm_FSU(FSA_and_FSB)
		// 181 Alarm_OC
		// 182 Alarm_PCU(CCU5)
		// 183 Alarm_YARD

		TYP_TRAINPROFILE = 184, // Object type number of train profile
		TYP_TDQUEUE = 190, // TDS Train Describer Queue
		TYP_AP_OPPOSITE_OBJECT = 193, /// Object is Master object for opposite AP
		TYP_CMDSET = 195,
		TYP_ROUTE2 = 196,
		TYP_COMBINED_ROUTE2 = 197,
		TYP_VIEWSET = 200,
		TYP_VIEWRECT = 201,
		TYP_APPLICATION = 203, // Application (UI,CDH,EH etc.)
		TYP_ADDON = 204, // Addon (Event,TDS,RA,PAS etc.)
		TYP_PICTURE = 206, // Picture
		TYP_MAIL_OBJECT = 207, // Object type for Mail SysObject
		TYP_WORKZONE = 210, // Work zone
		TYP_SHUNTZONE = 211, // Shunting zone
		TYP_BRIDGEZONE = 212, // Bridge zone
		TYP_STAFFCROSSINGZONE = 213, // Staff crossing zone
		TYP_OTHERZONE = 214, // Other zone
		TYP_ATR_AREA = 215, // Regulation area object type.
		TYP_TRACK_CIRCUIT_BOUNDARY = 216,
		TYP_CBR_RBU = 217, // CBR RBU (ref. ESSAHS15090D056 FFFIS ERTMS PCI - RBC Version 2.0.1)
		TYP_EDGE = 220, // used e.g. in RailGraph
		TYP_VERTEX = 221, // used e.g. in RailGraph
		TYP_BRANCH = 222, // used e.g. in Vpt Graph
		TYP_NODE = 223, // used e.g. in Vpt Graph
		TYP_BALISE = 224,
		TYP_REQUEST = 225, // Request -> IL command mapping
		TYP_BOUNDARY_EDGE = 227, // used e.g. in RailGraph
		TYP_POINT_LEG = 228,
		TYP_DOUBLE_SLIP = 229,
		TYP_ETCS_LEVEL = 230, // ETCS Level objects
		TYP_EXTERNAL_OWNER = 231, // actually VPT_SYSTEM in DB! Here it is given more general meaning because of RailGraph
		TYP_NETWORK = 232, // IL network, balise network, segment network, ...
		TYP_VEHICLE = 234, // used e.g. in TS
		TYP_SLIPPERY_AREA = 236, // slippery area, typically collection of tracks with slippery restriction information
		TYP_DARK_TRACK = 238, // Track without interlocking indications
		TYP_TRACK_DISPLAY_GROUP = 239, // TDS "Display Box Union"
		TYP_ANNOUNCEMENT_TYPE = 240, // Announcement type
		TYP_PHRASE_TYPE = 241, // Phrase type
		TYP_PHRASE = 242, // Phrase
		  //TODO: COLLISION on 243
		TYP_DRIVER = 243, // Driver of VEHICLE (project Metro De Porto)
		TYP_GROUP_SYMBOL = 243, // Symbol in UIC view that contains child CTC SYSOBJects
		TYP_RATO_SEGMENT = 245,
		TYP_RATO_REGION = 246,
		TYP_RATO_RATP = 247,
		TYP_RATO_TRS = 248,
		TYP_RATO_MISCIO = 249, // RATO misc IO device
		TYP_RATO_COMPOSITION = 250, // Fixed vehicle composition (RATO Train)
		TYP_RATO_EOCELL = 251,
		TYP_RATO_MASTER_RACK = 252,
		TYP_TRANCEIVER_PAIR = 253, // Trainceiver pair
		TYP_RATO = 254, // RATO as device
		TYP_ATR_STATE_BUTTON = 255, // ATR state button object type (state = enable,disable)
		TYP_SIVLINE = 256, // SIVLINE
		TYP_CAR = 258, // Car in vehicle
		TYP_CABIN = 259, // Cabin in vehicle
		TYP_LINE = 260, // Line (PIEDI)
		TYP_ADIF_TRACK = 261,
		TYP_ADIF_STRETCH = 262,
		TYP_RATOTRAIN = 300,
		TYP_SEGMENT = 301,
		//TYP_TRANSAREA = 302,
		//TYP_RATP = 303,
		TYP_REGION = 304,
		//TYP_EOCELL = 305,
		TYP_AUHTAREAGROUP = 306, //authority area group, collection of authority areas
		TYP_AVI_READER = 307,
		TYP_PLATFORM_DOOR = 308,
		TYP_TRAINSTOPAREA = 400,
		TYP_WASHPLANT = 401,
	}

	public enum SCOPE_TYPE
	{
		SCOPE_SYSTEM = 0,	// system specific object
		SCOPE_INTERLOCKING = 1,	// interlocking specific object
		SCOPE_NETWORK = 2,	// network specific object
		SCOPE_MEASURE_OR_CALCULATED = 3,	//  -"- specific object
		SCOPE_COMMAND = 4,	//  -"- specific object
		SCOPE_GUIDING = 5,	//  -"- specific object
		SCOPE_HISTORY = 6,	//  -"- specific object
		SCOPE_ROUTE = 7,	//  -"- specific object
		SCOPE_TRAFFIC = 8,	//  -"- specific object
		SCOPE_AREA = 9,	//  -"- specific object
		SCOPE_GRAPH = 10,	//  -"- specific object
		SCOPE_SCHEDULE = 11,   //  -"- specific object
	}

	public enum HT_TYPE
	{
		HT_IL = 1, // UI, CDH: Interlocking or Central Controller - Obj..
		HT_SITE = 2, // UI, CDH: Site or Logical Station - Object.
		HT_PHI_STAT = 3, // UI, CDH: Physical Station - Object.
		HT_OPER_PROF = 4, // AUTH: Operator - Profile.
		HT_PROF_ROLE = 5, // AUTH: Profile - Role.
		HT_ROLE_ACTIVITY = 6, // AUTH: Role - Activity.
		HT_PROFILE_AUTHAREA = 7, // AUTH: Profile - Authority Area.
		HT_AUTHAREA_OBJ = 8, // AUTH: Authority Area - Object.
		HT_RELEVANT_ACTIVITY = 9, // AUTH:activities relevant for an object of command type (menu, command, etc) of UIC
		HT_ROLE_AUTHAREA = 10, // AUTH: Role - Authority Area, predefined areas for new profile creation
		HT_LOG_STAT_GUIDEGRP = 11, // PAS: Logical Station - Guide Group.
		HT_GUIDEGRP_GUIDEDEV = 12, // PAS: Guide Group - Guide Device.
		HT_GUIDECLUSTER_GUIDEGRP = 14, // PAS: Guide Cluster - Guide Group.
		HT_CALC_SHEET = 15, // CB, CAL: Calculation sheet group - calc. sheet.
		HT_REPCHAIN_HISREP = 16, // REP: Report Chain - History Report.
		HT_CUSTHISTLEVEL_HISREP = 17, // REP: Custom History Level - History Report.
		HT_GRAPHSET_HISGRAPH = 18, // UI: Graph Set - History Graph.
		HT_LSTATION_STATIONGRP = 19, // UI: Locigal station - station grp.
		HT_OBJTYPE_MENU = 20, // UI: Object type -> sub menu
		HT_MENU_MENUITEM = 21, // UI: Menu -> MenuItem
		HT_RESTRICTION_PART = 22, // Restrictions parts
		HT_GUIDE_LAYOUT_GROUP = 23, // PAS: Guide layout group
		HT_PRED_AP_HIERACRCHY = 27, // PAS: Object is action point nominal or opposite for PRED 
		HT_PLATFORM_TRACK = 29, // PAS: Platform Track Sections
		HT_JOURNEY_PLAN = 41, // TTM:
		HT_JOURNEY_AND_TRAINS = 42, // TTM:
		HT_LINE_AND_ROUTE_PLANS = 43, // TTM:
		HT_VALIDITY_PERDIODIC_OBJS = 44, // TTM:
		HT_PERDIODIC_SCHEDULE_JOURNEYS = 45, // TTM:
		HT_PERDIODIC_SCHEDULE_PLANS = 46, // TTM:
		HT_STATIONS_PLATFORMS = 47, // PAS:
		HT_TRAINTYPE = 49, // Train HIERARCHYTYPE used e.g. in TDS list
		HT_STOPPED_SECTION_IN_LOGICAL_STATION = 50, // CDR:
		HT_STOPPED_SECTION_OWNED_BY_JUNCTION = 51, // CDR:
		HT_VALIDITY_PERIODS = 52, // TTM
		HT_LINE_AND_TRAINS = 53, // TTM
		HT_LINE_AND_TRAIN_PROFILES = 54, // TTM
		HT_SITE_ENTRY_OBJ_NOMINAL = 55, // RA
		HT_SITE_ENTRY_OBJ_OPPOSITE = 56, // RA
		HT_OBJTYPE_STORE = 58, // CDH: Object is of type that should stored
		HT_CBR = 59, // Connect Interlockings to CBR
		HT_CONFIRMATION_CMD = 60, // Connect confirmation commannd for critical cmd
		HT_COUPLED_POINTS = 61, // Coupled points 
		HT_CMDSET_COMMAND = 62, // UI, CDH: Connect CTC command to commans set
		HT_MAP_CMDSET = 63, // UI, CDH: Map command set to CTC object
		HT_ROUTE_VIAOBJ = 64, // UI, RA, ROS: Via object of route or combined route
		HT_ROUTE_FOCUSING = 65, // UI, RA, ROS: Via objects of route for Routing Automation
		HT_CONTROL_AREA = 77, // RA: Control area <-> interlocking objects
		HT_APP_MENU_ITEM = 78, // UI: menu item (member) of activity (master)
		HT_APP_MENU = 79, // UI/Addon: -> Menu
		HT_MACHINE_VIEWSET = 80, // UI: Machine N <-> N viewset
		HT_VIEWSET_VIEWRECT = 81, // UI: viewset N <-> viewrect
		HT_VIEWRECT_COMMAND = 82, // UI: viewser <-> N command
		HT_VIEWSET = 83, // UI: operator view set
		HT_TOOLBAR = 84, // UI: toolbar of application or Addon
		HT_PICTURE_AND_COMMAND = 85, // UI:
		HT_MAIL_SENDER = 86, /// Mail receiver SysId
		HT_MAIL_RECEIVER = 87, /// Mail sender SysId
		HT_NOTE_SYSOBJECT = 88, /// Note target SysId
		HT_ZONE_TRACKSECTION = 89, /// SP: Track section of zone
		HT_DOUBLE_SLIP_POINT = 90, /// connection from double slip to points
		HT_AUTMAREA_CONTROLOBJ = 92, /// Authority Area -> Auth Area control object (Spanish 3.2).
		HT_ETCS_LEVEL = 93, // CBR: ETCS Level - Object
		HT_EXTERNAL_OWNER = 94, // External Owner - Object
		HT_VPT_SYS_OBJECT = 94, // VPT: External Owner - Object
		HT_OPERATOR_ROLE_CATEGORIES = 96, // UI:
		HT_ANNOUNCEMENT_STRUCTURE = 97, // Announcement Type - Phrase Type
		HT_PHRASE_PHRASETYPE = 98, // Phrase - Phrase type
		HT_WS_LOGICALSITE = 99, // Workstation -> Locical site (under control of WS)
		HT_AUTOMATON_OBJECT = 100, // Triggering object connected to automaton
		HT_APPROACH_TRACK = 101, // SSDC Station approach track (member) AT indicatin master
		HT_LINE_CONNECT_TRACK = 102, // PIEDI
		HT_COMMUNICATION_DIR_LINES = 110, // PIEBISAT:
		HT_COMMUNICATION_DIR_EQUIPMENTS = 111, // PIEBISAT:
		HT_TRACK_DISPLAY_GROUP = 112, // TDS, TS: "Display Box Union"
		HT_VEHICLE_CARS = 113, // TDS, TS: 
		HT_PLATFORM_STOP_SECTION = 114, // TDS, TS:
		HT_PLATFORM_STOP_PROFILE = 115, // TDS, TS:
		HT_SYMBOL_GROUP = 116, // Groups SYSOBJs in UIC views under one clickable symbol
		HT_SLIPPERYAREA_TRACKSECTION = 117, /// Track section of Slippery Track Restriction Area
		HT_ATR_AREA_TO_CHILDAREA = 118, /// Master Area (divided to -> 1-N child Areas)
		HT_RATO_REGION = 119, // Connects various objects to Region
		HT_RATO_COMPOSITION = 120, // Connects Vehicles to Composition
		HT_RATO_MRACK = 121, // Connects Tranceiver Pairs to Master Rack
		HT_RATO_EOCELL = 122, // Connects Master Racks to EO Cells
		HT_OBJECTS_OF_SIVLINE = 123, // variour object types to SIVLINE
		HT_OBJTYPES_OF_NETWORK = 124, // object types connected to objects of type TYP_NETWORK
		HT_CAR_IN_VEHICLE = 125, // Car (member) connected to vehicle (master)
		HT_CABIN_IN_VEHICLE = 126, // Cabin (member) connected to vehicle (master)
		HT_PLATFORM_AREA = 130, // Platforms connected into a (predefined) area
		HT_SEGMENT_AREA = 131, // Segments connected into a (predefined) area
		HT_AREA_CMD_AREA = 132, // Objects connected into a (predefined) area for area commands (eg. InhibitATO cmd)
		HT_PLATFORM_ROADJUNCTION_NOMINAL = 133, // Platform connected to nominal road junction 
		HT_PLATFORM_ROADJUNCTION_OPPOSITE = 134, // Platform connected to opposite road junction 
		HT_EXT_SYSTEM = 135, // Connects objects to External system: CTC objects to ExtSys or eg. Server processes to Server
		HT_TRS_AM = 136, // Connects analog measurement objects to TSR objects
		HT_PLATFORM_REDUCEDAREA_NOMINAL = 137, // Platform connected to nominal reduced area 
		HT_PLATFORM_REDUCEDAREA_OPPOSITE = 138, // Platform connected to opposite reduced area 
		HT_TRACK_AND_LINE = 139, // Connects tracks and point legs to lines 
		HT_TRACK_TO_ADIF_TRACK = 140, // Connects tracks and point legs to ADIF_TRACKs 
		HT_ADIF_TRACK_TO_ADIF_STRETCH = 141, // Connects ADIF_TRACKs to ADIF_STRETCHes
		HT_AUTHAREAGROUP_TO_AUTHAREA = 142, // AUTM: Connects authority areas(sysid) to group (masterid)
		HT_AVI_READER_TO_TRACK_SECTIONS = 143, // TCS: Connects track sections(sysid) to AVI Reader (masterid)
		HT_PLATFORM_DOORS_LEFT = 144, // Platform doors left side
		HT_PLATFORM_DOORS_RIGHT = 145, // Platform doors right side
		HT_SVSA_TRACK = 198, // Connects Tracks to superVisioSection areas
		HT_TRACK_TRAINSTOPAREA = 203, // Connects Tracks to Train Stop Area
		HT_TRAIN_SLEEP_LOCATION = 204, // connects track to sleep location
	}

	public enum CLASS_TYPE
	{
		CLASS_NO_OBJECT = 0,
		CLASS_TRAIN = 42,
		CLAS_TDQUEUE = 43, // or is normal queue type 190 used instead
		CLASS_MAIN_SIGNAL = 51,
		CLASS_SHUNTING_SIGNAL = 52,
		CLASS_COMBINED_SIGNAL = 53,
		CLASS_DEPARTING_SIGNAL = 54,
		CLASS_CONTROL_OBJECT = 55,
		CLASS_FICTIVE_SIGNAL = 56,
		CLASS_BUFFER_STOP = 58,
		CLASS_LINE_BLOCK = 59,
		CLASS_POINT = 60,
		CLASS_DERAILER = 61,
		CLASS_CROSSING_TS = 62, //Track Section in Crossing
		CLASS_LEVEL_CROSSING = 63,
		CLASS_TRACK = 64,
		CLASS_TRAIN_BERTH = 65,
		CLASS_FIELD_TERMINAL = 67, //Same as Site
		CLASS_NON_CORE = 73, // class for Ertms non-core type objects
		CLASS_ZONE = 78,
		CLASS_STATIC_OBJECT = 86, // special object (generic indication)
		CLASS_TRACK_CIRCUIT = 87,
		CLASS_BORDER_SAFETY = 89, // class for Ertms Border Safety type objects
		CLASS_CROSSING = 92,
		CLASS_LOCKING_DEVICE = 94, // class for Ertms Locking Devices
		CLASS_DETECTOR = 95, // class for Ertms detector type objects
		CLASS_ANALOG_MEAS = 100, // Analog measurement 
		CLASS_COUNTER_VAL = 101, // Counter value 
		CLASS_GRADIENT = 105, // Gradient (rate of change) 
		CLASS_CALC_PNT = 110, // Derived (calculated) status point 
		CLASS_INTERLOCKING = 129,
		CLASS_LOGICAL_SITE = 130, // logical substation 
		CLASS_TRACK_CIRCUIT_BOUNDARY = 216,
		CLASS_EDGE = 220, // used e.g. in RailGraph
		CLASS_VERTEX = 221, // used e.g. in RailGraph
		CLASS_BALISE = 224,
		CLASS_BOUNDARY_EDGE = 227, // used e.g. in RailGraph
		CLASS_POINT_LEG = 228,
		CLASS_DARK_TRACK = 238, // Track without interlocking indications
		CLASS_RATO_SEGMENT = 245,
	}

	public enum EPointLegInSegmentExtension
	{
		plMerge,            ///< merge(blade)
		plLeft,             ///< left
		plRight             ///< right
	};
}
