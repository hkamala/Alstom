namespace RailgraphLib.HierarchyObjects
{
    using RailgraphLib.Interlocking;
    using System.Collections.Generic;
    using OBJID = UInt32;

	public class Route : HierarchyBaseObject
    {
		private OBJID m_bpid;
		private OBJID m_epid;
		private Enums.EDirection m_edir;
		private Station m_station;
		private SortedDictionary<int, Point> m_points = new SortedDictionary<int, Point>();
		private List<OBJID> m_focusObjects = new();

        public OBJID BPID { get { return m_bpid; } }
		public OBJID EPID { get { return m_epid; } }
		public Enums.EDirection EDir { get { return m_edir; } }
		public Station Station { get { return m_station; } }

		public void SetBPID(OBJID bpid) => m_bpid = bpid;
		public void SetEPID(OBJID epid) => m_epid = epid;
		public void SetDirection(Enums.EDirection edir) => m_edir = edir;
		public void SetStation(Station station) => m_station = station;


        public Route(OBJID sysid, string sysName, string externalID) : base(sysid, Enums.SYSOBJ_TYPE.TYP_ROUTE2, sysName, externalID)
		{
		}

		public void AddPoint(Point point, int seqNo)
		{
			if (!m_points.ContainsKey(seqNo))
				m_points.Add(seqNo, point);
		}
        
		public void AddFocusObject(OBJID obj)
        {
            if (!m_focusObjects.Contains(obj))
                m_focusObjects.Add(obj);
        }

        public Point GetPoint(int seqNo) => m_points.ContainsKey(seqNo) ? m_points[seqNo] : null;
		public List<OBJID> GetFocusObjects() => m_focusObjects;
		public IReadOnlyDictionary<int, Point> GetPointsBySeqNo() => m_points;
		public IReadOnlyList<Point> GetPoints() => m_points.Values.ToList();
    }
}
