using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using OBJID = UInt32;

	public class Track : HierarchyBaseObject
	{
		public Track(OBJID sysid, string sysName, string externalID) : base(sysid, Enums.SYSOBJ_TYPE.TYP_TRACK, sysName, externalID)
		{
		}

		private Platform m_platform;
		public Platform Platform { get { return m_platform; } }
		public void SetPlatform(Platform platform) => m_platform = platform;
	}
}
