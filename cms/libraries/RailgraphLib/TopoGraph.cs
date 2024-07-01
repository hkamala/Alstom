using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.FindCondition;
using RailgraphLib.HierarchyObjects;

namespace RailgraphLib
{
	using OBJID = UInt32;
	using VertexMmap = SortedDictionary<UInt32, List<TopoGraph.WiredConnection>>;

	public abstract class TopoGraph
	{
		private Graph m_rGraph;
		private VertexMmap m_nomCoreAdjacency = new VertexMmap();
		private VertexMmap m_oppCoreAdjacency = new VertexMmap();

		public enum EDirChangeDirection { noneDirChange, fromOppToNom, fromNomToOpp, fromNomToOppSingleEdge, fromOppToNomSingleEdge, fromNomToOppSingleEdgeAsMaster, fromOppToNomSingleEdgeAsMaster };
		public enum ESearchingStrategy
		{
			DirectFirst
		};

		public enum ETerminationReason
		{
			ok,                                                                     ///< there is at least one path available
			fromNotExist,                                                   ///< the given from(start) object is unknown
			targetNotExist,                                             ///< the given target object is unknown
			targetElementNotFound,                              ///< searched object(s) not found
			associationToBasicNetworkMissing,           ///< searching cannot be performed; network construction error
			viaNotExist,                                                    ///< the given via object(s) is unknown.
			orderedViaElementNotFound,                      ///< searched object(s) not found because of missed via.
		};

		public struct WiredConnection
		{
			public WiredConnection(UInt32 objId, bool bDirChange, int cost, int objIdLeg = 0, int fromLeg = 0)
			{
				m_objId = objId;
				m_bDirChange = bDirChange;
				m_cost = cost;
				m_objIdLeg = objIdLeg;
				m_fromLeg = fromLeg;
			}

			public OBJID m_objId;
			public bool m_bDirChange;
			public int m_cost;
			public int m_objIdLeg = 0;
			public int m_fromLeg = 0;
		}

		public struct AdjConnection
		{
			public Enums.HT_TYPE associationType;
			public OBJID masterId;
			public OBJID adjId;
			public OBJID jointId;
			public EDirChangeDirection eDirChange;
			public int leg;
			public int adjLeg;
			public int cost;
		};

		public TopoGraph(Graph graph) 
		{
			m_rGraph = graph;
		}

		public Graph getGraph() => m_rGraph;

		public List<Enums.HT_TYPE> getAdjAssociations()
		{
			List<Enums.HT_TYPE> connections = new List<Enums.HT_TYPE>();
			connections.Add((Enums.HT_TYPE)(Enums.ADJ_TYPE.ADJ_EDGE_TO_EDGE_CONNECTION));
			interestedAssociation(ref connections);
			return connections;
		}

		public void startInit() => initialize();
		public void startShutdown() => shutdown();

		protected abstract void initialize();
		protected abstract void shutdown();

		public virtual void interestedAssociation(ref List<Enums.HT_TYPE> associations) { }
		internal abstract bool canConnectGraphObjWithCoreObj(UInt32 UInt32, Enums.CLASS_TYPE classType);
		internal abstract void railGraphCreated();

		internal void toAdjacency(Enums.EDirection eDir, UInt32 from, ref WiredConnection rTo)
		{
			if (eDir == Enums.EDirection.dNominal)
			{
				if (!m_nomCoreAdjacency.ContainsKey(from))
					m_nomCoreAdjacency[from] = new List<WiredConnection>();
				m_nomCoreAdjacency[from].Add(rTo);
			}
			else if (eDir == Enums.EDirection.dOpposite)
			{
				if (!m_oppCoreAdjacency.ContainsKey(from))
					m_oppCoreAdjacency[from] = new List<WiredConnection>();
				m_oppCoreAdjacency[(from)].Add(rTo);
			}
			else
				throw new Exception("TopoGraph.ToAdjacency called with unknown dir ");
		}

		public virtual List<UInt32> findObj(UInt32 coreId, int distanceToVertex, bool bDistanceFromOppToNom)
		{
			List<UInt32> elements = new List<uint>();
			CoreObj coreObj = getCoreObj(coreId);
			if (coreObj == null)
				return elements;

			if (coreObj is Vertex vertex)
			{
				if (distanceToVertex == 0)
				{
					elements.Add(vertex.getAssociatedObjects().First());
					return elements;
				}
				else
					return elements; // todo; we don't know e.g point position
			}
			else if (coreObj is Edge edge)
			{
				if (bDistanceFromOppToNom)
				{
					foreach (UInt32 objId in edge.getAssociatedObjects())
					{
						GraphObj graphObj = getGraphObj(objId);
						if (graphObj != null)
						{
							if (distanceToVertex >= graphObj.getDistanceToOppVertex() && distanceToVertex <= graphObj.getDistanceToOppVertex() + graphObj.getLength())
								elements.Add(objId);
						} //else there is an uninterested element in edge
					}
				}
				else
				{
					foreach (UInt32 objId in edge.getAssociatedObjects())
					{
						GraphObj graphObj = getGraphObj(objId);
						if (graphObj != null)
						{
							if (distanceToVertex >= graphObj.getDistanceToNomVertex() && distanceToVertex <= graphObj.getDistanceToNomVertex() + graphObj.getLength())
								elements.Add(objId);
						} //else there is an uninterested element in edge
					}
				}
			}

			return elements;
		}

		public virtual List<UInt32> findIntersectingObjects(UInt32 graphObjId)
		{
			List<UInt32> elements = new List<uint>();
			CoreObj coreObj = getCoreObj(graphObjId);
			if (coreObj != null)
				return elements; // cannot be core object, return empty vector

			GraphObj graphObj = getGraphObj(graphObjId);
			if (graphObj == null)
				return elements; // Not known object

			coreObj = getCoreObj(graphObj.getId());
			if (coreObj == null)
				return elements;

			foreach (UInt32 obiId in coreObj.getAssociatedObjects())
			{
				GraphObj tmpGraphObj = getGraphObj(obiId);
				if (coreObj.getId() == tmpGraphObj.getId())
					continue; // take next one

				if ((tmpGraphObj.getDistanceToOppVertex() >= graphObj.getDistanceToOppVertex() && tmpGraphObj.getDistanceToOppVertex() <= graphObj.getDistanceToOppVertex() + graphObj.getLength()) ||
						 (tmpGraphObj.getDistanceToNomVertex() <= graphObj.getDistanceToNomVertex() + graphObj.getLength() && tmpGraphObj.getDistanceToNomVertex() >= graphObj.getDistanceToNomVertex()))
				{
					elements.Add(obiId);
				}
			}

			return elements;
		}

