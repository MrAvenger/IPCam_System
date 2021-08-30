﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using VisioForge.Types;
using VisioForge.Types.OutputFormat;

namespace VisioForge.Controls.UI.Dialogs.OutputFormats
{
    public partial class MP3SettingsDialog : Form
    {
        public MP3SettingsDialog()
        {
            InitializeComponent();

            LoadDefaults();
        }

        private void LoadDefaults()
        {
            cbLameCBRBitrate.SelectedIndex = 8;
            cbLameVBRMin.SelectedIndex = 8;
            cbLameVBRMax.SelectedIndex = 10;
            cbLameSampleRate.SelectedIndex = 0;
        }

        public void FillSettings(ref VFMP3Output mp3Output)
        {
            // main
            mp3Output.CBR_Bitrate = Convert.ToInt32(cbLameCBRBitrate.Text);
            mp3Output.VBR_MinBitrate = Convert.ToInt32(cbLameVBRMin.Text);
            mp3Output.VBR_MaxBitrate = Convert.ToInt32(cbLameVBRMax.Text);
            mp3Output.SampleRate = Convert.ToInt32(cbLameSampleRate.Text);
            mp3Output.VBR_Quality = tbLameVBRQuality.Value;
            mp3Output.EncodingQuality = tbLameEncodingQuality.Value;

            if (rbLameStandardStereo.Checked)
            {
                mp3Output.ChannelsMode = VFLameChannelsMode.StandardStereo;
            }
            else if (rbLameJointStereo.Checked)
            {
                mp3Output.ChannelsMode = VFLameChannelsMode.JointStereo;
            }
            else if (rbLameDualChannels.Checked)
            {
                mp3Output.ChannelsMode = VFLameChannelsMode.DualStereo;
            }
            else
            {
                mp3Output.ChannelsMode = VFLameChannelsMode.Mono;
            }

            mp3Output.VBR_Mode = rbLameVBR.Checked;

            // other
            mp3Output.Copyright = cbLameCopyright.Checked;
            mp3Output.Original = cbLameOriginal.Checked;
            mp3Output.CRCProtected = cbLameCRCProtected.Checked;
            mp3Output.ForceMono = cbLameForceMono.Checked;
            mp3Output.StrictlyEnforceVBRMinBitrate = cbLameStrictlyEnforceVBRMinBitrate.Checked;
            mp3Output.VoiceEncodingMode = cbLameVoiceEncodingMode.Checked;
            mp3Output.KeepAllFrequencies = cbLameKeepAllFrequences.Checked;
            mp3Output.StrictISOCompliance = cbLameStrictISOCompilance.Checked;
            mp3Output.DisableShortBlocks = cbLameDisableShortBlocks.Checked;
            mp3Output.EnableXingVBRTag = cbLameEnableXingVBRTag.Checked;
            mp3Output.ModeFixed = cbLameModeFixed.Checked;
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
