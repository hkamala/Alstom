using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Trip;
using Microsoft.UI;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConflictManagementLibrary.View
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FormReservation : Window
    {
        private AppWindow _apw;
        private OverlappedPresenter _presenter;

        public FormReservation()
        {
            this.InitializeComponent();
            //this.Title = "Trip Reservations";
            SizeInt32 size = new SizeInt32(1531, 647);
            this.AppWindow.Resize(size);
            GetAppWindowAndPresenter();

            _presenter.IsResizable = false;
            _presenter.IsMinimizable = false;
            _presenter.IsMaximizable = false;

            LoadData();
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

        private void LoadData()
        {
            var trips = TripDataGenerator.GenerateTrips(MyLogger);
            //lvTrips.ItemsSource = trips;
        }

        public class ReservationDataGenerator
        {
            public static List<Reservation> GenerateResrvations(IMyLogger logger)
            {
                return new List<Reservation>
                {
                    new Reservation(logger) { MyStationName = "StationName1", MyNodeNumber = "01", MyLinkReferenceUid = "01uidL", MyEdgeUid = "01-EDG", SubType = Trip.TrainSubType.Freight, Length = 0, Postfix = Trip.PostFixType.G, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 1, TripId = 102, Direction = "R", StartTime = "06/28/24 06:12", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.Suburban, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 1, TripId = 103, Direction = "L", StartTime = "06/28/24 06:24", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Spare, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 1, TripId = 104, Direction = "R", StartTime = "06/28/24 06:24", TypeOfTrain = Trip.TrainType.Default, SubType = Trip.TrainSubType.Train, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 2, TripId = 201, Direction = "L", StartTime = "06/28/24 09:48", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Passenger, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 2, TripId = 202, Direction = "R", StartTime = "06/28/24 09:48", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.Suburban, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 0 },
                    new Reservation(logger) { SerUid = 3, TripId = 301, Direction = "L", StartTime = "06/28/24 12:12", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Repair, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 1 },
                    new Reservation(logger) { SerUid = 3, TripId = 302, Direction = "R", StartTime = "06/28/24 12:12", TypeOfTrain = Trip.TrainType.Freight, SubType = Trip.TrainSubType.FastFreight, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 1 },
                    new Reservation(logger) { SerUid = 3, TripId = 303, Direction = "L", StartTime = "06/28/24 12:14", TypeOfTrain = Trip.TrainType.Passenger, SubType = Trip.TrainSubType.Local, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 2 },
                    new Reservation(logger) { SerUid = 3, TripId = 304, Direction = "R", StartTime = "06/28/24 12:14", TypeOfTrain = Trip.TrainType.Default, SubType = Trip.TrainSubType.Locomotive, Length = 0, Postfix = Trip.PostFixType.None, StartPosition = "PVO1_DAR", EndPosition = "P1C_SKU", ConflictCount = 2 }
                };
            }
        }
    }
}
