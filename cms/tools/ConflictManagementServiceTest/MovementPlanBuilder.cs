using ConflictManagementLibrary.Model.Movement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ConflictManagementLibrary.Helpers.GlobalDeclarations;
namespace ConflictManagementServiceTest
{
    public class MovementPlanBuilder
    {
        public List<MovementPlan> MyMovementPlans = new List<MovementPlan>();

        private IEnumerable<MovementTemplateData> MyMovementTemplates;

        public MovementPlanBuilder()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var curDir = Environment.CurrentDirectory;
                const string folderData = "Data";
                const string folderPlan = "MovementTemplate";
                var folder = Path.Combine(curDir, folderData, folderPlan);
                if (!Directory.Exists(folder))
                {
                    return;
                }

                var fileName = "MovementTemplate.json";
                var fullpath = Path.Combine(folder, fileName);
                MyMovementTemplates = DeserializeMyObjectFromFile<IEnumerable<MovementTemplateData>>(null, fullpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void BuildMovementTemplate()
        {
            try
            {
                if (MyMovementTemplates?.Count() > 0)
                {
                    var templateNames = MyMovementTemplates.Select(x => x.MovementTemplate).Distinct();
                    foreach (var name in templateNames)
                    {
                        var templates = MyMovementTemplates.Where(x => x.MovementTemplate.Trim().ToLower() == name.Trim().ToLower()).OrderBy(o => o.MovementAction_Seqno);
                        if (templates == null || !templates.Any()) continue;
                        var firstTemp = templates.FirstOrDefault();
                        var plan = MovementPlan.CreateInstance(firstTemp?.MovementTemplate, "RIGJ", firstTemp?.From, firstTemp?.To);
                        foreach (var item in templates)
                        {
                            var actiondata = item.ActionType.Split(':');
                            var actionType = actiondata.Length > 1 ? actiondata[1].Trim().ToUpper() : "ROUTE_AP_TIMING";
                            var action = RouteAction.CreateInstance(item.RouteObject, actionType, item.ActionLocation, "0:00:00", "0:00:00");
                            plan.MyRouteActions.Add(action);
                        }
                        MyMovementPlans.Add(plan);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }



            //var P2C_SARtoP2C_MAN = MovementPlan.CreateInstance("P2C_SAR to P2C_MAN", "RIGJ", "P2C_SAR", "P2C_MAN");
            //{
            //    P2C_SARtoP2C_MAN.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_SAR-SINP_SAR", "ROUTE_AP_TIMING", "P2C_SAR", "0:00:00", "0:00:00"));
            //    P2C_SARtoP2C_MAN.MyRouteActions.Add(RouteAction.CreateInstance("SIP_MAN-SIP2_MAN", "ROUTE_AP_TIMING", "TCP2P_SAR_MAN", "0:00:00", "0:00:00"));
            //}
            //MyMovementPlans.Add(P2C_SARtoP2C_MAN);


            //var P2C_MANtoP2C_ZIE = MovementPlan.CreateInstance("P2C_MAN to P2C_ZIE", "RIGJ", "P2C_MAN", "P2C_ZIE");
            //{
            //    P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_MAN-SINP_MAN", "ROUTE_AP_TIMING", "P2C_MAN", "0:00:00", "0:00:00"));
            //    P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP_ZIE-SIP2_ZIE", "ROUTE_AP_TIMING", "TCN6P_MAN_ZIE", "0:00:00", "0:00:00"));
            //}
            //MyMovementPlans.Add(P2C_MANtoP2C_ZIE);


            //var P2C_ZIEtoP3C_VEC = MovementPlan.CreateInstance("P2C_ZIE to P3C_VEC", "RIGJ", "P2C_ZIE", "P3C_VEC");
            //{
            //    P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_ZIE-SINP_ZIE", "ROUTE_AP_TIMING", "P2C_ZIE", "0:00:00", "0:00:00"));
            //    P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP_VEC-SIP3_VEC", "ROUTE_AP_TIMING", "TCN18P_ZIE_VEC", "0:00:00", "0:00:00"));
            //}
            //MyMovementPlans.Add(P2C_MANtoP2C_ZIE);

        }


        public void BuildMovementTemplate1()
        {
            var P2C_SARtoP2C_MAN = MovementPlan.CreateInstance("P2C_SAR to P2C_MAN", "RIGJ", "P2C_SAR", "P2C_MAN");
            {
                P2C_SARtoP2C_MAN.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_SAR-SINP_SAR", "ROUTE_AP_TIMING", "P2C_SAR", "0:00:00", "0:00:00"));
                P2C_SARtoP2C_MAN.MyRouteActions.Add(RouteAction.CreateInstance("SIP_MAN-SIP2_MAN", "ROUTE_AP_TIMING", "TCP2P_SAR_MAN", "0:00:00", "0:00:00"));
            }
            MyMovementPlans.Add(P2C_SARtoP2C_MAN);


            var P2C_MANtoP2C_ZIE = MovementPlan.CreateInstance("P2C_MAN to P2C_ZIE", "RIGJ", "P2C_MAN", "P2C_ZIE");
            {
                P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_MAN-SINP_MAN", "ROUTE_AP_TIMING", "P2C_MAN", "0:00:00", "0:00:00"));
                P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP_ZIE-SIP2_ZIE", "ROUTE_AP_TIMING", "TCN6P_MAN_ZIE", "0:00:00", "0:00:00"));
            }
            MyMovementPlans.Add(P2C_MANtoP2C_ZIE);


            var P2C_ZIEtoP3C_VEC = MovementPlan.CreateInstance("P2C_ZIE to P3C_VEC", "RIGJ", "P2C_ZIE", "P3C_VEC");
            {
                P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP2_ZIE-SINP_ZIE", "ROUTE_AP_TIMING", "P2C_ZIE", "0:00:00", "0:00:00"));
                P2C_MANtoP2C_ZIE.MyRouteActions.Add(RouteAction.CreateInstance("SIP_VEC-SIP3_VEC", "ROUTE_AP_TIMING", "TCN18P_ZIE_VEC", "0:00:00", "0:00:00"));
            }
            MyMovementPlans.Add(P2C_MANtoP2C_ZIE);

        }
    }
}

public class MovementTemplateData
{
    public string MovementTemplate { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public int MovementAction_Seqno { get; set; }
    public string RouteObject { get; set; }
    public string ActionType { get; set; }
    public string ActionLocation { get; set; }
}
