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

	public class Track : ILGraphObj
	{
		public Track(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

        public virtual EOccupation getOccupationState()
        {
            BINTYPE dynBits = ILGraph.getDynamicBits(getId());
            BINTYPE OccState = (dynBits & ArmdPredefinedIf.getTSOccupationMask());
            if (OccState == ArmdPredefinedIf.getTSOccupationFaultyBits())
                return EOccupation.occupationFaulty;
            else if (OccState == ArmdPredefinedIf.getTSOccupationReservedBits())
                return EOccupation.occupationOn;
            else if (OccState == ArmdPredefinedIf.getTSOccupationFreeBits())
                return EOccupation.occupationOff;
            else
                return EOccupation.occupationUnknown;
        }

		public bool isTrafficDirNominal()
		{
			BINTYPE staBits = ILGraph.getStaticBits(getId());

			if ((staBits & ArmdPredefinedIf.getTSLogicalDirMask()) == ArmdPredefinedIf.getTSLogicalDirNominalBits())
			{
				if ((!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSTrafficDirMask())) || (isILTrafficDirNormal()))
					return true;

				return false;
			}
			else
			{
				if ((!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSTrafficDirMask())) || (isILTrafficDirNormal()))
					return false;

				return true;
			}
		}

		public bool isILTrafficDirNormal()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());

			if ((dynBits & ArmdPredefinedIf.getTSTrafficDirMask()) == ArmdPredefinedIf.getTSTrafficDirNormalBits())
				return true;

			return false;
		}

		public bool isTrafficDirLocked()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSTrafficDirLockedMask()))
				return false;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());

			if ((dynBits & ArmdPredefinedIf.getTSTrafficDirLockedMask()) == ArmdPredefinedIf.getTSTrafficDirLockedBits())
				return true;

			return false;
		}

		public bool isCancelInProgress()
		{
			if (ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSRouteCancelInProgressMask()) == false)
				return false;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());

			if ((dynBits & ArmdPredefinedIf.getTSRouteCancelInProgressMask()) == ArmdPredefinedIf.getTSRouteCancelInProgressBits())
				return true;
			else if ((dynBits & ArmdPredefinedIf.getTSRouteCancelInProgressMask()) == ArmdPredefinedIf.getTSNotRouteCancelInProgressBits())
				return false;
			else
				return false;
		}

		public bool isLocked2Route()
		{
			ERouteLocking eLocking = this.getRouteLockedState();
			if ((eLocking == ERouteLocking.routeUp) || (eLocking == ERouteLocking.routeDown))
				return true;

			return false;
		}

		ERouteLocking getRouteLockedState()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSRouteMask()))
				return ERouteLocking.routeUnknown;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OperLocked = (dynBits & ArmdPredefinedIf.getTSRouteMask());

			if (OperLocked == ArmdPredefinedIf.getTSRouteUnlockedBits())
				return ERouteLocking.routeUnlocked;
			else if (OperLocked == ArmdPredefinedIf.getTSRouteLockedNominalBits())
				return ERouteLocking.routeUp;
			else if (OperLocked == ArmdPredefinedIf.getTSRouteLockedOppositeBits())
				return ERouteLocking.routeDown;

			return ERouteLocking.routeUnknown;
		}

        public virtual bool isBlocked()
        {
			var mask = ArmdPredefinedIf.getTSBlockedMask();
            return ArmdPredefinedIf.isARMDValueInUse(mask) && (ILGraph.getDynamicBits(getId()) & mask & ArmdPredefinedIf.getTSBlockedBits()) != 0;
        }

        public virtual bool isBlockedOverride()
        {
            var mask = ArmdPredefinedIf.getTSBlockedOverrideMask();
            return ArmdPredefinedIf.isARMDValueInUse(mask) && (ILGraph.getDynamicBits(getId()) & mask & ArmdPredefinedIf.getTSBlockedOverrideBits()) != 0;
        }
        public virtual bool isTrackOutOfControl()
        {
            if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getTSOccupationMask()))
                return false;

            return (ILGraph.getDynamicBits(getId()) & ArmdPredefinedIf.getTSOccupationMask()) == ArmdPredefinedIf.getTSOccupationUnknownBits();
        }

        public virtual bool isTrackFalseOccupied()
        {
            var mask = Armd.getArmdObj("DynTSFalseOccupiedMask");
            if (!ArmdPredefinedIf.isARMDValueInUse(mask))
                return false;

            var setBits = Armd.getArmdObj("DynTSFalseOccupiedSet");
            return (ILGraph.getDynamicBits(getId()) & mask) == setBits;
        }

	}
}
