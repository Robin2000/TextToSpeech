﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using JocysCom.TextToSpeech.Monitor.Audio;

namespace JocysCom.TextToSpeech.Monitor.Controls
{

	public partial class OptionsControl : UserControl
	{


		public OptionsControl()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			// Make Google Cloud invisible, because it is not finished yet.
			OptionsTabControl.TabPages.Remove(GoogleCloudTabPage);
			AddSilcenceBeforeNumericUpDown.Value = SettingsManager.Options.AddSilcenceBeforeMessage;
			AddSilenceAfterNumericUpDown.Value = SettingsManager.Options.DelayBeforeValue;
			LoggingFolderTextBox.Text = GetLogsPath(true);
			LoadSettings();
			SilenceBefore();
			SilenceAfter();
		}

		public bool IsDesignMode
		{
			get { return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
		}

		private void SilenceBefore()
		{
			// Show or hide silence before message tag.
			int silenceIntBeforeTag = Decimal.ToInt32(AddSilcenceBeforeNumericUpDown.Value);
			string silenceStringBeforeTag = AddSilcenceBeforeNumericUpDown.Value.ToString();
			if (silenceIntBeforeTag > 0)
			{
				SilenceBeforeTagLabel.Text = "<silence msec=\"" + silenceStringBeforeTag + "\" />";
			}
			else
			{
				SilenceBeforeTagLabel.Text = "";
			}
		}

		private void SilenceAfter()
		{
			// Show or hide silence after message tag.
			int silenceIntAfterTag = Decimal.ToInt32(AddSilenceAfterNumericUpDown.Value);
			string silenceStringAfterTag = AddSilenceAfterNumericUpDown.Value.ToString();
			if (silenceIntAfterTag > 0)
			{
				SilenceAfterTagLabel.Text = "<silence msec=\"" + silenceStringAfterTag + "\" />";
			}
			else
			{
				SilenceAfterTagLabel.Text = "";
			}
		}

		public decimal silenceBefore
		{
			get { return AddSilcenceBeforeNumericUpDown.Value; }
			set { AddSilcenceBeforeNumericUpDown.Value = value; }
		}

		public decimal silenceAfter
		{
			get { return AddSilenceAfterNumericUpDown.Value; }
			set { AddSilenceAfterNumericUpDown.Value = value; }
		}

		private void AddSilcenceBeforeNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			SilenceBefore();
		}

