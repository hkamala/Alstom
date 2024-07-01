using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Core
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;

	public class Edge : CoreGraphObj
	{
		public Edge(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eCoreDir) : base(objId, objType, classType, objName)
		{
			m_eCoreDir = eCoreDir;
		}

		public virtual Enums.EDirection getCoreDirection() => m_eCoreDir;

		private Enums.EDirection m_eCoreDir;
}
}
