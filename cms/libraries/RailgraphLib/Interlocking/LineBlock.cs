using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RailgraphLib.armd;

namespace RailgraphLib.Interlocking
{
	using OBJID = UInt32;
	using BINTYPE = UInt64;
	using OBJTYPE = UInt16;

	public class LineBlock : ILGraphObj
	{
		public LineBlock(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eDir) : base(objId, objType, classType, objName) 
		{
			m_eDirection = eDir;
		}

		public virtual BINTYPE getRouteDisabling()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getLineRouteDisabledMask()))
			{
				return 0;
			}

			return dynBits & ArmdPredefinedIf.getLineRouteDisabledMask();
		}

		public virtual bool isValidDirection(Enums.EDirection eDir) => m_eDirection == eDir;
		
		public virtual bool isDriveDirValid(Enums.EDirection eAspectDir)
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE DriveDir = (dynBits & ArmdPredefinedIf.getTSTrafficDriveDirMask());

			// track (right or left)
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			BINTYPE Track = (staBits & ArmdPredefinedIf.getTSLogicalDirMask());

			if (Track == ArmdPredefinedIf.getTSLogicalDirNominalBits()) // right track
			{
				if (((DriveDir == ArmdPredefinedIf.getTSTrafficDriveDirNormalBits()) && (eAspectDir == Enums.EDirection.dNominal)) ||
						 ((DriveDir == ArmdPredefinedIf.getTSTrafficDriveDirOppositeBits()) && (eAspectDir == Enums.EDirection.dOpposite)))
				{
					return true;
				}
			}
			else if (Track == ArmdPredefinedIf.getTSLogicalDirOppositeBits()) // left track
			{
				if (((DriveDir == ArmdPredefinedIf.getTSTrafficDriveDirNormalBits()) && (eAspectDir == Enums.EDirection.dOpposite)) ||
						 ((DriveDir == ArmdPredefinedIf.getTSTrafficDriveDirOppositeBits()) && (eAspectDir == Enums.EDirection.dNominal)))
				{
					return true;
				}
			}

			return false;
		}

		private Enums.EDirection m_eDirection;
	}
}
