using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.armd;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using OBJTYPE = UInt16;
	using BINTYPE = UInt64;

	public class ILGraphObj : GraphObj
	{
		public ILGraphObj(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName) : base(objId, objType, classType, objName) { }

		public bool isInReliableState()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			if ((dynBits & ArmdPredefinedIf.getRailObjCDHFaultyMask()) == ArmdPredefinedIf.getRailObjCDHFaultyOnBits())
				return false;

			return true;
		}

		public virtual bool isFaulty() => !isInReliableState();

		public virtual bool isInDarkArea()
		{
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if ((staBits & ArmdPredefinedIf.getRailObjInDarkAreaMask()) == ArmdPredefinedIf.getRailObjInDarkAreaBits())
				return true;
			return false;
		}

		public virtual bool isMarkedFaulty()
		{
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if ((staBits & ArmdPredefinedIf.getRailObjMarkedFaultyMask()) == ArmdPredefinedIf.getRailObjMarkedFaultyBits())
				return true;
			return false;
		}

		public virtual bool isOccupationUnreliable()
		{
			bool bUseStaticBits = true;
			BINTYPE mask = ArmdPredefinedIf.getRailObjOccupationUnreliableMask(ref bUseStaticBits);
			BINTYPE bits = ILGraph.getStaticBits(getId());
			if (!bUseStaticBits)
				bits = ILGraph.getDynamicBits(getId());

			if ((bits & mask) == ArmdPredefinedIf.getRailObjOccupationUnreliableBits())
				return true;

			return false;
		}

		public bool isInSystemBorder()
		{
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if ((staBits & ArmdPredefinedIf.getRailObjBorderMask()) == ArmdPredefinedIf.getRailObjInBorderBits())
				return true;

			return false;
		}

		public bool isRunningInhibitionOn()
		{
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if ((staBits & ArmdPredefinedIf.getRailObjInhibitionMask()) == ArmdPredefinedIf.getRailObjInhibitionOnBits())
				return true;

			return false;
		}

		public OBJID getInterlocking() => m_interlockingId;

		public OBJID getCbr() => m_cbrId;

		public OBJID getZone() => m_zoneId;

		public OBJID getControlArea() => m_controlAreaId;

		public OBJID getLogicalStation() => m_logicalStationId;

		public OBJID getPlatform() => m_platformId;

		public int getETCSLevel() => m_iETCSLevel;

		public OBJID getExternalOwner() => m_externalOwnerId;

		public override OBJID getLogicalAdj(Enums.EDirection eSearchDir, OBJID previousId)
		{
			IReadOnlyList<OBJID> adjElement = ILTopoGraph.getAdjacentElements(getId(), eSearchDir);

			if (adjElement.Count == 1)
				return adjElement[0];

            List<OBJID> tmpElements = new List<OBJID>();

			foreach (OBJID elem in adjElement)
			{
				if (!ILTopoGraph.isDirectionChange(elem, getId()))
					tmpElements.Add(elem);
			}

			if (tmpElements.Count == 0)
				return 0;   // no adjacency to given direction

			return tmpElements.FirstOrDefault((OBJID)0);
		}

		public override bool hasLogicalAdj(Enums.EDirection eSearchDir, OBJID target) => ILTopoGraph.getAdjacentElements(getId(), eSearchDir).Contains(target);

		public void setInterlocking(OBJID il) => m_interlockingId = il;

		public void setCbr(OBJID cbr) => m_cbrId = cbr;

		public void setZone(OBJID zone) => m_zoneId = zone;

		public void setControlArea(OBJID controlArea) => m_controlAreaId = controlArea;

		public void setLogicalStation(OBJID logicalStation) => m_logicalStationId = logicalStation;

		public void setPlatform(OBJID platform) => m_platformId = platform;

		public void setETCSLevel(int ETCSLevel) => m_iETCSLevel = ETCSLevel;

		public void setExternalOwner(OBJID externalOwnerId) => m_externalOwnerId = externalOwnerId;

		private OBJID m_interlockingId = 0; // associated interlocking
		private OBJID m_cbrId = 0;                  // associated Cbr
		private OBJID m_controlAreaId = 0;
		private OBJID m_logicalStationId = 0;
		private OBJID m_platformId = 0;         // associated platform
		private OBJID m_zoneId = 0;
		private int m_iETCSLevel = -1;
		private OBJID m_externalOwnerId = OBJID.MaxValue;    // associated external owner ID (VPT number etc.)
	}
}
