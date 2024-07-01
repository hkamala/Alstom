using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConflictManagementLibrary.Helpers;
using Newtonsoft.Json;

namespace ConflictManagementLibrary.Model.Movement
{
    public class MovementPlan
    {
        public string Description { get; set; }
        public string LineName { get; set; }
        public string FromName { get; set; }
        public string ToName { get; set; }
        public string FromNameAlternate { get; set; }
        public string ToNameAlternate { get; set; }

        public int LocationId { get; set; }
        public List<RouteAction> MyRouteActions { get; set; } = new List<RouteAction>();
        public List<RouteAction> MyRouteActionsAlternate { get; set; } = new List<RouteAction>();

        public bool UseAlternatePathToNewPlatform { get; set; }
        public bool UseAlternatePathToReRouteToPlan { get; set; }
        private MovementPlan(string description, string lineName, string fromName, string toName)
        {
            Description = description;
            LineName = lineName;
            FromName = fromName;
            ToName = toName;
        }

        [JsonConstructor]
        public MovementPlan()
        {

        }
        public static MovementPlan CreateInstance(string description, string lineName, string fromName, string toName)
        {
            return new MovementPlan(description, lineName, fromName, toName);
        }

        public void CheckForDuplicateRouteActions()
        {
            var temp = MyRouteActions.ToArray();
            foreach (var ra in temp)
            {
                var index = 0;
                foreach (var route in MyRouteActions)
                {
                    if (route.RouteName == ra.RouteName)
                    {
                        if (index == 0)
                        {
                            index ++;
                        }
                        else
                        {
                            MyLogger?.LogCriticalError("Duplicate Route Action <" + route.RouteName +
                                                                         "> found in Movement Template <" +
                                                                         Description + ">");
                            MyRouteActions.Remove(route);
                            break;
                        }
                    }
                }
            }
        }
        public string GetMovementPlanInformation()
        {
            var mp = this;
            var s = new StringBuilder();
            s.AppendLine("\n\nMovement Plan Template");
            s.AppendLine("Description<" + mp.Description + "> From Platform<" + mp.FromName + "> To Platform<" + mp.ToName + ">");
            s.AppendLine("\tRoute Actions");

            foreach (var ra in mp.MyRouteActions)
            {
                s.AppendLine("\t\tRoute Name<" + ra.RouteName +"> Location<" + ra.ActionLocation + "> Type<" + ra.ActionType + "> Start Time<" + ra.StartTime.ToString("MM/dd/yyyy HH:mm:ss") + "> Min Start Time<" + ra.StartTimeMinimum.ToString("MM/dd/yyyy HH:mm:ss") + ">");
            }
            return s.ToString();
        }

    }
}
