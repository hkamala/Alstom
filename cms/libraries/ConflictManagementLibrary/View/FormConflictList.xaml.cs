using ConflictManagementLibrary.Messages;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Microsoft.UI;
using static ConflictManagementLibrary.Model.Trip.Trip;
using ConflictManagementLibrary.Logging;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConflictManagementLibrary.View;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FormConflictList : Window
{
    /// <summary>
    /// Represents a Microsoft.UI.AppWindow object.
    /// </summary>
    private AppWindow _apw;
    /// <summary>
    /// Represents a Microsoft.UI.Windowing object.
    /// </summary>
    private OverlappedPresenter _presenter;
    #region Variables
    public ObservableCollection<Trip> MyTrips = new ObservableCollection<Trip>();
    public bool blockThread = false;
    #endregion

    #region Delegates
    public delegate void ConflictUpdateDelegate(Trip? theTrip);
    public ConflictUpdateDelegate? PerformConflictUpdate;

    public delegate void UpdateTripReservation(Trip? theTrip);
    public UpdateTripReservation? PerformUpdateTripReservation;

    private ObservableCollection<FormReservation> myFormReservations = new ObservableCollection<FormReservation>();
    #endregion
    public void LinkDelegate(UpdateTripReservation? updateTripReservation)
    {
        this.PerformUpdateTripReservation = updateTripReservation ?? throw new ArgumentNullException(nameof(updateTripReservation));
    }
    public void LinkConflictDelegate(ConflictUpdateDelegate? processConflictUpdate)
    {
        this.PerformConflictUpdate = processConflictUpdate ?? throw new ArgumentNullException(nameof(processConflictUpdate));
    }
    public FormConflictList()
    {
        this.InitializeComponent();
        this.Title = "Trip Conflict List";
        //SizeInt32 size = new SizeInt32(1200, 600);
        //this.AppWindow.Resize(size);

        GetAppWindowAndPresenter();
        _presenter.IsResizable = false;
        _presenter.IsMinimizable = false;
        _presenter.IsMaximizable = false;
        LoadData();
        Thread.CurrentThread.CurrentCulture = new CultureInfo(CurrentUiCulture);
    }
    #region GetAppWindowAndPresenter
    /// <summary>
    /// Retrieves the AppWindow and OverlappedPresenter objects associated with the current window.
    /// </summary>
    public void GetAppWindowAndPresenter()
    {
        // Get the window handle of the current XAML control
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // API to get the window object from UIElement (not a real API)

        // Get the WindowId from the window handle
        WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

        // Retrieve the AppWindow associated with the WindowId
        _apw = AppWindow.GetFromWindowId(myWndId);

        // Get the OverlappedPresenter object from the AppWindow
        _presenter = _apw.Presenter as OverlappedPresenter;
    }

    #endregion
    private void InitializeForm()
    {

        MyTrainSchedulerManager?.LinkDelegate(DoProcessTrip!, DoProcessStatus!, DoProcessEvent!, DoProcessForecast!);
        MyTrainSchedulerManager?.ProduceMessage1000();

    }
    #region Event Log Processsing
    private void DoProcessEvent(ConflictManagementMessages.CmsEventMessage theMessage)
    {
        try
        {
            //if (InvokeRequired)
            //{
            //    this.Invoke(new MethodInvoker(delegate { ProcessEvent(theMessage); }));
            //}

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
            //txtEvent.SelectionColor = ColorForLine(theMessage.MyEventMessage?.EventLevel);
            //txtEvent.AppendText(eventMessage);
            //txtEvent.ScrollToCaret();
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
            //if (InvokeRequired)
            //{
            //    this.Invoke(new MethodInvoker(delegate { ProcessStatus(theStatus); }));
            //}
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
                //tssConflictResolutionStatus.ForeColor = Color.Green;
                tssConflictResolutionStatus.Text = @"Automatic Conflict Resolution Status Is Enabled";
            }
            else
            {
                //tssConflictResolutionStatus.ForeColor = Color.Red;
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
            //if (InvokeRequired)
            //{
            //    this.Invoke(new MethodInvoker(delegate { ProcessTrip(theTrip, theCommand); }));
            //}
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
        UpdateReservationForms(theNewTrip);
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
    private void UpdateReservationForms(Trip theTrip)
    {
        try
        {
            foreach (var f in myFormReservations)
            {
                try
                {
                    //f.UpdateTrip(theTrip);
                }
                catch (Exception e)
                {
                    MyLogger?.LogException(e.ToString());
                }
            }
        }
        catch (Exception e)
        {
            MyLogger?.LogException(e.ToString());
        }
    }
    private void DoProcessForecast(Forecast theForecast)
    {
        try
        {
            //if (InvokeRequired)
            //{
            //    this.Invoke(new MethodInvoker(delegate { ProcessForecast(theForecast); }));
            //}
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
            //lvTrips.Refresh();
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
            //var trainType = "Default";
            if (theTrip.MyTrainPosition != null && theTrip.MyTrainPosition.Train != null)
            {
                if (theTrip.MyTrainPosition.Train.DefaultLength > 0)
                    length = theTrip.MyTrainPosition.Train.DefaultLength / 1000;
                //trainType = theTrip.MyTrainPosition.Train.TrainType;
            }
            //var i = 0;
            //var shaded = Color.FromArgb(240, 240, 240);
            //var lvItem = new ListViewItem(theTrip.ScheduledPlanName);
            //lvItem.SubItems.Add(theTrip.TripCode);
            //lvItem.SubItems.Add(theTrip.Direction);
            //lvItem.SubItems.Add(theTrip.StartTime);
            //lvItem.SubItems.Add(theTrip.TrainTypeString);
            //lvItem.SubItems.Add(theTrip.SubType.ToString());
            //lvItem.SubItems.Add(length.ToString());
            //lvItem.SubItems.Add(theTrip.Postfix.ToString());
            //lvItem.SubItems.Add(theTrip.StartPosition);
            //lvItem.SubItems.Add(theTrip.EndPosition);
            //lvItem.SubItems.Add(theTrip.MyConflicts.Count.ToString());
            //if (theTrip.MyConflicts.Count > 0) lvItem.ForeColor = Color.Red;
            //if (theTrip.IsAllocated) lvItem.Font = new Font(lvTrips.Font, FontStyle.Bold);
            //if (i++ % 2 == 1)
            //{
            //    lvItem.BackColor = shaded;
            //    lvItem.UseItemStyleForSubItems = true;
            //}
            //lvItem.Tag = theTrip;
            //lvTrips.Items.Add(lvItem);
            //lvItem.ToolTipText = GetToolTip(theTrip);
            //if (doRefresh)
            //{
            //    lvTrips.Refresh();
            //    this.Text = myName + @"  -  Trips (" + lvTrips.Items.Count + @")";
            //}
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
                tt.AppendLine(tl.Description + "  <" + tl.DepartureTimeActual.ToString("HH:mm") + "><" +
                    tl.ArrivalTimeActual.ToString("HH:mm") + ">");
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
    private void mnuAutoRouting_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            //if (mnuAutoRouting.Text == @"Disable Automatic Conflict Resolution")
            //{
            //    MyTrainSchedulerManager!.ProduceMessage1004(true);
            //    mnuAutoRouting.Text = @"Enable Automatic Conflict Resolution";
            //}
            //else
            //{
            //    MyTrainSchedulerManager!.ProduceMessage1004(false);
            //    mnuAutoRouting.Text = @"Disable Automatic Conflict Resolution";
            //}
        }
        catch (Exception ex)
        {
            MyLogger?.LogException(ex.ToString());
        }
    }
    private void LoadData()
    {
        var trips = TripDataGenerator.GenerateTrips(MyLogger);
        lvTrips.ItemsSource = trips;
    }
}

//public class MockTrip
//{
//    public string Control { get; set; }
//    public string Code { get; set; }
//    public string L_R { get; set; }
//    public string Time { get; set; }
//    public string TrainType { get; set; }
//    public string SubType { get; set; }
//    public int Length { get; set; }
//    public string PostFix { get; set; }
//    public string StartLocation { get; set; }
//    public string EndLocation { get; set; }
//    public int Conflicts { get; set; }
//}

public class TripDataGenerator
{
    public static List<Trip> GenerateTrips(IMyLogger logger)
    {
        return new List<Trip>
            {
                new Trip(logger) { SerUid = 1, TripId = 101, Direction = "L", StartTime = "06/28/24 06:12", TypeOfTrain = Trip.TrainType.Default, SubType = Trip.TrainSubType.Freight, Length = 0, Postfix = Trip.PostFixType.G, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 1, TripId = 102, Direction = "R", StartTime = "06/28/24 06:12", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.Suburban, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 1, TripId = 103, Direction = "L", StartTime = "06/28/24 06:24", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Spare, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 1, TripId = 104, Direction = "R", StartTime = "06/28/24 06:24", TypeOfTrain = Trip.TrainType.Default, SubType = Trip.TrainSubType.Train, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 2, TripId = 201, Direction = "L", StartTime = "06/28/24 09:48", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Passenger, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 2, TripId = 202, Direction = "R", StartTime = "06/28/24 09:48", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.Suburban, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                new Trip(logger) { SerUid = 3, TripId = 301, Direction = "L", StartTime = "06/28/24 12:12", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Repair, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 1 },
                new Trip(logger) { SerUid = 3, TripId = 302, Direction = "R", StartTime = "06/28/24 12:12", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.FastFreight, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 1 },
                new Trip(logger) { SerUid = 3, TripId = 303, Direction = "L", StartTime = "06/28/24 12:14", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Local, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 2 },
                new Trip(logger) { SerUid = 3, TripId = 304, Direction = "R", StartTime = "06/28/24 12:14", TypeOfTrain = Trip.TrainType.Default, SubType = Trip.TrainSubType.Locomotive, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 2 }
            };
    }
}