		public virtual Enums.EDirection findDirection(UInt32 startElement, UInt32 targetElement, int elementCount = 100, Enums.EDirection ePrimarySearchDir = Enums.EDirection.dUnknown)
		{
			if (startElement == 0 || targetElement == 0)
				return Enums.EDirection.dUnknown;

			if (startElement == targetElement)
				return Enums.EDirection.dUnknown;

			// check whether direction was defined by user or not 
			if (ePrimarySearchDir == Enums.EDirection.dUnknown || ePrimarySearchDir == Enums.EDirection.dBoth)
			{
				// Use nominal as primary searching dir
				ePrimarySearchDir = Enums.EDirection.dNominal;
			}

			Enums.EDirection eSecondaryDir = Enums.EDirection.dNominal;
			if (ePrimarySearchDir == Enums.EDirection.dNominal)
				eSecondaryDir = Enums.EDirection.dOpposite;

			FindAllCondition conditionPrimary = new FindAllCondition(startElement, targetElement, ePrimarySearchDir);
			conditionPrimary.setSearchDepth(elementCount);
			List<FindResult> resultList = new List<FindResult>();
			if (findPath(conditionPrimary, resultList) == ETerminationReason.ok)
			{
				foreach (FindResult findResult in resultList)
				{
					if (findResult.isDirectionChangeInPath())
						return Enums.EDirection.dBoth;
				}

				return ePrimarySearchDir;
			}
			else
			{
				FindAllCondition conditionSecondary = new FindAllCondition(startElement, targetElement, eSecondaryDir);
				conditionSecondary.setSearchDepth(elementCount);
				if (findPath(conditionSecondary, resultList) == ETerminationReason.ok)
				{
					foreach (FindResult findResult in resultList)
					{
						if (findResult.isDirectionChangeInPath())
							return Enums.EDirection.dBoth;
					}
					return eSecondaryDir;
				}

				return Enums.EDirection.dUnknown;
			}
		}

		public virtual Enums.EDirection findTargetDirection(UInt32 startElement, UInt32 targetElement, Enums.EDirection eMovingDir, int elementCount = 100)
		{
			if (startElement == 0 || targetElement == 0 || eMovingDir == Enums.EDirection.dUnknown || eMovingDir == Enums.EDirection.dBoth)
				return Enums.EDirection.dUnknown;

			if (startElement == targetElement)
				return eMovingDir;

			FindLogicalCondition condition = new FindLogicalCondition(startElement, targetElement, eMovingDir);
			condition.setSearchDepth(elementCount);
			FindResult result = new FindResult();
			if (findLogicalPath(ref condition, ref result))
			{
				int dirChangeCount = result.getDirectionChangeCountInPath();

				// If odd number of direction changes, target direction will change from moving direction
				if ((dirChangeCount % 2) == 1)
					return eMovingDir == Enums.EDirection.dNominal ? Enums.EDirection.dOpposite : Enums.EDirection.dNominal;

				return eMovingDir;
			}

			return Enums.EDirection.dUnknown;
		}

		public virtual ETerminationReason findPath(FindAllCondition rCondition, List<FindResult> rFindResultVector, ESearchingStrategy eSearchingStartegy = TopoGraph.ESearchingStrategy.DirectFirst)
		{
			VertexMmap vertex_mmap = new VertexMmap();
			DateTime m_startTime = DateTime.Now;
			TopoCondition cTopoCondition = new TopoCondition(this, rCondition);
			ETerminationReason eReason = cTopoCondition.validateConditions();

			if (eReason != ETerminationReason.ok)
				return eReason;

			bool bPerformCoreSearch = cTopoCondition.isCoreSearch();

			List<FindResult> findResultCoreNetworkVector = new List<FindResult>();

			switch (eSearchingStartegy)
			{
				case ESearchingStrategy.DirectFirst:
					{
						if (!vertex_mmap.ContainsKey(cTopoCondition.getStartCoreElement()))
							vertex_mmap.Add(cTopoCondition.getStartCoreElement(), new List<WiredConnection>());

						vertex_mmap[cTopoCondition.getStartCoreElement()].Add(new WiredConnection(0, false, 0));

						// 1. do direct first search with the core objects, jump lower level if needed and
						// save targets (core and graph element)
						CoreObj thisCoreObj = getCoreObj(cTopoCondition.getStartCoreElement());
						UInt32 targetGraphElement = 0;
						UInt32 idPrevObj = 0; // we must remember last valid object because there can be one empty core object between two points
						int iDepth = 0;

						FindAllCondition.EConditionalProceed eResultCondition = FindAllCondition.EConditionalProceed.cpFail;
						if (!bPerformCoreSearch)
							eResultCondition = isConditionFoundWithinCoreObj(thisCoreObj.getAssociatedObjects(), null /* previous */, cTopoCondition.getUserCondition().searchDir(), cTopoCondition.getUserCondition(), ref targetGraphElement, ref idPrevObj, ref iDepth);
						else
						{
							// for core network
							eResultCondition = cTopoCondition.getUserCondition().isConditionFound(thisCoreObj.getId(), 0);
							if (eResultCondition == FindAllCondition.EConditionalProceed.cpFound || eResultCondition == FindAllCondition.EConditionalProceed.cpFoundAndContinue)
								targetGraphElement = thisCoreObj.getId();
						}

						bool bPerformDFS = false;

						switch (eResultCondition)
						{
							case FindAllCondition.EConditionalProceed.cpContinue:
								bPerformDFS = true;
								break;
							case FindAllCondition.EConditionalProceed.cpFound:
								cTopoCondition.addTargets(thisCoreObj.getId(), targetGraphElement);
								break;
							case FindAllCondition.EConditionalProceed.cpFoundAndContinue:
								cTopoCondition.addTargets(thisCoreObj.getId(), targetGraphElement);
								// note: because target was found within first core it is not necessary to continue searching, no other possible paths
								break;
							case FindAllCondition.EConditionalProceed.cpBreak:
								// note: because target was not found within first core it is not necessary to continue searching, no other possible paths						
								break;
							case FindAllCondition.EConditionalProceed.cpFail:
								vertex_mmap.Clear();
								return ETerminationReason.targetElementNotFound;
							default: 
								break;
						}    // end - switch

						if (bPerformDFS)
						{
							bool bContinueSearching = true;
							directFirstSearch(cTopoCondition.getStartCoreElement(), cTopoCondition.getUserCondition().searchDir(), ref cTopoCondition, idPrevObj, ref vertex_mmap, ref bContinueSearching, iDepth);
						}

						// 2. collect core network paths from multiple core targets
						int allowedDepth = cTopoCondition.getUserCondition().searchDepth();

						foreach (var target in cTopoCondition.getTargets())
						{
							if (vertex_mmap.ContainsKey(target.Key))
							{
								// target exist, collect core path
								FindResult pFindResult = new FindResult(m_startTime);
								collectCoreNetworkPath(target.Key, ref findResultCoreNetworkVector, pFindResult, ref vertex_mmap, allowedDepth);
							}
						}
						vertex_mmap.Clear();

						// collect path for the user 
						if (bPerformCoreSearch)
						{
							foreach (var result in findResultCoreNetworkVector)
							{
								FindResult pResult = new FindResult(result);
								pResult.reverseResults();
								rFindResultVector.Add(pResult);
							}
							findResultCoreNetworkVector.Clear();
						}
						else
						{
							// 3. Collect element paths by using core paths and delete core results
							collectPath(cTopoCondition, ref findResultCoreNetworkVector, rFindResultVector);
						}

						break;
					}
				default: break;
			} // end - switch

			checkViaPoints(ref rFindResultVector, cTopoCondition.getUserCondition().getViaElements());

			if (rFindResultVector.Count > 0)
				return ETerminationReason.ok;

			return ETerminationReason.targetElementNotFound;
		}

