using RailgraphLib.Interlocking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.HierarchyObjects
{
	using EBISYSID = UInt32;

	public class HierarchyRelations
	{
		private int m_namepartType = 6000;
		private bool m_initialized = false;

		public List<Station> Stations = new List<Station>();
		public List<Platform> Platforms = new List<Platform>();
		public List<Route> Routes = new List<Route>();
		public List<Track> Tracks = new List<Track>();
		public List<Point> Points = new List<Point>();

		private List<HierarchyBaseObject> AllObjects = new List<HierarchyBaseObject>();

		private static HierarchyRelations m_instance;
		private HierarchyRelations(int namepartType)
		{
			m_namepartType=namepartType;
		}

		public static HierarchyRelations Instance(SolidDB.CSolidEntryPoint? solidDB = null, int namepartType = 6000)
		{
			if (m_instance == null && solidDB != null)
			{
				m_instance = new HierarchyRelations(namepartType);
				m_instance.Build(solidDB);
			}

			return m_instance;
		}

		private void Build(SolidDB.CSolidEntryPoint solidDb)
		{
			if (m_initialized)
				return;

			m_initialized = true;
			Stations.AddRange(ToObjects<Station>(solidDb.GetObjectsByType((int)Enums.SYSOBJ_TYPE.TYP_LOGICAL_SITE, m_namepartType)));
			Platforms.AddRange(ToObjects<Platform>(solidDb.GetObjectsByType((int)Enums.SYSOBJ_TYPE.TYP_PLATFORM, m_namepartType)));
			Tracks.AddRange(ToObjects<Track>(solidDb.GetObjectsByType((int)Enums.SYSOBJ_TYPE.TYP_TRACK, m_namepartType)));
			Routes.AddRange(ToObjects<Route>(solidDb.GetObjectsByType((int)Enums.SYSOBJ_TYPE.TYP_ROUTE2, m_namepartType)));
			Points.AddRange(ToObjects<Point>(solidDb.GetObjectsByType((int)Enums.SYSOBJ_TYPE.TYP_POINT, m_namepartType)));

			var hierarchies = solidDb.GetHierarchies(new List<Enums.HT_TYPE>());
			var route2 = solidDb.GetRoutes((int)Enums.HT_TYPE.HT_SITE);
			BuildStationToPlatformsRelation(hierarchies);
			BuildPlatformToTracksRelations(hierarchies);
			BuildRoutesToPointsRelations(hierarchies);
            BuildRoutesToFocusObjectsRelations(hierarchies);
            BuildStationsToRoutesRelations(route2);
		}

		internal List<T> ToObjects<T>(List<SortedDictionary<string, object>> dbObjects) where T : HierarchyBaseObject
		{
			List<T> retVal = new List<T>();

			foreach (var obj in dbObjects)
			{
				EBISYSID objID = (EBISYSID)(int)obj[SolidDB.CSolidEntryPoint.OBJID];
				Enums.SYSOBJ_TYPE objType = (Enums.SYSOBJ_TYPE)(int)obj[SolidDB.CSolidEntryPoint.OBJTYPENO];
				string sysName = (string)obj[SolidDB.CSolidEntryPoint.SYSNAME];
				string externalID = (string)obj[SolidDB.CSolidEntryPoint.NAMEPART];

				HierarchyBaseObject o = createObject(objID, objType, sysName, externalID);
				if (o != null)
					retVal.Add((T)o);
			}

			return retVal;
		}

		private HierarchyBaseObject createObject(EBISYSID sysid, Enums.SYSOBJ_TYPE objType, string sysName, string externalID)
		{
			switch (objType)
			{
				case Enums.SYSOBJ_TYPE.TYP_LOGICAL_SITE: return new Station(sysid, sysName, externalID);
				case Enums.SYSOBJ_TYPE.TYP_PLATFORM: return new Platform(sysid, sysName, externalID);
				case Enums.SYSOBJ_TYPE.TYP_PLATFORM_TRACK: return new Track(sysid, sysName, externalID);
				case Enums.SYSOBJ_TYPE.TYP_ROUTE2: return new Route(sysid, sysName, externalID);
				case Enums.SYSOBJ_TYPE.TYP_TRACK: return new Track(sysid, sysName, externalID);
				case Enums.SYSOBJ_TYPE.TYP_POINT: return new Point(sysid, sysName, externalID);
				default: return null;
			}
		}

		private void BuildStationToPlatformsRelation(List<SortedDictionary<string, object>> hierarchies)
		{
			foreach (var hier in hierarchies.Where(item => (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE] == Enums.HT_TYPE.HT_STATIONS_PLATFORMS))
			{
				EBISYSID stationID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.MASTERID];
				EBISYSID platformID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.ASSOCIATIONID];

				Station station = Stations.Where(item => item.SysID == stationID).FirstOrDefault();
				Platform platform = Platforms.Where(item => item.SysID == platformID).FirstOrDefault();
				if (station != null && platform != null)
				{
					station.AddPlatform(platform);
					platform.SetStation(station);
				}
			}
		}
		public IReadOnlyList<Platform> GetPlatformsByStation(EBISYSID stationID) => Stations.Where(item => item.SysID == stationID).FirstOrDefault()?.Platforms ?? new List<Platform>();
		public Station GetStationByPlatform(EBISYSID platformID) => Platforms.Where(item => item.SysID == platformID).FirstOrDefault()?.Station;

		private void BuildPlatformToTracksRelations(List<SortedDictionary<string, object>> hierarchies)
		{
			foreach (var hier in hierarchies.Where(item => (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE] == Enums.HT_TYPE.HT_PLATFORM_TRACK))
			{
				EBISYSID platformID= (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.MASTERID];
				EBISYSID trackID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.ASSOCIATIONID];

				Platform platform = Platforms.Where(item => item.SysID == platformID).FirstOrDefault();
				Track track = Tracks.Where(item => item.SysID == trackID).FirstOrDefault();
				if (platform != null && track != null)
				{
					platform.AddTrack(track);
					track.SetPlatform(platform);
				}
			}
		}

		public void BuildRoutesToPointsRelations(List<SortedDictionary<string, object>> hierarchies)
		{
			foreach (var hier in hierarchies.Where(item => (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE] == Enums.HT_TYPE.HT_ROUTE_VIAOBJ))
			{
				EBISYSID routeID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.MASTERID];
				EBISYSID pointID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.ASSOCIATIONID];
				int seqNo = (int)hier[SolidDB.CSolidEntryPoint.SEQNO];

				Route route = Routes.Where(item => item.SysID == routeID).FirstOrDefault();
				Point point = Points.Where(item => item.SysID == pointID).FirstOrDefault();
				if (route != null && point != null)
				{
					route.AddPoint(point, seqNo);
					point.SetRoute(route);
				}
			}
		}

		public void BuildRoutesToFocusObjectsRelations(List<SortedDictionary<string, object>> hierarchies)
		{
            foreach (var hier in hierarchies.Where(item => (Enums.HT_TYPE)(int)item[SolidDB.CSolidEntryPoint.ASSOCIATIONTYPE] == Enums.HT_TYPE.HT_ROUTE_FOCUSING))
            {
                EBISYSID routeID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.MASTERID];
                EBISYSID objID = (EBISYSID)(int)hier[SolidDB.CSolidEntryPoint.ASSOCIATIONID];
                //int seqNo = (int)hier[SolidDB.CSolidEntryPoint.SEQNO];

                Route route = Routes.Where(item => item.SysID == routeID).FirstOrDefault();
                if (route != null && objID != 0)
                {
					route.AddFocusObject(objID);
                }
            }
        }

        public IReadOnlyList<Track> GetTracksByPlatform(EBISYSID platformID) => Platforms.Where(item => item.SysID == platformID).FirstOrDefault()?.Tracks ?? new List<Track>();
		public Platform GetPlatformByTrack(EBISYSID trackID) => Tracks.Where(item => item.SysID == trackID).FirstOrDefault()?.Platform;
		public string GetNameBySysID(EBISYSID objID) => AllObjects.Where(item => item.SysID == objID).FirstOrDefault()?.SysName ?? String.Empty;
		public EBISYSID GetSysIDByName(string name) => AllObjects.Where(item => item.SysName == name).FirstOrDefault()?.SysID ?? 0;

		private void BuildStationsToRoutesRelations(List<SortedDictionary<string, object>> routes)
		{
			foreach (var dict in routes)
			{
				EBISYSID sysid = (EBISYSID)(int)dict[SolidDB.CSolidEntryPoint.OBJID];
				Route route = Routes.Where(item => item.SysID == sysid).FirstOrDefault();
				if (route == null)
					continue;

				route.SetBPID((EBISYSID)(int)dict[SolidDB.CSolidEntryPoint.BPID]);
				route.SetEPID((EBISYSID)(int)dict[SolidDB.CSolidEntryPoint.EPID]);
				route.SetDirection((Enums.EDirection)((int)dict[SolidDB.CSolidEntryPoint.DIRECTION] + 1));	// Route directions differ from other directions? 0=Nominal, 1=Opposite
				
				EBISYSID stationid = (EBISYSID)(int)dict[SolidDB.CSolidEntryPoint.STATIONID];
				Station station = Stations.Where(item => item.SysID == stationid).FirstOrDefault();
				if (station == null)
					continue;

				station.AddRoute(route);
				route.SetStation(station);
			}
		}

		public HierarchyBaseObject GetObjectByName(string name) => AllObjects.Where(item => item.SysName == name).FirstOrDefault();
		public HierarchyBaseObject GetObjectBySysId(EBISYSID sysid) => AllObjects.Where(item => item.SysID == sysid).FirstOrDefault();
		public Station GetStationByName(string name) => Stations.Where(item => item.SysName == name).FirstOrDefault();
		public Station GetStationBySysID(EBISYSID sysid) => Stations.Where(item => item.SysID == sysid).FirstOrDefault();
		public Platform GetPlatformByName(string name) => Platforms.Where(item => item.SysName == name).FirstOrDefault();
		public Platform GetPlatformBySysID(EBISYSID sysid) => Platforms.Where(item => item.SysID == sysid).FirstOrDefault();
		public Route GetRouteByName(string name) => Routes.Where(item => item.SysName == name).FirstOrDefault();
		public Route GetRouteBySysID(EBISYSID sysid) => Routes.Where(item => item.SysID == sysid).FirstOrDefault();
		public Track GetTrackByName(string name) => Tracks.Where(item => item.SysName == name).FirstOrDefault();
		public Track GetTrackBySysID(EBISYSID sysid) => Tracks.Where(item => item.SysID == sysid).FirstOrDefault();

	}
}
