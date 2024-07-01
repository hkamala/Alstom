using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.FindCondition;
using RailgraphLib.RailExtension;
using RailgraphLib.Interlocking;
using RailgraphLib.HierarchyObjects;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class ILTopoGraph : TopoGraph
	{
		public enum ETrackExtensionStatus
		{
			tesOk,                                              ///< track extension was solid
			tesNoElements,                              ///< there wasn't any elements in given extension
			tesIllegalElement,                      ///< start- or end element is not a common graph object
			tesStartOffsetTooLong,              ///< start distance is longer than length of start object
			tesEndOffsetTooLong,                    ///< end distance is longer than length of end object
			tesNotAdjacentElements,             ///< the elements are not adjacent
			tesCreationDirectionInvalid,    ///< creation direction does not match from start to end
			tesCombinedOffsetTooLong            ///< One-element track extension and offsets are too long when combined
		};

		public ILTopoGraph(Graph graph) : base(graph) 
		{
			m_pTopoGraph = this;
		}

		private enum ELegType { plMerge, plLeft, plRight };

		private readonly Enums.HT_TYPE PointToPointLegConnection = (Enums.HT_TYPE)Enums.ADJ_TYPE.ADJ_POINTLEG_CONNECTION;

		public List<ElementExtension> convert(IReadOnlyList<OBJID> trackVector, Enums.EDirection eDir) 
		{
			List<ElementExtension> trackExtensionVector = new List<ElementExtension>();
			List<OBJID> sourceTracks = new List<OBJID>();
			sourceTracks.AddRange(trackVector);
	
			while (sourceTracks.Count > 0)
			{
				List<OBJID> foundTracks = new List<OBJID>();

				// Get the last element as a starting point and remove it from source tracks
				OBJID iStartId = sourceTracks.Last();
				sourceTracks.RemoveAt(sourceTracks.Count - 1);

				List<FindResult> cResult = new List<FindResult>();

				// Search from the start element into opposite direction
				FindTrackElements trackSearchOpposite = new FindTrackElements(iStartId, Enums.EDirection.dOpposite, ref sourceTracks, this);
				findPath(trackSearchOpposite, cResult);
				foundTracks.AddRange(trackSearchOpposite.getFoundTrackElements());

				// Add start element into correct place
				foundTracks.Add(iStartId);

				// Search from the start element into nominal direction
				FindTrackElements trackSearchNominal = new FindTrackElements(iStartId, Enums.EDirection.dNominal, ref sourceTracks, this);
				findPath(trackSearchNominal, cResult);
				foundTracks.AddRange(trackSearchNominal.getFoundTrackElements());

				// Now we have adjacent tracks ordered into nominal direction in our collection.
				// Create track extension and add it into result set
				TrackExtension trackExtension = new TrackExtension(0, 0, foundTracks, Enums.EDirection.dNominal, eDir);
				trackExtensionVector.Add(trackExtension);
			}

			return trackExtensionVector;
		}

		public Enums.ECreateExtensionResult createTrackExtension(OBJID startId, OBJID endId, int distFromStart, int distFromEnd, Enums.EDirection eDir, ref List<OBJID> rViaElements, ref TrackExtension rTrackExtensionResult)
		{
			return (new TopoConverter()).createExtension(startId, endId, distFromStart, distFromEnd, eDir, rViaElements, this, rTrackExtensionResult);
		}

		/// finds a point or track according to given specification. \sa  EFindObjSpecification
		public ILGraphObj findPointOrTrack(ref List<OBJID> objects, Enums.EFindObjSpecification findSpec)
		{
			Point point = findPoint(objects);
			if (point != null)
				return point;

			return findTrack(objects, findSpec);
		}

		/// finds a track according to given specification. \sa  EFindObjSpecification
		public Track findTrack(List<OBJID> rObjects, Enums.EFindObjSpecification findSpec)
		{
			List<OBJID> objs = new List<OBJID>();
			objs.AddRange(rObjects);
			if (findSpec == Enums.EFindObjSpecification.fosNominal || findSpec == Enums.EFindObjSpecification.fosMiddleOrNominal)
				objs.Reverse();

			foreach (var obj in objs)
			{
				if (getGraphObj(obj) is Track track)
					return track;
			}

			return null;
		}
		
		/// finds the first point from the given vector by using forward iterator
		public Point findPoint(IReadOnlyList<OBJID> rObjects) 
		{
			foreach (var obj in rObjects)
			{
				if (getGraphObj(obj) is Point point)
					return point;
			}

			return null;
		}

		/// checks validity of given TrackExtension. \sa ETrackExtensionStatus
		public virtual ILTopoGraph.ETrackExtensionStatus getTrackExtensionStatus(ref TrackExtension trackExtension)
		{
			IReadOnlyList<OBJID> elements = trackExtension.getExtensionElements();

			if (elements.Count == 0)
				return ILTopoGraph.ETrackExtensionStatus.tesNoElements;

			// Valid start and end elements?
			GraphObj startObj = getGraphObj(elements.First());
			GraphObj endObj = getGraphObj(elements.Last());

			if (startObj == null || endObj == null)
				return ILTopoGraph.ETrackExtensionStatus.tesIllegalElement;

			// Created into proper direction?
			if (startObj != endObj && findDirection(startObj.getId(), endObj.getId()) != trackExtension.getStartDirection())
				return ILTopoGraph.ETrackExtensionStatus.tesCreationDirectionInvalid;

			// Start distance too long?
			if (startObj.getLength() < trackExtension.getStartDistance())
				return ILTopoGraph.ETrackExtensionStatus.tesStartOffsetTooLong;

			// End distance too long?
			if (endObj.getLength() < trackExtension.getEndDistance())
				return ILTopoGraph.ETrackExtensionStatus.tesEndOffsetTooLong;

			// One-element track extension and offsets are too long when combined
			if (startObj == endObj && startObj.getLength() < (trackExtension.getStartDistance() + trackExtension.getEndDistance()))
				return ILTopoGraph.ETrackExtensionStatus.tesCombinedOffsetTooLong;

			// Adjacent elements?
			for (int i = 0; i < elements.Count;)
			{
				IReadOnlyList<OBJID> adjacent = getAdjacentTracksOrPoints(elements[i], trackExtension.getStartDirection()); //akk: TODO: handle direction change

				if (++i != elements.Count)
				{
					if (adjacent.Contains(elements[i]))
						return ETrackExtensionStatus.tesNotAdjacentElements;
				}
				
			}

			// Fine!
			return ILTopoGraph.ETrackExtensionStatus.tesOk;
		}
		/// returns adjacent track(s) or point from given direction
		
		public virtual IReadOnlyList<OBJID> getAdjacentTracksOrPoints(OBJID iFromId, Enums.EDirection eDir)
		{
			List<OBJID> adjacent = new List<OBJID>();
			List<OBJID> searchedElements = new List<OBJID>();
			searchedElements.Add(iFromId);

			// Get all the adjacent tracks or points
			do
			{
				List<OBJID> adjElements = new List<OBJID>();

				foreach (var searchedElem in searchedElements)
					adjElements.AddRange(getAdjacentElements(searchedElem, eDir));

				searchedElements.Clear();

				foreach (var adj in adjElements)
				{
					// We are only interested in tracks (and those derived from tracks) and points
					GraphObj obj = getGraphObj(adj);
					if (obj is Track || obj is Point)
						adjacent.Add(adj);
					else // We must search behind the other elements again
						searchedElements.Add(adj);
				}

				adjElements.Clear();

			} while (searchedElements.Count != 0);

			return adjacent;
		}
		/// counts length of given TrackExtension
		
		public virtual int getExtensionLength(TrackExtension trackExtension) => base.getExtensionLength(trackExtension);

		/// checks whether the given object are in same track circuit or not
		public virtual bool hasSameTrackCircuit(OBJID objId1, OBJID objId2)
		{
			bool bHasSame = hasSameTrackCircuit(objId1, objId2, Enums.EDirection.dNominal);
			if (!bHasSame)
				return hasSameTrackCircuit(objId1, objId2, Enums.EDirection.dOpposite);

			return true;
		}

		/// returns all adjacent elements of given object to given direction
		public static IReadOnlyList<OBJID> getAdjacentElements(OBJID thisElement, Enums.EDirection eDir) => m_pTopoGraph.getAdjElements(thisElement, eDir);

		/// checks whether logical direction changes between the elements
		public static new bool isDirectionChange(OBJID thisElement, OBJID adjacentElement)
		{
			GraphObj pThis = m_pTopoGraph.getGraphObj(thisElement);
			GraphObj adj = m_pTopoGraph.getGraphObj(adjacentElement);
			return m_pTopoGraph.isDirectionChange(pThis.getCoreId(), adj.getCoreId());
		}

		protected override void initialize() { }

		protected override void shutdown() { }

		public override void interestedAssociation(ref List<Enums.HT_TYPE> associations) => associations.Add(PointToPointLegConnection);

		protected override void associationCreated(ref AdjConnection adj)
		{
			if (adj.associationType == (Enums.HT_TYPE)(int)PointToPointLegConnection)
			{
				OBJID point = adj.jointId;
				OBJID leg1 = adj.masterId;
				OBJID leg2 = adj.adjId;
				ELegType eLeg1Type = ELegType.plMerge;
				
				if(adj.leg == 1)
					eLeg1Type = ELegType.plLeft;
				else if(adj.leg == 2)
					eLeg1Type = ELegType.plRight;
				else if (adj.leg != 0)
					throw new Exception($"configuration error in db: unknown legno {adj.leg} with master: {leg1}");

				ELegType eLeg2Type = ELegType.plMerge;
				if (adj.adjLeg == 1)
					eLeg2Type = ELegType.plLeft;
				else if (adj.adjLeg == 2)
					eLeg2Type = ELegType.plRight;
				else if (adj.adjLeg != 0)
					throw new Exception($"configuration error in db: unknown adj legno {adj.adjLeg} with adjId: {leg2}");

				if (!(m_pTopoGraph.getGraphObj(point) is ILGraphObj ilObject))
					return;

				if (!(ilObject is Point pPoint))
					return;

				OBJID idPointLeg = leg1;
				// find point leg object and set association to point
				if (!(getGraphObj(idPointLeg) is ILGraphObj ilPointLeg))
					return;

				if (!(ilPointLeg is PointLeg pointLeg))
					idPointLeg = 0;
				else
					pointLeg.associatePoint(pPoint);

				switch (eLeg1Type)
				{
					case ELegType.plMerge:
						pPoint.setMergeAdjacentObject(idPointLeg);
						break;
					case ELegType.plLeft:
						pPoint.setLeftAdjacentObject(idPointLeg);
						break;
					case ELegType.plRight:
						pPoint.setRightAdjacentObject(idPointLeg);
						break;
					default: break;
				}

				idPointLeg = leg2;
				// find point leg object and set association to point
				ilPointLeg = getGraphObj(idPointLeg) as ILGraphObj;

				pointLeg = ilPointLeg as PointLeg;
				if (pointLeg != null)
					pointLeg.associatePoint(pPoint);
				else
					idPointLeg = 0;

				switch (eLeg2Type)
				{
				case ELegType.plMerge:
					{
						pPoint.setMergeAdjacentObject(idPointLeg);
						break;
					}
				case ELegType.plLeft:
					{
						pPoint.setLeftAdjacentObject(idPointLeg);
						break;
					}
				case ELegType.plRight:
					{
						pPoint.setRightAdjacentObject(idPointLeg);
						break;
					}
				default: break;
				}
			}
		}

		internal override bool canConnectGraphObjWithCoreObj(OBJID objId, Enums.CLASS_TYPE classType)
		{
			ILGraphObjType type = (ILGraphObjType)classType;

			switch (type)
			{
			case ILGraphObjType.TRACK_SECTION:
			case ILGraphObjType.DARK_TRACK:
			case ILGraphObjType.POINT_LEG:
			case ILGraphObjType.CROSSING_TRACK_TS:
			case ILGraphObjType.MAIN_SIGNAL:
			case ILGraphObjType.SHUNTING_SIGNAL:
			case ILGraphObjType.DEPART_SIGNAL:
			case ILGraphObjType.FICTIVE_SIGNAL:
			case ILGraphObjType.TRACK_CIRCUIT_BOUNDARY:
			case ILGraphObjType.COMBINED_SIGNAL:
			case ILGraphObjType.BUFFER_STOP:
			case ILGraphObjType.LINEBLOCK:
			case ILGraphObjType.CONTROL_OBJECT:
			case ILGraphObjType.POINT:
				return true;
			default: return false;
			}
		}

		internal override void railGraphCreated() { }

		protected bool hasSameTrackCircuit(OBJID objId1, OBJID objId2, Enums.EDirection eDir)
		{
			IReadOnlyList<OBJID> adjs = getAdjElements(objId1, eDir);
			if (adjs.Count > 0)
			{
				var found = adjs.Where(x => x == objId2).ToList();
				if (found.Count > 0)	
				{
					if (getGraphObj(found[0]) is GraphObj graphObj)
					{
						if (!(graphObj is TrackCircuitBoundary boundary))
							return true;
					}
				}
			}

			return false;
		}

		private static TopoGraph m_pTopoGraph;
	}
}
