using RailgraphLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using OBJID = UInt32;

	public class Point : HierarchyBaseObject
	{
		private Route m_route;
		public int m_seqNo;

		public Point(OBJID sysid, string sysName, string externalID) : base(sysid, Enums.SYSOBJ_TYPE.TYP_POINT, sysName, externalID)
		{
		}

		public Route Route { get { return m_route; } }
		public int SeqNo { get { return m_seqNo; } }

		public void SetRoute(Route route) => m_route = route;
		public void SetSeqNo(int seqNo) => m_seqNo = seqNo;
	}
}
