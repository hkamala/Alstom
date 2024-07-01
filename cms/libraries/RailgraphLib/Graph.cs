using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	public abstract class Graph
	{
		public Graph()
		{

		}

		public virtual GraphObj getGraphObj(OBJID key) => m_graphObjMap.GetValueOrDefault(key, null);
		public virtual CoreObj getCoreObj(OBJID key) => m_coreObjMap.GetValueOrDefault(key, null);
        public virtual GraphObj? getGraphObj(string name) => getGraphObjByName(name);
        public virtual CoreObj? getCoreObj(string name) => getCoreObjByName(name);
        public virtual List<OBJID> getInternalObjIdentity(OBJID exernalObjId) => m_ext2EbiMap.GetValueOrDefault(exernalObjId, new List<OBJID>());
		public virtual void iterateAllObjectsAndCallMethod(Action<OBJID> userMethod)
		{
			foreach (var kvp in m_graphObjMap)
				userMethod(kvp.Key);
		}
		public virtual void iterateAllCoreObjectsAndCallMethod(Action<OBJID> userMethod)
		{
			foreach (var kvp in m_coreObjMap)
				userMethod(kvp.Key);
		}
		public virtual bool isBoundaryEdge(OBJID id) => m_boundarySet.ContainsKey(id);
		protected abstract void initialize(bool initSharedMemory = true);
		protected abstract void shutdown();

		protected virtual CoreObj createCoreObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName)
		{
			CoreObj coreObj = null;
			switch (classType)
			{
				case Enums.CLASS_TYPE.CLASS_VERTEX:
					coreObj = createVertex(objId, objType, classType, objName);
					break;
				case Enums.CLASS_TYPE.CLASS_EDGE:
					coreObj = createEdge(objId, objType, classType, objName);
					break;
				case Enums.CLASS_TYPE.CLASS_BOUNDARY_EDGE:
					m_boundarySet.Add(objId, true);
					break;
				default:
					break;
			}

			return coreObj;
		}
		protected virtual GraphObj createGraphObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir) => new GraphObj(objId, objType, classType, objName, eDir);
		protected virtual CoreObj createVertex(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name) => new Vertex(objId, objType, classType, name);
		protected virtual CoreObj createEdge(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string name) => new Edge(objId, objType, classType, name);

		protected virtual GraphObj? getGraphObjByName(string name)
		{
			foreach (var obj in m_graphObjMap.Values)
				if (obj.getName().Equals(name))
					return obj;
            return null;
		}
        protected virtual CoreObj? getCoreObjByName(string name)
        {
            foreach (var obj in m_coreObjMap.Values)
                if (obj.getName().Equals(name))
                    return obj;
            return null;
        }
        public virtual void interestedAssociation(ref List<Enums.HT_TYPE> associations)
		{
			//shall be empty
		}
		public virtual void associationCreated(OBJID masterId, OBJID associationId, Enums.HT_TYPE associationType)
		{
			//shall be empty
		}
		public virtual void commonGraphCreated()
		{

		}
		public void startInit(bool initSharedMemory = true) => initialize(initSharedMemory);
		public void startShutdown() => shutdown();
		public GraphObj createGraphObject(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir)
		{
			GraphObj graphObj = createGraphObj(objId, objType, classType, objName, eDir);
			if (graphObj != null)
				m_graphObjMap.Add(objId, graphObj);

			return graphObj;
		}

		public CoreObj createCoreObject(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName)
		{
			CoreObj coreObj = createCoreObj(objId, objType, classType, objName);
			if (coreObj != null)
				m_coreObjMap.Add(objId, coreObj);

			return coreObj;
		}
		public List<Enums.HT_TYPE> getHierarchyConnections()
		{
			List<Enums.HT_TYPE> connections = new List<Enums.HT_TYPE>();
			interestedAssociation(ref connections);
			return connections;
		}
		public void hierarchyAssociationCreated(OBJID masterId, OBJID associationId, Enums.HT_TYPE associationType) => associationCreated(masterId, associationId, associationType);
		public void setInternalId(OBJID objNoExternal, OBJID objIdInternal)
		{
			if (!m_ext2EbiMap.ContainsKey(objNoExternal))
				m_ext2EbiMap.Add(objNoExternal, new List<uint>());

			m_ext2EbiMap[objNoExternal].Add(objIdInternal);
		}

		private SortedDictionary<OBJID, CoreObj> m_coreObjMap = new SortedDictionary<uint, CoreObj>();     // vertices and edges
		private SortedDictionary<OBJID, GraphObj> m_graphObjMap = new SortedDictionary<uint, GraphObj>();   // all except vertices and edges
		private SortedDictionary<OBJID, bool> m_boundarySet = new SortedDictionary<uint, bool>();   // boundary edges
		private static SortedDictionary<OBJID, List<OBJID>> m_ext2EbiMap = new SortedDictionary<uint, List<uint>>();        // this is common for all graphs
	}
}
