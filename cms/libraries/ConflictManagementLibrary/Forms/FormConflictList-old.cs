using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Management;
using ConflictManagementLibrary.Messages;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;

namespace ConflictManagementLibrary.Forms

{
    public partial class FormConflictList : Form
    {
        #region Variables
        private const int CP_NOCLOSE_BUTTON = 0x200;
        public ObservableCollection<Trip> MyTrips = new ObservableCollection<Trip>();
        public bool blockThread = false;
        #endregion

        #region Delegates
        public delegate void ConflictUpdateDelegate(Trip? theTrip);
        public ConflictUpdateDelegate? PerformConflictUpdate;
        public delegate void EventDelegate();

        #endregion

        #region Initialization
        public FormConflictList()
        {
            //CurrentUiCulture = CultureInfo.CurrentCulture.Name;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(CurrentUiCulture);
            InitializeComponent();
        }
        public void LinkDelegate(ConflictUpdateDelegate? processConflictUpdate)
        {
            this.PerformConflictUpdate = processConflictUpdate ?? throw new ArgumentNullException(nameof(processConflictUpdate));
        }
        private void FormConflictList_Load(object sender, EventArgs e)
        {
            InitializeForm();
        }
        private void InitializeForm()
        {
            //this.Size = new Size(1180, 465);
            colTrainService.Width = 100;
            colDirection.Width = 100;
            this.colTrainIdentifier.Width = 100;
            this.colTrainType.Width = 140;
            this.colStartTime.Width = 140;
            colTrainPostfix.Width = 100;
            //this.colTrainSubtype.Width = 100;
            this.colTrainLength.Width = 70;
            this.colLocationCurrent.Width = 130;
            this.coLocationNext.Width = 120;
            this.colNumOfConflicts.Width = 100;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            MyTrainSchedulerManager?.LinkDelegate(DoProcessTrip!, DoProcessStatus!, DoProcessEvent!, DoProcessForecast!);
            MyTrainSchedulerManager?.ProduceMessage1000();

        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }
        #endregion

