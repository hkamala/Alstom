using ConflictManagementLibrary.Model.Trip;
using Newtonsoft.Json;
using System;

namespace ConflictManagementLibrary.Model.Movement
{
    public class RouteAction
    {
        public string? RouteName { get; set; }
        public string ActionType { get; set; }
        public string ActionLocation { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StartTimeMinimum { get; set; }
        public string BeginLinkUid { get; set; }
        public string EndLinkUid { get; set; }
        public string MySignalName { get; set; }
        public bool HasSignalBeenCleared { get; set; }
        public bool HasConfirmationRouteClearedFromRos { get; set; }
        public bool IsOnTriggerPoint { get; set; }
        public bool HasBeenSentToRos { get; set; }
        public bool IsStationNeck { get; set; }

        [JsonIgnore] public RouteActionInfo MyRouteInfo { get; set; }
        [JsonIgnore] public bool IsTrainPastStartOfRoute;
        [JsonIgnore] public DateTime IsTimeRouteSentToRos = DateTime.Now;

        private RouteAction(string routeName, string actionType, string actionLocation, string startTime, string startTimeMinimum)
        {
            RouteName = routeName;
            ActionType = actionType;
            ActionLocation = actionLocation;
            StartTime = DateTime.Parse(startTime);
            StartTimeMinimum = DateTime.Parse(startTimeMinimum);
            GetRouteInfo();
            GetSignalName();
        }

        [JsonConstructor]
        public RouteAction()
        {
        }
        public static RouteAction CreateInstance(string routeName, string actionType, string actionLocation, string startTime, string startTimeMinimum)
        {
            return new RouteAction(routeName, actionType, actionLocation, startTime, startTimeMinimum);
        }

        private void GetSignalName()
        {
            try
            {
                var signalNames = RouteName?.Split("-");
                if (signalNames != null && signalNames.Length > 0)
                {
                    MySignalName = signalNames[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public void GetRouteInfo()
        {
            try
            {
                foreach (var routeActionInfo in MyRouteActionInfoList)
                {
                    if (routeActionInfo.MyRoute.SysName == RouteName)
                    {
                        MyRouteInfo = routeActionInfo;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
