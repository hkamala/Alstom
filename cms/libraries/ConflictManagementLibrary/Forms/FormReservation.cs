using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Model.Trip;


namespace ConflictManagementLibrary.Forms
{
    public partial class FormReservation : Form
    {
        public Trip? MyTrip;
        public FormReservation()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(CurrentUiCulture);
            InitializeComponent();
        }
        private void BuildMenu()
        {
            try
            {
                if (MyTrip != null )
                {
                    var i = 0;
                    if (MyTrip.TimedLocations.Count == 0) return;
                    var items = new ToolStripMenuItem[MyTrip.TimedLocations.Count - 1];
                    foreach (var tl in MyTrip.TimedLocations)
                    {
                        if (tl == MyTrip.TimedLocations[MyTrip.TimedLocations.Count-1]) break;
                        var name = tl.MyMovementPlan.FromName + " to " + tl.MyMovementPlan.ToName;
                        items[i] = new ToolStripMenuItem();
                        items[i].Name = "dynamicItem" + i.ToString();
                        items[i].Tag = tl.SystemGuid;
                        items[i].Text = name;
                        items[i].Click += new EventHandler(MenuItemClickHandler!);

                        i++;
                    }
                    mnuSendRoute.DropDownItems.AddRange(items);
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        private void MenuItemClickHandler(object sender, EventArgs e)
        {
            try
            {
                var clickedItem = (ToolStripMenuItem)sender;
                var platformPath = clickedItem.Text;
                var tripUid = MyTrip!.TripId.ToString();
                var startTime = MyTrip.StartTime;
                var timedLocationGuid = clickedItem.Tag.ToString();
                var platforms = platformPath.Split(" to ");
                var fromLocation = platforms[0];
                var toPlatform = platforms[1];
                var messageToSend = ConflictManagementLibrary.Messages.ConflictManagementMessages.SendRoutePlanRequest.CreateInstance(platformPath, tripUid, startTime, timedLocationGuid, fromLocation, toPlatform);
                if (MyTrainSchedulerManager != null) MyTrainSchedulerManager!.ProduceMessage1200(messageToSend);
            }
            catch (Exception ex)
            {
                GlobalDeclarations.MyLogger?.LogException(ex.ToString());
            }
        }
        public void AssociateTrip(Trip theTrip)
        {
            MyTrip = theTrip;
            if (MyTrip == null) return;
            this.Text = @"Trip Service # (" + MyTrip.ScheduledPlanName + @") Trip Service UID (" + MyTrip.ScheduledPlanId + @") Trip Code (" + MyTrip.TripCode + @") Trip Description (" + MyTrip.Name + @") Trip UID {" + MyTrip.TripId + @"} Trip SerUID {" + MyTrip.SerUid + "}";
            AddTripReservations();
            BuildMenu();
        }

        public void UpdateTrip(Trip theTrip)
        {
            if (theTrip.TripCode == MyTrip.TripCode && theTrip.StartTime == MyTrip.StartTime)
            {
                MyTrip = theTrip;
                AddTripReservations(true);
            }
        }
        private void AddTripReservations(bool clearForm = false)
        {
            try
            {
                if (MyTrip == null) return;
                var i = 0;

                if (clearForm)
                {
                    lvReservations.Items.Clear();
                    lvReservations.Refresh();
                }
                foreach (var r in MyTrip.MyReservations)
                {
                    var shaded = Color.DarkGray;//Color.FromArgb(240, 240, 240);
                    var lvItem = new ListViewItem(r.MyStationName);
                    lvItem.SubItems.Add(r.MyNodeNumber);
                    lvItem.SubItems.Add(r.MyLinkReferenceUid);
                    lvItem.SubItems.Add(r.MyEdgeName + " (" + r.MyEdgeUid + ")");
                    lvItem.SubItems.Add(r.MyTimedLocation?.MyMovementPlan.Description);
                    lvItem.SubItems.Add(r.MyTimedLocation?.Description);
                    lvItem.SubItems.Add(r.MyNextTimedLocation?.Description);
                    var begin = r.TimeBegin?.ToString("dd-MM-yy HH:mm:ss");
                    var end = r.TimeEnd?.ToString("dd-MM-yy HH:mm:ss");
                    lvItem.SubItems.Add(begin);
                    lvItem.SubItems.Add(end);
                    var totalReservationTime = r.TotalReservationTime?.TotalSeconds.ToString("000#") + "/" + r.TotalReservationTime?.TotalMinutes.ToString("00.00");
                    lvItem.SubItems.Add(totalReservationTime);
                    lvItem.SubItems.Add(GetDwellTime(r.MyNextTimedLocation));
                    if (r.HasBeenReleased) lvItem.Font = new Font(lvReservations.Font, FontStyle.Bold);
                    if (r.MyEdgeName == "E_PT1_PT5_5S_CAR") lvItem.Font = new Font(lvReservations.Font, FontStyle.Bold);
                    if (i++ % 2 == 1)
                    {
                        lvItem.BackColor = shaded;
                        lvItem.UseItemStyleForSubItems = true;
                    }
                    lvReservations.Items.Add(lvItem);
                    lvItem.ToolTipText = GetToolTip(r.MyTimedLocation);
                    lvReservations.Refresh();

                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        private string GetToolTip(TimedLocation? thePlatform)
        {
            var toolTip = new StringBuilder("None");
            try
            {
                if (thePlatform is { MyMovementPlan: { MyRouteActions: { } } })
                {
                    toolTip.Clear();
                    foreach (var ra in thePlatform.MyMovementPlan.MyRouteActions)
                    {
                        toolTip.AppendLine(ra.RouteName);
                    }

                    return toolTip.ToString();
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }

            return toolTip.ToString();
        }
        private string GetDwellTime(TimedLocation? thePlatform)
        {
            var dwellTime = "None";
            try
            {
                if (thePlatform != null)
                {
                    var ts = thePlatform.DepartureTimeActual - thePlatform.ArrivalTimeActual;
                    dwellTime = thePlatform.Description + " (" + ts.TotalSeconds.ToString("000#") + "/" + ts.TotalMinutes.ToString("00.00") +")";
                    return dwellTime;
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

            return dwellTime;
        }
        private void mnuSendRoute_Click(object sender, EventArgs e)
        {

        }
    }
}