		public virtual bool findLogicalPath(ref FindLogicalCondition condition, ref FindResult findResult)
		{
			OBJID prevId = 0; // == CallerId
			Enums.EDirection tmpDir = condition.searchDir();
			Enums.EDirection eSearchingDir = tmpDir;

			findResult.setSearchingDir(condition.searchDir());
			findResult.addBack(condition.from()); // start element

			bool bContinue = true;

			while (findResult.getResult().Last() != condition.target() && findResult.getResult().Count <= condition.searchDepth() && bContinue)
			{
				switch (condition.isConditionFound(findResult.getResult()))
				{
					case FindLogicalCondition.EConditionalProceed.cpContinue: 
						break;
					case FindLogicalCondition.EConditionalProceed.cpFound:
						bContinue = false;  // force stopping
						continue;
					case FindLogicalCondition.EConditionalProceed.cpFail:
						findResult.getResult().Clear();
						return false;
					default: break;
				}

				GraphObj graphObj = getGraphObj(findResult.getResult().Last());
				eSearchingDir = findResult.getSearchingDir();
				OBJID adjObjId = graphObj.getLogicalAdj(eSearchingDir, prevId);
				OBJID adjObjCoreId = 0;
				// check that path is also available to from adj object to this obj
				if (adjObjId != 0)
				{
					GraphObj adjGraphObj = getGraphObj(adjObjId);
					adjObjCoreId = adjGraphObj.getCoreId();
					Enums.EDirection eReversedDir = Enums.EDirection.dNominal;
					if (eSearchingDir == Enums.EDirection.dNominal)
						eReversedDir = Enums.EDirection.dOpposite;

					bool bDirChange = isDirectionChange(graphObj.getCoreId(), adjObjCoreId);
					Enums.EDirection eDir = (bDirChange ? eSearchingDir : eReversedDir);
					if (!adjGraphObj.hasLogicalAdj(eDir, graphObj.getId()))
					{
						// no logical path between two elements
						adjObjId = 0;
					}
				}

				if (adjObjId == 0)
				{
					findResult.getResult().Clear();
					findResult.setDirChangesInPath(0); //akk
					return false; // No valid route available.
				}
				
				// is direction changed
				if (isDirectionChange(graphObj.getCoreId(), adjObjCoreId))
				{
					if (eSearchingDir == Enums.EDirection.dNominal)
						eSearchingDir = Enums.EDirection.dOpposite;
					else
						eSearchingDir = Enums.EDirection.dNominal;

					findResult.dirChangeInPath();
					findResult.setSearchingDir(eSearchingDir);
				}

				prevId = findResult.getResult().Last();        // at least a point needs this
				findResult.addBack(adjObjId);
			}

			if (findResult.getResult().Count >= condition.searchDepth())
			{
				findResult.getResult().Clear();
				findResult.setDirChangesInPath(0); //akk
				return false;
			}

			/*	*******	 Checking via elements	*******/
			if (condition.getViaElements().Count > 0)
			{
				foreach (var viaPoint in condition.getViaElements())
				{
					if (!findResult.getResult().Contains(viaPoint))
					{
						findResult.getResult().Clear();
						findResult.setDirChangesInPath(0);
						return false;
					}
				}
			}

			findResult.pathFound(true);
			return true;
		}

		public virtual bool isDirectionChange(UInt32 thisElement, UInt32 adjacentElement)
		{
			if (thisElement == adjacentElement)
				return false;

			bool bDirectionChange = isDirectionChange(Enums.EDirection.dNominal, thisElement, adjacentElement);
			if (!bDirectionChange)
				bDirectionChange = isDirectionChange(Enums.EDirection.dOpposite, thisElement, adjacentElement);

			return bDirectionChange;
		}

		public IReadOnlyList<UInt32> getAdjElements(UInt32 thisElement, Enums.EDirection eDir)
		{
			List<UInt32> adjElementVector = new List<uint>();

			// check that given element is valid
			GraphObj graphObj = getGraphObj(thisElement);
			if (graphObj == null)
			{
				// check whether we are working in core network
				CoreObj core = getCoreObj(thisElement);
				if (core != null)
				{
					VertexMmap coreNetworkAdj = getCoreNetworkAdj(eDir);
					if (coreNetworkAdj.ContainsKey(thisElement))
					{
						List<WiredConnection> connections = coreNetworkAdj[thisElement];
						foreach (var wiredConnection in connections)
						{
							if (wiredConnection.m_objId != 0)
								adjElementVector.Add(wiredConnection.m_objId);
						}
					}
					
					// returns adjacent core object(s) or empty vector
					return adjElementVector;
				}
				else
				{
					// returns empty vector 
					return adjElementVector;
				}
			}

			CoreObj coreObj = getCoreObj(graphObj.getCoreId());

			if (eDir == Enums.EDirection.dNominal)
			{
				//Take next after one being searched for. Assuming it always finds at least one
				int iterElement = coreObj.getAssociatedObjects().FindIndex(x => x == thisElement) + 1;
				if (iterElement < coreObj.getAssociatedObjects().Count)
				{
					adjElementVector.Add(coreObj.getAssociatedObjects()[iterElement]);
					return adjElementVector; // we are inside edge, only one elements 
				}

				// We are at the end of the edge or in vertex , use the core network to find neighbours
				VertexMmap coreNetworkAdj = getCoreNetworkAdj(Enums.EDirection.dNominal);
				if (coreNetworkAdj.ContainsKey(graphObj.getCoreId()))
				{
					foreach (WiredConnection wiredConnection in coreNetworkAdj[graphObj.getCoreId()])
					{
						if (wiredConnection.m_objId == 0)
							continue;    // no adj core element, possible end point

						CoreObj adjCoreObj = getCoreObj(wiredConnection.m_objId);
						if (adjCoreObj.getAssociatedObjects().Count == 0)
						{
							// edge can be empty between two vertices or it can be the last object after vertex
							if (coreNetworkAdj.ContainsKey(adjCoreObj.getId()) && coreNetworkAdj[adjCoreObj.getId()].Count() == 1)
							{
								// we allowed jump only from edge
								List<WiredConnection> wiredConnections = coreNetworkAdj[adjCoreObj.getId()];
								CoreObj coreObjBehindEdge = getCoreObj(wiredConnections[0].m_objId);
								if (coreObjBehindEdge?.getAssociatedObjects().Count != 0)
									adjElementVector.Add(coreObjBehindEdge.getAssociatedObjects().First());
							}
						}
						else
						{
							// direction change is a special case
							if (isDirectionChange(coreObj.getId(), adjCoreObj.getId()))
								adjElementVector.Add(adjCoreObj.getAssociatedObjects().Last());
							else
								adjElementVector.Add(adjCoreObj.getAssociatedObjects().First());
						}
					}
				}

				return adjElementVector;
			}
			else
			{
				//Take previous before one being searched for. Assuming it always finds at least one
				int iterElement = coreObj.getAssociatedObjects().FindLastIndex(x => x == thisElement) - 1;
				if (iterElement >= 0)
				{
					adjElementVector.Add(coreObj.getAssociatedObjects()[iterElement]);
					return adjElementVector; // we are inside edge, only one elements 
				}

				VertexMmap coreNetworkAdj = getCoreNetworkAdj(Enums.EDirection.dOpposite);
				// We are at the end of the edge or vertex, use core network to find neighbour
				if (coreNetworkAdj.ContainsKey(graphObj.getCoreId()))
				{
					foreach (WiredConnection wiredConnection in coreNetworkAdj[graphObj.getCoreId()])
					{
						if (wiredConnection.m_objId == 0)
							continue;

						CoreObj adjCoreObj = getCoreObj(wiredConnection.m_objId);
						if (adjCoreObj.getAssociatedObjects().Count == 0)
						{
							// edge can be empty between two vertices or it can be the last object after vertex
							if (coreNetworkAdj.ContainsKey(adjCoreObj.getId()) && coreNetworkAdj[adjCoreObj.getId()].Count == 1)
							{
								// we allowed jump only from edge
								List<WiredConnection> wiredConnections = coreNetworkAdj[adjCoreObj.getId()];
								CoreObj coreObjBehindEdge = getCoreObj(wiredConnections[0].m_objId);
								if (coreObjBehindEdge?.getAssociatedObjects().Count != 0)
									adjElementVector.Add(coreObjBehindEdge.getAssociatedObjects().Last());
							}
						}
						else
						{
							bool bDirChange = isDirectionChange(coreObj.getId(), adjCoreObj.getId());
							if (bDirChange)
								adjElementVector.Add(adjCoreObj.getAssociatedObjects().First());
							else
								adjElementVector.Add(adjCoreObj.getAssociatedObjects().Last());
						}
					}
				}

				return adjElementVector;
			}
		}

