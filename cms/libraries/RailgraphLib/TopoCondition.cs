using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.FindCondition;

namespace RailgraphLib
{
	public class TopoCondition
	{
		public TopoCondition(TopoGraph rTopoGraph, FindAllCondition rUserCondition)
		{
			// using of exception could be better idea...
			m_rTopoGraph = rTopoGraph;
			m_rUserCondition = rUserCondition;
			m_eTerminateReason = TopoGraph.ETerminationReason.targetNotExist;
			m_eTerminateReason = validate();
		}
		public virtual FindAllCondition getUserCondition() => m_rUserCondition;

		public virtual FindAllCondition.EConditionalProceed isConditionFound(UInt32 current, UInt32 previous) => m_rUserCondition.isConditionFound(current, previous);

		public TopoGraph.ETerminationReason validateConditions() => m_eTerminateReason;
		public IReadOnlyDictionary<UInt32, UInt32> getTargets() => m_targetMap;
		public UInt32 getStartCoreElement() => m_fromCoreId;
		public void setStartCoreId(UInt32 coreId) => m_fromCoreId = coreId;
		public bool isCoreSearch() => m_bIsCoreSearch;

		public void addTargets(UInt32 coreId, UInt32 elementId)
		{
			if (!m_targetMap.ContainsKey(coreId))
				m_targetMap.Add(coreId, elementId);
		}

		public void ClearTargets() => m_targetMap.Clear();

		private TopoGraph.ETerminationReason validate()
		{
			if (m_rUserCondition.from() == 0)
				return TopoGraph.ETerminationReason.fromNotExist;

			CoreObj coreStart = m_rTopoGraph.getCoreObj(m_rUserCondition.from());
			m_bIsCoreSearch = coreStart != null;
			if (m_bIsCoreSearch)
				return validateCoreSearch();
			else
				return validateElementSearch();
		}

		private TopoGraph.ETerminationReason validateCoreSearch()
		{
			CoreObj coreStart = m_rTopoGraph.getCoreObj(m_rUserCondition.from());
			if (coreStart == null)
				return TopoGraph.ETerminationReason.fromNotExist;
				
			setStartCoreId(coreStart.getId());

			bool isTargetGiven = (m_rUserCondition.target() > 0);
			if (isTargetGiven)
			{
				UInt32 targetCoreId = m_rUserCondition.target();
				UInt32 targetElementId = targetCoreId; // these are same in core search

				CoreObj coreTarget = m_rTopoGraph.getCoreObj(targetCoreId);
				if (coreTarget == null)
					return TopoGraph.ETerminationReason.targetNotExist;

				addTargets(targetCoreId, targetElementId);
			}
			
			foreach (UInt32 objId in m_rUserCondition.getViaElements())
			{
				CoreObj core = m_rTopoGraph.getCoreObj(objId);
				if (core == null)
					return TopoGraph.ETerminationReason.viaNotExist;
			}

			return TopoGraph.ETerminationReason.ok;

		}
		private TopoGraph.ETerminationReason validateElementSearch()
		{
			GraphObj from = m_rTopoGraph.getGraphObj(m_rUserCondition.from());
			if (from == null)
				return TopoGraph.ETerminationReason.fromNotExist;
			
			setStartCoreId(from.getCoreId());

			bool isTargetGiven = (m_rUserCondition.target() > 0);
			if (isTargetGiven)
			{
				UInt32 targetElementId = m_rUserCondition.target();
				GraphObj target = m_rTopoGraph.getGraphObj(targetElementId);
				if (target == null)
					return TopoGraph.ETerminationReason.targetNotExist;

				UInt32 targetCoreId = target.getCoreId();
				CoreObj coreStart = m_rTopoGraph.getCoreObj(targetCoreId);
				if (coreStart == null)
					return TopoGraph.ETerminationReason.fromNotExist;

				addTargets(targetCoreId, targetElementId);
			}

			foreach (UInt32 objId in m_rUserCondition.getViaElements())
			{
				GraphObj graphObj = m_rTopoGraph.getGraphObj(objId);
				if (graphObj == null)
					return TopoGraph.ETerminationReason.viaNotExist;
			}

			// all ok
			return TopoGraph.ETerminationReason.ok;
		}

		private readonly FindAllCondition m_rUserCondition;
		private readonly TopoGraph m_rTopoGraph;
		private UInt32 m_fromCoreId = 0;
		private bool m_bIsCoreSearch = false;
		private TopoGraph.ETerminationReason m_eTerminateReason;

		private SortedDictionary<UInt32, UInt32>  m_targetMap = new SortedDictionary<uint, uint>(); // first = core object, second = graph element
	}
}
