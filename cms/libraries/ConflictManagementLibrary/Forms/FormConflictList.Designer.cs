namespace ConflictManagementLibrary.Forms
{
    partial class FormConflictList
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConflictList));
            cmuShowReservations = new ContextMenuStrip(components);
            mnuShowReservations = new ToolStripMenuItem();
            mnuDeleteTrip = new ToolStripMenuItem();
            mnuSaveTrip = new ToolStripMenuItem();
            imageList1 = new ImageList(components);
            statusStrip1 = new StatusStrip();
            tssConflictResolutionStatus = new ToolStripStatusLabel();
            menuStrip1 = new MenuStrip();
            mnuControl = new ToolStripMenuItem();
            mnuAutoRouting = new ToolStripMenuItem();
            panel1 = new Panel();
            panel2 = new Panel();
            splitContainer1 = new SplitContainer();
            lvTrips = new ListView();
            colTrainService = new ColumnHeader();
            colTrainIdentifier = new ColumnHeader();
            colDirection = new ColumnHeader();
            colStartTime = new ColumnHeader();
            colTrainType = new ColumnHeader();
            colTrainSubtype = new ColumnHeader();
            colTrainLength = new ColumnHeader();
            colTrainPostfix = new ColumnHeader();
            colLocationCurrent = new ColumnHeader();
            coLocationNext = new ColumnHeader();
            colNumOfConflicts = new ColumnHeader();
            txtEvent = new RichTextBox();
            cmuEvent = new ContextMenuStrip(components);
            mnuClearEvents = new ToolStripMenuItem();
            mnuShowRoutePlan = new ToolStripMenuItem();
            cmuShowReservations.SuspendLayout();
            statusStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            cmuEvent.SuspendLayout();
            SuspendLayout();
            // 
            // cmuShowReservations
            // 
            cmuShowReservations.ImageScalingSize = new Size(32, 32);
            cmuShowReservations.Items.AddRange(new ToolStripItem[] { mnuShowReservations, mnuShowRoutePlan, mnuDeleteTrip, mnuSaveTrip });
            cmuShowReservations.Name = "cmuShowReservations";
            resources.ApplyResources(cmuShowReservations, "cmuShowReservations");
            // 
            // mnuShowReservations
            // 
            mnuShowReservations.Name = "mnuShowReservations";
            resources.ApplyResources(mnuShowReservations, "mnuShowReservations");
            mnuShowReservations.Click += mnuShowReservations_Click;
            // 
            // mnuDeleteTrip
            // 
            mnuDeleteTrip.Name = "mnuDeleteTrip";
            resources.ApplyResources(mnuDeleteTrip, "mnuDeleteTrip");
            mnuDeleteTrip.Click += mnuDeleteTrip_Click;
            // 
            // mnuSaveTrip
            // 
            mnuSaveTrip.Name = "mnuSaveTrip";
            resources.ApplyResources(mnuSaveTrip, "mnuSaveTrip");
            mnuSaveTrip.Click += mnuSaveTrip_Click;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth8Bit;
            resources.ApplyResources(imageList1, "imageList1");
            imageList1.TransparentColor = Color.Transparent;
            // 
            // statusStrip1
            // 
            resources.ApplyResources(statusStrip1, "statusStrip1");
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tssConflictResolutionStatus });
            statusStrip1.Name = "statusStrip1";
            // 
            // tssConflictResolutionStatus
            // 
            resources.ApplyResources(tssConflictResolutionStatus, "tssConflictResolutionStatus");
            tssConflictResolutionStatus.Name = "tssConflictResolutionStatus";
            // 
            // menuStrip1
            // 
            resources.ApplyResources(menuStrip1, "menuStrip1");
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { mnuControl });
            menuStrip1.Name = "menuStrip1";
            // 
            // mnuControl
            // 
            mnuControl.DropDownItems.AddRange(new ToolStripItem[] { mnuAutoRouting });
            resources.ApplyResources(mnuControl, "mnuControl");
            mnuControl.Name = "mnuControl";
            // 
            // mnuAutoRouting
            // 
            resources.ApplyResources(mnuAutoRouting, "mnuAutoRouting");
            mnuAutoRouting.Name = "mnuAutoRouting";
            mnuAutoRouting.Click += mnuAutoRouting_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(statusStrip1);
            resources.ApplyResources(panel1, "panel1");
            panel1.Name = "panel1";
            // 
            // panel2
            // 
            panel2.Controls.Add(splitContainer1);
            resources.ApplyResources(panel2, "panel2");
            panel2.Name = "panel2";
            // 
            // splitContainer1
            // 
            splitContainer1.Cursor = Cursors.HSplit;
            resources.ApplyResources(splitContainer1, "splitContainer1");
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(lvTrips);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(txtEvent);
            // 
            // lvTrips
            // 
            lvTrips.Activation = ItemActivation.OneClick;
            lvTrips.BackColor = Color.Gainsboro;
            lvTrips.Columns.AddRange(new ColumnHeader[] { colTrainService, colTrainIdentifier, colDirection, colStartTime, colTrainType, colTrainSubtype, colTrainLength, colTrainPostfix, colLocationCurrent, coLocationNext, colNumOfConflicts });
            lvTrips.ContextMenuStrip = cmuShowReservations;
            resources.ApplyResources(lvTrips, "lvTrips");
            lvTrips.FullRowSelect = true;
            lvTrips.GridLines = true;
            lvTrips.MultiSelect = false;
            lvTrips.Name = "lvTrips";
            lvTrips.ShowItemToolTips = true;
            lvTrips.UseCompatibleStateImageBehavior = false;
            lvTrips.View = View.Details;
            lvTrips.ColumnClick += lvTrips_ColumnClick;
            lvTrips.ItemActivate += lvTrips_ItemActivate;
            // 
            // colTrainService
            // 
            resources.ApplyResources(colTrainService, "colTrainService");
            // 
            // colTrainIdentifier
            // 
            resources.ApplyResources(colTrainIdentifier, "colTrainIdentifier");
            // 
            // colDirection
            // 
            resources.ApplyResources(colDirection, "colDirection");
            // 
            // colStartTime
            // 
            resources.ApplyResources(colStartTime, "colStartTime");
            // 
            // colTrainType
            // 
            resources.ApplyResources(colTrainType, "colTrainType");
            // 
            // colTrainSubtype
            // 
            resources.ApplyResources(colTrainSubtype, "colTrainSubtype");
            // 
            // colTrainLength
            // 
            resources.ApplyResources(colTrainLength, "colTrainLength");
            // 
            // colTrainPostfix
            // 
            resources.ApplyResources(colTrainPostfix, "colTrainPostfix");
            // 
            // colLocationCurrent
            // 
            resources.ApplyResources(colLocationCurrent, "colLocationCurrent");
            // 
            // coLocationNext
            // 
            resources.ApplyResources(coLocationNext, "coLocationNext");
            // 
            // colNumOfConflicts
            // 
            resources.ApplyResources(colNumOfConflicts, "colNumOfConflicts");
            // 
            // txtEvent
            // 
            txtEvent.ContextMenuStrip = cmuEvent;
            resources.ApplyResources(txtEvent, "txtEvent");
            txtEvent.Name = "txtEvent";
            // 
            // cmuEvent
            // 
            resources.ApplyResources(cmuEvent, "cmuEvent");
            cmuEvent.ImageScalingSize = new Size(20, 20);
            cmuEvent.Items.AddRange(new ToolStripItem[] { mnuClearEvents });
            cmuEvent.Name = "cmuEvent";
            // 
            // mnuClearEvents
            // 
            mnuClearEvents.Name = "mnuClearEvents";
            resources.ApplyResources(mnuClearEvents, "mnuClearEvents");
            mnuClearEvents.Click += mnuClearEvents_Click;
            // 
            // mnuShowRoutePlan
            // 
            mnuShowRoutePlan.Name = "mnuShowRoutePlan";
            resources.ApplyResources(mnuShowRoutePlan, "mnuShowRoutePlan");
            mnuShowRoutePlan.Click += mnuShowRoutePlan_Click;
            // 
            // FormConflictList
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MainMenuStrip = menuStrip1;
            Name = "FormConflictList";
            Load += FormConflictList_Load;
            cmuShowReservations.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            cmuEvent.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.ImageList imageList1;
        private ContextMenuStrip cmuShowReservations;
        private ToolStripMenuItem mnuShowReservations;
        private StatusStrip statusStrip1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem mnuControl;
        private ToolStripMenuItem mnuAutoRouting;
        private Panel panel1;
        private Panel panel2;
        private ToolStripStatusLabel tssConflictResolutionStatus;
        private ToolStripMenuItem mnuDeleteTrip;
        private SplitContainer splitContainer1;
        private ListView lvTrips;
        private ColumnHeader colTrainService;
        private ColumnHeader colTrainIdentifier;
        private ColumnHeader colDirection;
        private ColumnHeader colStartTime;
        private ColumnHeader colTrainType;
        private ColumnHeader colTrainSubtype;
        private ColumnHeader colTrainLength;
        private ColumnHeader colTrainPostfix;
        private ColumnHeader colLocationCurrent;
        private ColumnHeader coLocationNext;
        private ColumnHeader colNumOfConflicts;
        private RichTextBox txtEvent;
        private ContextMenuStrip cmuEvent;
        private ToolStripMenuItem mnuClearEvents;
        private ToolStripMenuItem mnuSaveTrip;
        private ToolStripMenuItem mnuShowRoutePlan;
    }
}