		public int getExtensionLength(RailExtension.ElementExtension extension)
		{
			int length = -extension.getStartDistance() - extension.getEndDistance();

			IReadOnlyList<OBJID> elements = extension.getExtensionElements();
			foreach (var element in elements)
			{
				if (getGraphObj(element) is GraphObj obj)
					length += obj.getLength();
			}

			return length;
		}

		public GraphObj getGraphObj(UInt32 elementId) => m_rGraph.getGraphObj(elementId);
		public virtual CoreObj getCoreObj(UInt32 key) => m_rGraph.getCoreObj(key);

		public bool isInAdjacency(Enums.EDirection eDir, UInt32 from, UInt32 to)
		{
			if (to == 0)
				return true; // this means that we dont't add it to adjacency

			VertexMmap sourceMap = getCoreNetworkAdj(eDir);
			if (sourceMap.ContainsKey(from))
				return sourceMap[from].Any(x => x.m_objId == to);

			return false;
		}

		protected virtual VertexMmap getCoreNetworkAdj(Enums.EDirection eToDir) => eToDir == Enums.EDirection.dNominal ? m_nomCoreAdjacency : m_oppCoreAdjacency;

		protected bool isDirectionChange(Enums.EDirection eDir, UInt32 thisElement, UInt32 adjacentElement)
		{
			VertexMmap vMap = getCoreNetworkAdj(eDir);
			if (vMap.ContainsKey(thisElement))
			{
				List<WiredConnection> wiredConnections = vMap[thisElement];
				foreach (var wiredConnection in wiredConnections)
				{
					if (wiredConnection.m_objId == adjacentElement)
						return wiredConnection.m_bDirChange;
				}
			}

			return false;
		}

		protected bool directFirstSearch(UInt32 idVertex, Enums.EDirection eSearchDir, ref TopoCondition rCondition, UInt32 idPrevObj, ref VertexMmap vertex_mmap, ref bool bContinueSearching, int countOfIteratedElements)
		{
			// Find adjacent objects into wanted direction
			uint iAdjCount = 0;
			VertexMmap adjacentObjects = getCoreNetworkAdj(eSearchDir);
			List<WiredConnection> wiredConnections = new();	

			if (adjacentObjects.ContainsKey(idVertex))
				wiredConnections = adjacentObjects[idVertex];

			if (wiredConnections.Count != 0 && wiredConnections[0].m_objId != 0)
				iAdjCount = (uint)wiredConnections.Count;
			else
				wiredConnections.Clear();

			bool targetFound = false;

			foreach (WiredConnection wiredConnection in wiredConnections)
			{
				UInt32 idPrevElementObj = idPrevObj;
				int iteratedElementsCount = countOfIteratedElements;
				UInt32 idAdjacentObj = wiredConnection.m_objId;

				// If we hit into start element, prevent handling
				if (idAdjacentObj == rCondition.getStartCoreElement())
					continue;

				// Check if we have already handled this adjacency (in correct direction)
				bool adjacencyConnectionMissing = true;
				bool hasDirectionChange = wiredConnection.m_bDirChange;

				if (vertex_mmap.ContainsKey(idAdjacentObj))
				{
					var adjacencies = vertex_mmap[idAdjacentObj];
					foreach (var adj in adjacencies)
					{
						if (adj.m_objId == idVertex)
						{
							adjacencyConnectionMissing = false;
							targetFound = true;
							break;
						}
					}
				}

				if (bContinueSearching && adjacencyConnectionMissing)
				{
					// Legs reversed here because of reversed mapping!
					if (!vertex_mmap.ContainsKey(idAdjacentObj))
						vertex_mmap.Add(idAdjacentObj, new List<WiredConnection>());

					vertex_mmap[idAdjacentObj].Add(new WiredConnection(idVertex, wiredConnection.m_bDirChange, wiredConnection.m_cost, wiredConnection.m_fromLeg, wiredConnection.m_objIdLeg));

					Action<VertexMmap, UInt32, UInt32> removeInsertedElement = (vertex_mmap, idVertex, idAdjacentObj) =>
					{
						// Remove element inserted above, because path did not lead to target or search depth is reached
						if (!vertex_mmap.ContainsKey(idAdjacentObj))
							return;

						var wiredConnections = vertex_mmap[idAdjacentObj];
						int indexToRemove = wiredConnections.FindIndex(x => x.m_objId == idVertex);
						if (indexToRemove >= 0)
							wiredConnections.RemoveAt(indexToRemove);
					};

					CoreObj prevCoreObj = getCoreObj(idVertex);
					CoreObj thisCoreObj = getCoreObj(idAdjacentObj);

					UInt32 targetElementId = rCondition.getUserCondition().target();
					FindAllCondition.EConditionalProceed eResultCondition = FindAllCondition.EConditionalProceed.cpFail;
					bool bPerformCoreSearch = false;

					// Check if we are originally running core search
					CoreObj coreTstObj = getCoreObj(rCondition.getUserCondition().from());
					if (coreTstObj != null)
						bPerformCoreSearch = true;

					if (!bPerformCoreSearch)
					{
						List<UInt32> prevElementVector = new List<uint>();

						if (prevCoreObj.getAssociatedObjects().Count != 0)
							prevElementVector = prevCoreObj.getAssociatedObjects();
						else
							prevElementVector.Add(idPrevElementObj);

						List<UInt32> thisElementVector = new List<OBJID>(thisCoreObj.getAssociatedObjects());
						if (hasDirectionChange)
							thisElementVector.Reverse();

						eResultCondition = isConditionFoundWithinCoreObj(thisElementVector, prevElementVector, eSearchDir, rCondition.getUserCondition(), ref targetElementId, ref idPrevElementObj, ref iteratedElementsCount);
					}
					else
					{ // core search					
						eResultCondition = rCondition.getUserCondition().isConditionFound(thisCoreObj.getId(), prevCoreObj.getId());
						if (eResultCondition == FindAllCondition.EConditionalProceed.cpFound || eResultCondition == FindAllCondition.EConditionalProceed.cpFoundAndContinue)
							targetElementId = thisCoreObj.getId();

						if (++iteratedElementsCount > rCondition.getUserCondition().searchDepth())
							eResultCondition = FindAllCondition.EConditionalProceed.cpBreak;
					}

					bool bPerformDFS = false;
					switch (eResultCondition)
					{
						case FindAllCondition.EConditionalProceed.cpContinue:
							bPerformDFS = true;
							break;
						case FindAllCondition.EConditionalProceed.cpFound:
							rCondition.addTargets(thisCoreObj.getId(), targetElementId);
							bContinueSearching = false;
							targetFound = true;
							break;
						case FindAllCondition.EConditionalProceed.cpFoundAndContinue:
							rCondition.addTargets(thisCoreObj.getId(), targetElementId);
							bPerformDFS = false;
							targetFound = true;
							continue; // we don't continue with this edge/vertex, take next one if available
						case FindAllCondition.EConditionalProceed.cpBreak:
							removeInsertedElement(vertex_mmap, idVertex, idAdjacentObj);
							break;
						case FindAllCondition.EConditionalProceed.cpFail:
							rCondition.ClearTargets();
							bContinueSearching = false;
							return false;
						default:
							break;
					}       // end - switch

					if (bPerformDFS)
					{
						Enums.EDirection eNewDir = eSearchDir;
						if (hasDirectionChange)
						{
							if (eSearchDir == Enums.EDirection.dNominal)
								eNewDir = Enums.EDirection.dOpposite;
							else
								eNewDir = Enums.EDirection.dNominal;
						}

						bool targetFoundOnBranch = directFirstSearch(idAdjacentObj, eNewDir, ref rCondition, idPrevElementObj, ref vertex_mmap, ref bContinueSearching, iteratedElementsCount);
						if (!targetFoundOnBranch)
							removeInsertedElement(vertex_mmap, idVertex, idAdjacentObj);

						targetFound = targetFound || targetFoundOnBranch;
					}
				}
			}

			return targetFound;
		}
		
