namespace ConflictManagementLibrary.Forms
{
    partial class FormReservation
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormReservation));
            this.lvReservations = new System.Windows.Forms.ListView();
            this.colStation = new System.Windows.Forms.ColumnHeader();
            this.colNode = new System.Windows.Forms.ColumnHeader();
            this.colLink = new System.Windows.Forms.ColumnHeader();
            this.colEdge = new System.Windows.Forms.ColumnHeader();
            this.colRoute = new System.Windows.Forms.ColumnHeader();
            this.colBegin = new System.Windows.Forms.ColumnHeader();
            this.colEnd = new System.Windows.Forms.ColumnHeader();
            this.colTimeBegin = new System.Windows.Forms.ColumnHeader();
            this.colTimeEnd = new System.Windows.Forms.ColumnHeader();
            this.colTotalTime = new System.Windows.Forms.ColumnHeader();
            this.colDwellTime = new System.Windows.Forms.ColumnHeader();
            this.cmuRoutes = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuSendRoute = new System.Windows.Forms.ToolStripMenuItem();
            this.gerenateConflictToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuConflictTrackBlocked = new System.Windows.Forms.ToolStripMenuItem();
            this.cmuRoutes.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvReservations
            // 
            resources.ApplyResources(this.lvReservations, "lvReservations");
            this.lvReservations.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.lvReservations.BackColor = System.Drawing.Color.Gainsboro;
            this.lvReservations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colStation,
            this.colNode,
            this.colLink,
            this.colEdge,
            this.colRoute,
            this.colBegin,
            this.colEnd,
            this.colTimeBegin,
            this.colTimeEnd,
            this.colTotalTime,
            this.colDwellTime});
            this.lvReservations.ContextMenuStrip = this.cmuRoutes;
            this.lvReservations.FullRowSelect = true;
            this.lvReservations.GridLines = true;
            this.lvReservations.HotTracking = true;
            this.lvReservations.HoverSelection = true;
            this.lvReservations.Name = "lvReservations";
            this.lvReservations.ShowItemToolTips = true;
            this.lvReservations.UseCompatibleStateImageBehavior = false;
            this.lvReservations.View = System.Windows.Forms.View.Details;
            // 
            // colStation
            // 
            resources.ApplyResources(this.colStation, "colStation");
            // 
            // colNode
            // 
            resources.ApplyResources(this.colNode, "colNode");
            // 
            // colLink
            // 
            resources.ApplyResources(this.colLink, "colLink");
            // 
            // colEdge
            // 
            resources.ApplyResources(this.colEdge, "colEdge");
            // 
            // colRoute
            // 
            resources.ApplyResources(this.colRoute, "colRoute");
            // 
            // colBegin
            // 
            resources.ApplyResources(this.colBegin, "colBegin");
            // 
            // colEnd
            // 
            resources.ApplyResources(this.colEnd, "colEnd");
            // 
            // colTimeBegin
            // 
            resources.ApplyResources(this.colTimeBegin, "colTimeBegin");
            // 
            // colTimeEnd
            // 
            resources.ApplyResources(this.colTimeEnd, "colTimeEnd");
            // 
            // colTotalTime
            // 
            resources.ApplyResources(this.colTotalTime, "colTotalTime");
            // 
            // colDwellTime
            // 
            resources.ApplyResources(this.colDwellTime, "colDwellTime");
            // 
            // cmuRoutes
            // 
            resources.ApplyResources(this.cmuRoutes, "cmuRoutes");
            this.cmuRoutes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuSendRoute,
            this.gerenateConflictToolStripMenuItem});
            this.cmuRoutes.Name = "cmuRoutes";
            // 
            // mnuSendRoute
            // 
            resources.ApplyResources(this.mnuSendRoute, "mnuSendRoute");
            this.mnuSendRoute.Name = "mnuSendRoute";
            this.mnuSendRoute.Click += new System.EventHandler(this.mnuSendRoute_Click);
            // 
            // gerenateConflictToolStripMenuItem
            // 
            resources.ApplyResources(this.gerenateConflictToolStripMenuItem, "gerenateConflictToolStripMenuItem");
            this.gerenateConflictToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuConflictTrackBlocked});
            this.gerenateConflictToolStripMenuItem.Name = "gerenateConflictToolStripMenuItem";
            // 
            // mnuConflictTrackBlocked
            // 
            resources.ApplyResources(this.mnuConflictTrackBlocked, "mnuConflictTrackBlocked");
            this.mnuConflictTrackBlocked.Name = "mnuConflictTrackBlocked";
            // 
            // FormReservation
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.lvReservations);
            this.Name = "FormReservation";
            this.cmuRoutes.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private ListView lvReservations;
        private ColumnHeader colLink;
        private ColumnHeader colEdge;
        private ColumnHeader colRoute;
        private ColumnHeader colTimeBegin;
        private ColumnHeader colTimeEnd;
        private ColumnHeader colTotalTime;
        private ColumnHeader colStation;
        private ColumnHeader colNode;
        private ColumnHeader colBegin;
        private ColumnHeader colEnd;
        private ColumnHeader colDwellTime;
        private ContextMenuStrip cmuRoutes;
        private ToolStripMenuItem mnuSendRoute;
        private ToolStripMenuItem gerenateConflictToolStripMenuItem;
        private ToolStripMenuItem mnuConflictTrackBlocked;
    }
}