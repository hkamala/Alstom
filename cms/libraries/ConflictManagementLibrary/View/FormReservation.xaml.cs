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
            //SizeInt32 size = new SizeInt32(1531, 647);
            //this.AppWindow.Resize(size);
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
            var reservations = ReservationDataGenerator.GenerateReservations(MyLogger);
            lvReservations.ItemsSource = reservations;
        }

        public class ReservationDataGenerator
        {
            public static List<Reservation> GenerateReservations(IMyLogger logger)
            {
                return new List<Reservation>
                {
                    new Reservation(logger) { MyStationName = "StationName1", MyNodeNumber = "01", MyLinkReferenceUid = "01uidL", MyEdgeUid = "01-EDG", RouteName = "SmplRteName", BeginPlatform = "SmplBgnPlf", EndPlatform = "SmplEndPlatform", MyTripStartTime = "06/06/24 06:12", MyTripEndTime = "06/06/24 16:12", TotalTimeInSeconds = "36000", DwellTimeinSeconds = "36000"},
                    new Reservation(logger) { MyStationName = "StationName2", MyNodeNumber = "02", MyLinkReferenceUid = "02uidL", MyEdgeUid = "02-EDG", RouteName = "SmplRteName", BeginPlatform = "SmplBgnPlf", EndPlatform = "SmplEndPlatform", MyTripStartTime = "06/07/24 08:36", MyTripEndTime = "06/07/24 18:40", TotalTimeInSeconds = "50100", DwellTimeinSeconds = "50100"},
                    new Reservation(logger) { MyStationName = "StationName3", MyNodeNumber = "03", MyLinkReferenceUid = "03uidL", MyEdgeUid = "03-EDG", RouteName = "SmplRteName", BeginPlatform = "SmplBgnPlf", EndPlatform = "SmplEndPlatform", MyTripStartTime = "06/08/24 23:07", MyTripEndTime = "06/08/24 06:13", TotalTimeInSeconds = "60840", DwellTimeinSeconds = "60840"},
                    new Reservation(logger) { MyStationName = "StationName4", MyNodeNumber = "04", MyLinkReferenceUid = "04uidL", MyEdgeUid = "04-EDG", RouteName = "SmplRteName", BeginPlatform = "SmplBgnPlf", EndPlatform = "SmplEndPlatform", MyTripStartTime = "06/08/24 12:56", MyTripEndTime = "06/08/24 16:43", TotalTimeInSeconds = "13620", DwellTimeinSeconds = "13620"},
                };
            }
        }
    }
}
