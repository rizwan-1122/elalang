﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

namespace VersionTracker
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        
		private void grid_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
			if (e.Row.Cells[0].Value == null)
			{
				e.Row.Cells[0].Value = IncrementVersion(e.Row.Index == 0 ? CurrentVersion :
					grid.Rows[e.Row.Index - 1].Cells[0].Value.ToString(), 4);
				var c = (DataGridViewComboBoxCell)e.Row.Cells[1];
				c.Value = c.Items[0];
			}
        }


        private void newChangeListToolStripMenuItem_Click(object sender, EventArgs e)
        {
			using (var d = new NewChangeListDialog())
			{
				if (d.ShowDialog(this) == DialogResult.OK)
				{
					CurrentFile = d.FileName;
					CurrentVersion = d.Version;
					grid.Rows.Clear();
					grid.Rows[0].Cells[0].Value = CurrentVersion;
					var c = (DataGridViewComboBoxCell)grid.Rows[0].Cells[1];
					c.Value = c.Items[0];
					Registry.SetValue(@"HKEY_CURRENT_USER\Software\VersionTracker", "FileName", CurrentFile);
					UpdateState();
				}
			}
        }

		private void openChangeListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				CurrentFile = openFileDialog.FileName;
				ReadFile();
				UpdateState();
			}
		}


		private string IncrementVersion(string version, int revision)
		{
			if (!String.IsNullOrEmpty(version))
			{
				var v = new Version(version);
				var f = "{0}.{1}.{2}.{3}";

				switch (revision)
				{
					case 4: return String.Format(f, v.Major, v.Minor, v.Build, v.Revision + 1);
					case 3: return String.Format(f, v.Major, v.Minor, v.Build + 1, 0);
					case 2: return String.Format(f, v.Major, v.Minor + 1, 0, 0);
					case 1: return String.Format(f, v.Major + 1, 0, 0, 0);
				}
			}

			return String.Empty;
		}

		private void incrementMenu_Opening(object sender, CancelEventArgs e)
		{
			if (grid.SelectedCells.Count == 1 && grid.SelectedCells[0].ColumnIndex == 0)
			{
				var c = grid.SelectedCells[0].Value as String;
				revertChangeToolStripMenuItem.Enabled = grid.SelectedCells[0].Tag != null;

				if (!String.IsNullOrEmpty(c))
				{
					var v = new Version(c);

					incrementThirdRevisionToolStripMenuItem.Enabled = v.Revision > 0;
					incrementSecondRevisionToolStripMenuItem.Enabled = v.Build > 0;
					incrementFirstRevisionToolStripMenuItem.Enabled = v.Minor > 0;
				}
			}
		}

		private void incrementRevisionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (grid.SelectedCells.Count == 1)
			{
				var c = grid.SelectedCells[0];
				c.Tag = c.Value;
				c.Value = IncrementVersion(c.Value.ToString(), Convert.ToInt32(((ToolStripMenuItem)sender).Tag));
			}
		}

		private void revertChangeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (grid.SelectedCells.Count == 1)
			{
				var c = grid.SelectedCells[0];
				c.Value = c.Tag;
				c.Tag = null;
			}
		}



		private void viedToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			allItemsToolStripMenuItem.Checked = CurrentViewMode == ViewMode.All;
			currentMinorRevisionToolStripMenuItem.Checked = CurrentViewMode == ViewMode.MinorRevision;
			currentMajorRevisionToolStripMenuItem.Checked = CurrentViewMode == ViewMode.MajorRevision;
			currentMinorVersionToolStripMenuItem.Checked = CurrentViewMode == ViewMode.MinorVersion;
			currentMajorVersionToolStripMenuItem.Checked = CurrentViewMode == ViewMode.MajorVersion;
		}

		private void allItemsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentViewMode = ViewMode.All;
		}

		private void currentMinorRevisionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentViewMode = ViewMode.MinorRevision;
		}

		private void currentMajorRevisionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentViewMode = ViewMode.MajorRevision;
		}

		private void currentMinorVersionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentViewMode = ViewMode.MinorVersion;
		}

		private void currentMajorVersionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrentViewMode = ViewMode.MajorVersion;
		}


		public ViewMode CurrentViewMode { get; set; }

		public string CurrentFile { get; set; }

		public string CurrentVersion { get; set; }

		private void MainForm_Load(object sender, EventArgs e)
		{
			Application.Idle += new EventHandler(Application_Idle);
			var val = Registry.GetValue(@"HKEY_CURRENT_USER\Software\VersionTracker", "FileName", null);

			if (val != null)
			{
				CurrentFile = val.ToString();
				ReadFile();
			}

			UpdateState();
		}


		private void ReadFile()
		{
			using (var sr = new StreamReader(File.Open(CurrentFile, FileMode.Open)))
			{
				var line = String.Empty;

				while ((line = sr.ReadLine()) != null)
				{
					var arr = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

					if (arr.Length == 3)
					{
						grid.Rows.Insert(0,
							arr[0],
							arr[1],
							arr[2]
						);
					}
				}
			}
		}


		private void UpdateState()
		{
			if (!String.IsNullOrEmpty(CurrentFile))
			{
				Text = String.Format("{0} - {1}", ProductName, CurrentFile);
				grid.Visible = true;
			}
			else
			{
				Text = ProductName;
				grid.Visible = false;
			}
		}


		private int oldFix = -1;
		private int oldNew = -1;
		private int oldChange = -1;
		private void Application_Idle(object sender, EventArgs e)
		{
			var fix = 0;
			var news = 0;
			var change = 0;
			
			if (grid.Rows.Count > 1)
			{
				var vstr = grid.Rows[grid.Rows.Count - 2].Cells[0].Value as String;
				
				if (!String.IsNullOrEmpty(vstr))
				{
					var v = new Version(vstr);

					foreach (DataGridViewRow r in grid.Rows)
					{
						var vs = r.Cells[0].Value as String;

						if (vs == null)
							continue;

						var cv = new Version(vs);

						switch (CurrentViewMode)
						{
							case ViewMode.All:
								r.Visible = true;
								break;
							case ViewMode.MajorRevision:
								r.Visible = cv.Build == v.Build;
								break;
							case ViewMode.MajorVersion:
								r.Visible = cv.Major == v.Major;
								break;
							case ViewMode.MinorRevision:
								r.Visible = cv.Revision == v.Revision;
								break;
							case ViewMode.MinorVersion:
								r.Visible = cv.Minor == v.Minor;
								break;
						}

						if (r.Visible)
						{
							var val = r.Cells[1].Value.ToString();

							if (val == "Fix")
								fix++;
							else if (val == "Change")
								change++;
							else
								news++;
						}
					}
				}
			}

			if (fix != oldFix)
			{
				viewFixLabel.Text = String.Format("Fixes: {0}", fix);
				oldFix = fix;
			}

			if (news != oldNew)
			{
				viewNewLabel.Text = String.Format("New: {0}", news);
				oldNew = news;
			}

			if (change != oldChange)
			{
				viewChangeLabel.Text = String.Format("Changes: {0}", change);
				oldChange = change;
			}

			var str = String.Format("View: {0}", CurrentViewMode == ViewMode.All ? "All" :
				CurrentViewMode == ViewMode.MajorRevision ? "Major Revision" :
				CurrentViewMode == ViewMode.MajorVersion ? "Major Version" :
				CurrentViewMode == ViewMode.MinorRevision ? "Minor Revision" :
				"Minor Version");

			if (viewModeLabel.Text != str)
				viewModeLabel.Text = str;
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (CurrentFile != null)
			{
				using (var sr = new StreamWriter(File.Open(CurrentFile, FileMode.Create)))
				{
					foreach (var r in grid.Rows.Cast<DataGridViewRow>().Reverse())
					{
						if (r.Index < grid.Rows.Count - 1 || r.Cells[2].Value != null)
						{
							sr.WriteLine(String.Format("{0}:{1}:{2}",
								r.Cells[0].Value, r.Cells[1].Value, r.Cells[2].Value != null ?
								r.Cells[2].Value.ToString().Replace(":", String.Empty) : String.Empty));
						}
					}
				}
			}
		}

		private void grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.ColumnIndex > -1)
				grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
	}


	public enum ViewMode	
	{
		All,

		MajorVersion,
		
		MinorVersion,

		MajorRevision,

		MinorRevision,
	}
}
