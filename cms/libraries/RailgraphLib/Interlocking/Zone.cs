using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.armd;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using BINTYPE = UInt64;
	using OBJTYPE = UInt16;

	public class Zone : ILGraphObj
	{
		public enum EZoneType
		{
			WorkZone = Enums.SYSOBJ_TYPE.TYP_WORKZONE,
			ShuntingZone = Enums.SYSOBJ_TYPE.TYP_SHUNTZONE,
			BridgeZone = Enums.SYSOBJ_TYPE.TYP_BRIDGEZONE,
			StaffCrossingZone = Enums.SYSOBJ_TYPE.TYP_STAFFCROSSINGZONE,
			OtherZone = Enums.SYSOBJ_TYPE.TYP_OTHERZONE
		};

		public Zone(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

		public void addTrackId(OBJID idTrack)
		{
			if (!m_trackVector.Contains(idTrack))
				m_trackVector.Add(idTrack);
		}

		public IReadOnlyList<OBJID> getAssociatedTracks() => m_trackVector;

		// gets 
		public EZoneOwner getZoneOwner()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE Owner = (dynBits & ArmdPredefinedIf.getZoneOwnerMask());

			if (Owner == ArmdPredefinedIf.getZoneOwnerNoneBits())
				return EZoneOwner.ownerNone;
			else if (Owner == ArmdPredefinedIf.getZoneOwnerVptBits())
				return EZoneOwner.ownerVpt;
			else if (Owner == ArmdPredefinedIf.getZoneOwnerAreaSupervisorBits())
				return EZoneOwner.ownerAreaSupervisor;
			return EZoneOwner.ownerUndefined;
		}
		
		public EZoneState getZoneState()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE State = (dynBits & ArmdPredefinedIf.getZoneStateMask());

			if (State == ArmdPredefinedIf.getZoneStateDefaultBits())
				return EZoneState.stateDefault;
			else if (State == ArmdPredefinedIf.getZoneStateGivenBits())
				return EZoneState.stateGiven;
			else if (State == ArmdPredefinedIf.getZoneStateTakenBits())
				return EZoneState.stateTaken;
			else if (State == ArmdPredefinedIf.getZoneStateReturnedBits())
				return EZoneState.stateReturned;

			return EZoneState.stateUndefined;
		}
		
		public EZoneType getZoneType() => (EZoneType) getType();

		public bool matchTracks(ref List<OBJID> tracks) => true;

		private List<OBJID> m_trackVector = new List<OBJID>();
	}
}
