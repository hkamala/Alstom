using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib
{
	public class Vertex : CoreObj
	{
		private List<UInt32> m_objects = new List<uint>();

		public Vertex(UInt32 objId, UInt32 objType, Enums.CLASS_TYPE classType, string name) : base(objId, objType, classType, name)
		{

		}

		public override List<uint> getAssociatedObjects() => m_objects;

		public override void setAssociatedObjects(List<uint> objects)
		{
			m_objects = new List<uint>();
			m_objects.AddRange(objects);
		}
	}
}
