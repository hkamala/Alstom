using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using OBJID = UInt32;
	public class Platform : HierarchyBaseObject
	{
		private Station m_station;
		private List<Track> m_tracks = new List<Track>();

		public Platform(OBJID sysid, string sysName, string externalID) : base(sysid, Enums.SYSOBJ_TYPE.TYP_PLATFORM, sysName, externalID)
		{
		}

		public void SetStation(Station station) => m_station = station;
		public void AddTrack(Track track) => m_tracks.Add(track);

		public Station Station { get { return m_station; } }
		public IReadOnlyList<Track> Tracks { get { return m_tracks; } }
	}
}
