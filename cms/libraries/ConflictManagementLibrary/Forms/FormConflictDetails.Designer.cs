namespace ConflictManagementLibrary.Forms
{
    partial class FormConflictDetails
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConflictDetails));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tvConflictsCurrent = new System.Windows.Forms.TreeView();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.gbConflictSummary = new System.Windows.Forms.GroupBox();
            this.btnReject = new System.Windows.Forms.Button();
            this.txtConflictDescription = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnAccept = new System.Windows.Forms.Button();
            this.txtConflictResolution = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtConflictEntity = new System.Windows.Forms.TextBox();
            this.lblConflictEntity = new System.Windows.Forms.Label();
            this.txtConflictTime = new System.Windows.Forms.TextBox();
            this.lblConflictTime = new System.Windows.Forms.Label();
            this.txtConflictLocation = new System.Windows.Forms.TextBox();
            this.lblConflictLocation = new System.Windows.Forms.Label();
            this.txtSubtype = new System.Windows.Forms.TextBox();
            this.lblSubtype = new System.Windows.Forms.Label();
            this.txtConflictType = new System.Windows.Forms.TextBox();
            this.lblConflictType = new System.Windows.Forms.Label();
            this.gbRouteDetails = new System.Windows.Forms.GroupBox();
            this.lvPlan = new System.Windows.Forms.ListView();
            this.colLocation = new System.Windows.Forms.ColumnHeader();
            this.colArrival = new System.Windows.Forms.ColumnHeader();
            this.colDepart = new System.Windows.Forms.ColumnHeader();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.gbConflictSummary.SuspendLayout();
            this.gbRouteDetails.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            resources.ApplyResources(this.splitContainer1.Panel1, "splitContainer1.Panel1");
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            resources.ApplyResources(this.splitContainer1.Panel2, "splitContainer1.Panel2");
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2
            // 
            resources.ApplyResources(this.splitContainer2, "splitContainer2");
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            resources.ApplyResources(this.splitContainer2.Panel1, "splitContainer2.Panel1");
            this.splitContainer2.Panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer2.Panel1.Controls.Add(this.tvConflictsCurrent);
            // 
            // splitContainer2.Panel2
            // 
            resources.ApplyResources(this.splitContainer2.Panel2, "splitContainer2.Panel2");
            this.splitContainer2.Panel2.BackColor = System.Drawing.Color.LightGray;
            // 
            // tvConflictsCurrent
            // 
            resources.ApplyResources(this.tvConflictsCurrent, "tvConflictsCurrent");
            this.tvConflictsCurrent.BackColor = System.Drawing.Color.LightGray;
            this.tvConflictsCurrent.Name = "tvConflictsCurrent";
            this.tvConflictsCurrent.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            ((System.Windows.Forms.TreeNode)(resources.GetObject("tvConflictsCurrent.Nodes")))});
            this.tvConflictsCurrent.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvConflictsCurrent_AfterSelect);
            this.tvConflictsCurrent.Leave += new System.EventHandler(this.tvConflictsCurrent_Leave);
            // 
            // splitContainer3
            // 
            resources.ApplyResources(this.splitContainer3, "splitContainer3");
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            resources.ApplyResources(this.splitContainer3.Panel1, "splitContainer3.Panel1");
            this.splitContainer3.Panel1.BackColor = System.Drawing.Color.LightGray;
            this.splitContainer3.Panel1.Controls.Add(this.gbConflictSummary);
            // 
            // splitContainer3.Panel2
            // 
            resources.ApplyResources(this.splitContainer3.Panel2, "splitContainer3.Panel2");
            this.splitContainer3.Panel2.BackColor = System.Drawing.Color.LightGray;
            this.splitContainer3.Panel2.Controls.Add(this.gbRouteDetails);
            // 
            // gbConflictSummary
            // 
            resources.ApplyResources(this.gbConflictSummary, "gbConflictSummary");
            this.gbConflictSummary.Controls.Add(this.btnReject);
            this.gbConflictSummary.Controls.Add(this.txtConflictDescription);
            this.gbConflictSummary.Controls.Add(this.label2);
            this.gbConflictSummary.Controls.Add(this.btnAccept);
            this.gbConflictSummary.Controls.Add(this.txtConflictResolution);
            this.gbConflictSummary.Controls.Add(this.label1);
            this.gbConflictSummary.Controls.Add(this.txtConflictEntity);
            this.gbConflictSummary.Controls.Add(this.lblConflictEntity);
            this.gbConflictSummary.Controls.Add(this.txtConflictTime);
            this.gbConflictSummary.Controls.Add(this.lblConflictTime);
            this.gbConflictSummary.Controls.Add(this.txtConflictLocation);
            this.gbConflictSummary.Controls.Add(this.lblConflictLocation);
            this.gbConflictSummary.Controls.Add(this.txtSubtype);
            this.gbConflictSummary.Controls.Add(this.lblSubtype);
            this.gbConflictSummary.Controls.Add(this.txtConflictType);
            this.gbConflictSummary.Controls.Add(this.lblConflictType);
            this.gbConflictSummary.ForeColor = System.Drawing.Color.Black;
            this.gbConflictSummary.Name = "gbConflictSummary";
            this.gbConflictSummary.TabStop = false;
            // 
            // btnReject
            // 
            resources.ApplyResources(this.btnReject, "btnReject");
            this.btnReject.ForeColor = System.Drawing.Color.SteelBlue;
            this.btnReject.Name = "btnReject";
            this.btnReject.UseVisualStyleBackColor = true;
            this.btnReject.Click += new System.EventHandler(this.btnReject_Click);
            // 
            // txtConflictDescription
            // 
            resources.ApplyResources(this.txtConflictDescription, "txtConflictDescription");
            this.txtConflictDescription.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictDescription.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictDescription.Name = "txtConflictDescription";
            this.txtConflictDescription.ReadOnly = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.ForeColor = System.Drawing.Color.DimGray;
            this.label2.Name = "label2";
            // 
            // btnAccept
            // 
            resources.ApplyResources(this.btnAccept, "btnAccept");
            this.btnAccept.ForeColor = System.Drawing.Color.SteelBlue;
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
            // 
            // txtConflictResolution
            // 
            resources.ApplyResources(this.txtConflictResolution, "txtConflictResolution");
            this.txtConflictResolution.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictResolution.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictResolution.Name = "txtConflictResolution";
            this.txtConflictResolution.ReadOnly = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.ForeColor = System.Drawing.Color.DimGray;
            this.label1.Name = "label1";
            // 
            // txtConflictEntity
            // 
            resources.ApplyResources(this.txtConflictEntity, "txtConflictEntity");
            this.txtConflictEntity.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictEntity.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictEntity.Name = "txtConflictEntity";
            this.txtConflictEntity.ReadOnly = true;
            // 
            // lblConflictEntity
            // 
            resources.ApplyResources(this.lblConflictEntity, "lblConflictEntity");
            this.lblConflictEntity.ForeColor = System.Drawing.Color.DimGray;
            this.lblConflictEntity.Name = "lblConflictEntity";
            // 
            // txtConflictTime
            // 
            resources.ApplyResources(this.txtConflictTime, "txtConflictTime");
            this.txtConflictTime.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictTime.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictTime.Name = "txtConflictTime";
            this.txtConflictTime.ReadOnly = true;
            // 
            // lblConflictTime
            // 
            resources.ApplyResources(this.lblConflictTime, "lblConflictTime");
            this.lblConflictTime.ForeColor = System.Drawing.Color.DimGray;
            this.lblConflictTime.Name = "lblConflictTime";
            // 
            // txtConflictLocation
            // 
            resources.ApplyResources(this.txtConflictLocation, "txtConflictLocation");
            this.txtConflictLocation.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictLocation.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictLocation.Name = "txtConflictLocation";
            this.txtConflictLocation.ReadOnly = true;
            // 
            // lblConflictLocation
            // 
            resources.ApplyResources(this.lblConflictLocation, "lblConflictLocation");
            this.lblConflictLocation.ForeColor = System.Drawing.Color.DimGray;
            this.lblConflictLocation.Name = "lblConflictLocation";
            // 
            // txtSubtype
            // 
            resources.ApplyResources(this.txtSubtype, "txtSubtype");
            this.txtSubtype.BackColor = System.Drawing.Color.LightGray;
            this.txtSubtype.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtSubtype.Name = "txtSubtype";
            this.txtSubtype.ReadOnly = true;
            // 
            // lblSubtype
            // 
            resources.ApplyResources(this.lblSubtype, "lblSubtype");
            this.lblSubtype.ForeColor = System.Drawing.Color.DimGray;
            this.lblSubtype.Name = "lblSubtype";
            // 
            // txtConflictType
            // 
            resources.ApplyResources(this.txtConflictType, "txtConflictType");
            this.txtConflictType.BackColor = System.Drawing.Color.LightGray;
            this.txtConflictType.ForeColor = System.Drawing.Color.ForestGreen;
            this.txtConflictType.Name = "txtConflictType";
            this.txtConflictType.ReadOnly = true;
            // 
            // lblConflictType
            // 
            resources.ApplyResources(this.lblConflictType, "lblConflictType");
            this.lblConflictType.ForeColor = System.Drawing.Color.DimGray;
            this.lblConflictType.Name = "lblConflictType";
            // 
            // gbRouteDetails
            // 
            resources.ApplyResources(this.gbRouteDetails, "gbRouteDetails");
            this.gbRouteDetails.Controls.Add(this.lvPlan);
            this.gbRouteDetails.Name = "gbRouteDetails";
            this.gbRouteDetails.TabStop = false;
            // 
            // lvPlan
            // 
            resources.ApplyResources(this.lvPlan, "lvPlan");
            this.lvPlan.BackColor = System.Drawing.Color.LightGray;
            this.lvPlan.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colLocation,
            this.colArrival,
            this.colDepart});
            this.lvPlan.FullRowSelect = true;
            this.lvPlan.GridLines = true;
            this.lvPlan.Name = "lvPlan";
            this.lvPlan.ShowItemToolTips = true;
            this.lvPlan.UseCompatibleStateImageBehavior = false;
            this.lvPlan.View = System.Windows.Forms.View.Details;
            // 
            // colLocation
            // 
            resources.ApplyResources(this.colLocation, "colLocation");
            // 
            // colArrival
            // 
            resources.ApplyResources(this.colArrival, "colArrival");
            // 
            // colDepart
            // 
            resources.ApplyResources(this.colDepart, "colDepart");
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // FormConflictDetails
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.panel1);
            this.Name = "FormConflictDetails";
            this.Load += new System.EventHandler(this.FormConflictDetails_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.gbConflictSummary.ResumeLayout(false);
            this.gbConflictSummary.PerformLayout();
            this.gbRouteDetails.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.TreeView tvConflictsCurrent;
        private System.Windows.Forms.GroupBox gbConflictSummary;
        private System.Windows.Forms.GroupBox gbRouteDetails;
        private System.Windows.Forms.TextBox txtSubtype;
        private System.Windows.Forms.Label lblSubtype;
        private System.Windows.Forms.TextBox txtConflictType;
        private System.Windows.Forms.Label lblConflictType;
        private System.Windows.Forms.TextBox txtConflictEntity;
        private System.Windows.Forms.Label lblConflictEntity;
        private System.Windows.Forms.TextBox txtConflictTime;
        private System.Windows.Forms.Label lblConflictTime;
        private System.Windows.Forms.TextBox txtConflictLocation;
        private System.Windows.Forms.Label lblConflictLocation;
        private System.Windows.Forms.TextBox txtConflictResolution;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView lvPlan;
        private System.Windows.Forms.ColumnHeader colLocation;
        private System.Windows.Forms.ColumnHeader colArrival;
        private System.Windows.Forms.ColumnHeader colDepart;
        private System.Windows.Forms.Button btnAccept;
        private System.Windows.Forms.TextBox txtConflictDescription;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnReject;

    }
}