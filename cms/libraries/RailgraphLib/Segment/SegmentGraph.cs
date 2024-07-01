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

	class SegmentGraph : Graph
	{
		public SegmentGraph() { }

		public SegmentGraphObj? getSegmentGraphObj(OBJID segmentGraphObjId) 
		{
			SegmentGraphObj? pSegmentGraphObj = getGraphObj(segmentGraphObjId) as SegmentGraphObj;
			return pSegmentGraphObj; // zero or actual pointer

		}

		protected override void initialize(bool initSharedMemory = true) { }
		protected override void shutdown() { }

		protected virtual GraphObj createSegment(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir)
		{
			GraphObj pGraphObj = null;

			switch (classType)
			{
				case Enums.CLASS_TYPE.CLASS_RATO_SEGMENT:
					pGraphObj = createSegment(objId, objType, classType, objName, eDir);
					break;
				default:
					break;
			}

			return pGraphObj;
		}

		public override void commonGraphCreated() { }

		public override void interestedAssociation(ref List<HT_TYPE> associations) { }

		public override void associationCreated(uint masterId, uint associationId, HT_TYPE associationType) { }
	};
}
