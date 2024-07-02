using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Logging;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Reservation;
using ConflictManagementLibrary.Model.Trip;
using ConflictManagementLibrary.Test;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static ConflictManagementLibrary.View.DemoData;


//using System.Windows.Forms;
using Exception = System.Exception;
//using ListViewItem = System.Windows.Forms.ListViewItem;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ConflictManagementLibrary.View
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FormConflictDetails : Window
    {
        public Trip? MyTrip;
        private Conflict? currentConflict;
        public FormConflictDetails()
        {
            this.InitializeComponent();
            DisplayPlan();
            InitializeForm();
            DisplayCurrentConflictsInTreeView();
        }


        private TextBlock CreateTextBlock(string text, double fontSize)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Calibri"),
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                FontStyle = Windows.UI.Text.FontStyle.Normal,
            };
        }

        private TreeViewNode CreateTreeViewNode(string text, double fontSize)
        {
            return new TreeViewNode
            {
                Content = CreateTextBlock(text, fontSize)
            };
        }


        private void FormConflictDetails_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(CurrentUiCulture);
            InitializeForm();
        }

        private void InitializeForm()
        {
            //if (MyTrip != null) this.Title += @" - Trip " + MyTrip.TripCode;
            if (MyTrip != null) this.Title += @" - Trip " + DemoSingleTripData.GenerateTrips(MyLogger).TripCode;
            foreach (var child in gbRouteDetails.Children)
            {
                if (child is TextBlock textBlock)
                {
                    textBlock.Text += " - " + DemoSingleTripData.GenerateTrips(MyLogger).TripCode;
                }
            }
        }

        public void AssociateTrip(Trip theTrip)
        {
            MyTrip = theTrip;
            DisplayCurrentConflictsInTreeView();
            DisplayPlan();
        }

        private void DisplayPlan()
        {
            try
            {
                var i = 0;
                //foreach (var tl in MyTrip.TimedLocations) 
                // Used Mock data here
                foreach (var myTrip in DemoData.GenerateTrips(MyLogger))
                {
                    foreach (var tl in myTrip.TimedLocations)
                    {
                        var shaded = Microsoft.UI.Colors.DarkGray; // Windows.UI.Color.FromArgb(240, 240, 240);
                        var lvItem = new ListViewItem();
                        //lvItem.Content = tl.Description;

                        // Create a StackPanel to hold the sub-items
                        StackPanel stackPanel = new StackPanel();
                        stackPanel.Orientation = Orientation.Horizontal;

                        //lvItem.SubItems.Add(tl.ArrivalTimeAdjusted.ToString("dd/MM/yy HH:mm"));
                        //lvItem.SubItems.Add(tl.DepartureTimeAdjusted.ToString("dd/MM/yy HH:mm"));
                        //if (i++ % 2 == 1)
                        //{
                        //    lvItem.Background = new SolidColorBrush(shaded);
                        //}
                        //lvPlan.Items.Add(lvItem);
                        //lvItem.ToolTipText = GetToolTip(tl);


                        // Add description
                        TextBlock descriptionTextBlock = new TextBlock();
                        descriptionTextBlock.Text = tl.Description;
                        stackPanel.Children.Add(descriptionTextBlock);

                        // Add arrival time
                        TextBlock arrivalTimeTextBlock = new TextBlock();
                        arrivalTimeTextBlock.Text = tl.ArrivalTimeAdjusted.ToString("dd/MM/yy HH:mm");
                        stackPanel.Children.Add(arrivalTimeTextBlock);

                        // Add departure time
                        TextBlock departureTimeTextBlock = new TextBlock();
                        departureTimeTextBlock.Text = tl.DepartureTimeAdjusted.ToString("dd/MM/yy HH:mm");
                        stackPanel.Children.Add(departureTimeTextBlock);

                        // Set the StackPanel as the content of the ListViewItem
                        lvItem.Content = stackPanel;

                        if (i++ % 2 == 1)
                        {
                            lvItem.Background = new SolidColorBrush(shaded);
                        }

                        // Add the ListViewItem to the ListView
                        lvPlan.Items.Add(lvItem);

                        // Set ToolTip
                        ToolTip toolTip = new ToolTip();
                        toolTip.Content = GetToolTip(tl);
                        ToolTipService.SetToolTip(lvItem, toolTip);


                        // No need to call lvPlan.Refresh() in WinUI 3
                    }
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private static string GetToolTip(TimedLocation timedLocation)
        {
            var tt = new StringBuilder();
            try
            {
                if (timedLocation.MyMovementPlan == null) return "No Movement Plan Found";
                tt.AppendLine(timedLocation.MyMovementPlan.Description);
                foreach (var ra in timedLocation.MyMovementPlan.MyRouteActions)
                {
                    tt.AppendLine(ra.RouteName + "> <" + ra.ActionLocation + ">");
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
            return tt.ToString();
        }

        private void DisplayCurrentConflictsInTreeView()
        {
            try
            {
                TreeViewNode currentConflict = CreateTreeViewNode("Current Conflict", 20);
                tvConflictsCurrent.RootNodes.Add(currentConflict);
                //foreach (var conflict in MyTrip.MyConflicts)
                // Mock Data
                foreach (var trip in DemoData.GenerateTrips(MyLogger))
                {

                    foreach (var conflict in trip.MyConflicts)
                    {
                        TreeViewNode locationDetails = CreateTreeViewNode($"{conflict.MyLocationDetail}", 14);
                        currentConflict.Children.Add(locationDetails);

                        TreeViewNode typeOfConflict = CreateTreeViewNode($"Conflict Type <{conflict.MyTypeOfConflict}>", 14);
                        locationDetails.Children.Add(typeOfConflict);

                        TreeViewNode conflictType = CreateTreeViewNode($"Subtype <{conflict.MySubtypeOfConflict.MyDescription}>", 14);
                        locationDetails.Children.Add(conflictType);

                        TreeViewNode location = CreateTreeViewNode($"Location <{conflict.MyLocation}>", 14);
                        locationDetails.Children.Add(location);

                        TreeViewNode nameOfConflict = CreateTreeViewNode($"Name Of Conflict Entity <{conflict.MyEntity.MyDescription}>", 14);
                        locationDetails.Children.Add(nameOfConflict);

                        TreeViewNode resolution = CreateTreeViewNode($"Resolution<{conflict.MyResolution.MyTypeOfResolution}>", 14);
                        locationDetails.Children.Add(resolution);
                    }
                }
                tvConflictsCurrent.SelectedNodes[0].IsExpanded = true;
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private void tvConflictsCurrent_SelectionChanged(Microsoft.UI.Xaml.Controls.TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            try
            {
                ClearConflictDetails();
                var tv = (Microsoft.UI.Xaml.Controls.TreeView)sender;
                DisplayCurrentConflictDetails(tv.SelectedNode);
            }
            catch (Exception exception)
            {
                GlobalDeclarations.MyLogger?.LogException(exception.ToString());
            }
        }

        private void DisplayCurrentConflictDetails(TreeViewNode theNode)
        {
            try
            {
                var conflict = GetConflict(theNode.Content.ToString());
                if (conflict != null)
                {
                    UpdateTextBoxColor(conflict);
                    txtSubtype.Text = conflict.MySubtypeOfConflict.MyDescription.Trim();
                    txtConflictLocation.Text = conflict.MyLocation;
                    txtConflictTime.Text = conflict.MyDateTime.ToString("dd-MM HH:mm:ss");
                    txtConflictEntity.Text = conflict.MyEntity.MyEntityType.ToString();
                    txtConflictType.Text = conflict.MyTypeOfConflict.ToString();
                    txtConflictDescription.Text = conflict.MyEntity.MyDescription;
                    txtConflictResolution.Text = conflict.MyResolution.MyTypeOfResolution.ToString();
                    currentConflict = conflict;
                    if (conflict.MyReservation != null && conflict.MyReservation.MyTimedLocation != null)
                        UpdateTripPlanList(conflict.MyReservation.MyTimedLocation.Description);
                    if (!conflict.IsResolvable)
                    {
                        btnAccept.Visibility = Visibility.Collapsed;
                        btnReject.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        btnAccept.Visibility = Visibility.Visible;
                        btnReject.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private void UpdateTripPlanList(string platformName)
        {
            try
            {
                foreach (var lv in lvPlan.Items)
                {
                    var theItem = (ListViewItem)lv;
                    if (theItem.Content == platformName) theItem.Foreground = GetNodeColor(currentConflict!);
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private void DisplayPastConflictDetails(TreeViewNode theNode)
        {
            try
            {
                var conflict = GetPastConflict(theNode.Content.ToString());
                if (conflict != null)
                {
                    UpdateTextBoxColor(conflict);
                    txtSubtype.Text = conflict.MySubtypeOfConflict.MyDescription.Trim();
                    txtConflictLocation.Text = conflict.MyLocation;
                    txtConflictTime.Text = conflict.MyDateTime.ToString("dd/MM HH:mm");
                    txtConflictEntity.Text = conflict.MyEntity.MyEntityType.ToString();
                    txtConflictType.Text = conflict.MyTypeOfConflict.ToString();
                    txtConflictDescription.Text = conflict.MyEntity.MyDescription;
                    txtConflictResolution.Text = conflict.MyResolution.MyTypeOfResolution.ToString();
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private void ClearConflictDetails()
        {
            txtConflictEntity.Text = string.Empty;
            txtConflictLocation.Text = string.Empty;
            txtConflictType.Text = string.Empty;
            txtSubtype.Text = string.Empty;
            txtConflictResolution.Text = string.Empty;
            txtConflictTime.Text = string.Empty;
            txtConflictDescription.Text = string.Empty;
            ResetTripPlanView();
        }

        private void ResetTripPlanView()
        {
            try
            {
                foreach (var lv in lvPlan.Items)
                {
                    var theItem = (ListViewItem)lv;
                    // theItem.ForeColor = Color.Black;
                    theItem.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private Conflict? GetConflict(string theConflict)
        {
            try
            {
                //foreach (var conflict in MyTrip!.MyConflicts)
                // Mock Data 
                foreach (var trip in GenerateTrips(MyLogger))
                {
                    foreach (var conflict in trip!.MyConflicts)
                    {
                        if (theConflict != conflict.MyLocationDetail) continue;
                        return conflict;
                    }
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
            return null;
        }

        private static Conflict? GetPastConflict(string theConflictTime)
        {
            try
            {
                foreach (var conflict in AddConflict.MyConflictsPast)
                {
                    var stringDateTime = conflict.MyDateTime.ToString("yy-MMM-dd HH:mm:ss");
                    if (theConflictTime != stringDateTime) continue;
                    return conflict;
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
            return null;
        }

        private void tvConflictsCurrent_Leave(object sender, EventArgs e)
        {
            ClearConflictDetails();
        }

        private static SolidColorBrush GetNodeColor(Conflict theConflict)
        {
            if (!theConflict.IsConflicting) return new SolidColorBrush(Colors.Black);
            if (!theConflict.IsResolvable && (!theConflict.IsAccepted && !theConflict.IsRejected)) return new SolidColorBrush(Colors.Red);
            if (theConflict.IsAccepted) return new SolidColorBrush(Colors.Green);
            if (theConflict.IsRejected) return new SolidColorBrush(Colors.Blue);
            return new SolidColorBrush(Colors.Red);
        }

        private void UpdateTextBoxColor(Conflict theConflict)
        {
            SolidColorBrush theBrush = GetNodeColor(theConflict);
            foreach (var control in gbConflictSummary.Children)
            {
                if (control is TextBlock)
                {
                    var tb = (TextBlock)control;
                    tb.Foreground = theBrush;
                }
            }
        }

        private void UpdateConflictView()
        {
            try
            {
                //tvConflictsCurrent.Nodes[0].Nodes.Clear();
                tvConflictsCurrent.SelectedNodes.Clear();
                ClearConflictDetails();
                DisplayCurrentConflictsInTreeView();
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }
        private void DeleteConflict(Conflict theConflict)
        {
            try
            {
                if (MyTrip != null)
                {
                    foreach (var c in MyTrip.MyConflicts)
                    {
                        if (c.MyGuid == theConflict.MyGuid) MyTrip.MyConflicts.Remove(c);
                        //tvConflictsCurrent.Nodes.Clear();
                        tvConflictsCurrent.SelectedNodes.Clear();
                        ClearConflictDetails();
                        DisplayCurrentConflictsInTreeView();
                        break;

                    }
                }
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
        }

        private void tvConflictsCurrent_LostFocus(object sender, RoutedEventArgs e)
        {
            ClearConflictDetails();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentConflict != null)
                {
                    currentConflict.IsAccepted = true;
                    currentConflict.IsRejected = false;
                    UpdateConflictView();
                    GlobalDeclarations.MyTrainSchedulerManager!.ProduceMessage1001(currentConflict);
                    //this.Close();
                }
            }
            catch (Exception ex)
            {
                GlobalDeclarations.MyLogger?.LogException(ex.ToString());
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentConflict != null)
                {
                    currentConflict.IsRejected = true;
                    currentConflict.IsAccepted = false;
                    UpdateConflictView();
                    GlobalDeclarations.MyTrainSchedulerManager!.ProduceMessage1002(currentConflict);
                    //this.Close();
                    //DeleteConflict(currentConflict);
                }
            }
            catch (Exception ex)
            {
                GlobalDeclarations.MyLogger?.LogException(ex.ToString());
            }
        }
    }

    //public class DemoConflict
    //{
    //    public string MyLocationDetail { set; get; }
    //    public string MyTypeOfConflict { set; get; }
    //    public string MySubtypeOfConflictMyDescription { set; get; }
    //    public string MyLocation { set; get; }
    //    public string MyEntityMyDescription { set; get; }
    //    public string MyResolutionMyTypeOfResolution { set; get; }
    //}


    public class DemoData
    {
        public static List<Trip> GenerateTrips(IMyLogger logger)
        {
            return new List<Trip>
            {
            new Trip(logger)
            {
                CtcUid = "CTC123",
                SerUid = 1,
                ScheduledPlanId = "Plan123",
                Name = "Trip A",
                TripCode = "TA123",
                TripId = 1001,
                SysUid = "Sys123",
                ScheduledPlanName = "Plan A",
                Number = 1,
                StartTime = "08:00",
                StartPosition = "StationA",
                EndPosition = "StationB",
                Direction = "North",
                TypeOfTrain = Trip.TrainType.Passenger,
                SubType = Trip.TrainSubType.Passenger,
                TrainTypeString = "Passenger",
                TrainPriority = 1,
                Length = 200,
                Postfix = Trip.PostFixType.None,
                LocationCurrent = new TrainLocation { },
                LocationNext = new TrainLocation {  },
                TimedLocations = new List<TimedLocation>
                {
                    new TimedLocation
                    {
                        Description = "PV01_DAR",
                        ArrivalTimeAdjusted = DateTime.Now,
                        DepartureTimeAdjusted = DateTime.Now,
                        MyMovementPlan = new Model.Movement.MovementPlan()
                        {
                            Description = "mymovementplanDesc1",
                            MyRouteActions = new List<Model.Movement.RouteAction>
                            {
                                new Model.Movement.RouteAction()
                                {
                                    RouteName = "RouteName1",
                                    ActionLocation = "ActionLocation1"
                                }
                            },
                        },
                    },
                    new TimedLocation
                    {
                        Description = "PV02_DAR",
                        ArrivalTimeAdjusted = DateTime.Now,
                        DepartureTimeAdjusted = DateTime.Now,
                        MyMovementPlan = new Model.Movement.MovementPlan()
                        {
                            Description = "mymovementplanDesc2",
                            MyRouteActions = new List<Model.Movement.RouteAction>
                            {
                                new Model.Movement.RouteAction()
                                {
                                    RouteName = "RouteName2",
                                    ActionLocation = "ActionLocation2"
                                }
                            },
                        },
                    }
                },
                MyConflicts = new List<Conflict>
                {
                    new Conflict
                    {
                            MyLocationDetail = "MyLocationDetail",
                            MyTypeOfConflict = ConflictType.TypeOfConflict.Train,
                            MySubtypeOfConflict = new SubtypeOfConflict
                            {
                                MyDescription = "DESC",
                                MyConflictType = ConflictType.TypeOfConflict.Train,
                                MyIndex = 12
                            },
                            MyLocation = "MyLocation",
                            MyEntity = new ConflictEntity
                            {
                                MyDescription = "MyDescription",
                                MyEntityType = ConflictEntity.EntityType.Train
                            },
                            MyResolution = new ConflictResolution
                            {
                                MyTypeOfResolution = ConflictResolution.TypeOfResolution.HoldOtherTrain
                            },
                            MyDateTime = DateTime.Now,
                            MyReservation = new Reservation() { },
                        }
                    },
                },
            };
        }

        public class DemoSingleTripData
        {
            public static Trip GenerateTrips(IMyLogger logger)
            {
                return new Trip(logger)
                {
                    CtcUid = Guid.NewGuid().ToString(),
                    SerUid = 1,
                    ScheduledPlanId = "Plan123",
                    Name = "Trip A",
                    TripCode = "0201",
                    TripId = 1001,
                    SysUid = "Sys123",
                    ScheduledPlanName = "Plan A",
                    Number = 1,
                    StartTime = "08:00",
                    StartPosition = "StationA",
                    EndPosition = "StationB",
                    Direction = "North",
                    TypeOfTrain = Trip.TrainType.Passenger,
                    SubType = Trip.TrainSubType.Passenger,
                    TrainTypeString = "Passenger",
                    TrainPriority = 1,
                    Length = 200,
                    Postfix = Trip.PostFixType.None,
                    LocationCurrent = new TrainLocation { },
                    LocationNext = new TrainLocation { },
                    TimedLocations = new List<TimedLocation>
                    {
                    new TimedLocation
                    {
                        Description = "PV01_DAR",
                        ArrivalTimeAdjusted = DateTime.Now,
                        DepartureTimeAdjusted = DateTime.Now,
                        MyMovementPlan = new Model.Movement.MovementPlan()
                        {
                            Description = "mymovementplanDesc1",
                            MyRouteActions = new List<Model.Movement.RouteAction>
                            {
                                new Model.Movement.RouteAction()
                                {
                                    RouteName = "RouteName1",
                                    ActionLocation = "ActionLocation1"
                                }
                            },
                        },
                    },
                    new TimedLocation
                    {
                        Description = "PV02_DAR",
                        ArrivalTimeAdjusted = DateTime.Now,
                        DepartureTimeAdjusted = DateTime.Now,
                        MyMovementPlan = new Model.Movement.MovementPlan()
                        {
                            Description = "mymovementplanDesc2",
                            MyRouteActions = new List<Model.Movement.RouteAction>
                            {
                                new Model.Movement.RouteAction()
                                {
                                    RouteName = "RouteName2",
                                    ActionLocation = "ActionLocation2"
                                }
                            },
                        },
                    }
                },
                    MyConflicts = new List<Conflict>
                {
                    new Conflict
                    {
                        MyLocationDetail = "",
                        MyTypeOfConflict = ConflictType.TypeOfConflict.Train,
                        //MySubtypeOfConflict = SubtypeOfConflict.CreateSubType(ConflictType.TypeOfConflict.Train, "ABCD",122),
                        MySubtypeOfConflict = new SubtypeOfConflict  { MyDescription = "DESC", MyConflictType = ConflictType.TypeOfConflict.Train, MyIndex = 12}, // .CreateSubType(ConflictType.TypeOfConflict.Train, "ABCD",122),
                        MyLocation = "",
                        MyEntity = new ConflictEntity { MyDescription = "", MyEntityType = ConflictEntity.EntityType.Train },
                        //MyReservation = new Reservation {  },
                        MyResolution = new ConflictResolution
                        {
                            MyTypeOfResolution = ConflictResolution.TypeOfResolution.HoldOtherTrain
                        },
                        MyDateTime = DateTime.Now,
                        MyReservation = new Reservation() { },
                    }
                },
                };
            }
        }
    }

    //public class DemoData
    //{
    //    public static List<DemoConflict> GetMockData()
    //    {
    //        return new List<DemoConflict>
    //    {
    //        new DemoConflict
    //        {
    //            MyLocationDetail = "Open Line Between <MAN and ZIE>",
    //            MyTypeOfConflict = "Open Line Conflict with opposite Direction Train",
    //            MySubtypeOfConflictMyDescription = "Open Line Conflict with opposite Direction Train",
    //            MyLocation = "Mangali@2PC_MAN(320400)(E_PT1_MAN_PT4_ZIE)",
    //            MyEntityMyDescription = "Conflicting Reservation with Trip",
    //            MyResolutionMyTypeOfResolution = "Hold This Train"
    //        },
    //        new DemoConflict
    //        {
    //            MyLocationDetail = "Open Line Between <ABC and DEF>",
    //            MyTypeOfConflict = "Open Line Conflict with Maintenance",
    //            MySubtypeOfConflictMyDescription = "Open Line Conflict with Maintenance Work",
    //            MyLocation = "StationX@3PC_ABC(123456)(W_PT2_ABC_PT5_DEF)",
    //            MyEntityMyDescription = "Conflicting Maintenance Schedule",
    //            MyResolutionMyTypeOfResolution = "Divert to Alternate Route"
    //        },
    //        new DemoConflict
    //        {
    //            MyLocationDetail = "Open Line Between <GHI and JKL>",
    //            MyTypeOfConflict = "Signal Failure",
    //            MySubtypeOfConflictMyDescription = "Signal Failure Leading to Delays",
    //            MyLocation = "StationY@4PC_GHI(654321)(N_PT3_GHI_PT6_JKL)",
    //            MyEntityMyDescription = "Signal Failure at Block Section",
    //            MyResolutionMyTypeOfResolution = "Send Maintenance Crew"
    //        },
    //        new DemoConflict
    //        {
    //            MyLocationDetail = "Open Line Between <MNO and PQR>",
    //            MyTypeOfConflict = "Track Obstruction",
    //            MySubtypeOfConflictMyDescription = "Track Obstruction Due to Fallen Tree",
    //            MyLocation = "StationZ@5PC_MNO(987654)(S_PT4_MNO_PT7_PQR)",
    //            MyEntityMyDescription = "Tree Fallen on Track",
    //            MyResolutionMyTypeOfResolution = "Clear Obstruction"
    //        }
    //      };
    //    }
    //}

    //public class DemoLoc
    //{
    //    public string Description { get; set; }
    //    public DateTime ArrivalTimeAdjusted { get; set; }
    //    public DateTime DepartureTimeAdjusted { get; set; }
    //}

    //public class DemoPlanData
    //{
    //    public static List<DemoLoc> getPlanTripData()
    //    {
    //        return new List<DemoLoc>
    //        {
    //            new DemoLoc
    //            {
    //                Description = "PV01_DAR",
    //                ArrivalTimeAdjusted = DateTime.Now,
    //                DepartureTimeAdjusted = DateTime.Now,
    //            },
    //             new DemoLoc
    //            {
    //                Description = "PV02_DAR",
    //                ArrivalTimeAdjusted = DateTime.Now,
    //                DepartureTimeAdjusted = DateTime.Now,
    //            },

    //              new DemoLoc
    //            {
    //                Description = "GA01_UAI",
    //                ArrivalTimeAdjusted = DateTime.Now,
    //                DepartureTimeAdjusted = DateTime.Now,
    //            },

    //               new DemoLoc
    //            {
    //                Description = "ABC_142",
    //                ArrivalTimeAdjusted = DateTime.Now,
    //                DepartureTimeAdjusted = DateTime.Now,
    //            },
    //        };
    //    }
    //}
}
