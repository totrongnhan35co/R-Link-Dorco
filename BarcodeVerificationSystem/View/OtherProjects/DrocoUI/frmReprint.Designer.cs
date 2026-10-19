using System.Windows.Forms;

namespace BarcodeVerificationSystem.View.OtherProjects.DrocoUI
{
    partial class frmReprint
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmReprint));
            this.tabRePrint = new System.Windows.Forms.TabControl();
            this.tabRePrintQr = new System.Windows.Forms.TabPage();
            this.btnSendQrRePrint = new System.Windows.Forms.Button();
            this.txtQrReprint = new DesignUI.CuzUI.CuzTextBox();
            this.btnResetValueQr = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dgvListPalletQrReprint = new System.Windows.Forms.DataGridView();
            this.dgvListCartonQrReprint = new System.Windows.Forms.DataGridView();
            this.label2 = new System.Windows.Forms.Label();
            this.dgvListBoxQrReprint = new System.Windows.Forms.DataGridView();
            this.tabRePrint.SuspendLayout();
            this.tabRePrintQr.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListPalletQrReprint)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListCartonQrReprint)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListBoxQrReprint)).BeginInit();
            this.SuspendLayout();
            // 
            // tabRePrint
            // 
            this.tabRePrint.Controls.Add(this.tabRePrintQr);
            this.tabRePrint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabRePrint.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabRePrint.Location = new System.Drawing.Point(0, 0);
            this.tabRePrint.Name = "tabRePrint";
            this.tabRePrint.SelectedIndex = 0;
            this.tabRePrint.Size = new System.Drawing.Size(1701, 661);
            this.tabRePrint.TabIndex = 0;
            // 
            // tabRePrintQr
            // 
            this.tabRePrintQr.Controls.Add(this.btnSendQrRePrint);
            this.tabRePrintQr.Controls.Add(this.txtQrReprint);
            this.tabRePrintQr.Controls.Add(this.btnResetValueQr);
            this.tabRePrintQr.Controls.Add(this.label4);
            this.tabRePrintQr.Controls.Add(this.label3);
            this.tabRePrintQr.Controls.Add(this.label1);
            this.tabRePrintQr.Controls.Add(this.dgvListPalletQrReprint);
            this.tabRePrintQr.Controls.Add(this.dgvListCartonQrReprint);
            this.tabRePrintQr.Controls.Add(this.label2);
            this.tabRePrintQr.Controls.Add(this.dgvListBoxQrReprint);
            this.tabRePrintQr.Location = new System.Drawing.Point(4, 29);
            this.tabRePrintQr.Name = "tabRePrintQr";
            this.tabRePrintQr.Padding = new System.Windows.Forms.Padding(3);
            this.tabRePrintQr.Size = new System.Drawing.Size(1693, 628);
            this.tabRePrintQr.TabIndex = 1;
            this.tabRePrintQr.Text = "In lại mã Hộp/Thùng/Pallet";
            this.tabRePrintQr.UseVisualStyleBackColor = true;
            // 
            // btnSendQrRePrint
            // 
            this.btnSendQrRePrint.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnSendQrRePrint.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSendQrRePrint.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnSendQrRePrint.Location = new System.Drawing.Point(889, 2);
            this.btnSendQrRePrint.Name = "btnSendQrRePrint";
            this.btnSendQrRePrint.Size = new System.Drawing.Size(100, 40);
            this.btnSendQrRePrint.TabIndex = 163;
            this.btnSendQrRePrint.Text = "In lại";
            this.btnSendQrRePrint.UseVisualStyleBackColor = false;
            this.btnSendQrRePrint.Click += new System.EventHandler(this.btnSendQrRePrint_Click);
            // 
            // txtQrReprint
            // 
            this.txtQrReprint._ReadOnlyBackColor = System.Drawing.Color.WhiteSmoke;
            this.txtQrReprint._ReadOnlyBorderFocusColor = System.Drawing.Color.Gainsboro;
            this.txtQrReprint.BackColor = System.Drawing.SystemColors.Window;
            this.txtQrReprint.BorderColor = System.Drawing.SystemColors.ScrollBar;
            this.txtQrReprint.BorderFocusColor = System.Drawing.Color.Silver;
            this.txtQrReprint.BorderRadius = 6;
            this.txtQrReprint.BorderSize = 1;
            this.txtQrReprint.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.txtQrReprint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.txtQrReprint.Location = new System.Drawing.Point(153, 5);
            this.txtQrReprint.Margin = new System.Windows.Forms.Padding(4);
            this.txtQrReprint.Multiline = false;
            this.txtQrReprint.Name = "txtQrReprint";
            this.txtQrReprint.Padding = new System.Windows.Forms.Padding(10, 7, 10, 7);
            this.txtQrReprint.PasswordChar = false;
            this.txtQrReprint.PlaceholderColor = System.Drawing.Color.DarkGray;
            this.txtQrReprint.PlaceholderText = "";
            this.txtQrReprint.ReadOnly = false;
            this.txtQrReprint.Size = new System.Drawing.Size(610, 35);
            this.txtQrReprint.TabIndex = 162;
            this.txtQrReprint.UnderlinedStyle = false;
            // 
            // btnResetValueQr
            // 
            this.btnResetValueQr.BackColor = System.Drawing.Color.ForestGreen;
            this.btnResetValueQr.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResetValueQr.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnResetValueQr.Location = new System.Drawing.Point(784, 3);
            this.btnResetValueQr.Name = "btnResetValueQr";
            this.btnResetValueQr.Size = new System.Drawing.Size(100, 40);
            this.btnResetValueQr.TabIndex = 160;
            this.btnResetValueQr.Text = "Làm mới";
            this.btnResetValueQr.UseVisualStyleBackColor = false;
            this.btnResetValueQr.Click += new System.EventHandler(this.btnResetValueQr_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(1130, 56);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(183, 20);
            this.label4.TabIndex = 9;
            this.label4.Text = "Danh sách mã QR Pallet";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(567, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(185, 20);
            this.label3.TabIndex = 8;
            this.label3.Text = "Danh sách mã QR thùng";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(8, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(171, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Danh sách mã QR hộp";
            // 
            // dgvListPalletQrReprint
            // 
            this.dgvListPalletQrReprint.AllowUserToAddRows = false;
            this.dgvListPalletQrReprint.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            this.dgvListPalletQrReprint.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvListPalletQrReprint.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvListPalletQrReprint.BackgroundColor = System.Drawing.Color.White;
            this.dgvListPalletQrReprint.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvListPalletQrReprint.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.dgvListPalletQrReprint.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvListPalletQrReprint.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvListPalletQrReprint.ColumnHeadersHeight = 45;
            this.dgvListPalletQrReprint.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvListPalletQrReprint.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvListPalletQrReprint.EnableHeadersVisualStyles = false;
            this.dgvListPalletQrReprint.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(237)))), ((int)(((byte)(240)))));
            this.dgvListPalletQrReprint.Location = new System.Drawing.Point(1134, 83);
            this.dgvListPalletQrReprint.MultiSelect = false;
            this.dgvListPalletQrReprint.Name = "dgvListPalletQrReprint";
            this.dgvListPalletQrReprint.ReadOnly = true;
            this.dgvListPalletQrReprint.RowHeadersVisible = false;
            this.dgvListPalletQrReprint.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvListPalletQrReprint.RowTemplate.Height = 35;
            this.dgvListPalletQrReprint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvListPalletQrReprint.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvListPalletQrReprint.Size = new System.Drawing.Size(540, 520);
            this.dgvListPalletQrReprint.TabIndex = 6;
            this.dgvListPalletQrReprint.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvListPalletQrReprint_CellClick);
            this.dgvListPalletQrReprint.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_CellMouseDown);
            this.dgvListPalletQrReprint.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_ColumnHeaderMouseClick);
            // 
            // dgvListCartonQrReprint
            // 
            this.dgvListCartonQrReprint.AllowUserToAddRows = false;
            this.dgvListCartonQrReprint.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            this.dgvListCartonQrReprint.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvListCartonQrReprint.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvListCartonQrReprint.BackgroundColor = System.Drawing.Color.White;
            this.dgvListCartonQrReprint.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvListCartonQrReprint.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.dgvListCartonQrReprint.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvListCartonQrReprint.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvListCartonQrReprint.ColumnHeadersHeight = 45;
            this.dgvListCartonQrReprint.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvListCartonQrReprint.DefaultCellStyle = dataGridViewCellStyle6;
            this.dgvListCartonQrReprint.EnableHeadersVisualStyles = false;
            this.dgvListCartonQrReprint.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(237)))), ((int)(((byte)(240)))));
            this.dgvListCartonQrReprint.Location = new System.Drawing.Point(571, 83);
            this.dgvListCartonQrReprint.MultiSelect = false;
            this.dgvListCartonQrReprint.Name = "dgvListCartonQrReprint";
            this.dgvListCartonQrReprint.ReadOnly = true;
            this.dgvListCartonQrReprint.RowHeadersVisible = false;
            this.dgvListCartonQrReprint.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvListCartonQrReprint.RowTemplate.Height = 35;
            this.dgvListCartonQrReprint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvListCartonQrReprint.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvListCartonQrReprint.Size = new System.Drawing.Size(540, 520);
            this.dgvListCartonQrReprint.TabIndex = 5;
            this.dgvListCartonQrReprint.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvListCartonQrReprint_CellClick);
            this.dgvListCartonQrReprint.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_CellMouseDown);
            this.dgvListCartonQrReprint.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_ColumnHeaderMouseClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(8, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(138, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Nhập mã cần in lại";
            // 
            // dgvListBoxQrReprint
            // 
            this.dgvListBoxQrReprint.AllowUserToAddRows = false;
            this.dgvListBoxQrReprint.AllowUserToResizeRows = false;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            this.dgvListBoxQrReprint.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvListBoxQrReprint.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvListBoxQrReprint.BackgroundColor = System.Drawing.Color.White;
            this.dgvListBoxQrReprint.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvListBoxQrReprint.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.dgvListBoxQrReprint.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle8.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.White;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvListBoxQrReprint.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
            this.dgvListBoxQrReprint.ColumnHeadersHeight = 45;
            this.dgvListBoxQrReprint.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.WindowFrame;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvListBoxQrReprint.DefaultCellStyle = dataGridViewCellStyle9;
            this.dgvListBoxQrReprint.EnableHeadersVisualStyles = false;
            this.dgvListBoxQrReprint.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(237)))), ((int)(((byte)(240)))));
            this.dgvListBoxQrReprint.Location = new System.Drawing.Point(9, 83);
            this.dgvListBoxQrReprint.MultiSelect = false;
            this.dgvListBoxQrReprint.Name = "dgvListBoxQrReprint";
            this.dgvListBoxQrReprint.ReadOnly = true;
            this.dgvListBoxQrReprint.RowHeadersVisible = false;
            this.dgvListBoxQrReprint.RowsDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvListBoxQrReprint.RowTemplate.Height = 35;
            this.dgvListBoxQrReprint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvListBoxQrReprint.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvListBoxQrReprint.Size = new System.Drawing.Size(540, 520);
            this.dgvListBoxQrReprint.TabIndex = 0;
            this.dgvListBoxQrReprint.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvListBoxQrReprint_CellClick);
            this.dgvListBoxQrReprint.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_CellMouseDown);
            this.dgvListBoxQrReprint.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_ColumnHeaderMouseClick);
            // 
            // frmReprint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1701, 661);
            this.Controls.Add(this.tabRePrint);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmReprint";
            this.Text = "R-LINK";
            this.tabRePrint.ResumeLayout(false);
            this.tabRePrintQr.ResumeLayout(false);
            this.tabRePrintQr.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListPalletQrReprint)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListCartonQrReprint)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListBoxQrReprint)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabRePrint;
        private System.Windows.Forms.TabPage tabRePrintQr;
        private System.Windows.Forms.DataGridView dgvListBoxQrReprint;
        private Label label2;
        private DataGridView dgvListPalletQrReprint;
        private DataGridView dgvListCartonQrReprint;
        private Label label4;
        private Label label3;
        private Label label1;
        private Button btnResetValueQr;
        private DesignUI.CuzUI.CuzTextBox txtQrReprint;
        private Button btnSendQrRePrint;
    }
}
