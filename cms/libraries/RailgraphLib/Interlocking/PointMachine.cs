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

	public class PointMachine : ILGraphObj
	{
		public PointMachine(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

		public virtual EPointPosition getPosition()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE switchdir = (dynBits & ArmdPredefinedIf.getSwitchDirMask());
			if (switchdir == ArmdPredefinedIf.getSwitchDirMovingBits())
				return EPointPosition.pointMoving;
			else if (switchdir == ArmdPredefinedIf.getSwitchDirLeftBits())
				return EPointPosition.pointLeft;
			else if (switchdir == ArmdPredefinedIf.getSwitchDirRightBits())
				return EPointPosition.pointRight;
			else
				return EPointPosition.pointUnknown;
		}

		public virtual EPointPosition getNormalPosition()
		{
			EPointPosition ePos = EPointPosition.pointRight; // try right position

			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if ((staBits & ArmdPredefinedIf.getSwitchNormalDirLeftBits()) > 0)
				ePos = EPointPosition.pointLeft; // really left is normal

			return ePos;
		}

		public virtual EPointPosition getRoutePosition(OBJID PrevElement, OBJID NextElement)
		{
			EPointPosition ePos = EPointPosition.pointMoving;   // position not known

			Point point = this as Point;

			if (point == null)
				return ePos;

			if (((point.getLeftAdjacentObject() == PrevElement) && (point.getMergeAdjacentObject() == NextElement)) || ((point.getMergeAdjacentObject() == PrevElement) && (point.getLeftAdjacentObject() == NextElement)))
				ePos = EPointPosition.pointLeft;  // in left position
			else if (((point.getRightAdjacentObject() == PrevElement) && (point.getMergeAdjacentObject() == NextElement)) || ((point.getMergeAdjacentObject() == PrevElement) && (point.getRightAdjacentObject() == NextElement)))
				ePos = EPointPosition.pointRight;  // in right position
			return ePos;
		}

		public bool isControlModeLocal()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchCtrlModeMask()))
				return false;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());

			if ((dynBits & ArmdPredefinedIf.getSwitchCtrlModeMask()) == ArmdPredefinedIf.getSwitchCtrlModeLocalBits())
				return true;

			return false;
		}

		public bool isLocked2Route()
		{
			ERouteLocking eLocking = this.getRouteLockedState();
			if ((eLocking == ERouteLocking.routeUpLeft) || (eLocking == ERouteLocking.routeUpRight) || (eLocking == ERouteLocking.routeDownLeft) || (eLocking == ERouteLocking.routeDownRight))
				return true;

			return false;
		}

		public bool isLockedManually()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchLockedManuallyMask()))
				return false;

			bool bLocked = true; // locked
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OperLocked = (dynBits & ArmdPredefinedIf.getSwitchLockedManuallyMask());

			if (OperLocked == ArmdPredefinedIf.getSwitchNotLockedManuallyBits())
				bLocked = false;    // point not locked manually

			return bLocked;
		}

		public bool isOperationBlocked()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchOperationMask()))
			{
				return false;
			}

			bool bBlocked = true; // blocked
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OperBlocked = (dynBits & ArmdPredefinedIf.getSwitchOperationMask());

			if (OperBlocked == ArmdPredefinedIf.getSwitchOperationUnblockedBits())
				bBlocked = false;

			return bBlocked;
		}

		public bool isCancelInProgress()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchRouteCancelInProgressMask()))
				return false;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());

			if ((dynBits & ArmdPredefinedIf.getSwitchRouteCancelInProgressMask()) == ArmdPredefinedIf.getSwitchRouteCancelInProgressBits())
				return true;
			else if ((dynBits & ArmdPredefinedIf.getSwitchRouteCancelInProgressMask()) == ArmdPredefinedIf.getSwitchNotRouteCancelInProgressBits())
				return false;
			else
				return false;
		}

		public EPointLocking getManuallyLockedState()
		{
			if (ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchLockedManuallyMask()))
				return EPointLocking.pointLockingUnknown;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OperLocked = (dynBits & ArmdPredefinedIf.getSwitchLockedManuallyMask());

			if (OperLocked == ArmdPredefinedIf.getSwitchLockedManuallyLeftBits())
				return EPointLocking.pointLockedLeft;
			else if (OperLocked == ArmdPredefinedIf.getSwitchLockedManuallyRightBits())
				return EPointLocking.pointLockedRight;
			else if (OperLocked == ArmdPredefinedIf.getSwitchNotLockedManuallyBits())
				return EPointLocking.pointUnlocked;

			return EPointLocking.pointLockingUnknown;
		}
		public ERouteLocking getRouteLockedState()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchRouteMask()))
				return ERouteLocking.routeUnknown;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OperLocked = (dynBits & ArmdPredefinedIf.getSwitchRouteMask());

			if (OperLocked == ArmdPredefinedIf.getSwitchRouteUnlockedBits())
				return ERouteLocking.routeUnlocked;
			else if (OperLocked == ArmdPredefinedIf.getSwitchRouteLockedUpLeftBits())
				return ERouteLocking.routeUpLeft;
			else if (OperLocked == ArmdPredefinedIf.getSwitchRouteLockedUpRightBits())
				return ERouteLocking.routeUpRight;
			else if (OperLocked == ArmdPredefinedIf.getSwitchRouteLockedDownLeftBits())
				return ERouteLocking.routeDownLeft;
			else if (OperLocked == ArmdPredefinedIf.getSwitchRouteLockedDownRightBits())
				return ERouteLocking.routeDownRight;

			return ERouteLocking.routeUnknown;
		}
}
}
