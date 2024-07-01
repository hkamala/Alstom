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

	public class Point : PointMachine
	{
		public Point(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name) : base(objId, objType, classType, name) {}

		public virtual EOccupation getOccupationState()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE OccState = (dynBits & ArmdPredefinedIf.getSwitchOccupationMask());

			if (OccState == ArmdPredefinedIf.getSwitchOccupationFaultyBits())
				return EOccupation.occupationFaulty;
			else if (OccState == ArmdPredefinedIf.getSwitchOccupationReservedBits())
				return EOccupation.occupationOn;
			else if (OccState == ArmdPredefinedIf.getSwitchOccupationFreeBits())
				return EOccupation.occupationOff;
			else
				return EOccupation.occupationUnknown;
		}

		public override bool isFaulty()
		{
			if (!base.isFaulty())
			{
				BINTYPE dynBits = ILGraph.getDynamicBits(getId());
				BINTYPE FaultyState = (dynBits & ArmdPredefinedIf.getSwitchOccupationMask());

				if ((FaultyState == ArmdPredefinedIf.getSwitchOccupationFaultyBits()) || (FaultyState == ArmdPredefinedIf.getSwitchOccupationUnknownBits()))
					return true;
			}
			return false;
		}

		public override OBJID getLogicalAdj(Enums.EDirection eSearchDir, OBJID prevId = OBJID.MaxValue)
		{
			EPointPosition ePointPos = getPosition();

			if ((ePointPos == EPointPosition.pointMoving) || (ePointPos == EPointPosition.pointUnknown))
				return 0;
			else
			{
				List<OBJID> adjElements = new List<OBJID>();
				adjElements.AddRange(ILTopoGraph.getAdjacentElements(getId(), eSearchDir));
				if (adjElements.Count == 0)
					return 0; // no any adjacency available to given direction

				if (prevId != 0 && prevId != getMergeAdjacentObject() && adjElements.Count == 2)
				{
					adjElements.Clear();
					adjElements.Add(getMergeAdjacentObject());
				}

				if (adjElements.Count == 1)
				{
					// It is possible that point has only one adjacent leg in
					// both direction
					if (prevId != 0 && prevId != getMergeAdjacentObject())
					{
						if ((ePointPos == EPointPosition.pointLeft && prevId == getLeftAdjacentObject()) || (ePointPos == EPointPosition.pointRight && prevId == getRightAdjacentObject()))
							return getMergeAdjacentObject();

						return 0;
					}
					else
					{
						if (adjElements.First() == getMergeAdjacentObject())
							return getMergeAdjacentObject();
						else
						{
							if (ePointPos == EPointPosition.pointLeft)
								return getLeftAdjacentObject();
							else if (ePointPos == EPointPosition.pointRight)
								return getRightAdjacentObject();
						}
					}
				}
				else if (adjElements.Count == 2)
				{
					if (prevId == 0)
					{
						if (adjElements.Contains(getMergeAdjacentObject())) // direction changes in point, we cannot know whether to go merge or leg direction
							return 0;
					}

					if (prevId != 0 && prevId != getMergeAdjacentObject())
						return 0;

					// we don't need caller, path is always available from merge
					foreach (var adjElement in adjElements)
					{
						if (ePointPos == EPointPosition.pointLeft && adjElement == getLeftAdjacentObject())
							return getLeftAdjacentObject();

						if (ePointPos == EPointPosition.pointRight && adjElement == getRightAdjacentObject())
							return getRightAdjacentObject();
					}
				}
			}
			return 0;
		}

		public override bool hasLogicalAdj(Enums.EDirection eSearchDir, OBJID target)
		{
			if (base.hasLogicalAdj(eSearchDir, target))
			{
				EPointPosition ePointPos = getPosition();

				if ((ePointPos == EPointPosition.pointMoving) || (ePointPos == EPointPosition.pointUnknown))
					return false;

				if (target == getMergeAdjacentObject())
					return true;

				if (ePointPos == EPointPosition.pointLeft && target == getLeftAdjacentObject())
					return true;

				if (ePointPos == EPointPosition.pointRight && target == getRightAdjacentObject())
					return true;
			}

			return false;
		}

        public virtual bool isBlocked()
        {
            var mask = ArmdPredefinedIf.getSwitchBlockedMask();
            return ArmdPredefinedIf.isARMDValueInUse(mask) && (ILGraph.getDynamicBits(getId()) & mask & ArmdPredefinedIf.getSwitchBlockedBits()) != 0;
        }

        public virtual bool isBlockedOverride()
        {
            var mask = ArmdPredefinedIf.getSwitchBlockedOverrideMask();
            return ArmdPredefinedIf.isARMDValueInUse(mask) && (ILGraph.getDynamicBits(getId()) & mask & ArmdPredefinedIf.getSwitchBlockedOverrideBits()) != 0;
        }
		public virtual bool isPointOutOfControl()
        {
            if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSwitchDirMask()))
                return false;

            return (ILGraph.getDynamicBits(getId()) & ArmdPredefinedIf.getSwitchDirMask()) == ArmdPredefinedIf.getSwitchDirUnknownBits();
        }

        public virtual bool isPointFalseOccupied()
        {
            var mask = Armd.getArmdObj("DynSwitchFalseOccupiedMask");
            if (!ArmdPredefinedIf.isARMDValueInUse(mask))
                return false;

            var setBits = Armd.getArmdObj("DynSwitchFalseOccupiedSet");
            return (ILGraph.getDynamicBits(getId()) & mask) == setBits;
        }

		public OBJID getMergeAdjacentObject() => m_mergeAdjacentObject;

		public OBJID getLeftAdjacentObject() => m_leftAdjacentObject;

		public OBJID getRightAdjacentObject() => m_rightAdjacentObject;

		public bool isMergeAdjacentObject(OBJID AdjId) => getMergeAdjacentObject() == AdjId;

		public bool isLeftAdjacentObject(OBJID AdjId) => getLeftAdjacentObject() == AdjId;

		public bool isRightAdjacentObject(OBJID AdjId) => getRightAdjacentObject() == AdjId;

		public void setMergeAdjacentObject(OBJID id) => m_mergeAdjacentObject = id;

		public void setLeftAdjacentObject(OBJID id) => m_leftAdjacentObject = id;

		public void setRightAdjacentObject(OBJID id) => m_rightAdjacentObject = id;

		private OBJID m_mergeAdjacentObject = 0;
		private OBJID m_leftAdjacentObject = 0;
		private OBJID m_rightAdjacentObject = 0;
	}
}
