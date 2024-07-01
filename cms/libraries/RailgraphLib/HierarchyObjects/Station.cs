using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using OBJID = UInt32;

	public class Station : HierarchyBaseObject
    {
		private List<Platform> m_platforms = new List<Platform>();
		private List<Route> m_routes = new List<Route>();
		
		
		public Station(OBJID sysid, string sysName, string externalID) : base(sysid, Enums.SYSOBJ_TYPE.TYP_LOGICAL_SITE, sysName, externalID)
		{
		}

		public void AddPlatform(Platform platform) => m_platforms.Add(platform);
		public void AddRoute(Route route) => m_routes.Add(route);

        public IReadOnlyList<Platform> Platforms { get { return m_platforms; } }
		public IReadOnlyList<Route> Routes { get { return m_routes; } }
    }
}
