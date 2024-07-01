using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Model.Trip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConflictManagementLibrary.Forms
{
    public partial class FormRoutePlan : Form
    {
        public Trip? MyTrip;

        public FormRoutePlan()
        {
            InitializeComponent();
        }
        public void AssociateTrip(Trip theTrip)
        {
            MyTrip = theTrip;
            if (MyTrip == null) return;
            this.Text = @"Trip Service # (" + MyTrip.ScheduledPlanName + @") Trip Service UID (" + MyTrip.ScheduledPlanId + @") Trip Code (" + MyTrip.TripCode + @") Trip Description (" + MyTrip.Name + @") Trip UID {" + MyTrip.TripId + @"} Trip SerUID {" + MyTrip.SerUid + "}";
            AddTripRoutePlan();
            //BuildMenu();
        }
        private void AddTripRoutePlan(bool clearForm = false)
        {
            try
            {
                if (MyTrip == null) return;
                var i = 0;

                if (clearForm)
                {
                    lvRoute.Items.Clear();
                    lvRoute.Refresh();
                }
                foreach (var tl in MyTrip.TimedLocations)
                {
                    if (tl.MyMovementPlan == null) continue;
                    foreach (var ra in tl.MyMovementPlan.MyRouteActions)
                    {
                        var shaded = Color.DarkGray;//Color.FromArgb(240, 240, 240);
                        var lvItem = new ListViewItem(ra.RouteName);
                        lvItem.SubItems.Add(tl.MyMovementPlan.FromName);
                        lvItem.SubItems.Add(tl.MyMovementPlan.ToName);
                        lvItem.SubItems.Add(ra.ActionLocation);
                        if (i++ % 2 == 1)
                        {
                            lvItem.BackColor = shaded;
                            lvItem.UseItemStyleForSubItems = true;
                        }
                        lvRoute.Items.Add(lvItem);
                    }
                }
                lvRoute.Refresh();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }

    }
}
