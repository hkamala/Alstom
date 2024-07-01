using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	using OBJID = UInt32;

	public class NetworkCreator : NetworkCreatorIf, ExtNetworkCreatorIf
	{
		private bool m_initialized = false;
		private List<TopoGraph> m_topoGraphList = new List<TopoGraph>();
		private SolidDB.CSolidEntryPoint m_solidEntry;
		private TopoConverter m_topoConverter = new TopoConverter();

		private List<Enums.HT_TYPE> m_neededAdjAssociations;
		private List<Enums.HT_TYPE> m_neededHierarchyAssociations;
		private HierarchyObjects.HierarchyRelations m_hierarchyRelations;

		public NetworkCreator(SolidDB.CSolidEntryPoint solidEntry, HierarchyObjects.HierarchyRelations hierRelations)
		{
			m_solidEntry = solidEntry;
			m_neededAdjAssociations = new List<Enums.HT_TYPE>();
			m_neededHierarchyAssociations = new List<Enums.HT_TYPE>();
			m_hierarchyRelations = hierRelations;
		}
		public void add(TopoGraph topoGraph) => m_topoGraphList.Add(topoGraph);

		public void create()
		{
			if (m_initialized)
				throw new Exception("Network.Creator already called!");

			m_initialized = true;

			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				List<Enums.HT_TYPE> associations = topoGraph.getAdjAssociations();
				interestedAdjAssociation(associations);
				associations = topoGraph.getGraph().getHierarchyConnections();
				interestedHierarchyAssociation(associations);
			}

			createCoreObjects();
			createGraphObjects();
			createGraphHierarchies();
			createGraphAdjacencies();

			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				topoGraph.getGraph().startInit();
				topoGraph.startInit();
				topoGraph.getGraph().commonGraphCreated();
				topoGraph.railGraphCreated();
			}
		}

		private void createGraphAdjacencies()
		{
			var graphAdj = m_solidEntry.GetAdjacencies(m_neededAdjAssociations);

			foreach (var item in graphAdj)
			{
				TopoGraph.EDirChangeDirection eDirChange = TopoGraph.EDirChangeDirection.noneDirChange;

				int dirChange = (short)item[SolidDB.CSolidEntryPoint.DIRCHANGE];
				if (dirChange == 2)
					eDirChange = TopoGraph.EDirChangeDirection.fromNomToOpp;
				else if (dirChange == 3)
					eDirChange = TopoGraph.EDirChangeDirection.fromOppToNom;
				else if (dirChange == 4)
					eDirChange = TopoGraph.EDirChangeDirection.fromNomToOppSingleEdge;
				else if (dirChange == 5)
					eDirChange = TopoGraph.EDirChangeDirection.fromOppToNomSingleEdge;
				else if (dirChange == 6)
					eDirChange = TopoGraph.EDirChangeDirection.fromNomToOppSingleEdgeAsMaster;
				else if (dirChange == 7)
					eDirChange = TopoGraph.EDirChangeDirection.fromOppToNomSingleEdgeAsMaster;

				foreach (TopoGraph topoGraph in m_topoGraphList)
				{
					TopoGraph.AdjConnection adj;
					adj.associationType = (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE];
					adj.masterId = (UInt32)(int)item[SolidDB.CSolidEntryPoint.MASTERID];
					adj.adjId = (UInt32)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONID];
					adj.jointId = (UInt32)(int)item[SolidDB.CSolidEntryPoint.JOINTID];
					adj.eDirChange = eDirChange;
					adj.leg = (short)item[SolidDB.CSolidEntryPoint.LEGNO];
					adj.adjLeg = (short)item[SolidDB.CSolidEntryPoint.ADJLEGNO];
					adj.cost = (int)item[SolidDB.CSolidEntryPoint.COST];

					topoGraph.adjAssociationCreated(ref adj);
				}
			}
		}

		private void createGraphHierarchies()
		{
			var graphHierarchies = m_solidEntry.GetHierarchies(m_neededHierarchyAssociations);

			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				foreach (var item in graphHierarchies)
				{
					topoGraph.getGraph().hierarchyAssociationCreated((UInt32)(int)item[SolidDB.CSolidEntryPoint.MASTERID], 
						(UInt32)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONID], (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE]);
				}
			}
		}

		public void destroy()
		{
			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				topoGraph.getGraph().startShutdown();
				topoGraph.startShutdown();
			}
		}
		public TopoConverterIf getConverter() => m_topoConverter;

		public void createCoreObjects()
		{
			var coreObjs = m_solidEntry.ReadCoreObjects((int)Enums.SCOPE_TYPE.SCOPE_NETWORK, (int)Enums.SYSOBJ_TYPE.TYP_VERTEX, (int)Enums.SYSOBJ_TYPE.TYP_EDGE, (int)Enums.SYSOBJ_TYPE.TYP_BOUNDARY_EDGE);
			var offsets = m_solidEntry.ReadOffsetSections();

			if (coreObjs.Count == 0)
				return;

			foreach (var item in coreObjs)
			{
				Int64 staticBits = (Int64)item[SolidDB.CSolidEntryPoint.STATICBITS];
				if (((UInt64)staticBits & armd.ArmdPredefinedIf.getRailObjTdsFunctionalityMask()) == armd.ArmdPredefinedIf.getRailObjTdsDisabledBits())
					continue;

				foreach (TopoGraph topoGraph in m_topoGraphList)
				{
					Graph rGraph = topoGraph.getGraph();
					CoreObj coreObj = rGraph.createCoreObject((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID], (UInt16)(short)item[SolidDB.CSolidEntryPoint.OBJTYPE], 
						(Enums.CLASS_TYPE)(int)item[SolidDB.CSolidEntryPoint.CLASSTYPE], (string)item[SolidDB.CSolidEntryPoint.SYSNAME]);
					if (coreObj != null)
					{
						coreObj.setUsageDir((Enums.EDirection)((int)item[SolidDB.CSolidEntryPoint.USAGEDIR]));
						coreObj.setLength((int)item[SolidDB.CSolidEntryPoint.LEN]);
						coreObj.setDistanceFromInitPoint((int)item[SolidDB.CSolidEntryPoint.DISTFROMINITPOINT]);

						if (coreObj is Edge e && offsets.ContainsKey((int)e.getId()))
						{
							SortedDictionary<string, int> offsetVals = offsets[(int)e.getId()];
							e.setStartOffsetSection(offsetVals[SolidDB.CSolidEntryPoint.STARTOFFSETSECTION]);
							e.setStartOffsetSection(offsetVals[SolidDB.CSolidEntryPoint.STARTOFFSETSECTION]);
							e.setEndOffsetSection(offsetVals[SolidDB.CSolidEntryPoint.ENDOFFSETSECTION]); 
							e.setEndOffset(offsetVals[SolidDB.CSolidEntryPoint.ENDOFFSET]);
						}
					}

					// in some configuration the core object can be also graph object. Let's graph to do that decision
					GraphObj graphObj = rGraph.createGraphObject((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID], (UInt16)(short)item[SolidDB.CSolidEntryPoint.OBJTYPE], 
						(Enums.CLASS_TYPE)item[SolidDB.CSolidEntryPoint.CLASSTYPE], (string)item[SolidDB.CSolidEntryPoint.SYSNAME], (Enums.EDirection)((short)item[SolidDB.CSolidEntryPoint.RDIRE]));
					if (coreObj != null && graphObj != null)
					{
						UInt64 AMask = ((((UInt64)(int)item[SolidDB.CSolidEntryPoint.AUTHMASK2]) << 32) | ((UInt64)(int)item[SolidDB.CSolidEntryPoint.AUTHMASK1]));

						// typical usage dir is get via associated core obj
						graphObj.setUsageDir(coreObj.getUsageDir());

						// setting common properties for all graph objects
						graphObj.setOperName((string)item[SolidDB.CSolidEntryPoint.OPERNAME]);
						graphObj.setAuthorityMask(AMask);
						graphObj.setLength((int)item[SolidDB.CSolidEntryPoint.LEN]);
						graphObj.setDistanceToOppVertex((int)item[SolidDB.CSolidEntryPoint.DISTANCE1]);
						graphObj.setDistanceToNomVertex((int)item[SolidDB.CSolidEntryPoint.DISTANCE2]);

						UInt32 coreId = (UInt32)(int)item[SolidDB.CSolidEntryPoint.COREOBJID];
						if (coreId == 0)
						{
							// There is no higher level core network defined for this core network.
							// Set this to be same as objId of CoreObj that general finding methods can work.
							coreId = (UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID];

							// in coregraph there can be currently associated only one graph object to core object; Core object itself
							List<UInt32> elements = new List<uint>();
							elements.Add((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID]);
							coreObj.setAssociatedObjects(elements);
						}

						graphObj.setCoreId(coreId);
						graphObj.setExternalIdentity((UInt32)(short)item[SolidDB.CSolidEntryPoint.OBJNO]);
						graphObj.setDistanceFromInitPoint((int)item[SolidDB.CSolidEntryPoint.DISTFROMINITPOINT]);
					}
				}
			}
		}

		private void createGraphObjects()
		{
			var graphObjects = m_solidEntry.GetGraphObjects(Enums.SCOPE_TYPE.SCOPE_NETWORK);

			foreach (var item in graphObjects)
			{
				foreach (TopoGraph topoGraph in m_topoGraphList)
				{
					topoGraph.getGraph().setInternalId((UInt32)(short)item[SolidDB.CSolidEntryPoint.OBJNO], (UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID]);
					CoreObj coreObj = topoGraph.getGraph().getCoreObj((UInt32)(int)item[SolidDB.CSolidEntryPoint.COREOBJID]);
					if (coreObj == null)
						break;

					GraphObj graphObj = topoGraph.getGraph().createGraphObject((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID], (UInt16)(int)item[SolidDB.CSolidEntryPoint.OBJTYPENO], 
						(Enums.CLASS_TYPE)(int)item[SolidDB.CSolidEntryPoint.CLASSTYPE], (string)item[SolidDB.CSolidEntryPoint.SYSNAME], (Enums.EDirection)(short)item[SolidDB.CSolidEntryPoint.RDIRE]);
					if (graphObj == null)
						continue;

					UInt64 AMask = ((((UInt64)(int)item[SolidDB.CSolidEntryPoint.AUTHMASK2]) << 32) | ((UInt64)(int)item[SolidDB.CSolidEntryPoint.AUTHMASK1]));
					graphObj.setUsageDir(coreObj.getUsageDir());

					// setting common properties for all graph objects
					graphObj.setOperName((string)item[SolidDB.CSolidEntryPoint.OPERNAME]);
					graphObj.setAuthorityMask(AMask);
					graphObj.setLength((int)item[SolidDB.CSolidEntryPoint.LEN]);
					graphObj.setDistanceToOppVertex((int)item[SolidDB.CSolidEntryPoint.DISTANCE1]);
					graphObj.setDistanceToNomVertex((int)item[SolidDB.CSolidEntryPoint.DISTANCE2]);
					graphObj.setCoreId((UInt32)(int)item[SolidDB.CSolidEntryPoint.COREOBJID]);
					graphObj.setExternalIdentity((UInt32)(short)item[SolidDB.CSolidEntryPoint.OBJNO]);
					graphObj.setDistanceFromInitPoint((int)item[SolidDB.CSolidEntryPoint.DISTFROMINITPOINT]);

					// typical usage dir is get via associated core obj
					if (coreObj != null)
					{
						graphObj.setUsageDir(coreObj.getUsageDir());
						if (topoGraph.canConnectGraphObjWithCoreObj((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID], (Enums.CLASS_TYPE)item[SolidDB.CSolidEntryPoint.CLASSTYPE]))
						{
							List<UInt32> elements = coreObj.getAssociatedObjects();
							elements.Add((UInt32)(int)item[SolidDB.CSolidEntryPoint.OBJID]);
							coreObj.setAssociatedObjects(elements);
						}
					}
				}
			}
		} // while 
	
		public void createCoreObj(UInt32 objId, UInt16 objType, ExtNetworkCreatorIf.EClassType eClassType, string objName)
		{
			Enums.CLASS_TYPE classType = Enums.CLASS_TYPE.CLASS_VERTEX;
			if (eClassType == ExtNetworkCreatorIf.EClassType.ctEdge)
				classType = Enums.CLASS_TYPE.CLASS_EDGE;

			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				Graph graph = topoGraph.getGraph();
				CoreObj coreObj = graph.createCoreObject(objId, objType, classType, objName);
				if (coreObj == null)
					throw new Exception("NetworkCreator::createCoreObj/ createCoreObject failed");
			}
		}
		public void addAdjacency(Enums.EDirection eDir, OBJID from, OBJID to, int cost = 0)
		{
			bool bDirChange = false;
			TopoGraph.WiredConnection connectionFromTo = new TopoGraph.WiredConnection() { m_objId = to, m_bDirChange = bDirChange, m_cost = cost };
			
			foreach (TopoGraph topoGraph in m_topoGraphList)
				topoGraph.toAdjacency(eDir, from, ref connectionFromTo);
		}

		public void createExt()
		{
			if (m_initialized)
				return;

			m_initialized = true;

			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				topoGraph.getGraph().startInit();
				topoGraph.startInit();
				topoGraph.railGraphCreated();
			}
		}

		public void destroyExt()
		{
			foreach (TopoGraph topoGraph in m_topoGraphList)
			{
				topoGraph.getGraph().startShutdown();
				topoGraph.startShutdown();
			}
		}

		private void interestedAdjAssociation(List<Enums.HT_TYPE> associationList) => interestedAssociation(associationList, ref m_neededAdjAssociations);
		private void interestedHierarchyAssociation(List<Enums.HT_TYPE> associationList) => interestedAssociation(associationList, ref m_neededAdjAssociations);
		private void interestedAssociation(List<Enums.HT_TYPE> associationList, ref List<Enums.HT_TYPE> target)
		{
			if (associationList.Count == 0)
				return;

			associationList.Sort();
			target = target.Union(associationList).ToList();
		}
	}
}