		protected virtual FindAllCondition.EConditionalProceed isConditionFoundWithinCoreObj(List<UInt32> rThisElementVector, List<UInt32> prevElementVector, Enums.EDirection eDir, FindAllCondition rCondition, ref UInt32 foundObjId, ref UInt32 idPrevObj, ref int iteratedElementsCount)
		{
			if (rThisElementVector.Count == 0)
			{
				if (prevElementVector?.Count != 0)
				{
					if (eDir == Enums.EDirection.dNominal)
						idPrevObj = prevElementVector.Last();
					else
						idPrevObj = prevElementVector.First();
				}
				return FindAllCondition.EConditionalProceed.cpContinue;
			}

			UInt32 prevElementId = 0;

			if (prevElementVector?.Count > 0)
				prevElementId = eDir == Enums.EDirection.dNominal ? prevElementVector.Last() : prevElementVector.First();

			if (eDir == Enums.EDirection.dNominal)
			{
				int itThisElement = -1;
				if (prevElementId == 0) // first core element, find actual start point within it
					itThisElement = rThisElementVector.FindIndex(x => x == rCondition.from());
				else
					itThisElement = 0;

				for (; itThisElement != rThisElementVector.Count; ++itThisElement)
				{
					FindAllCondition.EConditionalProceed eReturnedCondition = FindAllCondition.EConditionalProceed.cpContinue;
					eReturnedCondition = rCondition.isConditionFound(rThisElementVector[itThisElement], prevElementId);

					if (eReturnedCondition != FindAllCondition.EConditionalProceed.cpContinue) // continue with this edge ?
					{
						foundObjId = rThisElementVector[itThisElement];
						idPrevObj = foundObjId;
						return eReturnedCondition;
					}
					prevElementId = rThisElementVector[itThisElement];
					idPrevObj = prevElementId;

					if (++iteratedElementsCount > rCondition.searchDepth())
						return FindAllCondition.EConditionalProceed.cpBreak;
				}
			}
			else
			{
				int itThisElement = rThisElementVector.Count;
				if (prevElementId == 0) // first core element, find actual start point within it
					itThisElement = rThisElementVector.FindLastIndex(x => x == rCondition.from());
				else
					itThisElement = rThisElementVector.Count - 1;

				for (; itThisElement >= 0; --itThisElement)
				{
					FindAllCondition.EConditionalProceed eReturnedCondition = rCondition.isConditionFound(rThisElementVector[itThisElement], prevElementId);

					if (eReturnedCondition != FindAllCondition.EConditionalProceed.cpContinue)  // continue with this edge ?
					{
						foundObjId = rThisElementVector[itThisElement];
						idPrevObj = foundObjId;
						return eReturnedCondition;
					}
					prevElementId = rThisElementVector[itThisElement];
					idPrevObj = prevElementId;

					if (++iteratedElementsCount > rCondition.searchDepth())
						return FindAllCondition.EConditionalProceed.cpBreak;
				}
			}

			return FindAllCondition.EConditionalProceed.cpContinue;

		}

		protected void checkViaPoints(ref List<FindResult> rFindResultVector, IReadOnlyList<UInt32> viaElementVector)
		{
			if (viaElementVector.Count > 0)
			{
				int index = 0;

				while (index < rFindResultVector.Count)
				{
					bool bAllViasExist = true;

					foreach (var iterViaElement in viaElementVector)
					{
						if (!rFindResultVector[index].getResult().Contains(iterViaElement))
						{
							bAllViasExist = false;
							break;
						}
					}

					if (bAllViasExist)
						index++;
					else
						rFindResultVector.RemoveAt(index);
				}
			}
		}

