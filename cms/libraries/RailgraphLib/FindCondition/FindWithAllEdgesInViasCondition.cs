using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.FindCondition
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class FindWithAllEdgesInViasCondition : FindAllCondition
	{
		public FindWithAllEdgesInViasCondition(ref Graph graph, OBJID from, OBJID target, Enums.EDirection eSearchDir, IReadOnlyList<OBJID> coreVias, ref List<OBJID> elements) : base(from, target, eSearchDir)
		{
			m_graph = graph;
			m_coreVias = coreVias;
			m_elements = elements;
			elements.Clear(); // <- reference to creator's vector!
		}

		public override EConditionalProceed isConditionFound(OBJID current, OBJID previous)
		{
			CoreObj coreObj = m_graph.getCoreObj(current);
			if (coreObj == null)
			{
				// Not core graph, get core object through graph object
				GraphObj graphObj = m_graph.getGraphObj(current);
				if (graphObj == null)
					return EConditionalProceed.cpFail;

				coreObj = m_graph.getCoreObj(graphObj.getCoreId());
			}

			if (coreObj == null)
				return EConditionalProceed.cpFail;

			// The only special case here is the one where zero length extension lies only in vertex. Otherwise there always are edges in core vias
			if (previous == 0 && m_coreVias.Count == 1 && coreObj is Vertex && m_coreVias.First() == coreObj.getId())
			{
				m_elements.Add(current);
				return EConditionalProceed.cpFound;
			}

			// Every edge in extension must exist in core vias!
			if (coreObj is Edge && !m_coreVias.Contains(coreObj.getId()))
				return EConditionalProceed.cpFoundAndContinue; // Has to use this to keep RailGraph searching on another branches of vertex!
		
			// Remove all elements found after previous and put current after it
			if (previous != 0)
			{
				int index = m_elements.FindIndex(x => x == previous);
				if (index >= 0)
				{
					m_elements.RemoveRange(index, m_elements.Count - index);
					m_elements.Add(previous);
				}
			}
			else
				m_elements.Clear();

			m_elements.Add(current);

			if (current == m_target)
			{
				// This is used for checking that possible whole loop is searched in correct order, and that all via edges have been passed
				List<OBJID> edges = new List<OBJID>();
				List<OBJID> viaEdges = new List<OBJID>();

				foreach (var element in m_elements)
				{
					CoreObj cO = m_graph.getCoreObj(element);
					if (cO == null)
					{
						GraphObj gO = m_graph.getGraphObj(element);
						cO = (gO != null ? m_graph.getCoreObj(gO.getCoreId()) : null);
					}
					if (cO != null && cO is Edge && !edges.Contains(cO.getId()))
						edges.Add(cO.getId());
				}

				foreach (var element in m_coreVias)
				{
					if (m_graph.getCoreObj(element) is Edge)
						viaEdges.Add(element);
				}

				// If all via edges were passed, we've found the path
				if (edges.Count == viaEdges.Count)
				{
					bool foundAll = true;
					foreach (var viaEdge in viaEdges)
					{
						if (!edges.Contains(viaEdge))
						{
							foundAll = false;
							break;
						}
					}
					if (foundAll)
						return EConditionalProceed.cpFound;
				}

				return EConditionalProceed.cpFoundAndContinue; // Has to use this to keep RailGraph searching on another branch on loop!
			}

			return EConditionalProceed.cpContinue;
		}

		private Graph m_graph;
		IReadOnlyList<OBJID> m_coreVias;
		List<OBJID> m_elements;

	}
}
