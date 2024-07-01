using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public class Edge : CoreObj
	{
		private List<UInt32> m_objects = new List<uint>();
		private int m_startoffsetsection;
		private int m_startoffset;
		private int m_endoffsetsection;
		private int m_endoffset;

		public Edge(UInt32 objId, UInt32 objType, Enums.CLASS_TYPE classType, string name) : base(objId, objType, classType, name)
		{

		}

		public void setStartOffsetSection(int startoffsetsection) => m_startoffsetsection = startoffsetsection;
		public void setStartOffset(int startoffset) => m_startoffset = startoffset;
		public void setEndOffsetSection(int endoffsetsection) => m_endoffsetsection = endoffsetsection;
		public void setEndOffset(int endoffset) => m_endoffset = endoffset;
		public int getStartOffsetSection() => m_startoffsetsection;
		public int getStartOffset() => m_startoffset;
		public int getEndOffsetSection() => m_endoffsetsection;
		public int getEndOffset() => m_endoffset;

		public override List<uint> getAssociatedObjects() => m_objects;
		public override void setAssociatedObjects(List<uint> objects)
		{
			m_objects = new List<uint>();
			m_objects.AddRange(objects);
		}
	}
}
