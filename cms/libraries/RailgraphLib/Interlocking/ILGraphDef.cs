using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Interlocking
{
	using OBJTYPE = UInt16;

	public enum ILGraphObjType
	{
		TRACK_SECTION = Enums.SYSOBJ_TYPE.TYP_TRACK,
		VIRTUAL_TRACK = Enums.SYSOBJ_TYPE.TYP_VIRTUAL_TRACK,
		DARK_TRACK = Enums.SYSOBJ_TYPE.TYP_DARK_TRACK,
		TRACK_CIRCUIT_BOUNDARY = Enums.SYSOBJ_TYPE.TYP_TRACK_CIRCUIT_BOUNDARY,

		// Track circuit group types 
		TRACK_CIRCUIT = Enums.SYSOBJ_TYPE.TYP_TRACK_CIRCUIT,

		// point types
		POINT = Enums.SYSOBJ_TYPE.TYP_POINT,
		POINT_MACHINE = Enums.SYSOBJ_TYPE.TYP_POINT,
		POINT_DERAILER = Enums.SYSOBJ_TYPE.TYP_DERAILER,
		POINT_LEG = Enums.SYSOBJ_TYPE.TYP_POINT_LEG,

		// Crossing group types 
		CROSSING_TRACK = Enums.SYSOBJ_TYPE.TYP_CROSSING,
		CROSSING_TRACK_TS = Enums.SYSOBJ_TYPE.TYP_CROSSING_TS,

		// Signal group types
		MAIN_SIGNAL = Enums.SYSOBJ_TYPE.TYP_MAIN_SIGNAL,
		SHUNTING_SIGNAL = Enums.SYSOBJ_TYPE.TYP_SHUNTING_SIGNAL,
		COMBINED_SIGNAL = Enums.SYSOBJ_TYPE.TYP_COMBINED_SIGNAL,
		DEPART_SIGNAL = Enums.SYSOBJ_TYPE.TYP_DEPARTING_SIGNAL,
		FICTIVE_SIGNAL = Enums.SYSOBJ_TYPE.TYP_FICTIVE_SIGNAL,

		// others
		BUFFER_STOP = Enums.SYSOBJ_TYPE.TYP_BUFFER_STOP,
		BALISE = Enums.SYSOBJ_TYPE.TYP_BALISE,
		LINEBLOCK = Enums.SYSOBJ_TYPE.TYP_LINE_BLOCK,
		LEVEL_CROSSING = Enums.SYSOBJ_TYPE.TYP_LEVEL_CROSSING,
		CONTROL_OBJECT = Enums.SYSOBJ_TYPE.TYP_CONTROL_OBJECT,
		ZONE = Enums.SYSOBJ_TYPE.TYP_ZONE,

		EXTERNAL_OWNER = Enums.SYSOBJ_TYPE.TYP_EXTERNAL_OWNER,
		WORKZONE = Enums.SYSOBJ_TYPE.TYP_WORKZONE,
		SHUNTZONE = Enums.SYSOBJ_TYPE.TYP_SHUNTZONE,
		BRIDGEZONE = Enums.SYSOBJ_TYPE.TYP_BRIDGEZONE,
		STAFFCROSSINGZONE = Enums.SYSOBJ_TYPE.TYP_STAFFCROSSINGZONE,
		OTHERZONE = Enums.SYSOBJ_TYPE.TYP_OTHERZONE,

		ETCS_LEVEL = Enums.SYSOBJ_TYPE.TYP_ETCS_LEVEL,
		INTERLOCKING = Enums.SYSOBJ_TYPE.TYP_INTERLOCKING,
	}
	
	/// aspect states used in RailGraph
	public enum EAspect
	{
		aspectClear,        ///< aspect showing proceed
		aspectStop,         ///< aspect showing stop/red
		aspectFaulty,       ///< aspect state is reported to faulty
		aspectUnknown       ///< aspect state is unknown
	};

	/// deprecated ???
	public enum ESignalPriorityMode
	{
		priorityUnknown,
		priorityOff,
		priorityOn
	};

	/*
	enum ERoute
	{
		routeReserved,
		routeUnLocked,
		routeCancelled,
		routeUnknown
	};*/

	/// Route Locking state of tracks and points
	public enum ERouteLocking
	{
		routeUnlocked,      ///< route not locked
		routeUp,                    ///< track section locked to up direction
		routeDown,              ///< track section locked to down direction
		routeUpLeft,            ///< point/crossing track locked to up-left direction
		routeUpRight,           ///< point/crossing track locked to up-right direction
		routeDownLeft,      ///< point/crossing track locked to down-left direction
		routeDownRight,     ///< point/crossing track locked to down-right direction
		routeUnknown            ///< state is unknown
	};

	/// point position locking state
	public enum EPointLocking
	{
		pointUnlocked,              ///< not locked
		pointLockedLeft,            ///< locked left
		pointLockedRight,           ///< locked right
		pointLockingUnknown     ///< locking state unknown
	};

	/// an occupation state of track type objects (track section, point, crossing etc.)
	public enum EOccupation
	{
		occupationOff,              ///< not occupied
		occupationOn,                   ///< occupied
		occupationFaulty,           ///< occupation reported to faulty (by interlocking)
		occupationUnknown           ///< state unknown
	};

	/// point position state
	public enum EPointPosition
	{
		pointLeft,                  ///< position is left
		pointRight,                 ///< position is right
		pointMoving,                ///< position is moving from left to right or right to left
		pointUnknown                ///< position unknown
	};

	/// Crossing position
	public enum ECrossingPosition
	{
		position01 = 0,         ///< from 0 to 1 direction
		position23,                 ///< from 2 to 3 direction
		positionBoth                ///< from 0 to 1 direction and from 2 to 3 direction
	};

	/// deprecated ???
	public enum EZoneOwner
	{
		ownerNone,
		ownerVpt,
		ownerAreaSupervisor,
		ownerUndefined
	};

	/// deprecated ???
	public enum EZoneState
	{
		stateDefault,
		stateGiven,
		stateTaken,
		stateReturned,
		stateUndefined
	};
}
