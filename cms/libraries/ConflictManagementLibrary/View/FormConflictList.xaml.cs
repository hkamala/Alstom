using ConflictManagementLibrary.Messages;
using ConflictManagementLibrary.Model.Movement;
using ConflictManagementLibrary.Model.Trip;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.UI;
using Windows.UI;
using ConflictManagementLibrary.Logging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.UI.Text;
using System.DirectoryServices;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConflictManagementLibrary.View;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FormConflictList : Window, INotifyPropertyChanged
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
    private bool _conflictResolutionEnabled;
    public event PropertyChangedEventHandler PropertyChanged;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;
    private string _lastSortedColumn = string.Empty;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public bool ConflictResolutionEnabled
    {
        get => _conflictResolutionEnabled;
        set
        {
            _conflictResolutionEnabled = value;
            OnPropertyChanged(); // Implement INotifyPropertyChanged if needed
        }
    }
    public string ConflictResolutionStatusText =>
    ConflictResolutionEnabled ? "Automatic Conflict Resolution Status Is Enabled" : "Automatic Conflict Resolution Status Is Disabled";

    public SolidColorBrush ConflictResolutionStatusColor =>
        new SolidColorBrush(ConflictResolutionEnabled ? Colors.Green : Colors.Red);

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
        InitializeForm();
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
        Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

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
            // Check if we are on the UI thread
            if (!DispatcherQueue.HasThreadAccess)
            {
                // If not, invoke on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProcessEvent(theMessage);
                });
            }
            else
            {
                // If already on the UI thread, just call the method
                ProcessEvent(theMessage);
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

            var paragraph = new Paragraph();
            var run = new Run
            {
                Text = eventMessage,
                Foreground = new SolidColorBrush(ColorForLine(theMessage.MyEventMessage?.EventLevel))
            };

            paragraph.Inlines.Add(run);
            txtEvent.Text = Convert.ToString(paragraph);

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
                return Colors.Red;
            }
            else if (line.Contains("WARN"))
            {
                return Colors.DarkOrange;
            }
            else if (line.Contains("DEBUG"))
            {
                return Colors.Blue;
            }
            else if (line.Contains("ERROR"))
            {
                return Colors.Coral;
            }
            else
            {
                return Colors.Black;
            }
        }
        return Colors.Black;
    }

    #endregion
    #region Trip Processing
    private void DoProcessStatus(ConflictManagementMessages.ConflictResolutionStatus theStatus)
    {
        try
        {
            // Check if we are on the UI thread
            if (!DispatcherQueue.HasThreadAccess)
            {
                // If not, invoke on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProcessStatus(theStatus);
                });
            }
            else
            {
                // If already on the UI thread, just call the method
                ProcessStatus(theStatus);
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
            ConflictResolutionEnabled = theStatus.ConflictResolutionEnabled;

            // Optionally log the status change
            MyLogger?.LogInfo($"Automatic Conflict Resolution Status Is {(ConflictResolutionEnabled ? "Enabled" : "Disabled")}");
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
            // Check if we are on the UI thread
            if (!DispatcherQueue.HasThreadAccess)
            {
                // If not, invoke on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProcessTrip(theTrip, theCommand);
                });
            }
            else
            {
                // If already on the UI thread, just call the method
                ProcessTrip(theTrip, theCommand);
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
            // Check if we are on the UI thread
            if (!DispatcherQueue.HasThreadAccess)
            {
                // If not, invoke on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    ProcessForecast(theForecast);
                });
            }
            else
            {
                // If already on the UI thread, just call the method
                ProcessForecast(theForecast);
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
        try
        {
            var length = 0;
            if (theTrip.MyTrainPosition != null && theTrip.MyTrainPosition.Train != null)
            {
                if (theTrip.MyTrainPosition.Train.DefaultLength > 0)
                    length = theTrip.MyTrainPosition.Train.DefaultLength / 1000;
            }

            // Create ListViewItem
            var lvItem = new ListViewItem();
            lvItem.Content = theTrip;

            // Customize appearance based on conditions
            if (theTrip.MyConflicts.Count > 0)
            {
                lvItem.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (theTrip.IsAllocated)
            {
                // Use FontWeights from Windows.UI.Text
                lvItem.FontWeight = FontWeights.Bold;
            }

            // Add ListViewItem to ListView
            lvTrips.Items.Add(lvItem);

            // Optionally refresh ListView and update title
            if (doRefresh)
            {
                lvTrips.UpdateLayout();
                this.Title = $"{this.Title} - Trips ({lvTrips.Items.Count})";
            }
        }
        catch (Exception e)
        {
            MyLogger?.LogException(e.ToString());
        }
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
    private void LoadData()
    {
        var trips = TripDataGenerator.GenerateTrips(MyLogger);
        lvTrips.ItemsSource = trips;
    }
    private Window m_window;
    private void lvTrips_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            var trip = e.ClickedItem as Trip;
            if (trip == null || trip.MyConflicts.Count <= 0) return;

            var form = new FormConflictDetails();
            form.AssociateTrip(trip);

            // Show the form (assuming it's a WinUI 3 window)
            form.Activate();
        }
        catch (Exception ex)
        {
            MyLogger?.LogException(ex.ToString());
        }
        //try
        //{
        //    var trip = FindTrip(Convert.ToString(((Trip)e.ClickedItem).TripId), Convert.ToString(((Trip)e.ClickedItem).SerUid));

        //    if (trip == null || trip.MyConflicts.Count <= 0) return;
        //    var form = new FormConflictDetails();
        //    form.AssociateTrip(trip);
        //    m_window = form;
        //    m_window.Activate();
        //}
        //catch (Exception ex)
        //{
        //    MyLogger?.LogException(ex.ToString());
        //}
    }
    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        var header = sender as Button;
        if (header != null)
        {
            var columnTag = header.Content.ToString();

            // Determine the sorting direction
            if (_lastSortedColumn == columnTag)
            {
                _sortDirection = _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
            else
            {
                _sortDirection = ListSortDirection.Ascending;
                _lastSortedColumn = columnTag;
            }

            SortTrips(columnTag, _sortDirection);
        }
    }
    private void SortTrips(string column, ListSortDirection direction)
    {
        var sortedTrips = new ObservableCollection<Trip>(lvTrips.Items.Cast<Trip>().OrderBy(trip => trip.GetType().GetProperty(column).GetValue(trip, null)));

        if (direction == ListSortDirection.Descending)
        {
            sortedTrips = new ObservableCollection<Trip>(sortedTrips.Reverse());
        }

        lvTrips.ItemsSource = sortedTrips;
    }
    private void lvTrips_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
    private void lvTrips_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var clickedItem = (e.OriginalSource as FrameworkElement)?.DataContext as Trip;
        if (clickedItem != null)
        {
            lvTrips.SelectedItem = clickedItem;
            var frameworkElement = sender as FrameworkElement;
            var flyout = frameworkElement?.Resources["RightClickMenu"] as MenuFlyout;
            flyout?.ShowAt(frameworkElement, e.GetPosition(frameworkElement));
        }
    }
    private void ShowReservations_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var lvItem = (Trip)lvTrips.SelectedItem;
            if (lvItem != null)
            {
                var trip = FindTrip(Convert.ToString(lvItem.TripId), Convert.ToString(lvItem.StartTime));
                if (trip == null) return;

                var reservation = new FormReservation();
                //reservation.AssociateTrip(trip);
                reservation.Activate();

                lvTrips.SelectedItem = null; // Deselect the item
            }
        }
        catch (Exception ex)
        {
            // Assuming MyLogger is a method to log exceptions
            MyLogger?.LogException(ex.ToString());
        }
    }

    private void ShowRoutePlan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var lvItem = (Trip)lvTrips.SelectedItem;
            if (lvItem != null)
            {
                var trip = FindTrip(Convert.ToString(lvItem.TripId), Convert.ToString(lvItem.StartTime));
                if (trip == null) return;

                var routePlan = new FormRoutePlan();
                //routePlan.AssociateTrip(trip);
                routePlan.Activate();

                lvTrips.SelectedItem = null; // Deselect the item
            }
        }
        catch (Exception ex)
        {
            // Assuming MyLogger is a method to log exceptions
            MyLogger?.LogException(ex.ToString());
        }
    }

    private void DeleteTrip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var lvItem = (Trip)lvTrips.SelectedItem;
            if (lvItem != null)
            {
                var trip = FindTrip(Convert.ToString(lvItem.TripId), Convert.ToString(lvItem.StartTime));
                MyTrainSchedulerManager!.ProduceMessage1003(trip!);
            }
        }
        catch (Exception ex)
        {
            MyLogger?.LogException(ex.ToString());
        }
    }

    private void SaveTrip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var lvItem = (Trip)lvTrips.SelectedItem;
            if (lvItem != null)
            {
                var trip = FindTrip(Convert.ToString(lvItem.TripId), Convert.ToString(lvItem.StartTime));
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
}

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
