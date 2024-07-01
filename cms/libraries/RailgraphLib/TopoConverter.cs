using RailgraphLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.RailExtension;
using RailgraphLib.FindCondition;

namespace RailgraphLib
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class TopoConverter : TopoConverterIf
	{
		public EConversionResult convertExtension(ElementExtension sourceExtension, ElementExtension targetExtension, TopoGraph sourceTopoGraph, TopoGraph targetTopoGraph)
		{
			CoreExtension coreExtension = new CoreExtension();
			EConversionResult eResult = convertToCoreExtension(sourceExtension, ref coreExtension, sourceTopoGraph);
			if (eResult != EConversionResult.crOk)
				return eResult;

			return convertToExtension(coreExtension, ref targetExtension, targetTopoGraph);
		}

		public EConversionResult convertToCoreExtension(ElementExtension sourceExtension, ref CoreExtension coreExtension, TopoGraph sourceTopoGraph)
		{
			// If someone (accidentally) tries to convert core extension to core extension, we will stop
			IReadOnlyList<OBJID> sourceElements = sourceExtension.getExtensionElements(); // to "moving" direction

			if (sourceElements.Count == 0)
				return EConversionResult.crNoElements;

			OBJID startSourceElement = sourceElements.First();
			OBJID endSourceElement = sourceElements.Last();

			Graph graph = sourceTopoGraph.getGraph();

			GraphObj startSourceElementObj = graph.getGraphObj(startSourceElement);
			GraphObj endSourceElementObj = graph.getGraphObj(endSourceElement);

			if (startSourceElementObj == null || endSourceElementObj == null)
				return EConversionResult.crInternalDefinitionError;

			bool bStartDistanceIsFromOppositeEnd = sourceExtension.getStartDirection() != Enums.EDirection.dOpposite;
			bool bEndDistanceIsFromOppositeEnd = sourceExtension.getEndDirection() == Enums.EDirection.dOpposite;

			int sourceStartDistanceFromCore = (bStartDistanceIsFromOppositeEnd ? startSourceElementObj.getDistanceToOppVertex() : startSourceElementObj.getDistanceToNomVertex());
			int distanceFromStartOfCoreElement = sourceStartDistanceFromCore + sourceExtension.getStartDistance();
			int sourceEndDistanceFromCore = (bEndDistanceIsFromOppositeEnd ? endSourceElementObj.getDistanceToOppVertex() : endSourceElementObj.getDistanceToNomVertex());
			int distanceFromEndOfCoreElement = sourceEndDistanceFromCore + sourceExtension.getEndDistance();

			List<OBJID> targetExtensionElements = new List<OBJID>();

			// Find core elements
			OBJID currentCoreElementId = 0;
			foreach (var element in sourceElements)
			{
				GraphObj sourceGraphObj = graph.getGraphObj(element);
				OBJID coreElementId = sourceGraphObj.getCoreId();
				if (coreElementId != currentCoreElementId)
				{
					targetExtensionElements.Add(coreElementId);
					currentCoreElementId = coreElementId;
				}
			}

			coreExtension = new CoreExtension(distanceFromStartOfCoreElement, distanceFromEndOfCoreElement, targetExtensionElements, sourceExtension.getStartDirection(), sourceExtension.getEndDirection());
			// valid class type are already in CoreExtension
			return EConversionResult.crOk;
		}

		public EConversionResult convertToExtension(CoreExtension coreExtension, ref ElementExtension targetExtension, TopoGraph targetTopoGraph)
		{
			IReadOnlyList<OBJID> coreElements = coreExtension.getExtensionElements();    // by usage dir
			if (coreElements.Count == 0)
				return EConversionResult.crNoElements;

			OBJID startCoreElement = coreElements.First();
			OBJID endCoreElement = coreElements.Last();

			int startDistanceFromCore = coreExtension.getStartDistance();
			int endDistanceFromCore = coreExtension.getEndDistance();

			bool bStartDistanceIsFromOppositeEnd = coreExtension.getStartDirection() != Enums.EDirection.dOpposite;
			bool bEndDistanceIsFromOppositeEnd = coreExtension.getEndDirection() == Enums.EDirection.dOpposite;

			List<OBJID> targetObjsFromStart = targetTopoGraph.findObj(startCoreElement, startDistanceFromCore, bStartDistanceIsFromOppositeEnd);
			List<OBJID> targetObjsFromEnd = targetTopoGraph.findObj(endCoreElement, endDistanceFromCore, bEndDistanceIsFromOppositeEnd);

			// If start and end specify point position on same element border, use reversed list as start position to enable rbegin() to get the proper element as start element
			// This means that only the first (into reverse direction!) 0-length element must be considered as start element, not the last one!
			bool startObjsTakeFirstZeroLengthObject = false;

			if (targetObjsFromStart.Count > 1 && targetObjsFromEnd.Count > 1)
			{
				List<OBJID> reversedStart = new List<OBJID>(targetObjsFromStart);
				reversedStart.Reverse();
				if (reversedStart == targetObjsFromEnd)
				{
					targetObjsFromStart = reversedStart;
					startObjsTakeFirstZeroLengthObject = true;
				}
			}

			GraphObj startTargetElementObj = null;      // Find the last valid one in the same position into usage direction
			GraphObj endTargetElementObj = null;        // Find the first valid one in the same position into usage direction
			Graph graph = targetTopoGraph.getGraph();

			IReadOnlyList<Enums.SYSOBJ_TYPE> validTargetClassTypes = targetExtension.getValidObjTypes();
			
			for (int revIt = targetObjsFromStart.Count - 1; revIt >= 0; revIt--)
			{
				if (isValidObject(graph.getGraphObj(targetObjsFromStart[revIt]), validTargetClassTypes))
				{
					GraphObj graphObj = graph.getGraphObj(targetObjsFromStart[revIt]);
					if (startTargetElementObj == null || graphObj.getLength() == 0)
					{
						startTargetElementObj = graphObj;
						if (graphObj.getLength() == 0 && startObjsTakeFirstZeroLengthObject)
							break;
					}
				}
			}

			for (int revIt = targetObjsFromEnd.Count - 1; revIt >= 0; revIt--)
			{
				if (isValidObject(graph.getGraphObj(targetObjsFromEnd[revIt]), validTargetClassTypes))
				{
					GraphObj graphObj = graph.getGraphObj(targetObjsFromEnd[revIt]);
					if (endTargetElementObj == null || graphObj.getLength() == 0)
						endTargetElementObj = graphObj;
				}
			}

			if (startTargetElementObj == null || endTargetElementObj == null)
				return EConversionResult.crInternalDefinitionError;

			int startDistanceFromElement = startDistanceFromCore - (bStartDistanceIsFromOppositeEnd ? startTargetElementObj.getDistanceToOppVertex() : startTargetElementObj.getDistanceToNomVertex());
			int endDistanceFromElement = endDistanceFromCore - (bEndDistanceIsFromOppositeEnd ? endTargetElementObj.getDistanceToOppVertex() : endTargetElementObj.getDistanceToNomVertex());

			//Enums.EDirection eSearchingDir = rCoreExtension.getUsageDirection(); // End (head) direction
			Enums.EDirection eSearchingDir = coreExtension.getStartDirection(); // Start (tail) direction

			List<OBJID> targetExtensionElements = new List<OBJID>();
			if (startTargetElementObj == endTargetElementObj)
				targetExtensionElements.Add(startTargetElementObj.getId());
			else
			{
				// Find possible target via objects. Calculate allowed search depth from associated element count. This may be larger than the default max. value
				// Search depth is not exact value, it just has to be large enough to lead into correct result. It can even be considerably larger, but not infinite.
				List<OBJID> viaTargetElements = new List<OBJID>();
				int searchDepth = coreElements.Count; // Vertices may be missing from core extension (at least in BHPB/RATOIF given extensions), so add their assumed associated objects into depth (point elements)
				searchDepth += 10; // Add some extra...
				if (startCoreElement != endCoreElement)
				{
					foreach (var coreElementId in coreElements)
					{
						CoreObj targetCoreObj = graph.getCoreObj(coreElementId);

						// Put first (possibly not valid) target element into vector as via object
						if (targetCoreObj.getAssociatedObjects().Count > 0)
						{
							OBJID viaId = 0;
							if (coreElementId == startCoreElement)
								viaId = bStartDistanceIsFromOppositeEnd ? targetCoreObj.getAssociatedObjects().Last() : targetCoreObj.getAssociatedObjects().First();
							else if (coreElementId == endCoreElement)
								viaId = bEndDistanceIsFromOppositeEnd ? targetCoreObj.getAssociatedObjects().Last() : targetCoreObj.getAssociatedObjects().First();
							else
								viaId = targetCoreObj.getAssociatedObjects().First();

							viaTargetElements.Add(viaId);

							// Add associated element count to search depth
							searchDepth += targetCoreObj.getAssociatedObjects().Count;
						}
					}
				}
				else
				{
					CoreObj coreObj = graph.getCoreObj(startCoreElement);
					if (coreObj != null) // Add associated element count to search depth
						searchDepth += coreObj.getAssociatedObjects().Count;
				}

				// First try to search by setting all edges into own via elements (fast search)
				List<OBJID> pathElements = new List<OBJID>();

				FindWithAllEdgesInViasCondition allCoreElementsInViasCondition = new FindWithAllEdgesInViasCondition(ref graph, startTargetElementObj.getId(), endTargetElementObj.getId(), eSearchingDir, coreElements, ref pathElements);
				allCoreElementsInViasCondition.addViaElements(viaTargetElements);
				allCoreElementsInViasCondition.setSearchDepth(searchDepth);

				List<FindResult> resultPathWithAllVias = new List<FindResult>();
				TopoGraph.ETerminationReason eReasonForTermination = targetTopoGraph.findPath(allCoreElementsInViasCondition, resultPathWithAllVias);

				bool pathWasFound = eReasonForTermination == TopoGraph.ETerminationReason.ok && pathElements.Count > 0 && pathElements.First() == startTargetElementObj.getId() && pathElements.Last() == endTargetElementObj.getId();

				if (!pathWasFound)
				{
					// Not found with above search, try conventional search
					FindAllCondition extensionCondition = new FindAllCondition(startTargetElementObj.getId(), endTargetElementObj.getId(), eSearchingDir);
					extensionCondition.addViaElements(viaTargetElements);
					extensionCondition.setSearchDepth(searchDepth);

					List<FindResult> resultPathVector = new List<FindResult>();
					eReasonForTermination = targetTopoGraph.findPath(extensionCondition, resultPathVector);
					if (eReasonForTermination != TopoGraph.ETerminationReason.ok || resultPathVector.Count == 0)
						return EConversionResult.crInternalDefinitionError;

					// First result is taken as default (with single edge loops there can be two results, because the direction change information of results can not be used in collectPath())
					FindResult findResult = resultPathVector.First();

					if (resultPathVector.Count() > 1)
					{
						// size() > 1 : multiple paths: via points not defined correctly or loop track with zero offsets or coming out from single edge loop?
						foreach (FindResult result in resultPathVector)
						{
							// Prefer direct match with vias instead of first result (I actually don't understand, how this could be possible with multiple results? akk/19.9.2019)
							if (result.getResult() == viaTargetElements)
							{
								findResult = result;
								break;
							}
						}
					}

					pathElements = findResult.getResult();
				}

				// Remove other than valid target elements
				foreach (var pathElement in pathElements)
				{
					if (isValidObject(graph.getGraphObj(pathElement), validTargetClassTypes))
						targetExtensionElements.Add(pathElement);
				}
			}

			targetExtension = new ElementExtension(startDistanceFromElement, endDistanceFromElement, targetExtensionElements, eSearchingDir, coreExtension.getEndDirection());
			// valid class types are already included in result extension
			return EConversionResult.crOk;
		}

		public ECreateExtensionResult createExtension(OBJID startId, OBJID endId, int distFromStart, int distFromEnd, EDirection edir, List<uint> viaElements, TopoGraph topoGraph, ElementExtension resultExtension)
		{
			IReadOnlyList<Enums.SYSOBJ_TYPE> validClassTypes = resultExtension.getValidObjTypes();
			Graph graph = topoGraph.getGraph();
			GraphObj startGraphObj = graph.getGraphObj(startId);
			GraphObj endGraphObj = graph.getGraphObj(endId);
			if (startGraphObj != null)
			{
				if (startGraphObj.getLength() < distFromStart)
					return ECreateExtensionResult.ceStartDistanceNotValid;

				Enums.CLASS_TYPE startClassType = startGraphObj.getClassType();

				if (!validClassTypes.Contains((Enums.SYSOBJ_TYPE)startClassType))
					return ECreateExtensionResult.ceStartNotExist;
			}
			else
				return ECreateExtensionResult.ceStartNotExist;

			if (endGraphObj != null)
			{
				if (endGraphObj.getLength() < distFromEnd)
					return ECreateExtensionResult.ceEndDistanceNotValid;

				Enums.CLASS_TYPE endClassType = endGraphObj.getClassType();
				if (!validClassTypes.Contains((Enums.SYSOBJ_TYPE)endClassType))
					return ECreateExtensionResult.ceEndNotExist;
			}
			else
				return ECreateExtensionResult.ceEndNotExist;

			if (startId == endId)
			{
				if (startGraphObj.getLength() < (distFromStart + distFromEnd))
					return ECreateExtensionResult.ceStartDistanceNotValid;
			}

			// ElementExtension* pExtension = 0;

			FindAllCondition extensionCondition = new FindAllCondition(startId, endId, edir);
			extensionCondition.addViaElements(viaElements);
			// No way to set search depth here, has to rely on max. depth value

			List<FindResult> resultPathVector = new List<FindResult>();
			TopoGraph.ETerminationReason eReasonForTermination = topoGraph.findPath(extensionCondition, resultPathVector);
			if (eReasonForTermination != TopoGraph.ETerminationReason.ok)
			{
				if (eReasonForTermination == TopoGraph.ETerminationReason.fromNotExist)
					return ECreateExtensionResult.ceStartNotExist;
				else if (eReasonForTermination == TopoGraph.ETerminationReason.targetElementNotFound)
					return ECreateExtensionResult.ceEndNotExist;

				return ECreateExtensionResult.csDefinitionError;
			}
			if (resultPathVector.Count != 1)
				return ECreateExtensionResult.csExtensionNotUnique;  // multiple paths, via point not defined correctly ?

			// remove other than valid target elements
			List<OBJID> extensionElements = new List<OBJID>();
			FindResult findResult = resultPathVector.First();
			foreach (var resultItem in findResult.getResult())
			{
				if (validClassTypes.Count == 0)
					extensionElements.Add(resultItem);
				else
				{
					GraphObj element = graph.getGraphObj(resultItem);
					Enums.CLASS_TYPE classType = element.getClassType();
					// todo: find more efficient method/algorithm
					if (validClassTypes.Contains((Enums.SYSOBJ_TYPE)classType))
						extensionElements.Add(resultItem);
				}
			}

			// create element extension result
			resultExtension = new ElementExtension(distFromStart, distFromEnd, extensionElements, edir, edir);
			resultExtension.validObjTypes(validClassTypes); // copies back
			return ECreateExtensionResult.ceOk;
		}

		private bool isValidObject(GraphObj graphObj, IReadOnlyList<Enums.SYSOBJ_TYPE> validClassTypes)
		{
			if (graphObj != null)
			{
				if (validClassTypes.Count == 0)
					return true;
				else
				{
					Enums.CLASS_TYPE objectClassType = graphObj.getClassType();
					if (validClassTypes.Contains((Enums.SYSOBJ_TYPE)objectClassType))
						return true;
				}
			}
			return false;
		}
	}
}
