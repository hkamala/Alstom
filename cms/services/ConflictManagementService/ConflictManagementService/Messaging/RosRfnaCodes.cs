namespace E2KService.MessageHandler
{
	// Rfna codes most probably (but not necessarily) returned by ROS, new Rfna codes can be defined in CTC DB...
	enum RosRfnaCode
	{
		// Commom responses 
		rGenInvalidCmd = 0,                         ///< Invalid command
		rGenUnknownTrain,                           ///< Train <Train Label> not known
		rGenInvalidParam,                           ///< Invalid combination of parameters (also as 'can not be realised' response)

		// Ok response
		rOk,                                        /// <3>

		// Operate infra element responses, common part
		rObjUnknownId,                              ///< Infrastructure element <ElementId> identification not known
		rObjUnknownPos,                             ///< Desired position not known for element <ElementId>

		// Route (Set, Prepare, Cancel, Block, Unblock) responses
		rRouteInUse,                                ///< Infrasegment <InfraSegment> is already in use for another conflicting route
		rRouteDefective,                            ///< Infrasegment <InfraSegment> is defective 
		rRouteOutOfGauge,                           ///< Out-of-gauge
		rRouteObjBlocked,                           ///< Element <ElementId> is blocked from operation and in wrong position for this route
		rRouteUnknown,                              ///< Infrasegment <InfraSegment> not known
		rRouteOccupied,                             ///< Normal route not allowed because infrasegment <InfraSegment> is occupied
		rRouteForced,                               ///< Route forced 
		rRouteIntegralRestriction,                  ///< Integral restriction 

		//  Cancel route
		rRouteNoTrainRoute,                         ///<14 No route identified for train <Trainlabel>

		rRouteCmdTimeout,                           ///<58 Not acknowledgement received from Cbi within specified time

		// negative responses from chd, 90..98 defined in tables CMDRESP and PRETESTS
		rNoCbiConnection = 90,                      ///< Connection broken to CBI
		rCbiCmdBlockingPeriod,                      ///< Command blocking period in progress on CBI after restart
		rCbiRestartNotAcked,                        ///< Restart of CBI not acked
		rCommandBlocked,                            ///< Command inhibition set for object
		rCmdCreationFailed,                         ///< Creation of command failed (DB fault)
		rCmdPending,                                ///< Command already pending
		rCmdPretestBlocking,                        ///< Blocked by reminder (not used in NS)
		rCmdPretestFailed,                          ///< Pretest inhibits the command 
		rCmdNotReadyforVerification,                ///< Critical command not ready for verif. command

		// Rfnas defined in Pretests table
		rObjBlocked = 100,                          ///< Infrastructure element (point) <ElementId> is blocked from operation
		rObjOutOfTrafficArea,                       ///< Infrastructure element (point) <ElementId> is not part of train traffic area
		rObjLocked,                                 ///< Infrastructure element (point) <ElementId> is locked
		rObjCranked,                                ///< Infrastructure element (point) <ElementId> is being cranked
		rPreDirControlNotInTta,                     ///< direction control not part of train traffic area
		rPreDirControlLocked,                       ///< direction control locked
		rObjTrackOccupied,                          ///< direction control open track is occupied: no changing allowed
		rPrePointBlkNotInTta,                       ///< point blocking, not part of train traffic area
		rPrePointUnblkNotInTta,                     ///< point unblocking, not part of train traffic area
		rPreUnused,                                 ///< not used yet (109)
		rPreObjectInvalid,                          ///< object's status is invalid, cmd rejected
		rPreTrackCircuitFailure,                    ///< TC failure, route Blocking/unblocking rejected ?????
		rRouteBlocked,                              ///< TRS: Route <InfraSegment> is (partially) blocked from set route
		rRouteOutOfTrafficArea,                     ///< TRS: Infrasegment <InfraSegment> not part of train traffic area
		rRouteIsSet,                                ///< block from set route: Route <Infrasegment> is set
		rRouteDirectionForbidden,                   ///< Direction of traffic currently not allowed
		rPointReservedforRoute,                     ///< Point reserved for route (no operate point cmd allowed)
		rPointRunthrough,                           ///< Point is run trough (no operate point cmd allowed)
		rPointOccupied,                             ///< Point is occupied, (no operate point cmd allowed)
		rGAInfrasegmReservedforRoute,               ///< track reserved for route, Give Area cmd inhibited
		rGAInfraelemReservedforRoute,               ///< (120) point reserved for route, Give Area cmd inhibited
		rBlSRInfraelemResforRoute,                  ///< reserved for route, Block from set route cmd inhibited
		rDemandPointReservedInWrongPos,             ///< Demand point is reserved in a conflicting position
		rDemandPointLockedInWrongPos,               ///< Demand point is locked in a conflicting position
		rDemandPointOccupiedInWrongPos,             ///< Demand point is occupied in a conflicting position
		rDemandPointBlockedOperInWrongPos,          ///< Demand point is blocked from operation in a conflicting position
		rDemandPointDefective,                      ///< Demand point is defective
		rDetectorAlarming,                          ///< Detector alarming
		rRouteReserved,                             ///< Infrasegment <InfraSegment> reserved for route

		rRosTrainPassedRedSignal = 1000,            ///< 1000 train passed a signal with stop aspect
		rRosRoutePretestTimeout,                    ///< 1001 route request too old 
		rRosDelayWarningTimeout,                    ///< 1002 delayed too long
		rRosWhiteTrainDetected,                     ///< 1003 white train detected
		rRosFlankPointReservedInWrongPos,           ///< 1004 Demand point is reserved in a conflicting position
		rRosPointLockedInUnknownPos,                ///< 1005 point locked in unknown position
		rRosRouteReserved,                          ///< 1006 reserved for another route
		rRosRouteIsSet,                             ///< 1007 route already set
		rRosRouteTryMax,                            ///< 1008 maximum number of sending attempts
		rRosPathNotExist,                           ///< 1009 unable to find path from begin to end of route
		rRosPointSwitchingTimeout,                  ///< 1010 timeout for points to move
		rRosLockedPointNotInOverlapDir,             ///< 1011 locked point not in required overlap direction
		rRosCommandQueued,                          ///< 1012 route setting command is queued
		rRosPointLockedAgainstRoute,                ///< 1013 point locked against route
		rRosValidityTimeHasExpired,                 ///< 1014 started journey expires
		rRosRouteQueuingDisabled,                   ///< 1015 route queuing disabled
		rRosFlankPointLockedInWrongPos,             ///< 1016 Demand point is locked in a conflicting position
		rRosRouteOccupied,                          ///< 1017 route is occupied
		rRosRouteDefective,                         ///< 1018 route is defective 
		rRosSignalIsRed,                            ///< 1019 signal is red 
		rRosRaDisabled,                             ///< 1020 automatic routing disabled
		rRosPathNotExistForReachableSingleObj,      ///< 1021 unable to find path from train location to single object
		rRosPathNotExistForReachableRoute,          ///< 1022 unable to find path from train location to begin of route
		rRosTrainPassedBeginOfRoute,                ///< 1023 train already passed begin of route
		rRosRouteIsNotSet,                          ///< 1024 route is not set (recovery route)
		rRosRatoAckTmo,                             ///< 1025 Not acknowledgement received from Ratos within 5 seconds

		// RA Server/ROS
		rRaNoActionTime = 1380,                     ///< 1380 no planned or regulated time
		rRfnaNotDefined = 1381,                     ///< !!! AHä 2.9.2010 Bug 236:  This is a fixed value. Do not change!!!
		rRosTmsWaitingTrainToAP = 1400,             ///< 1400 waiting for train to arrive action point
		rRosTmsWaitingExecutionTime = 1401,         ///< 1401 waiting for execution time
		rRosTmsCmdReceived = 1402,                  ///< 1402 command received
		rRosTmsWaitingAlternativeRoute = 1403,      ///< 1403 waiting for alternative route execution
		rRosTmsCmdCompleted = 1404,                 ///< 1404 command completed 
		rRosTmsCmdInvalidParam = 1405,              ///< 1405 command contains invalid parameter
		rRosTmsWaitingCmdToSet = 1406,              ///< 1406 waiting for command to set
		rRosTmsCmdCancelled = 1407,                 ///< 1407 train commands cancelled
		rRosTmsTargetAlreadyActivated = 1408,       ///< 1408 target (route, single obj, automaton) is already activated from another action point
		rRosTmsCmdDelayed = 1409,                   ///< 1409 command delayed
		rRosTmsCmdIgnored = 1410,                   ///< 1410 command ignored
		rRosPointInWrongPosLeft = 1411,             ///< 1411 point is in conflicting position in left direction
		rRosPointInWrongPosRight = 1412,            ///< 1412 point is in conflicting position in right direction
		rRosTmsTrainPropertyPretestError = 1413,    ///< 1413 TMS train property conflicts with CTC train property
		rRosMultiplePathExist = 1414,               ///< 1414 multiple paths exist between begin and end of route
		rRosRouteMaxCountForLongRoute = 1415,       ///< 1415 maximum number of basic routes for long route
		rBeginOfRouteDisabledForRA = 10067,

		// ConflictManagementService uses this when Rfna is not in the list above
		rRosRfnaUnknown = -1
	}
}
