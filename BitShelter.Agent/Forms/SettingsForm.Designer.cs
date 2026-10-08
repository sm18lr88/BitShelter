namespace BitShelter
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
      this.components = new System.ComponentModel.Container();
      this.btnAddSchedule = new System.Windows.Forms.Button();
      this.dgSnapshotRules = new System.Windows.Forms.DataGridView();
      this.plSvcConnection = new System.Windows.Forms.Panel();
      this.lblConnWait = new System.Windows.Forms.Label();
      this.retryConnTimer = new System.Windows.Forms.Timer(this.components);
      ((System.ComponentModel.ISupportInitialize)(this.dgSnapshotRules)).BeginInit();
      this.plSvcConnection.SuspendLayout();
      this.SuspendLayout();
      // 
      // btnAddSchedule
      // 
      this.btnAddSchedule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnAddSchedule.Location = new System.Drawing.Point(507, 2);
      this.btnAddSchedule.Name = "btnAddSchedule";
      this.btnAddSchedule.Size = new System.Drawing.Size(144, 28);
      this.btnAddSchedule.TabIndex = 5;
      this.btnAddSchedule.Text = "Add Schedule";
      this.btnAddSchedule.UseVisualStyleBackColor = true;
      this.btnAddSchedule.Click += new System.EventHandler(this.btnAddSchedule_Click);
      // 
      // dgSnapshotRules
      // 
      this.dgSnapshotRules.AllowUserToAddRows = false;
      this.dgSnapshotRules.AllowUserToDeleteRows = false;
      this.dgSnapshotRules.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.dgSnapshotRules.AllowUserToOrderColumns = true;
      this.dgSnapshotRules.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
      this.dgSnapshotRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.dgSnapshotRules.Location = new System.Drawing.Point(4, 36);
      this.dgSnapshotRules.MultiSelect = false;
      this.dgSnapshotRules.Name = "dgSnapshotRules";
      this.dgSnapshotRules.ReadOnly = true;
      this.dgSnapshotRules.Size = new System.Drawing.Size(647, 410);
      this.dgSnapshotRules.TabIndex = 6;
      // 
      // plSvcConnection
      // 
      this.plSvcConnection.Controls.Add(this.lblConnWait);
      this.plSvcConnection.Dock = System.Windows.Forms.DockStyle.Fill;
      this.plSvcConnection.Enabled = false;
      this.plSvcConnection.Location = new System.Drawing.Point(0, 0);
      this.plSvcConnection.Name = "plSvcConnection";
      this.plSvcConnection.Size = new System.Drawing.Size(656, 450);
      this.plSvcConnection.TabIndex = 7;
      this.plSvcConnection.Visible = false;
      // 
      // lblConnWait
      // 
      this.lblConnWait.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lblConnWait.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblConnWait.Location = new System.Drawing.Point(0, 0);
      this.lblConnWait.Name = "lblConnWait";
      this.lblConnWait.Padding = new System.Windows.Forms.Padding(24, 60, 24, 0);
      this.lblConnWait.Size = new System.Drawing.Size(656, 450);
      this.lblConnWait.TabIndex = 2;
      this.lblConnWait.Text = "Connecting to the BitShelter service...";
      this.lblConnWait.TextAlign = System.Drawing.ContentAlignment.TopCenter;
      // 
      // retryConnTimer
      // 
      this.retryConnTimer.Interval = 1000;
      this.retryConnTimer.Tick += new System.EventHandler(this.retryConnTimer_Tick);
      // 
      // SettingsForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(656, 450);
      this.Controls.Add(this.dgSnapshotRules);
      this.Controls.Add(this.btnAddSchedule);
      this.Controls.Add(this.plSvcConnection);
      this.Name = "SettingsForm";
      this.MinimumSize = new System.Drawing.Size(480, 320);
      this.Text = "BitShelter - Settings";
      ((System.ComponentModel.ISupportInitialize)(this.dgSnapshotRules)).EndInit();
      this.plSvcConnection.ResumeLayout(false);
      this.ResumeLayout(false);

        }

    #endregion
    private System.Windows.Forms.Button btnAddSchedule;
    private System.Windows.Forms.DataGridView dgSnapshotRules;
    private System.Windows.Forms.Panel plSvcConnection;
    private System.Windows.Forms.Label lblConnWait;
    private System.Windows.Forms.Timer retryConnTimer;
  }
}