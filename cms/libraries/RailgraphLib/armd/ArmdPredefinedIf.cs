using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.armd
{
	public class ArmdPredefinedIf
	{
		public static UInt64 getRailObjCDHFaultyMask() => Armd.getArmdObj("DynRailObjFaultyStateMask");
		public static UInt64 getRailObjCDHFaultyOnBits() => Armd.getArmdObj("DynRailObjFaultyStateOn");
		public static UInt64 getRailObjCDHFaultyOffBits() => Armd.getArmdObj("DynRailObjFaultyStateOff");

		public static UInt64 getRailObjBorderMask() => Armd.getArmdObj("StaRailObjInBorderMask");
		public static UInt64 getRailObjInBorderBits() => Armd.getArmdObj("StaRailObjInBorder");
		public static UInt64 getRailObjNotInBorderBits() => Armd.getArmdObj("StaRailObjNotInBorder");

		public static UInt64 getRailObjRAStateMask() => Armd.getArmdObj("StaRailRAStateMask");
		public static UInt64 getRailObjRAEnabledBits() => Armd.getArmdObj("StaRailRAEnabled");
		public static UInt64 getRailObjRADisabledBits() => Armd.getArmdObj("StaRailRADisabled");

		public static UInt64 getRailObjInhibitionMask() => Armd.getArmdObj("StaRailObjInhibitionMask");
		public static UInt64 getRailObjInhibitionOffBits() => Armd.getArmdObj("StaRailObjInhibitionOff");
		public static UInt64 getRailObjInhibitionOnBits() => Armd.getArmdObj("StaRailObjInhibitionOn");

		public static UInt64 getRailObjMarkedFaultyMask() => Armd.getArmdObj("StaRailObjMarkedFaultyMask");
		public static UInt64 getRailObjNotMarkedFaultyBits() => Armd.getArmdObj("StaRailObjNotMarkedFaulty");
	
		public static UInt64 getRailObjMarkedFaultyBits() => Armd.getArmdObj("StaRailObjMarkedFaulty");

		// this uses selector(static- or dynamicbits) from pretest table
		public static UInt64 getRailObjOccupationUnreliableMask(ref bool bUseStaticBits) => Armd.getArmdObj("RailObjOccupationUnreliableMask", ref bUseStaticBits);

		public static UInt64 getRailObjOccupationUnreliableBits() => Armd.getArmdObj("RailObjOccupationUnreliable");
		public static UInt64 getRailObjOccupationReliableBits() => Armd.getArmdObj("RailObjOccupationReliable");

		public static UInt64 getRailObjTdsFunctionalityMask() => Armd.getArmdObj("StaRailTdsFunctionalityMask");
		public static UInt64 getRailObjTdsEnabledBits() => Armd.getArmdObj("StaRailTdsEnabled");
		public static UInt64 getRailObjTdsDisabledBits() => Armd.getArmdObj("StaRailTdsDisabled");

		public static UInt64 getRailObjInDarkAreaMask() => Armd.getArmdObj("StaRailObjInDarkAreaMask");
		public static UInt64 getRailObjNotInDarkAreaBits() => Armd.getArmdObj("StaRailObjNotInDarkArea");
		public static UInt64 getRailObjInDarkAreaBits() => Armd.getArmdObj("StaRailObjInDarkArea");

		// Switch(point) 
		public static UInt64 getSwitchRouteMask() => Armd.getArmdObj("DynSwitchRouteMask");
		public static UInt64 getSwitchRouteUnlockedBits() => Armd.getArmdObj("DynSwitchRouteUnlocked");
		public static UInt64 getSwitchRouteLockedUpLeftBits() => Armd.getArmdObj("DynSwitchRouteLockedUpLeft");
		public static UInt64 getSwitchRouteLockedUpRightBits() => Armd.getArmdObj("DynSwitchRouteLockedUpRight");
		public static UInt64 getSwitchRouteLockedDownLeftBits() => Armd.getArmdObj("DynSwitchRouteLockedDownLeft");
		public static UInt64 getSwitchRouteLockedDownRightBits() => Armd.getArmdObj("DynSwitchRouteLockedDownRight");

		public static UInt64 getSwitchOperationMask() => Armd.getArmdObj("DynSwitchOperationMask");
		public static UInt64 getSwitchOperationUnblockedBits() => Armd.getArmdObj("DynSwitchOperationUnblocked");
		public static UInt64 getSwitchOperationBlockedBits() => Armd.getArmdObj("DynSwitchOperationBlocked");

		public static UInt64 getSwitchOccupationMask() => Armd.getArmdObj("DynSwitchOccupationMask");
		public static UInt64 getSwitchOccupationFaultyBits() => Armd.getArmdObj("DynSwitchOccupationFaulty");
		public static UInt64 getSwitchOccupationReservedBits() => Armd.getArmdObj("DynSwitchOccupationReserved");
		public static UInt64 getSwitchOccupationFreeBits() => Armd.getArmdObj("DynSwitchOccupationFree");
		public static UInt64 getSwitchOccupationUnknownBits() => Armd.getArmdObj("DynSwitchOccupationUnknown");

		public static UInt64 getSwitchCtrlModeMask() => Armd.getArmdObj("DynSwitchCtrlModeMask");
		public static UInt64 getSwitchCtrlModeCentralBits() => Armd.getArmdObj("DynSwitchCtrlModeCentral");
		public static UInt64 getSwitchCtrlModeLocalBits() => Armd.getArmdObj("DynSwitchCtrlModeLocal");

		public static UInt64 getSwitchLockedManuallyMask() => Armd.getArmdObj("DynSwitchLockedManuallyMask");
		public static UInt64 getSwitchLockedManuallyLeftBits() => Armd.getArmdObj("DynSwitchLockedManuallyLeft");
		public static UInt64 getSwitchLockedManuallyRightBits() => Armd.getArmdObj("DynSwitchLockedManuallyRight");
		public static UInt64 getSwitchNotLockedManuallyBits() => Armd.getArmdObj("DynSwitchNotLockedManually");

		public static UInt64 getSwitchDirMask() => Armd.getArmdObj("DynSwitchDirMask");
		public static UInt64 getSwitchDirUnknownBits() => Armd.getArmdObj("DynSwitchDirUnknown");
		public static UInt64 getSwitchDirLeftBits() => Armd.getArmdObj("DynSwitchDirLeft");
		public static UInt64 getSwitchDirRightBits() => Armd.getArmdObj("DynSwitchDirRight");
		public static UInt64 getSwitchDirMovingBits() => Armd.getArmdObj("DynSwitchDirMoving");

		public static UInt64 getSwitchNormalDirMask() => Armd.getArmdObj("StaSwitchNormalDirMask");
		public static UInt64 getSwitchNormalDirRightBits() => Armd.getArmdObj("StaSwitchNormalDirRight");
		public static UInt64 getSwitchNormalDirLeftBits() => Armd.getArmdObj("StaSwitchNormalDirLeft");

		public static UInt64 getSwitchRouteCancelInProgressMask() => Armd.getArmdObj("DynSwitchRouteCancelInProgressMask");
		public static UInt64 getSwitchRouteCancelInProgressBits() => Armd.getArmdObj("DynSwitchRouteCancelInProgress");
		public static UInt64 getSwitchNotRouteCancelInProgressBits() => Armd.getArmdObj("DynSwitchNotRouteCancelInProgress");

        public static UInt64 getSwitchBlockedMask() => Armd.getArmdObj("DynSwitchBlockedMask");
        public static UInt64 getSwitchBlockedBits() => Armd.getArmdObj("DynSwitchBlockedOn");
        public static UInt64 getSwitchUnblockedBits() => Armd.getArmdObj("DynSwitchBlockedOff");

        public static UInt64 getSwitchBlockedOverrideMask() => Armd.getArmdObj("DynSwitchBlockedOverrideMask");
        public static UInt64 getSwitchBlockedOverrideBits() => Armd.getArmdObj("DynSwitchBlockedOverrideOn");
        public static UInt64 getSwitchUnblockedOverrideBits() => Armd.getArmdObj("DynSwitchBlockedOverrideOff");

        // Signal
        public static UInt64 getSignalRAStateMask() => Armd.getArmdObj("DynSignalRaStateMask");
		public static UInt64 getSignalRAEnabledBits() => Armd.getArmdObj("DynSignalRaEnabled");
		public static UInt64 getSignalRADisabledBits() => Armd.getArmdObj("DynSignalRaDisabled");

		public static UInt64 getSignalAspectStateMask() => Armd.getArmdObj("DynSignalAspectStateMask");
		public static UInt64 getSignalAspectStateClearBits() => Armd.getArmdObj("DynSignalAspectStateClear");
		public static UInt64 getSignalAspectStateStopBits() => Armd.getArmdObj("DynSignalAspectStateStop");
		public static UInt64 getSignalAspectStateFaultyBits() => Armd.getArmdObj("DynSignalAspectStateFaulty");
		public static UInt64 getSignalAspectStateUnknownBits() => Armd.getArmdObj("DynSignalAspectStateUnknown");

		public static UInt64 getSignalFleetingMask() => Armd.getArmdObj("DynSignalFleetingMask");
		public static UInt64 getSignalFleetingOffBits() => Armd.getArmdObj("DynSignalFleetingOff");
		public static UInt64 getSignalFleetingOnBits() => Armd.getArmdObj("DynSignalFleetingOn");

		public static UInt64 getConditionalProceedMask() => Armd.getArmdObj("DynSignalConditionalProceedMask");
		public static UInt64 getConditionalProceedOnBits() => Armd.getArmdObj("DynSignalConditionalProceedOn");
		public static UInt64 getConditionalProceedOffBits() => Armd.getArmdObj("DynSignalConditionalProceedOff");

		public static UInt64 getSignalPriorityModeMask() => Armd.getArmdObj("DynSignalPriorityModeMask");
		public static UInt64 getSignalPriorityModeUnknown() => Armd.getArmdObj("DynSignalPriorityModeUnknown");
		public static UInt64 getSignalPriorityModeOffBits() => Armd.getArmdObj("DynSignalPriorityModeOff");
		public static UInt64 getSignalPriorityModeOnBits() => Armd.getArmdObj("DynSignalPriorityModeOn");

		// Track Section
		public static UInt64 getTSOccupationMask() => Armd.getArmdObj("DynTSOccupationMask");
		public static UInt64 getTSOccupationFaultyBits() => Armd.getArmdObj("DynTSOccupationFaulty");
		public static UInt64 getTSOccupationReservedBits() => Armd.getArmdObj("DynTSOccupationReserved");
		public static UInt64 getTSOccupationFreeBits() => Armd.getArmdObj("DynTSOccupationFree");
		public static UInt64 getTSOccupationUnknownBits() => Armd.getArmdObj("DynTSOccupationUnknown");

		public static UInt64 getTSRouteMask() => Armd.getArmdObj("DynTSRouteMask");
		public static UInt64 getTSRouteUnlockedBits() => Armd.getArmdObj("DynTSRouteUnlocked");
		public static UInt64 getTSRouteLockedNominalBits() => Armd.getArmdObj("DynTSRouteLockedNominal");
		public static UInt64 getTSRouteLockedOppositeBits() => Armd.getArmdObj("DynTSRouteLockedOpposite");

		public static UInt64 getTSTrafficDirMask() => Armd.getArmdObj("DynTSTrafficDirMask");
		public static UInt64 getTSTrafficDirReverseBits() => Armd.getArmdObj("DynTSTrafficDirReverse");
		public static UInt64 getTSTrafficDirNormalBits() => Armd.getArmdObj("DynTSTrafficDirNormal");

		public static UInt64 getTSLogicalDirMask() => Armd.getArmdObj("StaTSLogicalDirMask");
		public static UInt64 getTSLogicalDirNominalBits() => Armd.getArmdObj("StaTSLogicalDirNominal");
		public static UInt64 getTSLogicalDirOppositeBits() => Armd.getArmdObj("StaTSLogicalDirOpposite");

		public static UInt64 getTSTrafficDirLockedMask() => Armd.getArmdObj("DynTSTrafficDirLockedMask");
		public static UInt64 getTSTrafficDirUnlockedBits() => Armd.getArmdObj("DynTSTrafficDirUnlocked");
		public static UInt64 getTSTrafficDirLockedBits() => Armd.getArmdObj("DynTSTrafficDirLocked");

		public static UInt64 getTSTrafficDriveDirMask() => Armd.getArmdObj("DynTSTrafficDriveDirMask");
		public static UInt64 getTSTrafficDriveDirUndefinedBits() => Armd.getArmdObj("DynTSTrafficDriveDirUndefined");
		public static UInt64 getTSTrafficDriveDirNormalBits() => Armd.getArmdObj("DynTSTrafficDriveDirNormal");
		public static UInt64 getTSTrafficDriveDirOppositeBits() => Armd.getArmdObj("DynTSTrafficDriveDirOpposite");
		public static UInt64 getTSTrafficDriveDirBlockedBits() => Armd.getArmdObj("DynTSTrafficDriveDirBlocked");

		public static UInt64 getTSRouteCancelInProgressMask() => Armd.getArmdObj("DynTSRouteCancelInProgressMask");
		public static UInt64 getTSRouteCancelInProgressBits() => Armd.getArmdObj("DynTSRouteCancelInProgress");
		public static UInt64 getTSNotRouteCancelInProgressBits() => Armd.getArmdObj("DynTSNotRouteCancelInProgress");

		public static UInt64 getTSBlockedMask() => Armd.getArmdObj("DynTSBlockedMask");
        public static UInt64 getTSBlockedBits() => Armd.getArmdObj("DynTSBlockedOn");
        public static UInt64 getTSUnblockedBits() => Armd.getArmdObj("DynTSBlockedOff");

        public static UInt64 getTSBlockedOverrideMask() => Armd.getArmdObj("DynTsBlockedOverrideMask");
        public static UInt64 getTSBlockedOverrideBits() => Armd.getArmdObj("DynTsBlockedOverrideOn");
        public static UInt64 getTSUnblockedOverrideBits() => Armd.getArmdObj("DynTsBlockedOverrideOff");

        // Crossing
        public static UInt64 getCrossingRouteMask() => Armd.getArmdObj("DynCrossingRouteMask");
		public static UInt64 getCrossingRouteUnlockedBits() => Armd.getArmdObj("DynCrossingRouteUnlocked");
		public static UInt64 getCrossingRouteReservedBits() => Armd.getArmdObj("DynCrossingRouteReserved");
		public static UInt64 getCrossingRouteCancelledBits() => Armd.getArmdObj("DynCrossingRouteCancelled");
		public static UInt64 getCrossingRouteUnknownBits() => Armd.getArmdObj("DynCrossingRouteUnknown");

		public static UInt64 getCrossingPositionMask() => Armd.getArmdObj("DynCrossingPositionMask");
		public static UInt64 getCrossingPosition1UnknownBits() => Armd.getArmdObj("DynCrossingPositionUnknown_1");
		public static UInt64 getCrossingPosition01Bits() => Armd.getArmdObj("DynCrossingPosition_01");
		public static UInt64 getCrossingPosition02Bits() => Armd.getArmdObj("DynCrossingPosition_02");
		public static UInt64 getCrossingPosition2UnknownBits() => Armd.getArmdObj("DynCrossingPositionUnknown_2");

		public static UInt64 getCrossingOccupationMask() => Armd.getArmdObj("DynCrossingOccupationMask");
		public static UInt64 getCrossingOccupationFaultyBits() => Armd.getArmdObj("DynCrossingOccupationFaulty");
		public static UInt64 getCrossingOccupationReservedBits() => Armd.getArmdObj("DynCrossingOccupationReserved");
		public static UInt64 getCrossingOccupationFreeBits() => Armd.getArmdObj("DynCrossingOccupationFree");
		public static UInt64 getCrossingOccupationUnknownBits() => Armd.getArmdObj("DynCrossingOccupationUnknown");

		// Line Block
		public static UInt64 getLineRouteDisabledMask() => Armd.getArmdObj("DynLineRouteDisabledMask");

		// Zone
		public static UInt64 getZoneOwnerMask() => Armd.getArmdObj("DynZoneOwnerMask");
		public static UInt64 getZoneOwnerNoneBits() => Armd.getArmdObj("DynZoneOwnerNone");
		public static UInt64 getZoneOwnerVptBits() => Armd.getArmdObj("DynZoneOwnerVpt");
		public static UInt64 getZoneOwnerAreaSupervisorBits() => Armd.getArmdObj("DynZoneOwnerAreaSupervisor");

		public static UInt64 getZoneStateMask() => Armd.getArmdObj("DynZoneStateMask");
		public static UInt64 getZoneStateDefaultBits() => Armd.getArmdObj("DynZoneStateDefault");
		public static UInt64 getZoneStateGivenBits() => Armd.getArmdObj("DynZoneStateGiven");
		public static UInt64 getZoneStateTakenBits() => Armd.getArmdObj("DynZoneStateTaken");
		public static UInt64 getZoneStateReturnedBits() => Armd.getArmdObj("DynZoneStateReturned");

		//control center aas
		public static UInt64 getControlCenterMask() => Armd.getArmdObj("DynControlCenterMask");
		public static UInt64 getControlCenterBackupBits() => Armd.getArmdObj("DynControlCenterBackup");
		public static UInt64 getControlCenterCentralBits() => Armd.getArmdObj("DynControlCenterCentral");
		public static UInt64 getControlCenterLocalBits() => Armd.getArmdObj("DynControlCenterLocal");
		public static UInt64 getControlCenterNoneBits() => Armd.getArmdObj("DynControlCenterNone");
		public static UInt64 getControlCenterUnknownBits() => Armd.getArmdObj("DynControlCenterUnknown");

		//Platform
		public static UInt64 getPlatformMask() => Armd.getArmdObj("DynPlatformMask");
		public static UInt64 getPlatformSkipOffBits() => Armd.getArmdObj("DynPlatformSkipOff");
		public static UInt64 getPlatformSkipOnBits() => Armd.getArmdObj("DynPlatformSkipOn");

		public static UInt64 getPlatformHoldMask() => Armd.getArmdObj("DynPlatformHoldMask");
		public static UInt64 getPlatformHoldOffBits() => Armd.getArmdObj("DynPlatformHoldOff");
		public static UInt64 getPlatformHoldOnBits() => Armd.getArmdObj("DynPlatformHoldOn");

		public static UInt64 getPlatformServiceMask() => Armd.getArmdObj("DynPlatformServiceMask");
		public static UInt64 getPlatformNotInServiceBits() => Armd.getArmdObj("DynPlatformNotInService");
		public static UInt64 getPlatformInServiceBits() => Armd.getArmdObj("DynPlatformInService");

		// RA control area64
		public static UInt64 getControlAreaRAMask() => Armd.getArmdObj("DynCtrlAreaRAMask");
		public static UInt64 getControlAreaRAEnableBits() => Armd.getArmdObj("DynCtrlAreaRAEnabled");
		public static UInt64 getControlAreaRADisableBits() => Armd.getArmdObj("DynCtrlAreaRADisabled");

		public static UInt64 getRAFifoMask() => Armd.getArmdObj("DynRAFifoMask");
		public static UInt64 getRAFifoEnableBits() => Armd.getArmdObj("DynRAFifoEnabled");
		public static UInt64 getRAFifoDisableBits() => Armd.getArmdObj("DynRAFifoDisabled");

		public static UInt64 getNomPropagationStopMarkerMask() => Armd.getArmdObj("StaNomPropagationStopMarkerMask");
		public static UInt64 getNomPropagationOnStopMarker() => Armd.getArmdObj("StaNomPropagationOnStopMarker");
		public static UInt64 getNomPropagationOffStopMarker() => Armd.getArmdObj("StaNomPropagationOffStopMarker");

		public static UInt64 getOppPropagationStopMarkerMask() => Armd.getArmdObj("StaOppPropagationStopMarkerMask");
		public static UInt64 getOppPropagationOnStopMarker() => Armd.getArmdObj("StaOppPropagationOnStopMarker");
		public static UInt64 getOppPropagationOffStopMarker() => Armd.getArmdObj("StaOppPropagationOffStopMarker");

		public static UInt64 getRouteQueuingMask() => Armd.getArmdObj("StaRouteQueueingDisabledMask");
		public static UInt64 getRouteQueuingEnabledBits() => Armd.getArmdObj("StaRouteQueueingEnabled");
		public static UInt64 getRouteQueuingDisabledBits() => Armd.getArmdObj("StaRouteQueueingDisabled");

		public static UInt64 getPlatformTypeMask() => Armd.getArmdObj("StaPlatformTypeMask");
		public static UInt64 getPlatformTypeNormal() => Armd.getArmdObj("StaPlatformTypeNormal");
		public static UInt64 getPlatformTypePseudo() => Armd.getArmdObj("StaPlatformTypePseudo");

		public static UInt64 getErrorValue() => Armd.getArmdObj("ARMDInitErrorValue");
		public static bool isARMDValueInUse(UInt64 value) => value != Armd.getArmdObj("ARMDValueNotInUse");
	}
}
