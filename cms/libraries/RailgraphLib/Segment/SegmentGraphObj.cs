using RailgraphLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Enums;

namespace RailgraphLib.Segment
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	class SegmentGraphObj : GraphObj
	{
		public SegmentGraphObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName)
			: base(objId, objType, classType, objName)
		{

		}

		public virtual OBJID getExternalSegmentObjId() => getExternalIdentity();
	};
}
