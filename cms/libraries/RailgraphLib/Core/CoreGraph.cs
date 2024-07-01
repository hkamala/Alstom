using RailgraphLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Core
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	public class CoreGraph : Graph
	{
		public CoreGraph() { }
		
		public CoreGraphObj getCoreGraphObj(OBJID coreGraphObjId) => getGraphObj(coreGraphObjId) as CoreGraphObj;

		protected override void initialize(bool initSharedMemory) { }

		protected override void shutdown() { }

		protected override GraphObj createGraphObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name, Enums.EDirection eDir)
		{
			switch (classType)
			{
				case Enums.CLASS_TYPE.CLASS_VERTEX: return createVertex(objId, objType, classType, name, eDir);
				case Enums.CLASS_TYPE.CLASS_EDGE: return createEdge(objId, objType, classType, name, eDir);
				default: return null;
			};
		}
		protected virtual GraphObj createVertex(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir) => new Vertex(objId, objType, classType, objName, eDir);

		protected virtual GraphObj createEdge(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir) => new Edge(objId, objType, classType, objName, eDir);

		public override void commonGraphCreated() { }

		public override void interestedAssociation(ref List<Enums.HT_TYPE> associations) { }
		public override void associationCreated(UInt32 masterId, UInt32 associationId, Enums.HT_TYPE associationType) { }
	}
}