		private void AddSilenceAfterNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			SilenceAfter();
		}

		public string GetLogsPath(bool create)
		{
			var path = Path.Combine(MainHelper.AppDataPath, "Logs"); 
			if (create && !Directory.Exists(path))
				Directory.CreateDirectory(path);
			return path;
		}

		private void OpenButton_Click(object sender, EventArgs e)
		{
			MainHelper.OpenUrl(LoggingFolderTextBox.Text);
		}

		public byte[] SearchPattern;
		public JocysCom.ClassLibrary.IO.LogFileWriter Writer;
		public object WriterLock = new object();

		void LoadSettings()
		{
			// Load settings into form.
			LoggingTextBox.Text = SettingsManager.Options.LogText;
			SearchPattern = Encoding.ASCII.GetBytes(LoggingTextBox.Text);
			LoggingCheckBox.Checked = SettingsManager.Options.LogEnable;
			CacheDataWriteCheckBox.Checked = SettingsManager.Options.CacheDataWrite;
			CacheDataReadCheckBox.Checked = SettingsManager.Options.CacheDataRead;
			CacheDataGeneralizeCheckBox.Checked = SettingsManager.Options.CacheDataGeneralize;
			UpdateWinCapState();
			var allowWinCap = SettingsManager.Options.UseWinCap & MainHelper.GetWinPcapVersion() != null;
			CaptureSocButton.Checked = !allowWinCap;
			CaptureWinButton.Checked = allowWinCap;
			// Update writer settings.
			SaveSettings();
			// Attach events.
			LoggingTextBox.TextChanged += LoggingTextBox_TextChanged;
			LoggingCheckBox.CheckedChanged += LoggingCheckBox_CheckedChanged;
			CacheDataWriteCheckBox.CheckedChanged += CacheDataWriteCheckBox_CheckedChanged;
			CacheDataReadCheckBox.CheckedChanged += CacheDataReadCheckBox_CheckedChanged;
			CacheDataGeneralizeCheckBox.CheckedChanged += CacheDataGeneralizeCheckBox_CheckedChanged;
			LoggingPlaySoundCheckBox.CheckedChanged += LoggingPlaySoundCheckBox_CheckedChanged;
			CaptureSocButton.CheckedChanged += CaptureSocButton_CheckedChanged;
			CaptureWinButton.CheckedChanged += CaptureWinButton_CheckedChanged;
			EnumeratePlaybackDevices();
			UpdatePlayBackDevice();
		}

		public void UpdateWinCapState()
		{
			var version = MainHelper.GetWinPcapVersion();
			if (version != null)
			{
				CaptureWinButton.Text = string.Format("WinPcap {0}", version.ToString());
				CaptureWinButton.Enabled = true;
			}
			else
			{
				CaptureWinButton.Text = "WinPcap";
				CaptureWinButton.Enabled = false;
			}
		}

		// Save value
		private void LoggingPlaySoundCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var value = LoggingPlaySoundCheckBox.Checked;
			SettingsManager.Options.LogSound = value;
		}

		private void LoggingTextBox_TextChanged(object sender, EventArgs e)
		{
			var text = LoggingTextBox.Text;
			SettingsManager.Options.LogText = text;
			SearchPattern = string.IsNullOrEmpty(text)
				? null
				: Encoding.ASCII.GetBytes(LoggingTextBox.Text);
		}

		void SaveSettings()
		{
			SettingsManager.Options.LogEnable = LoggingCheckBox.Checked;
			LoggingTextBox.Enabled = !LoggingCheckBox.Checked;
			LoggingPlaySoundCheckBox.Enabled = !LoggingCheckBox.Checked;
			LoggingFolderTextBox.Enabled = !LoggingCheckBox.Checked;
			OpenButton.Enabled = !LoggingCheckBox.Checked;
			FilterTextLabel.Enabled = !LoggingCheckBox.Checked;
			LogFolderLabel.Enabled = !LoggingCheckBox.Checked;
			lock (WriterLock)
			{
				var en = SettingsManager.Options.LogEnable;
				if (Writer == null && en && !IsDisposed && !Disposing)
				{
					Writer = new ClassLibrary.IO.LogFileWriter();
					Writer.LogFilePrefix = GetLogsPath(true) + "\\log_";
					Writer.LogFileAutoFlush = true;
				}
				else if (Writer != null && !en)
				{
					Writer.Dispose();
					Writer = null;
				}
			}
		}

		private void CacheDataWriteCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.CacheDataWrite = CacheDataWriteCheckBox.Checked;
		}

		private void CacheDataReadCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.CacheDataRead = CacheDataReadCheckBox.Checked;
		}

		private void CacheDataGeneralizeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.CacheDataGeneralize = CacheDataGeneralizeCheckBox.Checked;
		}

		private void LoggingCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SaveSettings();
		}

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
			lock (WriterLock)
			{
				if (Writer != null)
				{
					Writer.Dispose();
					Writer = null;
				}
			}
			base.Dispose(disposing);
		}

		private void CaptureSocButton_CheckedChanged(object sender, EventArgs e)
		{
			if (CaptureSocButton.Checked)
			{
				SettingsManager.Options.UseWinCap = false;
				Program.TopForm.StopNetworkMonitor();
				Program.TopForm.StartNetworkMonitor();
			}
		}

		private void CaptureWinButton_CheckedChanged(object sender, EventArgs e)
		{
			if (CaptureWinButton.Checked)
			{
				SettingsManager.Options.UseWinCap = true;
				Program.TopForm.StopNetworkMonitor();
				Program.TopForm.StartNetworkMonitor();
			}
		}

		private void OpenCacheButton_Click(object sender, EventArgs e)
		{
			var dir = MainHelper.GetCreateCacheFolder();
			MainHelper.OpenUrl(dir.FullName);
		}

		string _CacheMessageFormat;

		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static string SizeSuffix(long value, int decimalPlaces = 0)
		{
			if (value < 0)
			{
				throw new ArgumentException("Bytes should not be negative", "value");
			}
			var mag = (int)Math.Max(0, Math.Log(value, 1024));
			var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
			return String.Format("{0} {1}", adjustedSize, SizeSuffixes[mag]);
		}

		private void OptionsControl_Load(object sender, EventArgs e)
		{
			_CacheMessageFormat = CacheLabel.Text;
			var files = MainHelper.GetCreateCacheFolder().GetFiles("*.*", SearchOption.AllDirectories);
			var count = files.Count();
			var size = SizeSuffix(files.Sum(x => x.Length), 1);
			CacheLabel.Text = string.Format(_CacheMessageFormat, count, size);
		}

		private void HowToButton_Click(object sender, EventArgs e)
		{
			var message = "";
			message += "1. Enable logging.\r\n";
			message += "2.Enter and send specified text message(for example: me66age) through game or program chat.\r\n";
			message += "3.Information about found packets with specified text(for example: me66age) will be logged to TXT file.\r\n";
			MessageBox.Show(message, "How To...");
		}

		#region Playback Devices

		private void RefreshPlaybackDevices_Click(object sender, EventArgs e)
		{
			EnumeratePlaybackDevices();
			UpdatePlayBackDevice();
		}

		bool suspendEvents;

		void EnumeratePlaybackDevices()
		{
			suspendEvents = true;
			// Setup our sound listener
			PlaybackDeviceComboBox.Items.Clear();
			var names = AudioPlayer.GetDeviceNames();
			foreach (var name in names)
				PlaybackDeviceComboBox.Items.Add(name);
			// Restore audio settings.
			if (PlaybackDeviceComboBox.Items.Contains(SettingsManager.Options.PlaybackDevice))
			{
				PlaybackDeviceComboBox.SelectedItem = SettingsManager.Options.PlaybackDevice;
			}
			else
			{
				PlaybackDeviceComboBox.SelectedIndex = 0;
			}
			suspendEvents = false;
		}

		private void PlaybackDeviceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (suspendEvents) return;
			SettingsManager.Options.PlaybackDevice = (string)PlaybackDeviceComboBox.SelectedItem;
			UpdatePlayBackDevice();
		}

		void UpdatePlayBackDevice()
		{
		}

		#endregion

	}
}
