using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public class OffsetSection
	{
		private int m_startOffsetSection = -1;
		private int m_endOffsetSection = -1;
		private int m_startOffset = -1;
		private int m_endOffset = -1;

		public int StartOffsetSection { get { return m_startOffsetSection; } }
		public int EndOffsetSection { get { return m_endOffsetSection; } }
		public int StartOffset { get { return m_startOffset; } }
		public int EndOffset { get { return m_endOffset; } }

		public void SetStartOffsetSection(int offset) => m_startOffsetSection = offset;
		public void SetEndOffsetSection(int offset) => m_endOffsetSection = offset;
		public void SetStartOffset(int offset) => m_startOffset = offset;
		public void SetEndOffseT(int offset) => m_endOffset = offset;

		public bool IsValid() => m_startOffset != -1;

		public int OffsetLength() => Math.Abs(m_endOffset - m_startOffset);
		public int OffsetSectionLength() => Math.Abs(m_endOffsetSection - m_startOffsetSection);
		public bool IsOffsetInOffset(int offset) => Math.Min(m_startOffset, m_endOffset) <= offset && offset <= Math.Max(m_startOffset, m_endOffset);
		public bool IsOffsetInOffsetSection(int offset) => Math.Min(m_startOffsetSection, m_endOffsetSection) <= offset && offset <= Math.Max(m_startOffsetSection, m_endOffsetSection);
		public int OffsetSectionFactor() => m_startOffsetSection < m_endOffsetSection ? 1 : -1;
	}
}
