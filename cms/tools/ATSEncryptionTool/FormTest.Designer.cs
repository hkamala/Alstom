namespace ATSEncryptionTool
{
    partial class FormTest
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

        //#region Windows Form Designer generated code

        ///// <summary>
        ///// Required method for Designer support - do not modify
        ///// the contents of this method with the code editor.
        ///// </summary>
        //private void InitializeComponent()
        //{
        //    this.components = new System.ComponentModel.Container();
        //    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //    this.ClientSize = new System.Drawing.Size(800, 450);
        //    this.Text = "FormTest";
        //}

        //#endregion
        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.KeyNameComboBox = new System.Windows.Forms.ComboBox();
            this.DecryptedValue = new System.Windows.Forms.RichTextBox();
            this.ValueToEncrypt = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.EncryptValueButton = new System.Windows.Forms.Button();
            this.EncrptedValueText = new System.Windows.Forms.RichTextBox();
            this.SaveValueButtone = new System.Windows.Forms.Button();
            this.DeleteKeyButton = new System.Windows.Forms.Button();
            this.AddKeyButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.X509CertificateThumbValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // KeyNameComboBox
            // 
            this.KeyNameComboBox.FormattingEnabled = true;
            this.KeyNameComboBox.Location = new System.Drawing.Point(32, 103);
            this.KeyNameComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.KeyNameComboBox.Name = "KeyNameComboBox";
            this.KeyNameComboBox.Size = new System.Drawing.Size(322, 33);
            this.KeyNameComboBox.TabIndex = 0;
            this.KeyNameComboBox.SelectedIndexChanged += new System.EventHandler(this.KeyTextChanged);
            this.KeyNameComboBox.TextUpdate += new System.EventHandler(this.KeyTextUpdated);
            this.KeyNameComboBox.TextChanged += new System.EventHandler(this.KeyNameChange);
            this.KeyNameComboBox.Leave += new System.EventHandler(this.NewKeyEntered);
            // 
            // DecryptedValue
            // 
            this.DecryptedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.DecryptedValue.EnableAutoDragDrop = true;
            this.DecryptedValue.Location = new System.Drawing.Point(415, 100);
            this.DecryptedValue.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.DecryptedValue.Name = "DecryptedValue";
            this.DecryptedValue.ReadOnly = true;
            this.DecryptedValue.Size = new System.Drawing.Size(307, 36);
            this.DecryptedValue.TabIndex = 2;
            this.DecryptedValue.TabStop = false;
            this.DecryptedValue.Text = "";
            // 
            // ValueToEncrypt
            // 
            this.ValueToEncrypt.Location = new System.Drawing.Point(776, 103);
            this.ValueToEncrypt.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ValueToEncrypt.Name = "ValueToEncrypt";
            this.ValueToEncrypt.Size = new System.Drawing.Size(304, 31);
            this.ValueToEncrypt.TabIndex = 3;
            this.ValueToEncrypt.Leave += new System.EventHandler(this.ValueEntered);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(826, 70);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(215, 25);
            this.label1.TabIndex = 4;
            this.label1.Text = "Enter the Value to Encrypt";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(109, 70);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 25);
            this.label2.TabIndex = 5;
            this.label2.Text = "Enter the Key Name";
            // 
            // EncryptValueButton
            // 
            this.EncryptValueButton.Enabled = false;
            this.EncryptValueButton.Location = new System.Drawing.Point(773, 160);
            this.EncryptValueButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.EncryptValueButton.Name = "EncryptValueButton";
            this.EncryptValueButton.Size = new System.Drawing.Size(307, 38);
            this.EncryptValueButton.TabIndex = 6;
            this.EncryptValueButton.Text = "Encrypt Value ";
            this.EncryptValueButton.UseVisualStyleBackColor = true;
            this.EncryptValueButton.Click += new System.EventHandler(this.EncryptValueButton_Click);
            // 
            // EncrptedValueText
            // 
            this.EncrptedValueText.Location = new System.Drawing.Point(774, 207);
            this.EncrptedValueText.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.EncrptedValueText.Name = "EncrptedValueText";
            this.EncrptedValueText.Size = new System.Drawing.Size(304, 427);
            this.EncrptedValueText.TabIndex = 7;
            this.EncrptedValueText.Text = "";
            // 
            // SaveValueButtone
            // 
            this.SaveValueButtone.Enabled = false;
            this.SaveValueButtone.Location = new System.Drawing.Point(774, 659);
            this.SaveValueButtone.Name = "SaveValueButtone";
            this.SaveValueButtone.Size = new System.Drawing.Size(304, 34);
            this.SaveValueButtone.TabIndex = 8;
            this.SaveValueButtone.Text = "Save new Encryption to key";
            this.SaveValueButtone.UseVisualStyleBackColor = true;
            this.SaveValueButtone.Click += new System.EventHandler(this.SaveValueButton_Click);
            // 
            // DeleteKeyButton
            // 
            this.DeleteKeyButton.Enabled = false;
            this.DeleteKeyButton.Location = new System.Drawing.Point(451, 306);
            this.DeleteKeyButton.Name = "DeleteKeyButton";
            this.DeleteKeyButton.Size = new System.Drawing.Size(229, 34);
            this.DeleteKeyButton.TabIndex = 9;
            this.DeleteKeyButton.Text = "Delete Key and Value";
            this.DeleteKeyButton.UseVisualStyleBackColor = true;
            this.DeleteKeyButton.Click += new System.EventHandler(this.DeleteKeyButton_Click);
            // 
            // AddKeyButton
            // 
            this.AddKeyButton.Enabled = false;
            this.AddKeyButton.Location = new System.Drawing.Point(451, 207);
            this.AddKeyButton.Name = "AddKeyButton";
            this.AddKeyButton.Size = new System.Drawing.Size(229, 39);
            this.AddKeyButton.TabIndex = 10;
            this.AddKeyButton.Text = "Add Key and Value";
            this.AddKeyButton.UseVisualStyleBackColor = true;
            this.AddKeyButton.Click += new System.EventHandler(this.AddKeyButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(497, 70);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(141, 25);
            this.label3.TabIndex = 11;
            this.label3.Text = "Decrypted Value";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 716);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(302, 25);
            this.label4.TabIndex = 12;
            this.label4.Text = "Certificate (X509CertificateThumb) is ";
            // 
            // X509CertificateThumbValue
            // 
            this.X509CertificateThumbValue.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.X509CertificateThumbValue.Location = new System.Drawing.Point(332, 716);
            this.X509CertificateThumbValue.Name = "X509CertificateThumbValue";
            this.X509CertificateThumbValue.Size = new System.Drawing.Size(770, 24);
            this.X509CertificateThumbValue.TabIndex = 13;
            this.X509CertificateThumbValue.TabStop = false;
            // 
            // FormTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1143, 750);
            this.Controls.Add(this.X509CertificateThumbValue);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.AddKeyButton);
            this.Controls.Add(this.DeleteKeyButton);
            this.Controls.Add(this.SaveValueButtone);
            this.Controls.Add(this.EncrptedValueText);
            this.Controls.Add(this.EncryptValueButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ValueToEncrypt);
            this.Controls.Add(this.DecryptedValue);
            this.Controls.Add(this.KeyNameComboBox);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "FormTest";
            this.Text = "ATS Encryption Tool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ComboBox KeyNameComboBox;
        private RichTextBox DecryptedValue;
        private TextBox ValueToEncrypt;
        private Label label1;
        private Label label2;
        private Button EncryptValueButton;
        private RichTextBox EncrptedValueText;
        private Button SaveValueButtone;
        private Button DeleteKeyButton;
        private Button AddKeyButton;
        private Label label3;
        private Label label4;
        private TextBox X509CertificateThumbValue;
    }
}