using ABI.System;
using ConflictManagementLibrary.Helpers;
using ConflictManagementLibrary.Model.Conflict;
using ConflictManagementLibrary.Model.Trip;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

//using System.Windows.Forms;
using Exception = System.Exception;
//using ListViewItem = System.Windows.Forms.ListViewItem;
using TreeView = Microsoft.UI.Xaml.Controls;

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
        }

        private void FormConflictDetails_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(CurrentUiCulture);
            InitializeForm();
        }

        private void InitializeForm()
        {
            if (MyTrip != null) this.Title += @" - Trip " + MyTrip.TripCode;
            //if (MyTrip != null) gbRouteDetails += @" - " + MyTrip.TripCode;
            //############## Added Loop for find child element
            foreach (var child in gbRouteDetails.Children)
            {
                if (child is TextBlock textBlock)
                {
                    textBlock.Text += " - " + MyTrip.TripCode;
                }
            }
        }

        public void AssociateTrip(Trip theTrip)
        {
            MyTrip = theTrip;
            DisplayCurrentConflictsInTreeView();
           // DisplayPlan();
        }

        //private void DisplayPlan()
        //{
        //    try
        //    {
        //        var i = 0;
        //        foreach (var tl in MyTrip.TimedLocations)
        //        {
        //            var shaded = Windows.UI.Colors.DarkGray; // Windows.UI.Color.FromArgb(240, 240, 240);
        //            var lvItem = new ListViewItem();
        //            lvItem.Content = tl.Description;
        //            lvItem.SubItems.Add(tl.ArrivalTimeAdjusted.ToString("dd/MM/yy HH:mm"));
        //            lvItem.SubItems.Add(tl.DepartureTimeAdjusted.ToString("dd/MM/yy HH:mm"));
        //            if (i++ % 2 == 1)
        //            {
        //                lvItem.Background = new Windows.UI.Xaml.Media.SolidColorBrush(shaded);
        //            }
        //            lvPlan.Items.Add(lvItem);
        //            lvItem.ToolTipText = GetToolTip(tl);

        //            // No need to call lvPlan.Refresh() in WinUI 3
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        GlobalDeclarations.MyLogger?.LogException(e.ToString());
        //    }
        //}

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
            //try
            //{
            //    foreach (var conflict in MyTrip.MyConflicts)
            //    {
            //        var theFont = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            //        //var nodeDateTime = new TreeNode(conflict.MyDateTimeCreated.ToString("yy-MMM-dd HH:mm:ss.ffff"));
            //        var nodeDateTime = new TreeNode(conflict.MyLocationDetail);

            //        //nodeDateTime.NodeFont = theFont;
            //        nodeDateTime.NodeFont = theFont;
            //        nodeDateTime.ForeColor = GetNodeColor(conflict);
            //        tvConflictsCurrent.SelectedNodes.Add((TreeViewNode)nodeDateTime);
            //        //tvConflictsCurrent.Nodes[0].Nodes.Add(nodeDateTime);
            //        //tvConflictsCurrent.SelectedNodes[0]. .Add(nodeDateTime);
            //        var nodeTypeConflict = new TreeNode("Conflict Type <" + conflict.MyTypeOfConflict + ">");
            //        nodeTypeConflict.NodeFont = theFont;
            //        nodeDateTime.Nodes.Add(nodeTypeConflict);
            //        var nodeSubtypeConflict = new TreeNode("Subtype <" + conflict.MySubtypeOfConflict.MyDescription + ">");
            //        nodeSubtypeConflict.NodeFont = theFont;
            //        nodeDateTime.Nodes.Add(nodeSubtypeConflict);
            //        var nodeLocation = new TreeNode("Location <" + conflict.MyLocation + ">");
            //        nodeLocation.NodeFont = theFont;
            //        nodeDateTime.Nodes.Add(nodeLocation);
            //        var nodeEntity = new TreeNode("Name Of Conflict Entity <" + conflict.MyEntity.MyDescription + ">");
            //        nodeEntity.NodeFont = theFont;
            //        nodeDateTime.Nodes.Add(nodeEntity);
            //        var nodeResolution = new TreeNode("Resolution <" + conflict.MyResolution.MyTypeOfResolution + ">");
            //        nodeResolution.NodeFont = theFont;
            //        nodeDateTime.Nodes.Add(nodeResolution);
            //    }
            //    //tvConflictsCurrent.Nodes[0].Expand();
            //    tvConflictsCurrent.SelectedNodes[0].IsExpanded = true;
            //}
            //catch (Exception e)
            //{
            //    GlobalDeclarations.MyLogger?.LogException(e.ToString());
            //}
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

        //private void tvConflictsPast_SelectionChanged(object sender, TreeViewEventArgs e)
        //{
        //    try
        //    {
        //        ClearConflictDetails();
        //        var tv = (System.Windows.Forms.TreeView)sender;
        //        DisplayPastConflictDetails(tv.SelectedNode);
        //    }
        //    catch (Exception exception)
        //    {
        //        MyLogger?.LogException(exception.ToString());
        //    }
        //}

        private void DisplayCurrentConflictDetails(Microsoft.UI.Xaml.Controls.TreeViewNode theNode)
        {
            //try
            //{
            //    var conflict = GetConflict(theNode.Content.ToString());
            //    if (conflict != null)
            //    {
            //        UpdateTextBoxColor(conflict);
            //        txtSubtype.Text = conflict.MySubtypeOfConflict.MyDescription.Trim();
            //        txtConflictLocation.Text = conflict.MyLocation;
            //        txtConflictTime.Text = conflict.MyDateTime.ToString("dd-MM HH:mm:ss");
            //        txtConflictEntity.Text = conflict.MyEntity.MyEntityType.ToString();
            //        txtConflictType.Text = conflict.MyTypeOfConflict.ToString();
            //        txtConflictDescription.Text = conflict.MyEntity.MyDescription;
            //        txtConflictResolution.Text = conflict.MyResolution.MyTypeOfResolution.ToString();
            //        currentConflict = conflict;
            //        if (conflict.MyReservation != null && conflict.MyReservation.MyTimedLocation != null)
            //            UpdateTripPlanList(conflict.MyReservation.MyTimedLocation.Description);
            //        if (!conflict.IsResolvable)
            //        {
            //            btnAccept.Visibility = Visibility.Visible;
            //            btnReject.Visibility = Visibility.Visible;
            //        }
            //        else
            //        {
            //            btnAccept.Visibility = Visibility.Visible;
            //            btnReject.Visibility = Visibility.Visible;
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    GlobalDeclarations.MyLogger?.LogException(e.ToString());
            //}
        }

        private void UpdateTripPlanList(string platformName)
        {
            //try
            //{
            //    foreach (var lv in lvPlan.Items)
            //    {
            //        var theItem = (ListViewItem)lv;
            //        if (theItem.Text == platformName) theItem.ForeColor = GetNodeColor(currentConflict!);
            //    }
            //}
            //catch (Exception e)
            //{
            //    GlobalDeclarations.MyLogger?.LogException(e.ToString());
            //}
        }
        private void DisplayPastConflictDetails(TreeNode theNode)
        {
            try
            {
                var conflict = GetPastConflict(theNode.Text);
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
            //try
            //{
            //    foreach (var lv in lvPlan.Items)
            //    {
            //        var theItem = (ListViewItem)lv;
            //        theItem.ForeColor = Color.Black;
            //    }
            //}
            //catch (Exception e)
            //{
            //    GlobalDeclarations.MyLogger?.LogException(e.ToString());
            //}
        }
        private Conflict? GetConflict(string theConflict)
        {
            try
            {
                foreach (var conflict in MyTrip!.MyConflicts)
                {
                    if (theConflict != conflict.MyLocationDetail) continue;
                    return conflict;
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
                //foreach (var conflict in AddConflict.MyConflictsPast)
                //{
                //    var stringDateTime = conflict.MyDateTime.ToString("yy-MMM-dd HH:mm:ss");
                //    if (theConflictTime != stringDateTime) continue;
                //    return conflict;
                //}
            }
            catch (Exception e)
            {
                GlobalDeclarations.MyLogger?.LogException(e.ToString());
            }
            return null;
        }
        private void tvConflictsCurrent_Leave(object sender, EventArgs e)
        {
            //ClearConflictDetails();
        }
        private static Color GetNodeColor(Conflict theConflict)
        {
            if (!theConflict.IsConflicting) return Color.Black;
            if (!theConflict.IsResolvable && (!theConflict.IsAccepted && !theConflict.IsRejected)) return Color.Red;
            if (theConflict.IsAccepted) return Color.Green;
            if (theConflict.IsRejected) return Color.Blue;
            return Color.Red;
        }
        private void UpdateTextBoxColor(Conflict theConflict)
        {
            //var theColor = GetNodeColor(theConflict);
            //foreach (var control in gbConflictSummary.Children)
            //{
            //    if (control.GetType() == typeof(System.Windows.Forms.TextBox))
            //    {
            //        var tb = (System.Windows.Forms.TextBox)control;
            //        tb.ForeColor = theColor;
            //    }
            //}


            Color theColor = GetNodeColor(theConflict);
            Windows.UI.Color winUIColor = Windows.UI.Color.FromArgb(theColor.A, theColor.R, theColor.G, theColor.B);
            SolidColorBrush theBrush = new SolidColorBrush(winUIColor);
            foreach (var control in gbConflictSummary.Children)
            {
                if (control is TextBlock)
                {
                    var tb = (TextBlock)control;
                    tb.Foreground = theBrush;
                }
            }
        }

        private void btnAccept_Click(object sender, EventArgs e)
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
        private void btnReject_Click(object sender, EventArgs e)
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
            //ClearConflictDetails();
        }

    }
}
