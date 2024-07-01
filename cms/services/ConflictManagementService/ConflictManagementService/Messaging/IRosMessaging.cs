using ConflictManagementService.Model;
using System;
using System.Collections.Generic;

namespace E2KService.MessageHandler
{
    internal interface IRosMessaging
    {
        internal class PretestResult
        {
            internal int PretestId;
            internal bool Success;
            internal List<Tuple<string /*object*/, RosRfnaCode, string /*severity*/ >> RejectInfo = new();

			public PretestResult(int pretestId, XSD.PretestResponse.rcsMsg result)
			{
                // Build result from response received from ROS
                PretestId = pretestId;
                Success = result.data.PretestResponse.Success;

                foreach (var rejectReason in result.data.PretestResponse.RejectReasons)
                {
                    RosRfnaCode rfna = RosRfnaCode.rRosRfnaUnknown;
                    string obj = "";
                    string severity = "";

                    try
                    {
                        obj = rejectReason.Obj;
                        severity = rejectReason.Severity;
                        rfna = (RosRfnaCode)rejectReason.RFNA;
                    }
                    catch
                    {
                        rfna = RosRfnaCode.rRosRfnaUnknown;
                    }

                    RejectInfo.Add(new(obj, rfna, severity));
                }
            }
		}

		delegate void DelegatePretestResult(int pretestId, bool success, PretestResult? result);

        abstract void PretestRouteAvailable(int pretestId, Train train, RailgraphLib.HierarchyObjects.Route route, string command, DelegatePretestResult result);
        abstract void PretestRouteReachable(int pretestId, Train train, RailgraphLib.HierarchyObjects.Route route, string command, DelegatePretestResult result);
        abstract void PretestSingleObject(int pretestId, RailgraphLib.Interlocking.ILGraphObj element, string command, DelegatePretestResult result);
		abstract void SendRoutePlan(RoutePlan routePlan, string tripId = "");
		abstract void SendCancelRoutePlan(XSD.CancelRoutePlan.rcsMsg cancelRoutePlan);
        abstract void SendScheduledRoutePlan(ScheduledRoutePlan scheduledRoutePlan);
        abstract void SendScheduledRoutePlanRequest(ScheduledPlan scheduledPlan);
    }
}