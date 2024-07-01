using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SecurityLibrary;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ATSEncryptionTool
{
    public partial class FormTest : Form
    {
        private string[] allKeys;
        private bool newkeyEntered = false;
        public FormTest()
        {
            InitializeComponent();
            X509CertificateThumbValue.Text = SecurityManager.CertificateThumbValue();
            X509CertificateThumbValue.ReadOnly = true;
            allKeys = SecurityManager.GetAllKeys();
            for (int i = 0; i < allKeys.Length; ++i)
            {
                KeyNameComboBox.Items.Add (allKeys[i]);
            }

        }

        private void DecryptValueButton_Click(object sender, EventArgs e)
        {
            DecryptedValue.ReadOnly = false;
            DecryptedValue.Text = "";
            DecryptedValue.Text = SecurityManager.GetCredential(KeyNameComboBox.Text);
            DecryptedValue.ReadOnly = true;
        }

        private void AddKeyButton_Click(object sender, EventArgs e)
        {
            SecurityManager.AddKey(KeyNameComboBox.Text, EncrptedValueText.Text);
            allKeys = SecurityManager.GetAllKeys();
            KeyNameComboBox.Items.Clear();
            for (int i = 0; i < allKeys.Length; ++i)
            {
                KeyNameComboBox.Items.Add(allKeys[i]);
            }
            KeyNameComboBox.ResetText();
            AddKeyButton.Enabled = false;
            EncrptedValueText.Text = "";
            ValueToEncrypt.Text = "";
            EncryptValueButton.Enabled = false;
            DecryptedValue.Text = "";
            newkeyEntered = false;

        }

        private void DeleteKeyButton_Click(object sender, EventArgs e)
        {
            SecurityManager.DeleteKey(KeyNameComboBox.Text);
            allKeys = SecurityManager.GetAllKeys();
            KeyNameComboBox.Items.Clear();
            for (int i = 0; i < allKeys.Length; ++i)
            {
                KeyNameComboBox.Items.Add(allKeys[i]);
            }
            KeyNameComboBox.ResetText();
            DeleteKeyButton.Enabled=false;
            DecryptedValue.Text = "";
        }

        private void EncryptValueButton_Click(object sender, EventArgs e)
        {
            EncrptedValueText.Text = SecurityManager.GetEncryption(ValueToEncrypt.Text);
            if (KeyNameComboBox.Text != "" && EncrptedValueText.Text != "")
            {
                if (newkeyEntered)
                {
                    AddKeyButton.Enabled = true;
                    SaveValueButtone.Enabled = false;
                }
                else
                {
                    SaveValueButtone.Enabled = true;
                    AddKeyButton.Enabled = false;
                }
            }
        }


        private void SaveValueButton_Click(object sender, EventArgs e)
        {
            SecurityManager.DeleteKey(KeyNameComboBox.Text);
            SecurityManager.AddKey(KeyNameComboBox.Text, EncrptedValueText.Text);
            allKeys = SecurityManager.GetAllKeys();
            KeyNameComboBox.Items.Clear();
            for (int i = 0; i < allKeys.Length; ++i)
            {
                KeyNameComboBox.Items.Add(allKeys[i]);
            }
            KeyNameComboBox.ResetText();
            AddKeyButton.Enabled = false;
            EncrptedValueText.Text = "";
            ValueToEncrypt.Text = "";
            EncryptValueButton.Enabled = false;
            DecryptedValue.Text = "";
            newkeyEntered = false;
        }

        private void KeyTextChanged(object sender, EventArgs e)
        {
            AddKeyButton.Enabled = false;
            SaveValueButtone.Enabled = false;
            EncrptedValueText.Text = "";
            ValueToEncrypt.Text = "";
            EncryptValueButton.Enabled = false;
            //DeleteKeyButton.Enabled = false;
            DecryptedValue.ReadOnly = false;
            DecryptedValue.Text = SecurityManager.GetCredential(KeyNameComboBox.Text);
            DecryptedValue.ReadOnly = true;
            if (!newkeyEntered)
            {
                DeleteKeyButton.Enabled = true;
            }
        }

        private void KeyTextUpdated(object sender, EventArgs e)
        {
            newkeyEntered = true;
            //DeleteKeyButton.Enabled = false;
        }

        private void NewKeyEntered(object sender, EventArgs e)
        {
            AddKeyButton.Enabled = false;
            SaveValueButtone.Enabled = false;
            EncrptedValueText.Text = "";
            ValueToEncrypt.Text = "";
            EncryptValueButton.Enabled = false;

            if (newkeyEntered)
            {
                DecryptedValue.ReadOnly = false;
                DecryptedValue.Text = SecurityManager.GetCredential(KeyNameComboBox.Text);
                DecryptedValue.ReadOnly = true;
                if (DecryptedValue.Text.Equals("No Key Value Found"))
                {
                 DeleteKeyButton.Enabled = false;
                }
                else
                {
                    DeleteKeyButton.Enabled = true;

                }
            }
        }

        private void ValueEntered(object sender, EventArgs e)
        {
            EncryptValueButton.Enabled = true;
        }

        private void KeyNameChange(object sender, EventArgs e)
        {
            DeleteKeyButton.Enabled = false; 
            if (KeyNameComboBox.SelectedIndex > -1)
            {
                DeleteKeyButton.Enabled = true;
            }
        }
    }
}
