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

	public class TrackSection : Track
	{
		public TrackSection(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

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

		public override bool isFaulty()
		{
			if (!base.isFaulty())
			{
				BINTYPE dynBits = ILGraph.getDynamicBits(getId());
				BINTYPE FaultyState = (dynBits & ArmdPredefinedIf.getTSOccupationMask());

				if ((FaultyState == ArmdPredefinedIf.getTSOccupationFaultyBits()) || (FaultyState == ArmdPredefinedIf.getTSOccupationUnknownBits()))
					return true;
			}
			return false;
		}
	}
}
