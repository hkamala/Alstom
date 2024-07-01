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

	public class Crossing : ILGraphObj
	{
		public Crossing(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName)
		{

		}

		public CrossingTS getValidCrossingTS() // ????
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE CrossingPosition = (dynBits & ArmdPredefinedIf.getCrossingPositionMask());

			if (CrossingPosition == ArmdPredefinedIf.getCrossingPosition01Bits())
				return null;
			else if (CrossingPosition == ArmdPredefinedIf.getCrossingPosition02Bits())
				return null;
			else
				return null; // unknown state
		}

		public ECrossingPosition getCrossingPosition(OBJID PrevObjId) => ECrossingPosition.positionBoth;

		public virtual EOccupation getOccupationState() => EOccupation.occupationUnknown;

		public override OBJID getLogicalAdj(Enums.EDirection eSearchDir, OBJID previousId = OBJID.MaxValue) => 0;

		public void addCrossingTS(CrossingTS pCrossingTS, ECrossingPosition eCrossingPosition) => m_crossingTSVector[(int)eCrossingPosition] = pCrossingTS;

		private List<CrossingTS> m_crossingTSVector = new List<CrossingTS>(Enum.GetNames(typeof(ECrossingPosition)).Length);
	}
}