        #region Event Log Processsing
        private void DoProcessEvent(ConflictManagementMessages.CmsEventMessage theMessage)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { ProcessEvent(theMessage); }));
                }

            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void ProcessEvent(ConflictManagementMessages.CmsEventMessage theMessage)
        {
            try
            {
                var message = theMessage.MyEventMessage?.MessageOfEvent;
                var timeStamp = theMessage.MyEventMessage?.EventTimeStamp;
                var eventMessage = timeStamp + "\t" + message + '\n';
                txtEvent.SelectionColor = ColorForLine(theMessage.MyEventMessage?.EventLevel);
                txtEvent.AppendText(eventMessage);
                txtEvent.ScrollToCaret();
                MyLogger?.LogInfo(eventMessage);
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }

        }
        private Color ColorForLine(string? line)
        {
            if (line != null)
            {

                if (line.Contains("FATAL"))
                {
                    return Color.Red;
                }
                else if (line.Contains("WARN"))
                {
                    return Color.DarkOrange;
                }
                else if (line.Contains("DEBUG"))
                {
                    return Color.Blue;
                }
                else if (line.Contains("ERROR"))
                {
                    return Color.Coral;
                }
                else
                {
                    return Color.Black;
                }
            }
            return Color.Black;
        }

        #endregion

        #region Trip Processing
        private void DoProcessStatus(ConflictManagementMessages.ConflictResolutionStatus theStatus)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { ProcessStatus(theStatus); }));
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void ProcessStatus(ConflictManagementMessages.ConflictResolutionStatus theStatus)
        {
            try
            {
                if (theStatus.ConflictResolutionEnabled)
                {
                    tssConflictResolutionStatus.ForeColor = Color.Green;
                    tssConflictResolutionStatus.Text = @"Automatic Conflict Resolution Status Is Enabled";
                }
                else
                {
                    tssConflictResolutionStatus.ForeColor = Color.Red;
                    tssConflictResolutionStatus.Text = @"Automatic Conflict Resolution Status Is Disabled";
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void DoProcessTrip(Trip theTrip, string theCommand)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { ProcessTrip(theTrip, theCommand); }));
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void ProcessTrip(Trip theTrip, string theCommand)
        {
            var tripExist = DoesTripExist(theTrip);
            if (tripExist == null)
            {
                AddTrip(theTrip);
                lock (MyTrips)
                {
                    MyTrips.Add(theTrip);
                }
                return;
            }

            switch (theCommand)
            {
                case "UPDATE":
                case "ALLOCATE":
                    {
                        ReplaceTrip(tripExist, theTrip);
                        PerformConflictUpdate?.Invoke(theTrip);
                        break;
                    }
                case "DELETE":
                    {
                        DeleteTrip(theTrip);
                        break;
                    }
                default:
                    {
                        ReplaceTrip(tripExist, theTrip);
                        break;
                    }
            }
        }
        private void DeleteTrip(Trip theTrip)
        {
            lock (MyTrips)
            {
                var index = FindTripIndex(theTrip);
                MyTrips.RemoveAt(index);
            }
            //TODO - need to redo this logic and only update the trip and not all trips
            RefreshListView();
        }
        private void ReplaceTrip(Trip theOldTrip, Trip theNewTrip)
        {
            lock (MyTrips)
            {
                var index = MyTrips.IndexOf(theOldTrip);
                MyTrips.Remove(theOldTrip);
                MyTrips.Insert(index, theNewTrip);
            }
            //TODO - need to redo this logic and only update the trip and not all trips
            RefreshListView();
        }
        private Trip? DoesTripExist(Trip theTrip)
        {
            lock (MyTrips)
            {
                foreach (var t in MyTrips.Where(t => t.TripId == theTrip.TripId))
                {
                    return t;
                }
            }
            return null;
        }
        private void DoProcessForecast(Forecast theForecast)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { ProcessForecast(theForecast); }));
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void ProcessForecast(Forecast theForecast)
        {
            try
            {
                //TODO Swetha....you can do your updates from here
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void RefreshListView()
        {
            try
            {
                lvTrips.Items.Clear();
                lock (MyTrips)
                {
                    foreach (var t in MyTrips)
                    {
                        AddTrip(t, false);
                    }
                }
                lvTrips.Refresh();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
        }
        private void AddTrip(Trip theTrip, bool doRefresh = true)
        {
            while (blockThread)
            {
                Thread.Sleep(5);
            }
            try
            {
                blockThread = true;
                var length = 0;
                var trainType = "Default";
                if (theTrip.MyTrainPosition != null && theTrip.MyTrainPosition.Train != null)
                {
                    if (theTrip.MyTrainPosition.Train.DefaultLength > 0)
                        length = theTrip.MyTrainPosition.Train.DefaultLength / 1000;
                    trainType = theTrip.MyTrainPosition.Train.TrainType;
                }
                var i = 0;
                var shaded = Color.FromArgb(240, 240, 240);
                var lvItem = new ListViewItem(theTrip.ServiceName);
                lvItem.SubItems.Add(theTrip.TripCode);
                theTrip.Direction = theTrip.Direction == "L" ? "N" : "P";

                lvItem.SubItems.Add(theTrip.Direction);
                lvItem.SubItems.Add(theTrip.StartTime);
                lvItem.SubItems.Add(trainType);
                lvItem.SubItems.Add(theTrip.SubType.ToString());
                lvItem.SubItems.Add(length.ToString());
                lvItem.SubItems.Add(theTrip.Postfix.ToString());
                lvItem.SubItems.Add(theTrip.StartPosition);
                lvItem.SubItems.Add(theTrip.EndPosition);
                lvItem.SubItems.Add(theTrip.MyConflicts.Count.ToString());
                if (theTrip.MyConflicts.Count > 0) lvItem.ForeColor = Color.Red;
                if (theTrip.IsAllocated) lvItem.Font = new Font(lvTrips.Font, FontStyle.Bold);
                if (i++ % 2 == 1)
                {
                    lvItem.BackColor = shaded;
                    lvItem.UseItemStyleForSubItems = true;
                }
                lvItem.Tag = theTrip;
                lvTrips.Items.Add(lvItem);
                lvItem.ToolTipText = GetToolTip(theTrip);
                if (doRefresh) lvTrips.Refresh();
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            blockThread = false;
        }
        private static string GetToolTip(Trip theTrip)
        {
            var tt = new StringBuilder();
            try
            {
                foreach (var tl in theTrip.TimedLocations)
                {
                    tt.AppendLine(tl.Description + "  <" + tl.DepartureTimePlan.ToString("HH:mm") + "><" +
                        tl.ArrivalTimePlan.ToString("HH:mm") + ">");
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return tt.ToString();
        }
        private Trip? FindTrip(string TripCode, string startTime)
        {
            try
            {
                lock (MyTrips)
                {
                    foreach (var t in MyTrips.Where(t => t.TripCode == TripCode && t.StartTime == startTime))
                    {
                        return t;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return null;
        }

        private int FindTripIndex(Trip theTrip)
        {
            var index = -1;
            try
            {
                lock (MyTrips)
                {
                    foreach (var t in MyTrips)
                    {
                        index += 1;
                        if (t.TripId == theTrip.TripId && t.StartTime == theTrip.StartTime) return index;
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger?.LogException(e.ToString());
            }
            return index;
        }

        #endregion

        #region User Interaction
        private void lvTrips_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            try
            {
                if (lvTrips.Sorting == SortOrder.None) lvTrips.Sorting = SortOrder.Descending;
                lvTrips.Sorting = lvTrips.Sorting == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;

                var sorter = lvTrips.ListViewItemSorter as ItemComparer;
                if (sorter == null)
                {
                    sorter = new ItemComparer(e.Column);
                    lvTrips.ListViewItemSorter = sorter;

                }
                else
                {
                    // Set the column number that is to be sorted
                    sorter.Column = e.Column;
                }
                lvTrips.Sort();
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }
        private void lvTrips_ItemActivate(object sender, EventArgs e)
        {
            try
            {
                if (lvTrips.SelectedItems.Count > 0)
                {
                    var lvItem = lvTrips.SelectedItems[0];
                    if (lvItem != null)
                    {
                        var trip = FindTrip(lvItem.SubItems[1].Text, lvItem.SubItems[3].Text);


                        if (trip == null || trip.MyConflicts.Count <= 0) return;
                        var form = new Forms.FormConflictDetails();
                        form.AssociateTrip(trip);
                        //form.ShowDialog();
                        form.Show(this);
                        lvItem.Selected = false;
                        lvItem.Focused = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }
        private void mnuShowReservations_Click(object sender, EventArgs e)
        {
            try
            {
                var lvItem = lvTrips.SelectedItems[0];
                if (lvItem != null)
                {
                    var form = new Forms.FormReservation();
                    var trip = FindTrip(lvItem.SubItems[1].Text, lvItem.SubItems[3].Text);
                    if (trip == null) return;
                    form.AssociateTrip(trip);
                    //form.ShowDialog();
                    form.Show(this);
                    lvItem.Selected = false;
                    lvItem.Focused = false;
                }
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }
        private void mnuAutoRouting_Click(object sender, EventArgs e)
        {
            try
            {
                if (mnuAutoRouting.Text == @"Disable Automatic Conflict Resolution")
                {
                    MyTrainSchedulerManager!.ProduceMessage1004(true);
                    mnuAutoRouting.Text = @"Enable Automatic Conflict Resolution";
                }
                else
                {
                    MyTrainSchedulerManager!.ProduceMessage1004(false);
                    mnuAutoRouting.Text = @"Disable Automatic Conflict Resolution";
                }
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }
        private void mnuDeleteTrip_Click(object sender, EventArgs e)
        {
            try
            {
                var lvItem = lvTrips.SelectedItems[0];
                if (lvItem != null)
                {
                    var trip = FindTrip(lvItem.SubItems[1].Text, lvItem.SubItems[3].Text);
                    MyTrainSchedulerManager!.ProduceMessage1003(trip!);
                }
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }
        private void mnuSaveTrip_Click(object sender, EventArgs e)
        {
            try
            {
                var lvItem = lvTrips.SelectedItems[0];
                if (lvItem != null)
                {
                    var trip = FindTrip(lvItem.SubItems[1].Text, lvItem.SubItems[3].Text);
                    var tripCode = trip?.TripCode;
                    var tripUid = trip?.TripId.ToString();
                    MyTrainSchedulerManager!.ProduceMessage1006(tripCode, tripUid);
                }

            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }

        private void mnuClearEvents_Click(object sender, EventArgs e)
        {
            try
            {
                txtEvent.Text = "";
            }
            catch (Exception ex)
            {
                MyLogger?.LogException(ex.ToString());
            }
        }

        public void MovetoTrip(string tripId)
        {
            foreach (ListViewItem item in lvTrips.Items)
            {
                if(((Trip)item.Tag).TripId.ToString().Equals(tripId))
                {
                    if (item != null) item.Selected = true;
                }                
            } 
        }

        #endregion

    }

    #region Listview Sorter
    public class ItemComparer : IComparer
    {
        //column used for comparison
        public int Column { get; set; }
        public ItemComparer(int colIndex)
        {
            Column = colIndex;
        }
        public int Compare(object a, object b)
        {
            int result;
            ListViewItem itemA = a as ListViewItem;
            ListViewItem itemB = b as ListViewItem;
            if (itemA == null && itemB == null)
                result = 0;
            else if (itemA == null)
                result = -1;
            else if (itemB == null)
                result = 1;
            if (itemA == itemB)
                result = 0;
            //alphabetic comparison
            result = String.Compare(itemA?.SubItems[Column].Text, itemB?.SubItems[Column].Text);
            return result;
        }
        private int col;
        private SortOrder order;
        public int CompareDate(object x, object y)
        {
            int returnVal;
            // Determine whether the type being compared is a date type.
            try
            {
                // Parse the two objects passed as a parameter as a DateTime.
                System.DateTime firstDate =
                    DateTime.Parse(((ListViewItem)x).SubItems[col].Text);
                System.DateTime secondDate =
                    DateTime.Parse(((ListViewItem)y).SubItems[col].Text);

                // Compare the two dates.
                returnVal = DateTime.Compare(firstDate, secondDate);
            }
            // If neither compared object has a valid date format, compare
            // as a string.
            catch
            {
                // Compare the two items as a string.
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                    ((ListViewItem)y).SubItems[col].Text);
            }

            // Determine whether the sort order is descending.
            if (order == SortOrder.Descending)
                // Invert the value returned by String.Compare.
                returnVal *= -1;

            return returnVal;
        }
        public class ListViewItemDateTimeComparer : IComparer
        {
            private int col;
            private SortOrder order;
            public ListViewItemDateTimeComparer()
            {
                col = 0;
                order = SortOrder.Ascending;
            }

            public ListViewItemDateTimeComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }

            public int Compare(object x, object y)
            {
                int returnVal;
                // Determine whether the type being compared is a date type.
                try
                {
                    // Parse the two objects passed as a parameter as a DateTime.
                    System.DateTime firstDate =
                        DateTime.Parse(((ListViewItem)x).SubItems[col].Text);
                    System.DateTime secondDate =
                        DateTime.Parse(((ListViewItem)y).SubItems[col].Text);

                    // Compare the two dates.
                    returnVal = DateTime.Compare(firstDate, secondDate);
                }
                // If neither compared object has a valid date format, compare
                // as a string.
                catch
                {
                    // Compare the two items as a string.
                    returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
                        ((ListViewItem)y).SubItems[col].Text);
                }

                // Determine whether the sort order is descending.
                if (order == SortOrder.Descending)
                    // Invert the value returned by String.Compare.
                    returnVal *= -1;

                return returnVal;
            }
        }
    }
    #endregion

}
