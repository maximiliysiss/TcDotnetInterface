﻿using System;
using System.Windows.Forms;
using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.FileSystem;
using OY.TotalCommander.TcPluginInterface.Packer;

namespace OY.TotalCommander.TcPluginTools;

public partial class PasswordControl : UserControl
{
    private const string FakePasswordText = "0123456789";

    private readonly PluginPassword pluginPassword;

    public PasswordControl(TcPlugin plugin, string storeName)
    {
        Stored = false;
        if (plugin is FsPlugin || plugin is PackerPlugin)
        {
            pluginPassword = plugin.Password;
        }

        InitializeComponent();
        StoreName = storeName;
        WarningText = string.Empty;
        btnClearPassword.Visible = false;
        if (pluginPassword == null || string.IsNullOrEmpty(StoreName))
        {
            cbxUseMasterPassword.Visible = false;
            //btnClearPassword.Visible = false;
        }
        else
        {
            cbxUseMasterPassword.Visible = true;
            cbxUseMasterPassword.Enabled = pluginPassword.TcMasterPasswordDefined;
            //btnClearPassword.Visible = true;
            //btnClearPassword.Enabled = pluginPassword.TcMasterPasswordDefined;
        }
        //  ?? Load ?? this.ParentForm.FormClosing += new FormClosingEventHandler(parentForm_Closing);
    }

    public string Password
    {
        get => txtPassword.Text;
        set
        {
            if (value.Equals("!") && cbxUseMasterPassword.Enabled)
            {
                Stored = true;
                txtPassword.Text = FakePasswordText;
                //cbxUseMasterPassword.Enabled = pluginPassword.TcMasterPasswordDefined;
                cbxUseMasterPassword.Checked = true;
                btnClearPassword.Enabled = true;
            }
            else
            {
                txtPassword.Text = value;
            }
        }
    }

    public bool Stored { get; private set; }

    public string StoreName { get; }

    public string WarningText
    {
        get => lblWarning.Text;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                lblWarning.Visible = false;
            }
            else
            {
                lblWarning.Visible = true;
                lblWarning.Text = value;
            }
        }
    }

    public void parentForm_Closing(object sender, FormClosingEventArgs e)
    {
        //SavePassword();
    }

    public void SavePassword()
    {
        if (cbxUseMasterPassword.Checked && !Stored)
        {
            var cryptRes = pluginPassword.Save(StoreName, txtPassword.Text);
            if (cryptRes == CryptResult.OK)
            {
                Stored = true;
                //txtPassword.Text = FakePasswordText;
                cbxUseMasterPassword.Checked = true;
                btnClearPassword.Enabled = true;
            }
            else
            {
                MessageBox.Show("Error saving password: " + cryptRes, "ERROR");
            }
        }
    }

    private void btnClearPassword_Click(object sender, EventArgs e)
    {
        var cryptRes = pluginPassword.Delete(StoreName);
        if (cryptRes == CryptResult.OK)
        {
            MessageBox.Show("Password deleted.");
            Stored = false;
            txtPassword.Text = string.Empty;
            cbxUseMasterPassword.Checked = false;
            btnClearPassword.Enabled = false;
        }
        else
        {
            MessageBox.Show("Error deleting password: " + cryptRes, "ERROR");
        }
    }

    private void cbxUseMasterPassword_CheckedChanged(object sender, EventArgs e) => txtPassword.Enabled = !cbxUseMasterPassword.Checked;
}
