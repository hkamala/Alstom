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

	public class Balise : ILGraphObj
	{
		public Balise(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eBaliseDir) : base(objId, objType, classType, objName)
		{
			m_eBaliseDir = eBaliseDir;
			m_associatedTrackId = 0;
			m_iDistanceToNomSideOfAssociatedTrack = 0;
			m_iDistanceToOppSideOfAssociatedTrack = 0;
		}

		public virtual Enums.EDirection getBaliseDirection() => m_eBaliseDir;

		public virtual OBJID getAssociatedTrack() => m_associatedTrackId;

		public virtual int getDistanceToNomTrackEdge() => m_iDistanceToNomSideOfAssociatedTrack;

		public virtual int getDistanceToOppTrackEdge() => m_iDistanceToOppSideOfAssociatedTrack;

		public virtual int getDistanceToTrackEdge(Enums.EDirection eFindEdgeToDirection)
		{
			if (eFindEdgeToDirection == Enums.EDirection.dNominal)
				return getDistanceToNomTrackEdge();
			else if (eFindEdgeToDirection == Enums.EDirection.dOpposite)
				return getDistanceToOppTrackEdge();
			else
				return 0;
		}

		// set

		public virtual void setAssociatedTrack(OBJID idAssociatedTrack) => m_associatedTrackId = idAssociatedTrack;

		public virtual void setDistanceToNomTrackEdge(int distance) => m_iDistanceToNomSideOfAssociatedTrack = distance;

		public virtual void setDistanceToOppTrackEdge(int distance) => m_iDistanceToOppSideOfAssociatedTrack = distance;

		private OBJID m_associatedTrackId;
		private int m_iDistanceToNomSideOfAssociatedTrack;
		private int m_iDistanceToOppSideOfAssociatedTrack;
		private Enums.EDirection m_eBaliseDir;
};
}