		protected bool collectCoreNetworkPath(UInt32 fromElement, ref List<FindResult> findResultVector, FindResult findResult, ref VertexMmap vertex_mmap, int allowedDepth)
		{
			// Check if we are going to loop in circle track
			if (findResult.getResult().Where(x => x == fromElement).Any())
				return false;

			// Do not handle possible reverse adjacency caused by successful search to both directions on circle track!
			// The above means that don't handle the adjacency to element we are currectly coming from
			// Also prevent turning backwards to possible another leg in vertex
			UInt32 comingFromObject = findResult.getResult().Count == 0 ? 0 : findResult.getResult().Last();

			bool isVertex = getCoreObj(fromElement) is Vertex;
			List<KeyValuePair<UInt32, UInt32>> adjacencies = new List<KeyValuePair<uint, uint>>();
			bool goingToSingleEdgeLoop = false;

			if (comingFromObject == 0)
			{
				// We start here the backward searching, we may be either on edge or vertex, take every adjacency into account
				if (vertex_mmap.ContainsKey(fromElement))
				{
					var wiredConnections = vertex_mmap[fromElement];
					foreach (var wiredConnection in wiredConnections)
						adjacencies.Add(new KeyValuePair<uint, uint>(fromElement, wiredConnection.m_objId));
				}
			}
			else if (!isVertex)
			{
				// Element is edge (or boundary edge)
				if (vertex_mmap.ContainsKey(fromElement))
				{
					var wiredConnections = vertex_mmap[fromElement];
					foreach (var wiredConnection in wiredConnections.Where(x => x.m_objId != comingFromObject))
						adjacencies.Add(new KeyValuePair<uint, uint>(fromElement, wiredConnection.m_objId));
				}
			}
			else
			{
				// Element is vertex
				// Lambda function for checking if element is single edge loop
				Func<TopoGraph, UInt32, bool> isComingFromSingleEdgeLoop = (tG, idElement) => 
				{
					IReadOnlyList<UInt32> adjacentElements = tG.getAdjElements(idElement, Enums.EDirection.dNominal);
					if (adjacentElements.Count == 1 && tG.getCoreObj(adjacentElements.First()) is Vertex)
					{
						UInt32 nominalVertex = adjacentElements.First();
						adjacentElements = tG.getAdjElements(idElement, Enums.EDirection.dOpposite);
						if (adjacentElements.Count == 1 && tG.getCoreObj(adjacentElements.First()) is Vertex)
						{
							UInt32 oppositeVertex = adjacentElements.First();
							return nominalVertex == oppositeVertex;
						}
					}
					return false;
				};

				// Try to find out allowed adjacencies by neighbor elements
				Action<TopoGraph, UInt32, UInt32, Func<TopoGraph, UInt32, bool>, VertexMmap, List<UInt32>> getAdjacentObjectsOfFrom = (tG, fromElement, comingFromObject, isComingFromSingleEdgeLoop, allAdjacencies, objects) =>
				{
					bool shouldClear = false;
					bool dirChanges = false;
					if (allAdjacencies.ContainsKey(fromElement))
					{
						var wiredConnections = allAdjacencies[fromElement];
						foreach (var wiredConnection in wiredConnections)
						{
							dirChanges = dirChanges || tG.isDirectionChange(fromElement, wiredConnection.m_objId);
							if (wiredConnection.m_objId != comingFromObject)
								objects.Add(wiredConnection.m_objId);
							else
							{
								if (!isComingFromSingleEdgeLoop(tG, comingFromObject))
									shouldClear = true;
							}

						}
					}
					if (shouldClear && !dirChanges)
					{
						objects.Clear();
						return;
					}
				};

				List<UInt32> adjacentObjectsToNominal = new List<uint>();
				List<UInt32> adjacentObjectsToOpposite = new List<uint>();
				List<UInt32> allowedAdjacencies = new List<uint>();

				getAdjacentObjectsOfFrom(this, fromElement, comingFromObject, isComingFromSingleEdgeLoop, getCoreNetworkAdj(Enums.EDirection.dNominal), adjacentObjectsToNominal);
				getAdjacentObjectsOfFrom(this, fromElement, comingFromObject, isComingFromSingleEdgeLoop, getCoreNetworkAdj(Enums.EDirection.dOpposite), adjacentObjectsToOpposite);

				// Is this single edge loop we are going to?
				goingToSingleEdgeLoop = adjacentObjectsToNominal == adjacentObjectsToOpposite;

				// Build allowed adjacencies. Doesn't matter if element is multiple times in there!
				allowedAdjacencies.AddRange(adjacentObjectsToNominal);
				allowedAdjacencies.AddRange(adjacentObjectsToOpposite);

				if (vertex_mmap.ContainsKey(fromElement))
				{
					List<WiredConnection> wiredConnections = vertex_mmap[fromElement];
					foreach (WiredConnection wiredConnection in wiredConnections)
					{
						if (findResult.getResult().FindIndex(x => x == wiredConnection.m_objId) == -1 && (wiredConnection.m_objId == 0 || allowedAdjacencies.Find(x => x == wiredConnection.m_objId) >= 0))
							adjacencies.Add(new KeyValuePair<uint, uint>(fromElement, wiredConnection.m_objId));
					}
				}

				// Single edge loop has two ways in...
				if (goingToSingleEdgeLoop && adjacencies.Count == 1)
					adjacencies.Add(adjacencies.First());

				adjacentObjectsToNominal.Clear();
				adjacentObjectsToOpposite.Clear();
				allowedAdjacencies.Clear();
			}

			// Adjacency chain ended?
			if (adjacencies.Count == 0)
			{
				// If on correct end, we have found the result!
				if (fromElement == 0)
				{
					// Clone original result to found one(s)
					FindResult newResult = new FindResult(findResult);
					newResult.pathFound(true);
					findResultVector.Add(newResult);
					return true;
				}
				else // Not on correct end, failed in this branch
					return false;
			}

			bool found = false;
			bool first = true;
			foreach (var adjacency in adjacencies)
			{
				UInt32 idAdjacentObj = adjacency.Value;
				findResult.addBack(fromElement);

				// Calculate count of direction changes
				bool bDirChange = isDirectionChange(fromElement, idAdjacentObj);
				bDirChange = bDirChange && (!goingToSingleEdgeLoop || first);  // Only the first single edge loop entrance will change direction!

				if (bDirChange)
					findResult.dirChangeInPath();

				bool foundFromBranch = collectCoreNetworkPath(idAdjacentObj, ref findResultVector, findResult, ref vertex_mmap, allowedDepth);

				// Remove added information to keep find result in order for next recursive call
				findResult.popBack();
				if (bDirChange)
					findResult.decChangesInPath();

				found |= foundFromBranch;
				first = false;
			}

			return found;
		}

