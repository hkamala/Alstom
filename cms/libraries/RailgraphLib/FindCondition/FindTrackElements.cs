using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.Interlocking;

namespace RailgraphLib.FindCondition
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class FindTrackElements : FindAllCondition
	{
		public FindTrackElements(OBJID trackId, Enums.EDirection eDir, ref List<OBJID> sourceTrackElements, TopoGraph topoGraph) : base(trackId, 0, eDir)
		{
			m_eDir = eDir;
			m_sourceTrackElements = sourceTrackElements;
			m_topoGraph = topoGraph;
			m_startTrackId = trackId;
		}

		public override EConditionalProceed isConditionFound(OBJID current, OBJID previous) 
		{
			GraphObj pObj = m_topoGraph.getGraphObj(current);
			if (current != m_startTrackId && (pObj is Track || pObj is Point))
			{
				int currIndex = m_sourceTrackElements.FindIndex(x => x == current);
				if (currIndex < 0)
				{
					if (m_topoGraph.getGraphObj(previous) is Point)
						return EConditionalProceed.cpBreak;

					return EConditionalProceed.cpFound;
				}

				m_sourceTrackElements.RemoveAt(currIndex);

				// Add found track to target collection ordered into nominal direction
				if (m_eDir == Enums.EDirection.dNominal)
					m_trackElements.Add(current);
				else
					m_trackElements.Insert(0, current);
			}

			return EConditionalProceed.cpContinue;
		}

		public IReadOnlyList<OBJID> getFoundTrackElements() => m_trackElements;

		private TopoGraph m_topoGraph;
		private List<OBJID> m_sourceTrackElements;
		Enums.EDirection m_eDir;
		List<OBJID> m_trackElements = new List<OBJID>();
		OBJID m_startTrackId;
	}
}
