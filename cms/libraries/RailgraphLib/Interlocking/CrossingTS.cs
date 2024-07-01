using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using BINTYPE = UInt64;
	using OBJTYPE = UInt16;

	public class CrossingTS : TrackSection
	{
		public CrossingTS(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

		public OBJID getCrossingId() => m_crossing.getId();
		
		public ECrossingPosition getCrossingPosition() => m_crossing.getCrossingPosition(getId());

		public override EOccupation getOccupationState() => m_crossing.getOccupationState();

		public void associateCrossing(Crossing crossing) => m_crossing = crossing;

		private Crossing m_crossing;    // reference to crossing object
	}
}
