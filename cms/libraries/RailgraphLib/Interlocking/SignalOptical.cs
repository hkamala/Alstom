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

	public class SignalOptical : ILGraphObj 
	{
		public SignalOptical(OBJID objId, OBJTYPE objType, Enums.CLASS_TYPE classType, string objName, Enums.EDirection eAspectDir) : base(objId, objType, classType, objName)
		{
			m_eAspectDir = eAspectDir;
		}

		public Enums.EDirection getAspectDir() => m_eAspectDir;

		public bool isAspectDirValid(Enums.EDirection aspectDir) => getAspectDir() == aspectDir;

		public EAspect getAspectState()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE SignalState = (dynBits & ArmdPredefinedIf.getSignalAspectStateMask());

			if (SignalState == ArmdPredefinedIf.getSignalAspectStateFaultyBits())
				return EAspect.aspectFaulty;
			else if (SignalState == ArmdPredefinedIf.getSignalAspectStateStopBits())
				return EAspect.aspectStop;
			else if (SignalState == ArmdPredefinedIf.getSignalAspectStateClearBits())
				return EAspect.aspectClear;
			else
				return EAspect.aspectUnknown;
		}

		public virtual ESignalPriorityMode getPriorityMode()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE priorityState = (dynBits & ArmdPredefinedIf.getSignalPriorityModeMask());

			if (priorityState == ArmdPredefinedIf.getSignalPriorityModeUnknown())
				return ESignalPriorityMode.priorityUnknown;
			else if (priorityState == ArmdPredefinedIf.getSignalPriorityModeOffBits())
				return ESignalPriorityMode.priorityOff;
			else if (priorityState == ArmdPredefinedIf.getSignalPriorityModeOnBits())
				return ESignalPriorityMode.priorityOn;
			else
				return ESignalPriorityMode.priorityUnknown;
		}

		public virtual bool isConditionalProceed()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getConditionalProceedMask()))
				return false;

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE proceedState = (dynBits & ArmdPredefinedIf.getConditionalProceedMask());
			return (proceedState == ArmdPredefinedIf.getConditionalProceedOnBits());
		}
	
		public virtual bool isFleetingOn()
		{
			if (!ArmdPredefinedIf.isARMDValueInUse(ArmdPredefinedIf.getSignalFleetingMask()))
			{
				return false;
			}

			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			if ((dynBits & ArmdPredefinedIf.getSignalFleetingMask()) ==
					 ArmdPredefinedIf.getSignalFleetingOnBits())
			{
				return true;
			}
			return true;
		}
	
		public virtual bool isRoutingEnabled()
		{
			BINTYPE dynBits = ILGraph.getDynamicBits(getId());
			BINTYPE staBits = ILGraph.getStaticBits(getId());
			if (((dynBits & ArmdPredefinedIf.getSignalRAStateMask()) == ArmdPredefinedIf.getSignalRAEnabledBits()) &&
					 ((staBits & ArmdPredefinedIf.getRailObjRAStateMask()) == ArmdPredefinedIf.getRailObjRAEnabledBits()))
			{
				return true;
			}

			return false;
		}

		private Enums.EDirection m_eAspectDir;
};
}