		protected void collectPath(TopoCondition cTopoCondition, ref List<FindResult> findResultCoreNetworkVector, List<FindResult> findResultVector)
		{
			Func<TopoGraph, UInt32, bool> isSingleEdgeLoop = (tg, idElement) =>
			{
				IReadOnlyList<UInt32> adjacentElements = tg.getAdjElements(idElement, Enums.EDirection.dNominal);
				if (adjacentElements.Count == 1 && tg.getCoreObj(adjacentElements.First()) is Vertex v)
				{
					UInt32 nominalVertex = adjacentElements.First();

					adjacentElements = tg.getAdjElements(idElement, Enums.EDirection.dOpposite);
					if (adjacentElements.Count == 1 && tg.getCoreObj(adjacentElements.First()) is Vertex)
					{
						UInt32 oppositeVertex = adjacentElements.First();
						return nominalVertex == oppositeVertex;
					}
				}

				return false;
			};

			foreach (FindResult iterFindCoreResult in findResultCoreNetworkVector)
			{
				int iterCoreBeginId = iterFindCoreResult.getResult().Count - 1;
				int iterCoreEndId = 0;

				//akk if (iterFindCoreResult.getResult().Count > 0)
				//akk 	iterCoreEndId++;

				var itTargets = cTopoCondition.getTargets()[iterFindCoreResult.getResult()[iterCoreEndId]];

				if (iterCoreBeginId == iterCoreEndId)
				{
					CoreObj coreObj1 = getCoreObj(iterFindCoreResult.getResult()[iterCoreBeginId]);
					bool bPathAvailable = false;

					if (cTopoCondition.getUserCondition().searchDir() == Enums.EDirection.dNominal)
					{
						var iterStartElement = coreObj1.getAssociatedObjects().FindIndex(x => x == cTopoCondition.getUserCondition().from());
						var iterEndElement = coreObj1.getAssociatedObjects().FindIndex(x => x == itTargets);

						for (int itElement = iterStartElement; itElement != coreObj1.getAssociatedObjects().Count; ++itElement)
						{
							if (coreObj1.getAssociatedObjects()[itElement] == coreObj1.getAssociatedObjects()[iterEndElement])
							{
								bPathAvailable = true;
								break; // fast exit
							}
						}
					}
					else
					{
						var iterStartElement = coreObj1.getAssociatedObjects().FindLastIndex(x => x == cTopoCondition.getUserCondition().from());
						var iterEndElement = coreObj1.getAssociatedObjects().FindLastIndex(x => x == itTargets);

						for (int itReverseElement = iterStartElement; itReverseElement >= 0; --itReverseElement)	//akk: "reverse" iteration, so --, not ++
						{
							if (coreObj1.getAssociatedObjects()[itReverseElement] == coreObj1.getAssociatedObjects()[iterEndElement])
							{
								bPathAvailable = true;
								break; // fast exit
							}
						}
					}

					if (!bPathAvailable)
						continue;
                }
                
                FindResult findResult = new FindResult();

                Enums.EDirection eSearchDir = cTopoCondition.getUserCondition().searchDir();
                UInt32 prevCoreId = 0;

                for (int iterCoreId= iterFindCoreResult.getResult().Count - 1; iterCoreId >= 0; iterCoreId--)
                {
                    CoreObj coreObj2 = getCoreObj(iterFindCoreResult.getResult()[iterCoreId]);
                    List<UInt32> graphElements = coreObj2.getAssociatedObjects();
                    if (prevCoreId != 0 && prevCoreId != iterFindCoreResult.getResult()[iterCoreId] && isDirectionChange(prevCoreId, iterFindCoreResult.getResult()[iterCoreId]))
                    {
                        bool changeDir = true;

                        // Quick hack: by default, entering single edge loop from end without direction change point, exiting from other end...
                        // Via points are checked to know, which way single edge associated elements are taken into path and what is the search direction
                        // This helps at least with conversions, which use the first element (on correct end) as via point
                        // Note: this test qualifies now only with type 4 and 6 single edge associations, not with type 5 or 7! At this point we don't know which one it is...
                        if (isSingleEdgeLoop(this, iterFindCoreResult.getResult()[iterCoreId]))
                        {
                            changeDir = false;
                            if (graphElements.Count > 1 && cTopoCondition.getUserCondition().getViaElements().Where(x => x == graphElements.Last()).Any())
                                changeDir = true;
                        }

                        // If previous core element was single edge loop, we will check which end was used to get out of it, and not change direction, if NOT coming out over direction change point!
                        // Note: this test qualifies now only with type 4 and 6 single edge associations, not with type 5 or 7! At this point we don't know which one it is...
                        if (changeDir && isSingleEdgeLoop(this, prevCoreId))
                        {
                            CoreObj prevCoreObj = getCoreObj(prevCoreId);
                            List<UInt32> prevGraphElements = prevCoreObj.getAssociatedObjects();
                            if (findResult.getResult().Last() == prevGraphElements.First())
                                changeDir = false;
                        }

                        if (changeDir)
                        {
                            // change direction
                            eSearchDir = (eSearchDir == Enums.EDirection.dNominal ? Enums.EDirection.dOpposite : Enums.EDirection.dNominal);
                            findResult.setSearchingDir(eSearchDir);
                            findResult.dirChangeInPath();
                            // std::reverse(graphElements.begin(), graphElements.end());
                        }
                    }
                    // append
                    // this shoud be here for Possible direction change point
                    if (eSearchDir == Enums.EDirection.dNominal)
                        findResult.getResult().AddRange(graphElements);
                    else
                    {
                        // pFindResult->insert(pFindResult->end(), pCoreObj->getGraphObjVector().rbegin(), pCoreObj->getGraphObjVector().rend());				
                        for (int i = graphElements.Count - 1; i >= 0; i--)
                            findResult.getResult().Add(graphElements[i]);
                    }

                    if (iterCoreId == iterCoreBeginId)
                    {
                        int iterElement = findResult.getResult().FindIndex(x => x == cTopoCondition.getUserCondition().from());
                        if (iterElement > 0)
                            findResult.getResult().RemoveRange(0, iterElement);

                    }   // first element

                    if (iterCoreId == iterCoreEndId)
                    {
                        // check that element really exist in core object, // remove after debugging
                        int iterElement = graphElements.FindIndex(x => x == itTargets);

                        // find correct iterator from result
                        int iterToRemove = findResult.getResult().FindIndex(x => x == itTargets) + 1;
                        if (iterToRemove >= 0 && iterToRemove < findResult.getResult().Count)
                            findResult.getResult().RemoveRange(iterToRemove, findResult.getResult().Count - iterToRemove);
                    }    // last element

                    // set previous before next iteration
                    prevCoreId = iterFindCoreResult.getResult()[iterCoreId];
                }
                // No checks for search depth
                findResult.pathFound(true);
                findResultVector.Add(findResult);
            }

            findResultCoreNetworkVector.Clear();
		}
		protected virtual void associationCreated(ref AdjConnection adj) 
		{
		}

