using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class PointLeg : TrackSection
	{
		public PointLeg(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) {}

		public override EOccupation getOccupationState() => m_point.getOccupationState();

		public override bool isFaulty() => m_point.isFaulty();

		public override bool isInDarkArea() => m_point.isInDarkArea();

		public override bool isMarkedFaulty() => m_point.isMarkedFaulty();
		
		public override bool isOccupationUnreliable() => m_point.isOccupationUnreliable();

		public void associatePoint(Point point) => m_point = point;
		
		public Point getPoint() => m_point;

		private Point m_point;
	}
}
