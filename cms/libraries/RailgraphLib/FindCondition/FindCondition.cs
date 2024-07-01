using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.FindCondition
{
	public class FindCondition
	{
		public static readonly int maxSearchDepthDefault = 100;

		public FindCondition(UInt32 from, Enums.EDirection eSearchDir)
		{
			m_from = from;
			m_eSearchDir = eSearchDir;
		}

		public FindCondition(UInt32 from, UInt32 target, Enums.EDirection eSearchDir)
		{
			m_from = from;
			m_target = target;
			m_eSearchDir = eSearchDir;
		}

		public void addViaElement(UInt32 viaElementId) => m_viaElements.Add(viaElementId);
		public void addViaElements(List<UInt32> rViaElements) => m_viaElements.AddRange(rViaElements);

		public virtual bool setSearchDepth(int depth)
		{
			m_maxSearchDepth = depth;
			return true;
		}

		public int searchDepth() => m_maxSearchDepth;
		public Enums.EDirection searchDir() => m_eSearchDir;
		public UInt32 target() => m_target;
		public UInt32 from() => m_from;
		public IReadOnlyList<UInt32> getViaElements() => m_viaElements;
		public bool orderedViaElements() => m_bOrderedVias;

		protected bool m_bOrderedVias = false; // via list is went through from the first to last element during a search
		protected UInt32 m_from;
		protected UInt32 m_target = 0;
		protected int m_maxSearchDepth = maxSearchDepthDefault;   // how many elements is examinated in a path
		protected Enums.EDirection m_eSearchDir;
		protected List<UInt32> m_viaElements = new List<uint>();
	}
}