		public void adjAssociationCreated(ref AdjConnection adj)
		{
			if (adj.associationType == (Enums.HT_TYPE)Enums.ADJ_TYPE.ADJ_EDGE_TO_EDGE_CONNECTION)
			{
				Graph graph = getGraph();

				bool bMasterValid = false;
				bool bAssociationValid = false;
				bool bJointValid = false;

				OBJID masterId = adj.masterId;
				OBJID associationId = adj.adjId;
				OBJID jointId = adj.jointId;
				EDirChangeDirection eDirChangeDir = adj.eDirChange;
				bool bDirChange = eDirChangeDir != EDirChangeDirection.noneDirChange;
				int cost = adj.cost;

				if (!bMasterValid)
				{
					CoreObj coreObj = graph.getCoreObj(masterId);
					if (coreObj != null)
						bMasterValid = true;
				}

				if (!bAssociationValid)
				{
					CoreObj coreObj = graph.getCoreObj(associationId);
					if (coreObj != null)
						bAssociationValid = true;
				}

				if (!bJointValid)
				{
					CoreObj coreObj = graph.getCoreObj(jointId);
					if (coreObj != null)
						bJointValid = true;
				}

				if (!bMasterValid)
					masterId = 0;

				if (!bAssociationValid)
					associationId = 0;

				if (!bJointValid)
					jointId = 0;

				// Logical direction change is defined between the edges in database but in railgraph
				// we have vertex between those. We will define that an actual dir change happens on
				// after vertex(jointId) and before the last object(associationId). If there is a point
				// between the edges the description in db shall be always from zero leg to left or
				// right leg.
				// NOTE! The leg numbers are not set here to wired connections!
				bool bFirstConnectionDirChangeAlwaysFalse = false;
				OBJID tmpMasterId = 0;
				bool inAdjacency = false;
				bool boundaryEdge = graph.isBoundaryEdge(masterId);

				// Master <> Joint
				if (!boundaryEdge)
				{
					// eDirChangeDir:  <--- oppToNom/oppToNomSingleEdge/oppToNomSingleEdgeAsMaster --->
					Enums.EDirection eConnectionDir = (eDirChangeDir == EDirChangeDirection.fromOppToNom || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdge || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdgeAsMaster) ? Enums.EDirection.dOpposite : Enums.EDirection.dNominal;
					tmpMasterId = masterId;
					inAdjacency = isInAdjacency(eConnectionDir, masterId, jointId);
					if (!inAdjacency && bMasterValid)
					{
						// from edge to vertex
						WiredConnection jointConnection = new WiredConnection(jointId, bFirstConnectionDirChangeAlwaysFalse, cost);
						toAdjacency(eConnectionDir, masterId, ref jointConnection);
					}

					// Single edge loop as master must define the other leg adjacency, that does not have direction change point
					if (eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdgeAsMaster || eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdgeAsMaster)
					{
						eConnectionDir = eConnectionDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
						inAdjacency = isInAdjacency(eConnectionDir, masterId, jointId);
						if (!inAdjacency && bMasterValid)
						{
							// from edge to vertex
							WiredConnection jointConnection = new WiredConnection(jointId, false, cost);
							toAdjacency(eConnectionDir, masterId, ref jointConnection);
						}
					}
				}

				{   // reversed direction/connection table
					Enums.EDirection eConnectionDir = (eDirChangeDir == EDirChangeDirection.fromOppToNom || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdge || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdgeAsMaster) ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
					inAdjacency = isInAdjacency(eConnectionDir, jointId, tmpMasterId);
					if (!inAdjacency && bJointValid)
					{
						// reversed direction from vertex to edge
						WiredConnection masterConnection = new WiredConnection(tmpMasterId, bFirstConnectionDirChangeAlwaysFalse, cost);
						toAdjacency(eConnectionDir, jointId, ref masterConnection);
					}

					// Single edge loop as master must define the other leg adjacency, that does not have direction change point
					if (eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdgeAsMaster || eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdgeAsMaster)
					{
						eConnectionDir = eConnectionDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
						inAdjacency = isInAdjacency(eConnectionDir, jointId, masterId);
						if (!inAdjacency && bJointValid)
						{
							// from vertex to edge
							WiredConnection masterConnection = new WiredConnection(masterId, false, cost);
							toAdjacency(eConnectionDir, jointId, ref masterConnection);
						}
					}
				}

				// Association <-> Joint
				OBJID tmpAssociationId = 0;
				boundaryEdge = graph.isBoundaryEdge(associationId);
				if (!boundaryEdge)
				{
					// eDirChangeDir:  ---> nomToOpp/nomToOppSingleEdge/nomToOppSingleEdgeAsMaster <---
					Enums.EDirection eConnectionDir = (eDirChangeDir == EDirChangeDirection.fromNomToOpp || eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdge || eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdgeAsMaster) ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
					tmpAssociationId = associationId;
					inAdjacency = isInAdjacency(eConnectionDir, associationId, jointId);
					if (!inAdjacency && bAssociationValid)
					{
						// from edge to vertex
						WiredConnection jointConnection = new WiredConnection(jointId, bDirChange, cost);
						toAdjacency(eConnectionDir, associationId, ref jointConnection);
					}

					// Single edge loop as adjacent must define the other leg adjacency, that does not have direction change point
					if (eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdge || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdge)
					{
						eConnectionDir = eConnectionDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
						inAdjacency = isInAdjacency(eConnectionDir, associationId, jointId);
						if (!inAdjacency && bAssociationValid)
						{
							// from edge to vertex
							WiredConnection jointConnection = new WiredConnection(jointId, false, cost);
							toAdjacency(eConnectionDir, associationId, ref jointConnection);
						}
					}
				}

				{
					// Note the logic change in eConnectionDir resolving!
					Enums.EDirection eConnectionDir = (eDirChangeDir == EDirChangeDirection.fromOppToNom || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdge || eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdgeAsMaster) ? Enums.EDirection.dOpposite : Enums.EDirection.dNominal;
					inAdjacency = isInAdjacency(eConnectionDir, jointId, tmpAssociationId);
					if (!inAdjacency && bJointValid)
					{
						// reversed direction from vertex to edge
						WiredConnection adjConnection = new WiredConnection(tmpAssociationId, bDirChange, cost);
						toAdjacency(eConnectionDir, jointId, ref adjConnection);
					}

					// Single edge loop as adjacent must define the other leg adjacency, that does not have direction change point
					if (eDirChangeDir == EDirChangeDirection.fromOppToNomSingleEdge || eDirChangeDir == EDirChangeDirection.fromNomToOppSingleEdge)
					{
						eConnectionDir = eConnectionDir == Enums.EDirection.dOpposite ? Enums.EDirection.dNominal : Enums.EDirection.dOpposite;
						inAdjacency = isInAdjacency(eConnectionDir, jointId, associationId);
						if (!inAdjacency && bJointValid)
						{
							// from vertex to edge
							WiredConnection associationConnection = new WiredConnection(associationId, false, cost);
							toAdjacency(eConnectionDir, jointId, ref associationConnection);
						}
					}
				}
			} // if(associationType == EdgeToEdgeConnection)

			// call this anyway, somebody can be also interested about EdgeToEdgeConnection
			associationCreated(ref adj);
		}

		private bool isValidObject(GraphObj graphObj, IReadOnlyList<Enums.CLASS_TYPE> validClassTypes)
		{
			if (graphObj != null)
			{
				if (validClassTypes.Count() == 0)
					return true;
				else
				{
					Enums.CLASS_TYPE objectClassType = graphObj.getClassType();
					return validClassTypes.Contains(objectClassType);
				}
			}
			return false;
		}
	}
}
