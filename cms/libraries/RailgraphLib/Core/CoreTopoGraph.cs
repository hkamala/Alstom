using RailgraphLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.RailExtension;
using RailgraphLib.HierarchyObjects;

namespace RailgraphLib.Core
{
	using OBJID = UInt32;

	public class CoreTopoGraph : TopoGraph
	{
		public CoreTopoGraph(CoreGraph coreGraph) : base(coreGraph)
		{

		}

		protected override void initialize() {}

		protected override void shutdown() {}

		internal override bool canConnectGraphObjWithCoreObj(OBJID UInt32, CLASS_TYPE classType)
		{
			if (classType == Enums.CLASS_TYPE.CLASS_VERTEX || classType == Enums.CLASS_TYPE.CLASS_EDGE)
				return true;

			return false;
		}

		internal override void railGraphCreated() { }

		public ECreateExtensionResult createCoreExtension(OBJID startId, OBJID endId, int distanceFromStart, int distanceFromEnd, Enums.EDirection eDir, ref List<OBJID> viaElements, ref CoreExtension coreExtensionResult)
		{
			TopoConverter converter = new TopoConverter();
			return converter.createExtension(startId, endId, distanceFromStart, distanceFromEnd, eDir, viaElements, this, coreExtensionResult);
		}
	}
}
