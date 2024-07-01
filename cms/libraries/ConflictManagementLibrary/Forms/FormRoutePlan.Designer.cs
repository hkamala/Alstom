namespace ConflictManagementLibrary.Forms
{
    partial class FormRoutePlan
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
            lvRoute = new ListView();
            colName = new ColumnHeader();
            colFrom = new ColumnHeader();
            colTo = new ColumnHeader();
            colTrigger = new ColumnHeader();
            colSent = new ColumnHeader();
            colRos = new ColumnHeader();
            colSig = new ColumnHeader();
            colPast = new ColumnHeader();
            SuspendLayout();
            // 
            // lvRoute
            // 
            lvRoute.Activation = ItemActivation.OneClick;
            lvRoute.BackColor = Color.LightGray;
            lvRoute.Columns.AddRange(new ColumnHeader[] { colName, colFrom, colTo, colTrigger, colPast, colSent, colRos, colSig });
            lvRoute.Dock = DockStyle.Fill;
            lvRoute.FullRowSelect = true;
            lvRoute.GridLines = true;
            lvRoute.HotTracking = true;
            lvRoute.HoverSelection = true;
            lvRoute.Location = new Point(0, 0);
            lvRoute.Name = "lvRoute";
            lvRoute.Size = new Size(1504, 849);
            lvRoute.TabIndex = 0;
            lvRoute.UseCompatibleStateImageBehavior = false;
            lvRoute.View = View.Details;
            // 
            // colName
            // 
            colName.Text = "Route Name";
            colName.Width = 200;
            // 
            // colFrom
            // 
            colFrom.Text = "From Platform";
            colFrom.Width = 200;
            // 
            // colTo
            // 
            colTo.Text = "To Platform";
            colTo.Width = 200;
            // 
            // colTrigger
            // 
            colTrigger.Text = "Trigger Point";
            colTrigger.Width = 200;
            // 
            // colSent
            // 
            colSent.Text = "Sent To ROS";
            colSent.Width = 150;
            // 
            // colRos
            // 
            colRos.Text = "ROS Confirmed";
            colRos.Width = 150;
            // 
            // colSig
            // 
            colSig.Text = "Signal Cleared";
            colSig.Width = 150;
            // 
            // colPast
            // 
            colPast.Text = "On/Past Trigger Point";
            colPast.Width = 200;
            // 
            // FormRoutePlan
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1504, 849);
            Controls.Add(lvRoute);
            Name = "FormRoutePlan";
            Text = "FormRoutePlan";
            ResumeLayout(false);
        }

        #endregion

        private ListView lvRoute;
        private ColumnHeader colName;
        private ColumnHeader colFrom;
        private ColumnHeader colTo;
        private ColumnHeader colTrigger;
        private ColumnHeader colSent;
        private ColumnHeader colRos;
        private ColumnHeader colSig;
        private ColumnHeader colPast;
    }
}