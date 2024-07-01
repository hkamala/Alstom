using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using BINTYPE = UInt64;
	using OBJTYPE = UInt16;

	public class FictiveSignal : ILGraphObj
	{
		public FictiveSignal(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection aspectDir) : base(objId, objType, classType, objName) 
		{
			m_eLogicalDir = aspectDir;
		}

		public virtual bool isRoutingEnabled() => true;

		public virtual Enums.EDirection getLogicalDir() => m_eLogicalDir;
		
		private Enums.EDirection m_eLogicalDir;
	}
}
