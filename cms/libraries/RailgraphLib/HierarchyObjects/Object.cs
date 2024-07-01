using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using OBJID = UInt32;

	public class HierarchyBaseObject
	{
		public HierarchyBaseObject(OBJID sysid, Enums.SYSOBJ_TYPE type, string sysName, string externalID)
		{
			SysID = sysid;
			SysName = sysName;
			Type = type;
			ExternalID = externalID;
		}

		public OBJID SysID { get; }
		public string SysName { get; }
		public Enums.SYSOBJ_TYPE Type { get; }
		public string ExternalID { get; }
	}
}
