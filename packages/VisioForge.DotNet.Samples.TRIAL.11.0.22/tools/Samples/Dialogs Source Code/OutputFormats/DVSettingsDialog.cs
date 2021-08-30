﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using VisioForge.Types;
using VisioForge.Types.OutputFormat;

namespace VisioForge.Controls.UI.Dialogs.OutputFormats
{
    public partial class DVSettingsDialog : Form
    {
        public DVSettingsDialog()
        {
            InitializeComponent();

            LoadDefaults();
        }

        private void LoadDefaults()
        {
            cbDVChannels.SelectedIndex = 1;
            cbDVSampleRate.SelectedIndex = 0;
        }

        public void FillSettings(ref VFDVOutput dvOutput)
        {
            dvOutput.Audio_Channels = Convert.ToInt32(cbDVChannels.Text);
            dvOutput.Audio_SampleRate = Convert.ToInt32(cbDVSampleRate.Text);

            if (rbDVPAL.Checked)
            {
                dvOutput.Video_Format = VFDVVideoFormat.PAL;
            }
            else
            {
                dvOutput.Video_Format = VFDVVideoFormat.NTSC;
            }

            dvOutput.Type2 = rbDVType2.Checked;
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            const string url = "https://github.com/visioforge/.Net-SDK-s-samples/tree/master/Dialogs%20Source%20Code/OutputFormats";
            var startInfo = new ProcessStartInfo("explorer.exe", url);
            Process.Start(startInfo);
        }
    }
}
