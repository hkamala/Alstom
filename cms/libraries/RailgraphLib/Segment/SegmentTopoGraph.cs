using RailgraphLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Enums;
using RailgraphLib.RailExtension;

namespace RailgraphLib.Segment
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	class SegmentTopoGraph : TopoGraph
	{
		public SegmentTopoGraph(ref SegmentGraph rSegmentGraph) : base(rSegmentGraph) 
		{
		}

		public ECreateExtensionResult createSegmentExtension(OBJID startId, OBJID endId, int distanceFromStart, int distanceFromEnd, EDirection eDir, 
			ref List<OBJID> rViaElements, ref SegmentExtension rSegmentExtensionResult)
		{
			TopoConverter converter = new TopoConverter();
			return converter.createExtension(startId, endId, distanceFromStart, distanceFromEnd, eDir, rViaElements, this, rSegmentExtensionResult);
		}

		protected override void initialize() { }
		protected override void shutdown() { }
		internal override bool canConnectGraphObjWithCoreObj(OBJID objId, CLASS_TYPE classType) => classType == CLASS_TYPE.CLASS_RATO_SEGMENT;
		internal override void railGraphCreated() { }
	};
}
