using RailgraphLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Segment
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	class Segment : SegmentGraphObj
	{
		public Segment(OBJID objId, OBJTYPE objType, CLASS_TYPE classType, string objName, EDirection eSegmentDir)
			: base(objId, objType, classType, objName)
		{
			m_eSegmentDir = eSegmentDir;
		}

		public virtual EDirection getSegmentDirection() => m_eSegmentDir;
		
		private EDirection m_eSegmentDir;
	};
}
