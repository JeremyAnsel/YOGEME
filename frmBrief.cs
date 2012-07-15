/*
 * YOGEME.exe, All-in-one Mission Editor for the X-wing series, TIE through XWA
 * Copyright (C) 2007-2012 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the GPL v3.0 or later
 * 
 * VERSION: 1.1
 */

/* CHANGELOG
 * v1.0, 110921
 * - Release
 */

/* CHANGELOG
 * 120223 - added Platform use, replaced local EventType with BaseBriefing.EventType, added BaseBriefing.EventParameterCount use
 */

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Idmr.Common;
using Idmr.Platform;

namespace Idmr.Yogeme
{
	/// <summary>The briefing forms for YOGEME, one form for all platforms</summary>
	public partial class frmBrief : Form
	{
		#region Vars
		BriefData[] _briefData;
		BriefData tempBD;
		Platform.Tie.Briefing _tieBriefing;
		Platform.Xvt.Briefing _xvtBriefing;
		Platform.Xwa.Briefing _xwaBriefing;
		bool _loading = false;
		Color _normalColor;
		Color _highlightColor;
		Color _titleColor;
		short[,] _events;	// this will contain the event listing for use, raw data is in Briefing.Events[]
		short zoomX = 48;
		short zoomY;
		int w, h;
		short mapX, mapY;	// mapX and mapY will be different, namely the grid coordinates of the center, like how TIE handles it
		Bitmap _map;
		int[,] _fgTags = new int[8, 2];	// [#, 0=FG/Icon 1=Time]
		int[,] _textTags = new int[8, 4];	// [#, X, Y, color]
		DataTable tableTags = new DataTable("Tags");
		DataTable tableStrings = new DataTable("Strings");
		int _timerInterval;
		BaseBriefing.EventType _eventType = BaseBriefing.EventType.None;
		short tempX, tempY;
		int[,] tempTags;
		Settings.Platform _platform;
		string[] _tags;
		string[] _strings;
		int _maxEvents;
		string _message = "";
		int _regionDelay = -1;
		int _page = 1;
		short _icon = 0;
		#endregion

		public frmBrief(Platform.Tie.FlightGroupCollection fg, ref Platform.Tie.Briefing briefing)
		{
			_loading = true;
			_platform = Settings.Platform.TIE;
			_titleColor = Color.FromArgb(0xFC, 0xFC, 0x54);
			_normalColor = Color.FromArgb(0xFC, 0xFC, 0xFC);
			_highlightColor = Color.FromArgb(0x00, 0xA8, 0x00);
			zoomY = zoomX;			// in most cases, these will remain the same
			_tieBriefing = briefing;
			_maxEvents = Platform.Tie.Briefing.EventQuantityLimit;
			_events = new short[_maxEvents,6];
			InitializeComponent();
			this.Text = "YOGEME Briefing Editor - TIE";
			#region layout edit
			// final layout update, as in VS it's spread out
			Height = 422;
			Width = 756;
			tabBrief.Width = 752;
			Point loc = new Point(608, 188);
			pnlShipTag.Location = loc;
			pnlTextTag.Location = loc;
			#endregion
			Import(fg);	// FGs are separate so they can be updated without running the BRF as well
			ImportDat(Application.StartupPath + "\\images\\TIE_BRF.dat", 34);
			_tags = _tieBriefing.BriefingTag;
			_strings = _tieBriefing.BriefingString;
			ImportStrings();
			_timerInterval = Platform.Tie.Briefing.TicksPerSecond;
			txtLength.Text = Convert.ToString(Math.Round(((decimal)_tieBriefing.Length / _timerInterval), 2));
			hsbTimer.Maximum = _tieBriefing.Length + 11;
			w = pctBrief.Width;
			h = pctBrief.Height;
			mapX = 0;
			mapY = 0;
			lstEvents.Items.Clear();
			ImportEvents(_tieBriefing.Events);
			hsbTimer.Value = 0;
			numUnk1.Value = _tieBriefing.Unknown1;
			numUnk3.Enabled = false;
			cboText.SelectedIndex = 0;
			cboFGTag.SelectedIndex = 0;
			cboTextTag.SelectedIndex = 0;
			cboColorTag.SelectedIndex = 0;
			_loading = false;
		}
		public frmBrief(Platform.Xvt.FlightGroupCollection fg, ref Platform.Xvt.BriefingCollection briefing)
		{
			_loading = true;
			_platform = Settings.Platform.XvT;
			_titleColor = Color.FromArgb(0xFC, 0xFC, 0x00);
			_normalColor = Color.FromArgb(0xF8, 0xFC, 0xF8);
			_highlightColor = Color.FromArgb(0x40, 0xC4, 0x40);
			zoomY = zoomX;
			_xvtBriefing = briefing[0];
			_maxEvents = Platform.Xvt.Briefing.EventQuantityLimit;
			_events = new short[_maxEvents, 6];
			InitializeComponent();
			this.Text = "YOGEME Briefing Editor - XvT/BoP";
			Import(fg);
			#region XvT layout change
			Height = 422;
			Width = 756;
			tabBrief.Width = 752;
			Point loc = new Point(608, 188);
			pnlShipTag.Location = loc;
			pnlTextTag.Location = loc;
			pctBrief.Size = new Size(360, 214);
			pctBrief.Left = 150;
			lblCaption.BackColor = Color.FromArgb(0, 0, 0x78);
			lblCaption.Font = new Font("Times New Roman", 8F, FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			lblCaption.Size = new Size(360, 28);
			lblCaption.Location = new Point(150, 254);
			lblTitle.BackColor = Color.FromArgb(0x10, 0x10, 0x20);
			lblTitle.Font = new Font("Times New Roman", 8F, FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			lblTitle.Size = new Size(360, 16);
			lblTitle.TextAlign = ContentAlignment.TopCenter;
			lblTitle.ForeColor = _titleColor;
			lblTitle.Text = "*Defined in .LST file*";
			lblTitle.Location = new Point(150, 24);
			cmdTitle.Enabled = false;
			cboColorTag.Items.Clear();
			cboColorTag.Items.Add("Green");
			cboColorTag.Items.Add("Red");
			cboColorTag.Items.Add("Yellow");
			cboColorTag.Items.Add("Blue");
			cboColorTag.Items.Add("Purple");
			cboColorTag.Items.Add("Black");
			cboColor.Items.Clear();
			cboColor.Items.Add("Green");
			cboColor.Items.Add("Red");
			cboColor.Items.Add("Yellow");
			cboColor.Items.Add("Blue");
			cboColor.Items.Add("Purple");
			cboColor.Items.Add("Black");
			#endregion
			ImportDat(Application.StartupPath + "\\images\\XvT_BRF.dat", 22);
			_tags = _xvtBriefing.BriefingTag;
			_strings = _xvtBriefing.BriefingString;
			ImportStrings();
			_timerInterval = Platform.Xvt.Briefing.TicksPerSecond;
			txtLength.Text = Convert.ToString(Math.Round(((decimal)_xvtBriefing.Length / _timerInterval), 2));
			hsbTimer.Maximum = _xvtBriefing.Length + 11;
			w = pctBrief.Width;
			h = pctBrief.Height;
			mapX = 0;
			mapY = 0;
			lstEvents.Items.Clear();
			ImportEvents(_xvtBriefing.Events);
			hsbTimer.Value = 0;
			numUnk1.Value = _xvtBriefing.Unknown1;
			numUnk3.Value = _xvtBriefing.Unknown3;
			cboText.SelectedIndex = 0;
			cboFGTag.SelectedIndex = 0;
			cboTextTag.SelectedIndex = 0;
			cboColorTag.SelectedIndex = 0;
			_loading = false;
		}
		public frmBrief(ref Platform.Xwa.BriefingCollection briefing)
		{
			_loading = true;
			_platform = Settings.Platform.XWA;
			_titleColor = Color.FromArgb(0x63, 0x82, 0xFF);
			_normalColor = Color.FromArgb(0xFF, 0xFF, 0xFF);
			_highlightColor = _titleColor;
			zoomX = 32;
			zoomY = zoomX;
			_xwaBriefing = briefing[0];
			_maxEvents = Platform.Xwa.Briefing.EventQuantityLimit;
			_events = new short[_maxEvents, 6];
			InitializeComponent();
			this.Text = "YOGEME Briefing Editor - XWA";
			#region XWA layout change
			label7.Text = "Icon:";
			Height = 480;
			Width = 756;
			tabBrief.Width = 752;
			Point loc = new Point(608, 246);
			pnlShipTag.Location = loc;
			pnlTextTag.Location = loc;
			pnlShipInfo.Location = loc;
			pnlRotate.Location = loc;
			pnlMove.Location = loc;
			pnlNew.Location = loc;
			pnlRegion.Location = loc;
			cmdNewShip.Visible = true;
			cmdMoveShip.Visible = true;
			cmdRotate.Visible = true;
			cmdShipInfo.Visible = true;
			cmdRegion.Visible = true;
			pctBrief.Size = new Size(510, 294);
			pctBrief.Left += 36;
			w = pctBrief.Width;
			h = pctBrief.Height;
			lblTitle.BackColor = Color.FromArgb(0x18, 0x18, 0x18);
			lblTitle.Size = new Size(510, 28);
			lblTitle.Left += 36;
			lblTitle.Top -= 4;
			lblTitle.TextAlign = ContentAlignment.TopCenter;
			lblTitle.ForeColor = _titleColor;
			lblTitle.Text = "*Defined in .LST file*";
			lblTitle.Font = new Font("Arial", 10F, FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			cmdTitle.Enabled = false;
			lblCaption.BackColor = Color.FromArgb(0x20, 0x30, 0x88);
			lblCaption.Font = new Font("Arial", 8F, FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			lblCaption.Size = new Size(510, 40);
			lblCaption.Top += 68;
			lblCaption.Left += 36;
			vsbBRF.Left -= 38;
			vsbBRF.Height = 294;
			tabBrief.Height += 58;
			hsbBRF.Top += 70;
			hsbBRF.Width = 510;
			hsbBRF.Left += 36;
			lblInstruction.Top += 58;
			pnlBottomRight.Top += 58;
			pnlBottomLeft.Top += 58;
			dataT.Height += 58;
			dataS.Height += 58;
			lstEvents.Height += 58;
			cboColorTag.Items.Clear();
			cboColorTag.Items.Add("Green");
			cboColorTag.Items.Add("Red");
			cboColorTag.Items.Add("Yellow");
			cboColorTag.Items.Add("Purple");
			cboColorTag.Items.Add("Pink");
			cboColorTag.Items.Add("Blue");
			cboColor.Items.Clear();
			cboColor.Items.Add("Green");
			cboColor.Items.Add("Red");
			cboColor.Items.Add("Yellow");
			cboColor.Items.Add("Purple");
			cboColor.Items.Add("Pink");
			cboColor.Items.Add("Blue");
			cboEvent.Items.Add("New Icon");
			cboEvent.Items.Add("Show Ship Data");
			cboEvent.Items.Add("Move Icon");
			cboEvent.Items.Add("Rotate Icon");
			cboEvent.Items.Add("Switch to Region");
			cboCraft.Items.AddRange(Platform.Xwa.Strings.CraftType);
			cboNCraft.Items.AddRange(Platform.Xwa.Strings.CraftType);
			#endregion
			ImportDat(Application.StartupPath + "\\images\\XWA_BRF.dat", 56);
			_tags = _xwaBriefing.BriefingTag;
			_strings = _xwaBriefing.BriefingString;
			ImportStrings();
			_timerInterval = Platform.Xwa.Briefing.TicksPerSecond;
			txtLength.Text = Convert.ToString(Math.Round(((decimal)_xwaBriefing.Length / _timerInterval), 2));
			hsbTimer.Maximum = _xwaBriefing.Length + 11;
			w = pctBrief.Width;
			h = pctBrief.Height;
			mapX = 0;
			mapY = 0;
			lstEvents.Items.Clear();
			_briefData = new BriefData[100];	// this way I don't have to deal with expanding the array
			string[] names = new string[100];
			for (int i=0;i<_briefData.Length;i++) names[i] = "Icon #" + i;
			cboFG.Items.AddRange(names);
			cboFGTag.Items.AddRange(names);
			cboInfoCraft.Items.AddRange(names);
			cboRCraft.Items.AddRange(names);
			cboMoveIcon.Items.AddRange(names);
			cboNewIcon.Items.AddRange(names);
			ImportEvents(_xwaBriefing.Events);
			hsbTimer.Value = 0;
			numUnk1.Value = _xwaBriefing.Unknown1;
			numUnk3.Enabled = false;
			cboText.SelectedIndex = 0;
			cboFGTag.SelectedIndex = 0;
			cboTextTag.SelectedIndex = 0;
			cboColorTag.SelectedIndex = 0;
			cboInfoCraft.SelectedIndex = 0;
			cboRCraft.SelectedIndex = 0;
			cboRotateAmount.SelectedIndex = 0;
			cboMoveIcon.SelectedIndex = 0;
			cboNewIcon.SelectedIndex = 0;
			cboNCraft.SelectedIndex = 0;
			cboIconIff.SelectedIndex = 0;
			_loading = false;
		}

		public void Import(Platform.Tie.FlightGroupCollection fg)
		{
			_briefData = new BriefData[fg.Count];
			cboFG.Items.Clear();
			cboFGTag.Items.Clear();
			for (int i = 0; i < fg.Count; i++) Import(i, fg[i].CraftType, fg[i].Waypoints[14], fg[i].IFF, fg[i].Name);
		}
		public void Import(Platform.Xvt.FlightGroupCollection fg)
		{
			_briefData = new BriefData[fg.Count];
			cboFG.Items.Clear();
			cboFGTag.Items.Clear();
			for (int i = 0; i < fg.Count; i++) Import(i, fg[i].CraftType, fg[i].Waypoints[14], fg[i].IFF, fg[i].Name);
		}

		void Import(int index, int craftType, BaseFlightGroup.BaseWaypoint waypoint, byte iff, string name)
		{
			_briefData[index].Craft = craftType;
			_briefData[index].Waypoint = (short[])waypoint;
			_briefData[index].IFF = iff;
			_briefData[index].Name = name;
			cboFG.Items.Add(name);
			cboFGTag.Items.Add(name);
		}
		void ImportDat(string filename, int size)
		{
			try
			{
				FileStream fs = File.OpenRead(filename);
				BinaryReader br = new BinaryReader(fs);
				int count = br.ReadInt16();
				Bitmap bm = new Bitmap(count * size, size, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				Graphics g = Graphics.FromImage(bm);
				SolidBrush sb = new SolidBrush(Color.Black);
				g.FillRectangle(sb, 0, 0, bm.Width, bm.Height);
				byte[] blue = { 0, 0x48, 0x60, 0x78, 0x94, 0xAC, 0xC8, 0xE0, 0xFC };
				byte[] green = { 0, 0, 4, 0x10, 0x24, 0x3C, 0x58, 0x78, 0xA0 };
				for (int i=0;i<count;i++)
				{
					fs.Position = i*2+2;
					fs.Position = br.ReadUInt16();
					byte b;
					w = br.ReadByte();	// using these vars just because I can
					h = br.ReadByte();
					int x;
					for (int q=0;q<h;q++)
					{
						for (int r=0;r<(w+1)/2;r++)
						{
							b = br.ReadByte();
							int p1 = b & 0xF;
							int p2 = (b & 0xF0) >> 4;
							x = (size-w)/2 + size*i + r*2;
							if (_platform == Settings.Platform.TIE)
							{
								x = size/2 - w + size*i + r*4;
								if (p1 != 0)
								{
									bm.SetPixel(x, size/2 - h + q*2, Color.FromArgb(0, green[p1], blue[p1]));
									bm.SetPixel(x + 1, size/2 - h + q*2, Color.FromArgb(0, green[p1], blue[p1]));
									bm.SetPixel(x, size/2 - h + q*2 + 1, Color.FromArgb(0, green[p1], blue[p1]));
									bm.SetPixel(x + 1, size/2 - h + q*2 + 1, Color.FromArgb(0, green[p1], blue[p1]));
								}
								if (p2 != 0)
								{
									bm.SetPixel(x + 2, size/2 - h + q*2, Color.FromArgb(0, green[p2], blue[p2]));
									bm.SetPixel(x + 3, size/2 - h + q*2, Color.FromArgb(0, green[p2], blue[p2]));
									bm.SetPixel(x + 2, size/2 - h + q*2 + 1, Color.FromArgb(0, green[p2], blue[p2]));
									bm.SetPixel(x + 3, size/2 - h + q*2 + 1, Color.FromArgb(0, green[p2], blue[p2]));
								}
							}
							else if (_platform == Settings.Platform.XvT)
							{
								p1 = (p1 != 0 ? (5 - p1) * 0x28 : 0);
								p2 = (p2 != 0 ? (5 - p2) * 0x28 : 0);
								if (p1 != 0) bm.SetPixel(x, (size-h)/2 + q, Color.FromArgb(p1, p1, p1));
								if (p2 != 0) bm.SetPixel(x + 1, (size-h)/2 + q, Color.FromArgb(p2, p2, p2));
							}
							else
							{
								p1 = (p1 != 0 ? p1 * 0x10 + 0xF : 0);
								p2 = (p2 != 0 ? p2 * 0x10 + 0xF : 0);
								if (p1 != 0) bm.SetPixel(x, (size-h)/2 + q, Color.FromArgb(p1, p1, p1));
								if (p2 != 0) bm.SetPixel(x + 1, (size-h)/2 + q, Color.FromArgb(p2, p2, p2));
							}
						}
					}
				}
				imgCraft.ImageSize = new Size(size, size);
				imgCraft.Images.AddStrip(bm);
				fs.Close();
			}
			catch (Exception x)
			{
				MessageBox.Show(x.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
		}
		void ImportEvents(short[] rawEvents)
		{
			BaseBriefing brief = (_platform == Settings.Platform.TIE ? (BaseBriefing)_tieBriefing : (_platform == Settings.Platform.XvT ? (BaseBriefing)_xvtBriefing : (BaseBriefing)_xwaBriefing));
			int offset = 0;
			for (int i=0;i<_maxEvents;i++)
			{
				_events[i, 0] = rawEvents[offset++];		// time
				_events[i, 1] = rawEvents[offset++];		// event
				if (_events[i, 1] == 0 || _events[i, 1] == (short)BaseBriefing.EventType.EndBriefing) break;
				else
				{
					for (int j = 2; j < 2 + brief.EventParameterCount[_events[i, 1]]; j++, offset++) _events[i, j] = rawEvents[offset];
					if (_platform == Settings.Platform.XWA && _events[i, 1] == (short)BaseBriefing.EventType.XwaMoveIcon && _briefData[_events[i, 2]].Waypoint != null && _briefData[_events[i, 2]].Waypoint[0] == 0 && _briefData[_events[i, 2]].Waypoint[1] == 0)
					{	// this prevents Exception if Move instruction is before NewIcon, and only assigns initial position
						_briefData[_events[i, 2]].Waypoint[0] = _events[i, 3];
						_briefData[_events[i, 2]].Waypoint[1] = _events[i, 4];
					}
				}
				// okay, now that's in a usable format, list the event in lstEvents
				lstEvents.Items.Add("");
				ListUpdate(i);
			}
		}
		void ImportStrings()
		{
			tableTags.Columns.Add("tag");
			tableStrings.Columns.Add("string");
			for (int i=0;i<_tags.Length;i++)
			{
				DataRow dr = tableTags.NewRow();
				dr[0] = _tags[i];
				tableTags.Rows.Add(dr);
				dr = tableStrings.NewRow();
				dr[0] = _strings[i];
				tableStrings.Rows.Add(dr);
			}
			dataTags.Table = tableTags;
			dataStrings.Table = tableStrings;
			dataT.DataSource = dataTags;
			dataS.DataSource = dataStrings;
			this.tableTags.RowChanged += new DataRowChangeEventHandler(tableTags_RowChanged);
			this.tableStrings.RowChanged += new DataRowChangeEventHandler(tableStrings_RowChanged);
			LoadTags();
			LoadStrings();
		}

		public void Save()
		{
			BaseBriefing brief = (_platform == Settings.Platform.TIE ? (BaseBriefing)_tieBriefing : (_platform == Settings.Platform.XvT ? (BaseBriefing)_xvtBriefing : (BaseBriefing)_xwaBriefing));
			int offset = 0;
			brief.Unknown1 = (short)numUnk1.Value;
			for (int evnt = 0; evnt < _maxEvents; evnt++)
			{
				for (int i = 0; i < 2; i++, offset++) brief.Events[offset] = _events[evnt, i];
				if (_events[evnt, 1] == (short)BaseBriefing.EventType.EndBriefing) break;
				else for (int i = 2; i < 2 + brief.EventParameterCount[_events[evnt, 1]]; i++, offset++)
					brief.Events[offset] = _events[evnt, i];
			}
			if (_platform == Settings.Platform.XvT) _xvtBriefing.Unknown3 = (short)numUnk3.Value;
		}

		void tabBrief_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabBrief.SelectedIndex != 0) hsbTimer.Value = 1;
			else hsbTimer.Value = 0;	// force refresh, since pct doesn't want to update when hidden
		}

		#region frmBrief
		void frmBrief_Activated(object sender, EventArgs e) { MapPaint(); }
		void frmBrief_Closed(object sender, EventArgs e) { _map.Dispose(); }
		void frmBrief_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Save();
			/*if (_platform==Settings.Platform.TIE) TIESave();
			else if (_platform==Settings.Platform.XvT) XvTSave();
			else XWASave();*/
		}
		void frmBrief_Load(object sender, EventArgs e)
		{
			for (int i=0;i<8;i++) _fgTags[i, 0] = -1;
			for (int i=0;i<8;i++) _textTags[i, 0] = -1;
			_map = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		}
		#endregion	frmBrief
		#region tabDisplay
		#region Timer related
		void StartTimer()
		{
			cmdPlay.Enabled = false;
			cmdPlay.Visible = false;
			cmdPause.Enabled = true;
			cmdPause.Visible = true;
			cmdPause.Focus();
			tmrBrief.Start();
		}
		void StopTimer()
		{
			tmrBrief.Stop();
			cmdPause.Enabled = false;
			cmdPause.Visible = false;
			cmdPlay.Enabled = true;
			cmdPlay.Visible = true;
			cmdPlay.Focus();
			tmrBrief.Interval = 1000 / _timerInterval;
		}

		void cmdFF_Click(object sender, EventArgs e)
		{
			if (hsbTimer.Value == hsbTimer.Maximum-11) return;
			tmrBrief.Interval = 500 / _timerInterval;
			StartTimer();
		}
		void cmdNext_Click(object sender, EventArgs e)
		{
			int i;
			for (i=0;i<_maxEvents;i++)
			{
				if (_events[i, 0] <= hsbTimer.Value || _events[i, 1] != 3) continue;	// find next stop point after current position
				break;
			}
			if (i == _maxEvents) hsbTimer.Value = hsbTimer.Maximum-11;	// tmr_Tick takes care of halting
			else hsbTimer.Value = _events[i, 0];
		}
		void cmdPause_Click(object sender, EventArgs e) { StopTimer(); }
		void cmdPlay_Click(object sender, EventArgs e)
		{
			if (hsbTimer.Value == hsbTimer.Maximum-11) return;	// prevent starting if already at the end
			tmrBrief.Interval = 1000 / _timerInterval;
			StartTimer();
		}
		void cmdStart_Click(object sender, EventArgs e)
		{
			for (int i=0;i<8;i++)
			{
				_fgTags[i, 0] = -1;
				_fgTags[i, 1] = 0;
			}
			for (int i=0;i<8;i++)
			{
				_textTags[i, 0] = -1;
				_textTags[i, 1] = 0;
				_textTags[i, 2] = 0;
				_textTags[i, 3] = 0;
			}
			hsbTimer.Value = 0;
		}
		void cmdStop_Click(object sender, EventArgs e)
		{
			StopTimer();
			for (int i=0;i<8;i++)
			{
				_fgTags[i, 0] = -1;
				_fgTags[i, 1] = 0;
			}
			for (int i=0;i<8;i++)
			{
				_textTags[i, 0] = -1;
				_textTags[i, 1] = 0;
				_textTags[i, 2] = 0;
				_textTags[i, 3] = 0;
			}
			hsbTimer.Value = 0;
		}

		void hsbTimer_ValueChanged(object sender, EventArgs e)
		{
			if (hsbTimer.Value == 0)
			{
				#region reset
				_page = 1;
				mapX = 0; 
				mapY = 0;
				zoomX = 48;
				if (_platform == Settings.Platform.XWA) zoomX = 32;
				zoomY = zoomX;
				for (int h=0;h<8;h++)
				{
					_fgTags[h, 0] = -1;
					_fgTags[h, 1] = 0;
				}
				for (int h=0;h<8;h++)
				{
					_textTags[h, 0] = -1;
					_textTags[h, 1] = 0;
					_textTags[h, 2] = 0;
					_textTags[h, 3] = 0;
				}
				if (_platform == Settings.Platform.XWA) _briefData = new BriefData[_briefData.Length];
				_message = "";
				lblTitle.Visible = true;
				lblCaption.Visible = true;
				#endregion
			}
			if (_regionDelay != -1)
			{
				_message = "";
				_regionDelay = -1;
				lblCaption.Visible = true;
				lblTitle.Visible = true;
			}
			bool paint = false;
			for (int i=0;i<_maxEvents;i++)
			{
				if (_events[i,0] < hsbTimer.Value) continue;
				if (_events[i,0] > hsbTimer.Value || _events[i,1] == (int)BaseBriefing.EventType.None || _events[i,1] == (int)BaseBriefing.EventType.EndBriefing) break;
				#region event processing
				if (_events[i,1] == (int)BaseBriefing.EventType.PageBreak)
				{
					if (_platform == Settings.Platform.TIE) lblTitle.Text = "";
					lblCaption.Text = "";
					_page++;
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.TitleText && _platform == Settings.Platform.TIE)	// XvT and XWA use .LST files
				{
					if (_strings[_events[i, 2]].StartsWith(">"))
					{
						lblTitle.TextAlign = ContentAlignment.TopCenter;
						lblTitle.ForeColor = _titleColor;
						lblTitle.Text = _strings[_events[i, 2]].Replace(">", "");
					}
					else
					{
						lblTitle.TextAlign = ContentAlignment.TopLeft;
						lblTitle.ForeColor = _normalColor;
						lblTitle.Text = _strings[_events[i, 2]];
					}
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.CaptionText)
				{
					if (_strings[_events[i, 2]].StartsWith(">"))
					{
						lblCaption.TextAlign = ContentAlignment.TopCenter;
						lblCaption.ForeColor = _titleColor;
						lblCaption.Text = _strings[_events[i, 2]].Replace(">", "").Replace("$", "\r\n");
					}
					else
					{
						lblCaption.TextAlign = ContentAlignment.TopLeft;
						lblCaption.ForeColor = _normalColor;
						lblCaption.Text = _strings[_events[i, 2]].Replace("$", "\r\n");
					}
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.MoveMap)
				{
					mapX = _events[i, 2];
					mapY = _events[i, 3];
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.ZoomMap)
				{
					zoomX = _events[i, 2];
					zoomY = _events[i, 3];
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.ClearFGTags)
				{
					for (int h=0;h<8;h++)
					{
						_fgTags[h, 0] = -1;
						_fgTags[h, 1] = 0;
					}
					paint = true;
				}
				else if (_events[i, 1] >= (int)BaseBriefing.EventType.FGTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.FGTag8)
				{
					int v = _events[i, 1] - (int)BaseBriefing.EventType.FGTag1;
					_fgTags[v, 0] = _events[i, 2];	// FG number
					_fgTags[v, 1] = _events[i, 0];	// time started, for MapPaint
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.ClearTextTags)
				{
					for (int h=0;h<8;h++)
					{
						_textTags[h, 0] = -1;
						_textTags[h, 1] = 0;
						_textTags[h, 2] = 0;
						_textTags[h, 3] = 0;
					}
					paint = true;
				}
				else if (_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8)
				{
					int v = _events[i, 1] - (int)BaseBriefing.EventType.TextTag1;
					_textTags[v, 0] = _events[i, 2];	// tag#
					_textTags[v, 1] = _events[i, 3];	// X
					_textTags[v, 2] = _events[i, 4];	// Y
					_textTags[v, 3] = _events[i, 5];	// color
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaNewIcon)
				{
					_briefData[_events[i, 2]].Craft = _events[i, 3]-1;
					_briefData[_events[i, 2]].IFF = (byte)_events[i, 4];
					_briefData[_events[i, 2]].Name = "Icon #" + _events[i, 2].ToString();
					_briefData[_events[i, 2]].Waypoint = new short[4];
					if (_events[i, 3] != 0) _briefData[_events[i, 2]].Waypoint[3] = 1;
					else _briefData[_events[i, 2]].Waypoint[3] = 0;
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaShipInfo)
				{
					if (_events[i, 2] == 1)
					{
						if (_briefData[_events[i, 3]].Craft != 0) _message = "Ship Info: " + Platform.Xwa.Strings.CraftType[_briefData[_events[i, 3]].Craft+1]; 
						else _message = "Ship Info: <flight group not found>";
						lblTitle.Visible = false;
						lblCaption.Visible = false;
					}
					else
					{
						_message = "";
						lblTitle.Visible = true;
						lblCaption.Visible = true;
					}
					paint = true;
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaMoveIcon)
				{
					try
					{
						_briefData[_events[i, 2]].Waypoint[0] = _events[i, 3];
						_briefData[_events[i, 2]].Waypoint[1] = _events[i, 4];
						paint = true;
					}
					catch { /* do nothing*/ }
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaRotateIcon)
				{
					try
					{
						_briefData[_events[i, 2]].Waypoint[2] = _events[i, 3];
						paint = true;
					}
					catch { /* do nothing*/ }
				}
				else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaChangeRegion)
				{
					for (int h=0;h<8;h++)
					{
						_fgTags[h, 0] = -1;
						_fgTags[h, 1] = 0;
						_textTags[h, 0] = -1;
						_textTags[h, 1] = 0;
						_textTags[h, 2] = 0;
						_textTags[h, 3] = 0;
					}
					_briefData = new BriefData[_briefData.Length];
					_message = "Region " + (_events[i, 2]+1);
					_regionDelay = _timerInterval * 3;
					lblTitle.Visible = false;
					lblCaption.Visible = false;
					paint = true;
				}
				// don't need to account for EndBriefing
				#endregion
			}
			for (int h=0;h<8;h++) if (hsbTimer.Value - _fgTags[h, 1] < 13) paint = true;
			lblTime.Text = String.Format("{0:Time: 0.00}",(decimal)hsbTimer.Value / _timerInterval);
			if (hsbTimer.Value == (hsbTimer.Maximum-11) || hsbTimer.Value == 0) StopTimer();
			if (paint) MapPaint();	// prevent MapPaint from running if no change
		}

		void tmrBrief_Tick(object sender, EventArgs e)
		{
			if (_regionDelay == -1) hsbTimer.Value++;
			else if (_regionDelay == 0)
			{
				_message = "";
				_regionDelay--;
				lblCaption.Visible = true;
				lblTitle.Visible = true;
			}
			else _regionDelay--;
		}
		#endregion	Timer related

		void DrawGrid(int x, int y, Graphics g)
		{
			Pen pn = new Pen(Color.FromArgb(0x50, 0, 0));
			pn.Width = 1;
			if (_platform == Settings.Platform.TIE)
			{
				pn.Color = Color.FromArgb(0x48, 0, 0);
				pn.Width = 2;
			}
			int mod = (_platform == Settings.Platform.TIE ? 2 : 1);
			if (zoomX >= 32)
			{
				for (int i = 0;i < 12/mod;i++)
				{
					if (i % 4 == 0) continue;						// don't draw where there'll be maj lines
					g.DrawLine(pn, 0, zoomY*i*mod + y-1, w, zoomY*i*mod + y-1);	//min lines, every zoom pixels
					g.DrawLine(pn, 0, y-1 - zoomY*i*mod, w, y-1 - zoomY*i*mod);
					g.DrawLine(pn, zoomX*i*mod + x, 0, zoomX*i*mod + x, h);
					g.DrawLine(pn, x - zoomX*i*mod, 0, x - zoomX*i*mod, h);
				}
			}
			else if (zoomX >= 16)
			{
				for (int i = 0;i < 10/mod;i++)
				{
					if (i % 2 == 0) continue;
					g.DrawLine(pn, 0, zoomY*2*i*mod + y-1, w, zoomY*2*i*mod + y-1);	//min lines, every zoomx2 pixels
					g.DrawLine(pn, 0, y-1 - zoomY*2*i*mod, w, y-1 - zoomY*2*i*mod);
					g.DrawLine(pn, zoomX*2*i*mod + x, 0, zoomX*2*i*mod + x, h);
					g.DrawLine(pn, x - zoomX*2*i*mod, 0, x - zoomX*2*i*mod, h);
				}
			}
			// else if (j < 16) just don't draw them
			pn.Color = Color.FromArgb(0x90, 0, 0);
			if (_platform == Settings.Platform.TIE) pn.Color = Color.FromArgb(0x78, 0, 0);
			g.DrawLine(pn, 0, y-1, w, y-1);	// origin lines
			g.DrawLine(pn, x, 0, x, h);
			for (int i=0;i<36;i++)
			{
				g.DrawLine(pn, 0, zoomY*4*i*mod + y-1, w, zoomY*4*i*mod + y-1);	//maj lines, every zoomx4 pixels
				g.DrawLine(pn, 0, y-1 - zoomY*4*i*mod, w, y-1 - zoomY*4*i*mod);
				g.DrawLine(pn, zoomX*4*i*mod + x, 0, zoomX*4*i*mod + x, h);
				g.DrawLine(pn, x - zoomX*4*i*mod, 0, x - zoomX*4*i*mod, h);
			}
		}
		void EnableOkCancel(bool state)
		{
			cmdOk.Enabled = state;
			cmdCancel.Enabled = state;
			cmdClear.Enabled = !state;
			if (_platform == Settings.Platform.TIE) cmdTitle.Enabled = !state;
			cmdCaption.Enabled = !state;
			cmdFG.Enabled = !state;
			cmdText.Enabled = !state;
			cmdZoom.Enabled = !state;
			cmdMove.Enabled = !state;
			cmdBreak.Enabled = !state;
			if (!state)
			{
				pnlShipInfo.Visible = false;
				pnlShipTag.Visible = false;
				pnlTextTag.Visible = false;
				pnlRotate.Visible = false;
				pnlMove.Visible = false;
				pnlNew.Visible = false;
				pnlRegion.Visible = false;
			}
			if (_platform == Settings.Platform.XWA)
			{
				cmdMoveShip.Enabled = !state;
				cmdNewShip.Enabled = !state;
				cmdRotate.Enabled = !state;
				cmdShipInfo.Enabled = !state;
				cmdRegion.Enabled = !state;
			}
		}
		int FindExisting(BaseBriefing.EventType eventType)
		{
			int i;
			for (i = 0; i < _maxEvents; i++)
			{
				if (_events[i, 0] < hsbTimer.Value) continue;
				if (_events[i, 0] > hsbTimer.Value) return (i+10000);	// did not find existing, return next available + marker
				if (_events[i, 1] == (int)eventType) return i;	// found existing
			}
			return (i+10000);	// actually somehow got through the entire loop. odds of this happening is likely zero, but some moron will do it eventually
		}
		int FindNext() { return FindNext(hsbTimer.Value); }
		int FindNext(int time)
		{
			int i;
			for (i = 0; i < _maxEvents; i++)
			{
				if (_events[i, 0] < time) continue;
				if (_events[i, 0] > time) break;
			}
			return i;
		}
		Bitmap FlatMask(Bitmap craftImage, byte iff, byte intensity)
		{
			// okay, this one is just for FG tags.  flat image, I only care about the shape.
			Bitmap bmpNew = new Bitmap(craftImage);
			BitmapData bmData = GraphicsFunctions.GetBitmapData(bmpNew, PixelFormat.Format24bppRgb);
			byte[] pix = new byte[bmData.Stride*bmData.Height];
			GraphicsFunctions.CopyImageToBytes(bmData, pix);
			#region declare IFF
			byte[] rgb = new byte[3];
			switch (iff)
			{
				case 0:		// green
					rgb[1] = 1;
					break;
				case 2:		// blue
					rgb[1] = 2;
					rgb[2] = 1;
					break;
				case 3:		// purple
				case 5:		// purple2
					rgb[0] = 1;
					rgb[2] = 1;
					break;
				default:	// red
					rgb[0] = 1;
					break;
			}
			#endregion
			for (int y = 0;y < bmpNew.Height;y++)
			{
				for (int x = 0, pos = bmData.Stride*y;x < bmpNew.Width;x++)
				{
					if (pix[pos+x*3] == 0) continue;
					pix[pos+x*3] = (byte)(intensity * rgb[2]);
					pix[pos+x*3+1] = (byte)(rgb[1] == 2 ? (intensity==0xe0 ? 0x78 : (intensity==0x78 ? 0x10 : (intensity==0xfc ? 0xa0 : (intensity==0xc8 ? 0x58 : (intensity==0x94 ? 0x24 : 4))))) : intensity * rgb[1]);
					pix[pos+x*3+2] = (byte)(intensity * rgb[0]);
				}
			}
			GraphicsFunctions.CopyBytesToImage(pix, bmData);
			bmpNew.UnlockBits(bmData);
			bmpNew.MakeTransparent(Color.Black);
			return bmpNew;
		}
		int[] GetTagSize(int craft)
		{
			FileStream fs = File.OpenRead(Application.StartupPath + "\\images\\XvT_BRF.dat");
			BinaryReader br = new BinaryReader(fs);
			fs.Position = craft*2+2;
			fs.Position = br.ReadInt16();
			int[] size = new int[2];
			size[0] = br.ReadByte();
			size[1] = br.ReadByte();
			fs.Close();
			return size;	// size of base craft image as [width,height]
		}
		void ImageQuad(int x, int y, int spacing, Bitmap craftImage, Graphics g)
		{
			g.DrawImageUnscaled(craftImage, x+spacing, y+spacing);
			g.DrawImageUnscaled(craftImage, x+spacing, y-spacing);
			g.DrawImageUnscaled(craftImage, x-spacing, y-spacing);
			g.DrawImageUnscaled(craftImage, x-spacing, y+spacing);
		}
		public void MapPaint()
		{
			if (_platform == Settings.Platform.TIE) TIEPaint();
			else if (_platform == Settings.Platform.XvT) XvTPaint();
			else if (_platform == Settings.Platform.XWA) XWAPaint();
		}
		void TIEMask(Bitmap craftImage, byte iff)
		{
			// works a little different than mission map, everything guides off the B value and IFF
			// image is stored as blue due to non-standard G values that do not fit a good equation
			BitmapData bmData = GraphicsFunctions.GetBitmapData(craftImage, PixelFormat.Format24bppRgb);
			byte[] pix = new byte[bmData.Stride*bmData.Height];
			GraphicsFunctions.CopyImageToBytes(bmData, pix);
			#region declare IFF
			byte[] rgb = new byte[3];
			switch (iff)
			{
				case 0:		// green
					rgb[1] = 1;
					break;
				case 2:		// blue
					rgb[1] = 2;
					rgb[2] = 1;
					break;
				case 3:		// purple
				case 5:		// purple2
					rgb[0] = 1;
					rgb[2] = 1;
					break;
				default:	// red
					rgb[0] = 1;
					break;
			}
			#endregion
			for (int y = 0;y < craftImage.Height;y++)
			{
				for (int x = 0, pos = bmData.Stride*y;x < craftImage.Width;x++)
				{
					pix[pos + x * 3 + 2] = (byte)(pix[pos + x * 3] * rgb[0]);
					pix[pos+x*3+1] = (rgb[1] == 2 ? pix[pos+x*3+1] : (byte)(pix[pos+x*3] * rgb[1]));
					pix[pos + x * 3] = (byte)(pix[pos + x * 3] * rgb[2]);
				}
			}
			GraphicsFunctions.CopyBytesToImage(pix, bmData);
			craftImage.UnlockBits(bmData);
			craftImage.MakeTransparent(Color.Black);
		}
		void TIEPaint()
		{
			if (_loading) return;
			int X = 2*(-zoomX*mapX/256) + w/2;	// values are written out like this to force even numbers
			int Y = 2*(-zoomY*mapY/256) + h/2;
			Pen pn = new Pen(Color.FromArgb(0x48, 0, 0));
			pn.Width = 2;
			Graphics g3;
			g3 = Graphics.FromImage(_map);		//graphics obj, load from the memory bitmap
			g3.Clear(SystemColors.Control);		//clear it
			SolidBrush sb = new SolidBrush(Color.Black);
			g3.FillRectangle(sb, 0, 0, w, h);
			g3.DrawLine(pn, 0, 1, w, 1);
			g3.DrawLine(pn, 0, h-3, w, h-3);
			g3.DrawLine(pn, 1, 0, 1, h);
			g3.DrawLine(pn, w-1, 0, w-1, h);
			DrawGrid(X, Y, g3);
			Bitmap bmptemp;
			#region FG tags
			Bitmap bmptemp2;
			for (int i=0;i<8;i++)
			{
				if (_fgTags[i, 0] == -1 || _briefData[_fgTags[i, 0]].Waypoint[3] != 1) continue;
				sb = new SolidBrush(Color.FromArgb(0xE0, 0, 0));	// defaults to Red
				SolidBrush sb2 = new SolidBrush(Color.FromArgb(0x78, 0, 0));
				byte IFF = _briefData[_fgTags[i, 0]].IFF;
				int wpX = 2*(int)Math.Round((double)zoomX*_briefData[_fgTags[i, 0]].Waypoint[0]/256, 0) + X;
				int wpY = 2*(int)Math.Round((double)zoomY*_briefData[_fgTags[i, 0]].Waypoint[1]/256, 0) + Y;
				int frame = hsbTimer.Value - _fgTags[i, 1];
				if (_fgTags[i, 1] == 0) frame = 12;	// if tagged at t=0, just the box
				switch (frame)
				{
					case 0:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 32, bmptemp2, g3);
						break;
					case 1:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 32, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 28, bmptemp2, g3);
						break;
					case 2:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 32, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 28, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 24, bmptemp2, g3);
						break;
					case 3:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 32, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 28, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 24, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 20, bmptemp2, g3);
						break;
					case 4:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 28, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 24, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 20, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 16, bmptemp2, g3);
						break;
					case 5:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 24, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 20, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 16, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 12, bmptemp2, g3);
						break;
					case 6:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 20, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 16, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 12, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 8, bmptemp2, g3);
						break;
					case 7:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 16, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 12, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 8, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xFC);
						ImageQuad(wpX-16, wpY-16, 4, bmptemp2, g3);
						break;
					case 8:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 12, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 8, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0xC8);
						ImageQuad(wpX-16, wpY-16, 4, bmptemp2, g3);
						break;
					case 9:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 8, bmptemp2, g3);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x94);
						ImageQuad(wpX-16, wpY-16, 4, bmptemp2, g3);
						break;
					case 10:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = FlatMask(bmptemp, IFF, 0x60);
						ImageQuad(wpX-16, wpY-16, 4, bmptemp2, g3);
						break;
					case 11:
						if (IFF == 0) { sb.Color = Color.FromArgb(0, 0xE0, 0); sb2.Color = Color.FromArgb(0, 0x78, 0); }	// green
						else if (IFF == 2) { sb.Color = Color.FromArgb(0, 0x78, 0xE0); sb2.Color = Color.FromArgb(0, 0x10, 0x78); }	// blue
						else if (IFF == 3 || IFF == 5) { sb.Color = Color.FromArgb(0xE0, 0, 0xE0); sb2.Color = Color.FromArgb(0x78, 0, 0x78); }	// purple
						g3.FillRectangle(sb, wpX - 8, wpY - 8, 18, 18);
						g3.FillRectangle(sb2, wpX - 6, wpY - 6, 14, 14);
						break;
					default:
						// 12 or greater, just the box
						if (IFF == 0) { sb.Color = Color.FromArgb(0, 0xE0, 0); sb2.Color = Color.FromArgb(0, 0x78, 0); }	// green
						else if (IFF == 2) { sb.Color = Color.FromArgb(0, 0x78, 0xE0); sb2.Color = Color.FromArgb(0, 0x10, 0x78); }	// blue
						else if (IFF == 3 || IFF == 5) { sb.Color = Color.FromArgb(0xE0, 0, 0xE0); sb2.Color = Color.FromArgb(0x78, 0, 0x78); }	// purple
						g3.FillRectangle(sb, wpX - 12, wpY - 12, 26, 26);
						g3.FillRectangle(sb2, wpX - 10, wpY - 10, 22, 22);
						break;
				}
			}
			#endregion // FG tags
			#region text tags
			for (int i=0;i<8;i++)
			{
				if (_textTags[i, 0] == -1) continue;
				sb = new SolidBrush(Color.FromArgb(0xAC, 0, 0));	// default to red
				int clr = _textTags[i, 3];
				if (clr == 0) sb.Color = Color.FromArgb(0, 0xAC, 0);	// green
				// else if (clr == 1) sb.Color = Color.FramArgb(0xAC,0,0);	// red
				else if (clr == 2) sb.Color = Color.FromArgb(0xAC, 0, 0xAC);	// purple
				else if (clr == 3) sb.Color = Color.FromArgb(0, 0x2C, 0xAC);	// blue
				else if (clr == 4) sb.Color = Color.FromArgb(0xA8, 0, 0);	// red2
				else if (clr == 5) sb.Color = Color.FromArgb(0xFC, 0x54, 0x54);	// light red
				else if (clr == 6) sb.Color = Color.FromArgb(0x44, 0x44, 0x44);	// gray
				else if (clr == 7) sb.Color = Color.FromArgb(0xCC, 0xCC, 0xCC);	// white
				g3.DrawString(_tags[_textTags[i, 0]], new Font("MS Reference Sans Serif", 10), sb, 2*(int)Math.Round((double)zoomX*_textTags[i, 1]/256, 0) + X, 2*(int)Math.Round((double)zoomY*_textTags[i, 2]/256, 0) + Y);
			}
			#endregion	// text tags
			for (int i=0;i<_briefData.Length;i++)
			{
				if (_briefData[i].Waypoint[3] != 1) continue;
				if (zoomX >= 32) bmptemp = new Bitmap(imgCraft.Images[_briefData[i].Craft]);
				else bmptemp = new Bitmap(imgCraft.Images[_briefData[i].Craft+88]);	// small icon
				TIEMask(bmptemp, _briefData[i].IFF);
				// simple base-256 grid coords * zoom to get pixel location, * 2 to enlarge, + map offset, - pic size/2 to center
				// forced to even numbers
				g3.DrawImageUnscaled(bmptemp, 2*(int)Math.Round((double)zoomX*_briefData[i].Waypoint[0]/256, 0) + X - 16, 2*(int)Math.Round((double)zoomX*_briefData[i].Waypoint[1]/256, 0) + Y - 16);
			}
			g3.DrawString("#" + _page, new Font("Arial", 8), new SolidBrush(Color.White), w-20, 4);
			pctBrief.Invalidate();		// since it's drawing to memory, this refreshes the pct.  Removes the flicker when zooming
			g3.Dispose();
		}
		Bitmap XvTMask(Bitmap craftImage, byte iff, byte frame)
		{
			Bitmap bmpNew = new Bitmap(craftImage);
			BitmapData bmData = bmpNew.LockBits(new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			byte[] pix = new byte[bmData.Stride*bmData.Height];
			GraphicsFunctions.CopyImageToBytes(bmData, pix);
			#region define FG tag colors
			byte[] p = new byte[5];
			byte intensity = 255;
			if (frame == 2) intensity = 0xC5;
			else if (frame == 3) intensity = 0x8F;
			else if (frame == 4) intensity = 0x5B;
			p[0] = (byte)(0xB8 * (intensity+1) / 256);
			p[1] = (byte)(0x98 * (intensity+1) / 256);
			p[2] = (byte)(0x70 * (intensity+1) / 256);
			p[3] = (byte)(0x58 * (intensity+1) / 256);
			#endregion
			#region define IFF color distribution
			byte[] mask = new byte[3];
			byte[,] rgb = new byte[5, 3];
			switch (iff)
			{
				case 0:		// green
					mask[1] = 1;
					rgb[0, 0] = 0x38;
					rgb[0, 1] = 0xD4;
					rgb[1, 0] = 0x18;
					rgb[1, 1] = 0xA8;
					rgb[2, 0] = 8;
					rgb[2, 1] = 0x7C;
					rgb[3, 1] = 0x54;
					break;
				case 2:		// blue
					mask[2] = 1;
					rgb[0, 0] = 0x58;
					rgb[0, 1] = 0xDC;
					rgb[0, 2] = 0xF8;
					rgb[1, 0] = 0x28;
					rgb[1, 1] = 0x84;
					rgb[1, 2] = 0xC0;
					rgb[2, 0] = 8;
					rgb[2, 1] = 0x3C;
					rgb[2, 2] = 0x90;
					rgb[3, 1] = 8;
					rgb[3, 2] = 0x58;
					break;
				case 3:		// yellow
					mask[0] = 1;
					mask[1] = 1;
					rgb[0, 0] = 0xF8;
					rgb[0, 1] = 0xFC;
					rgb[1, 0] = 0xD0;
					rgb[1, 1] = 0xCC;
					rgb[2, 0] = 0xA8;
					rgb[2, 1] = 0x9C;
					rgb[3, 0] = 0x80;
					rgb[3, 1] = 0x74;
					break;
				case 5:		// purple
					mask[0] = 1;
					mask[2] = 1;
					rgb[0, 0] = 0x90;
					rgb[0, 1] = 0x88;
					rgb[0, 2] = 0xF0;
					rgb[1, 0] = 0x70;
					rgb[1, 1] = 0x5C;
					rgb[1, 2] = 0xB0;
					rgb[2, 0] = 0x50;
					rgb[2, 1] = 0x30;
					rgb[2, 2] = 0x78;
					rgb[3, 0] = 0x30;
					rgb[3, 1] = 8;
					rgb[3, 2] = 0x40;
					break;
				default:	// red
					mask[0] = 1;
					rgb[0, 0] = 0xF8;
					rgb[0, 1] = 0x24;
					rgb[1, 0] = 0xC0;
					rgb[1, 1] = 0x10;
					rgb[2, 0] = 0x80;
					rgb[2, 1] = 4;
					rgb[3, 0] = 0x48;
					break;
			}
			#endregion
			byte b;
			for (int y = 0;y < bmpNew.Height;y++)
			{
				for (int x = 0, pos = y*bmData.Stride;x < bmpNew.Width;x++)
				{
					// stupid thing returns BGR instead of RGB
					b = pix[pos+x*3+1];
					if (frame == 0)
					{
						pix[pos+x*3] = rgb[4 - (b/0x28), 2];
						pix[pos+x*3+1] = rgb[4 - (b/0x28), 1];
						pix[pos+x*3+2] = rgb[4 - (b/0x28), 0];
						continue;
					}
					b = p[4 - (b/0x28)];
					pix[pos+x*3] = (byte)(b * mask[2]);
					pix[pos+x*3+1] = (byte)(b * mask[1]);
					pix[pos+x*3+2] = (byte)(b * mask[0]);
				}
			}
			GraphicsFunctions.CopyBytesToImage(pix, bmData);
			bmpNew.UnlockBits(bmData);
			bmpNew.MakeTransparent(Color.Black);
			return bmpNew;
		}
		void XvTPaint()
		{
			if (_loading) return;
			int X = 2*(-zoomX*mapX/256) + w/2;	// values are written out like this to force even numbers
			int Y = 2*(-zoomY*mapY/256) + h/2;
			Pen pn = new Pen(Color.FromArgb(0x50, 0, 0));
			pn.Width = 1;
			Graphics g3;
			g3 = Graphics.FromImage(_map);		//graphics obj, load from the memory bitmap
			g3.Clear(SystemColors.Control);		//clear it
			SolidBrush sb = new SolidBrush(Color.Black);
			g3.FillRectangle(sb, 0, 0, w, h);
			DrawGrid(X, Y, g3);
			Bitmap bmptemp;
			#region FG tags
			Bitmap bmptemp2;
			for (int i=0;i<8;i++)
			{
				if (_fgTags[i, 0] == -1 || _briefData[_fgTags[i, 0]].Waypoint[3] != 1) continue;
				sb = new SolidBrush(Color.FromArgb(0xE0, 0, 0));	// defaults to Red
				SolidBrush sb2 = new SolidBrush(Color.FromArgb(0x78, 0, 0));
				byte IFF = _briefData[_fgTags[i, 0]].IFF;
				int wpX = (int)Math.Round((double)zoomX*_briefData[_fgTags[i, 0]].Waypoint[0]/256, 0) + X;
				int wpY = (int)Math.Round((double)zoomY*_briefData[_fgTags[i, 0]].Waypoint[1]/256, 0) + Y;
				int frame = hsbTimer.Value - _fgTags[i, 1];
				if (_fgTags[i, 1] == 0) frame = 12;	// if tagged at t=0, just the box
				int[] pos = GetTagSize(_briefData[_fgTags[i, 0]].Craft);
				switch (frame)
				{
					case 0:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 16, bmptemp2, g3);
						break;
					case 1:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 16, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 14, bmptemp2, g3);
						break;
					case 2:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 16, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 14, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 12, bmptemp2, g3);
						break;
					case 3:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 16, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 14, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 12, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 10, bmptemp2, g3);
						break;
					case 4:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 14, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 12, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 10, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 8, bmptemp2, g3);
						break;
					case 5:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 12, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 10, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 8, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 6, bmptemp2, g3);
						break;
					case 6:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 10, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 8, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 6, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 4, bmptemp2, g3);
						break;
					case 7:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 8, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 6, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 4, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 1);
						ImageQuad(wpX-11, wpY-11, 2, bmptemp2, g3);
						break;
					case 8:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 6, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 4, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 2);
						ImageQuad(wpX-11, wpY-11, 2, bmptemp2, g3);
						break;
					case 9:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 4, bmptemp2, g3);
						bmptemp2 = XvTMask(bmptemp, IFF, 3);
						ImageQuad(wpX-11, wpY-11, 2, bmptemp2, g3);
						break;
					case 10:
						bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
						bmptemp2 = XvTMask(bmptemp, IFF, 4);
						ImageQuad(wpX-11, wpY-11, 2, bmptemp2, g3);
						break;
					case 11:
						if (IFF == 0) { sb.Color = Color.FromArgb(0, 0xE0, 0); sb2.Color = Color.FromArgb(0, 0x78, 0); }	// green
						else if (IFF == 2) { sb.Color = Color.FromArgb(0, 0, 0xE0); sb2.Color = Color.FromArgb(0, 0, 0x78); }	// blue
						else if (IFF == 3) { sb.Color = Color.FromArgb(0xE0, 0xE0, 0); sb2.Color = Color.FromArgb(0x78, 0x78, 0); }	// yellow
						else if (IFF == 5) { sb.Color = Color.FromArgb(0xE0, 0, 0xE0); sb2.Color = Color.FromArgb(0x78, 0, 0x78); }	// purple
						g3.FillRectangle(sb, wpX - (pos[0]/2), wpY - (pos[1]/2), pos[0], pos[1]);
						g3.FillRectangle(sb2, wpX - (pos[0]/2-1), wpY - (pos[1]/2-1), pos[0]-2, pos[1]-2);
						break;
					default:
						// 12 or greater, just the box
						if (IFF == 0) { sb.Color = Color.FromArgb(0, 0xE0, 0); sb2.Color = Color.FromArgb(0, 0x78, 0); }	// green
						else if (IFF == 2) { sb.Color = Color.FromArgb(0, 0, 0xE0); sb2.Color = Color.FromArgb(0, 0, 0x78); }	// blue
						else if (IFF == 3) { sb.Color = Color.FromArgb(0xE0, 0xE0, 0); sb2.Color = Color.FromArgb(0x78, 0x78, 0); }	// yellow
						else if (IFF == 5) { sb.Color = Color.FromArgb(0xE0, 0, 0xE0); sb2.Color = Color.FromArgb(0x78, 0, 0x78); }	// purple
						g3.FillRectangle(sb, wpX - (pos[0]/2+2), wpY - (pos[1]/2+2), pos[0]+4, pos[1]+4);
						g3.FillRectangle(sb2, wpX - (pos[0]/2+1), wpY - (pos[1]/2+1), pos[0]+2, pos[1]+2);
						break;
				}
			}
			#endregion // FG tags
			#region text tags
			for (int i=0;i<8;i++)
			{
				if (_textTags[i, 0] == -1) continue;
				sb = new SolidBrush(Color.FromArgb(0xA8, 0, 0));	// default to red
				int clr = _textTags[i, 3];
				if (clr == 0) sb.Color = Color.FromArgb(0, 0xAC, 0);	// green
				//else if (clr == 1) sb.Color = Color.FromArgb(0xA8, 0, 0);	// red
				else if (clr == 2) sb.Color = Color.FromArgb(0xA8, 0xAC, 0);	// yellow
				else if (clr == 3) sb.Color = Color.FromArgb(0, 0x2C, 0xA8);	// blue
				else if (clr == 4) sb.Color = Color.FromArgb(0xA8, 0, 0xA8);	// purple
				else if (clr == 5) sb.Color = Color.Black;	// black, although this is just retarded against a near-black BG
				g3.DrawString(_tags[_textTags[i, 0]], new Font("MS Reference Sans Serif", 6), sb, (int)Math.Round((double)zoomX*_textTags[i, 1]/256, 0) + X, (int)Math.Round((double)zoomY*_textTags[i, 2]/256, 0) + Y);
			}
			#endregion	// text tags
			for (int i=0;i<_briefData.Length;i++)
			{
				if (_briefData[i].Waypoint[3] != 1) continue;
				bmptemp = new Bitmap(imgCraft.Images[_briefData[i].Craft]);
				bmptemp = XvTMask(bmptemp, _briefData[i].IFF, 0);
				int[] pos = GetTagSize(_briefData[i].Craft);
				// simple base-256 grid coords * zoom to get pixel location, + map offset, - pic size/2 to center
				g3.DrawImageUnscaled(bmptemp, (int)Math.Round((double)zoomX*_briefData[i].Waypoint[0]/256, 0) + X - 11 + (pos[0]%2), (int)Math.Round((double)zoomX*_briefData[i].Waypoint[1]/256, 0) + Y - 11);
			}
			g3.DrawString("#" + _page, new Font("Arial", 8), new SolidBrush(Color.White), w-20, 4);
			pctBrief.Invalidate();		// since it's drawing to memory, this refreshes the pct.  Removes the flicker when zooming
			g3.Dispose();
		}
		Bitmap XWAMask(Bitmap craftImage, byte iff)
		{
			Bitmap bmpNew = new Bitmap(craftImage);
			BitmapData bmData = bmpNew.LockBits(new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
			byte[] pix = new byte[bmData.Stride*bmData.Height];
			GraphicsFunctions.CopyImageToBytes(bmData, pix);
			#region define IFF color distribution
			byte[] rgb = new byte[3];
			switch (iff)
			{
				case 0:		// green
					rgb[0] = 0x40;
					rgb[1] = 0xBC;
					rgb[2] = 0x20;
					break;
				case 2:		// blue
					rgb[0] = 0x68;
					rgb[1] = 0x8C;
					rgb[2] = 0xF8;
					break;
				case 3:		// yellow
					rgb[0] = 0xE8;
					rgb[1] = 0xD0;
					rgb[2] = 0x40;
					break;
				case 5:		// purple
					rgb[0] = 0xF8;
					rgb[1] = 0x80;
					rgb[2] = 0xF8;
					break;
				default:	// red
					rgb[0] = 0xF8;
					rgb[1] = 0x54;
					rgb[2] = 0x50;
					break;
			}
			#endregion
			for (int y = 0;y < bmpNew.Height;y++)
			{
				for (int x = 0, pos = y*bmData.Stride;x < bmpNew.Width;x++)
				{
					pix[pos+x*3] = (byte)((pix[pos+x*3] * rgb[2]) / 256);
					pix[pos+x*3+1] = (byte)((pix[pos+x*3+1] * rgb[1]) / 256);
					pix[pos+x*3+2] = (byte)((pix[pos+x*3+2] * rgb[0]) / 256);
				}
			}
			GraphicsFunctions.CopyBytesToImage(pix, bmData);
			bmpNew.UnlockBits(bmData);
			bmpNew.MakeTransparent(Color.Black);
			return bmpNew;
		}
		void XWAPaint()
		{
			if (_loading) return;
			int X = 2*(-zoomX*mapX/256) + w/2;	// values are written out like this to force even numbers
			int Y = 2*(-zoomY*mapY/256) + h/2;
			Pen pn = new Pen(Color.FromArgb(0x50, 0, 0));
			pn.Width = 1;
			Graphics g3;
			g3 = Graphics.FromImage(_map);		//graphics obj, load from the memory bitmap
			g3.Clear(SystemColors.Control);		//clear it
			SolidBrush sb = new SolidBrush(Color.FromArgb(0x18, 0x18, 0x18));
			g3.FillRectangle(sb, 0, 0, w, h);
			if (_message != "")
			{
				sb.Color = Color.FromArgb(0xE7, 0xE3, 0);	// yellow
				StringFormat sf = new StringFormat();
				sf.Alignment = StringAlignment.Center;
				g3.DrawString(_message, new Font("Arial", 12, FontStyle.Bold), sb, w/2, h/2, sf);
				pctBrief.Invalidate();		// since it's drawing to memory, this refreshes the pct.  Removes the flicker when zooming
				g3.Dispose();
				return;
			}
			DrawGrid(X, Y, g3);
			Bitmap bmptemp;
			#region FG tags
			for (int i=0;i<8;i++)
			{
				if (_fgTags[i, 0] == -1 || _briefData[_fgTags[i, 0]].Waypoint[3] != 1) continue;
				sb = new SolidBrush(Color.FromArgb(0xE0, 0, 0));	// defaults to Red
				SolidBrush sb2 = new SolidBrush(Color.FromArgb(0x78, 0, 0));
				byte IFF = _briefData[_fgTags[i, 0]].IFF;
				if (IFF == 0) { sb.Color = Color.FromArgb(0, 0xE0, 0); sb2.Color = Color.FromArgb(0, 0x78, 0); }	// green
				else if (IFF == 2) { sb.Color = Color.FromArgb(0x60, 0x60, 0xE0); sb2.Color = Color.FromArgb(0x20, 0x20, 0x78); }	// blue
				else if (IFF == 3) { sb.Color = Color.FromArgb(0xE0, 0xE0, 0); sb2.Color = Color.FromArgb(0x78, 0x78, 0); }	// yellow
				else if (IFF == 5) { sb.Color = Color.FromArgb(0xE0, 0, 0xE0); sb2.Color = Color.FromArgb(0x78, 0, 0x78); }	// purple
				int wpX = (int)Math.Round((double)zoomX*_briefData[_fgTags[i, 0]].Waypoint[0]/256, 0) + X;
				int wpY = (int)Math.Round((double)zoomY*_briefData[_fgTags[i, 0]].Waypoint[1]/256, 0) + Y;
				int frame = hsbTimer.Value - _fgTags[i, 1];
				if (_fgTags[i, 1] == 0) frame = 12;	// if tagged at t=0, just the box
				byte r = sb.Color.R, b = sb.Color.B, g = sb.Color.G;
				bmptemp = new Bitmap(imgCraft.Images[_briefData[_fgTags[i, 0]].Craft]);
				bmptemp = XWAMask(bmptemp, IFF);
				if (_briefData[_fgTags[i, 0]].Waypoint[2] == 1) bmptemp.RotateFlip(RotateFlipType.Rotate270FlipNone);
				else if (_briefData[_fgTags[i, 0]].Waypoint[2] == 2) bmptemp.RotateFlip(RotateFlipType.Rotate180FlipNone);
				else if (_briefData[_fgTags[i, 0]].Waypoint[2] == 3) bmptemp.RotateFlip(RotateFlipType.Rotate90FlipNone);
				else if (_briefData[_fgTags[i, 0]].Waypoint[2] == 4) bmptemp.RotateFlip(RotateFlipType.RotateNoneFlipX);
				switch (frame)
				{
					case 0:
						ImageQuad(wpX-28, wpY-28, 16, bmptemp, g3);
						break;
					case 1:
						ImageQuad(wpX-28, wpY-28, 16, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 14, bmptemp, g3);
						break;
					case 2:
						ImageQuad(wpX-28, wpY-28, 16, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 14, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 12, bmptemp, g3);
						break;
					case 3:
						ImageQuad(wpX-28, wpY-28, 16, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 14, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 12, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 10, bmptemp, g3);
						break;
					case 4:
						ImageQuad(wpX-28, wpY-28, 14, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 12, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 10, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 8, bmptemp, g3);
						break;
					case 5:
						ImageQuad(wpX-28, wpY-28, 12, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 10, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 8, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 6, bmptemp, g3);
						break;
					case 6:
						ImageQuad(wpX-28, wpY-28, 10, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 8, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 6, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 4, bmptemp, g3);
						break;
					case 7:
						ImageQuad(wpX-28, wpY-28, 8, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 6, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 4, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 2, bmptemp, g3);
						break;
					case 8:
						g3.FillRectangle(sb2, wpX - 12, wpY - 11, 25, 22);
						ImageQuad(wpX-28, wpY-28, 6, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 4, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 2, bmptemp, g3);
						break;
					case 9:
						if (r == 0xE0) r = 0x90;
						else if (r == 0x60) r = 0x30;
						if (g == 0xE0) r = 0x94;
						if (b == 0xE0) b = 0x90;
						else if (b == 0x60) b = 0x30;
						sb.Color = Color.FromArgb(r, g, b);
						g3.FillRectangle(sb, wpX - 13, wpY - 12, 27, 24);
						g3.FillRectangle(sb2, wpX - 12, wpY - 11, 25, 22);
						ImageQuad(wpX-28, wpY-28, 4, bmptemp, g3);
						ImageQuad(wpX-28, wpY-28, 2, bmptemp, g3);
						break;
					case 10:
						if (r == 0xE0) r = 0xA8;
						else if (r == 0x60) r = 0x40;
						if (g == 0xE0) r = 0xAC;
						if (b == 0xE0) b = 0xA8;
						else if (b == 0x60) b = 0x40;
						sb.Color = Color.FromArgb(r, g, b);
						g3.FillRectangle(sb, wpX - 14, wpY - 13, 29, 26);
						g3.FillRectangle(sb2, wpX - 13, wpY - 12, 27, 24);
						ImageQuad(wpX-28, wpY-28, 2, bmptemp, g3);
						break;
					case 11:
						if (r == 0xE0) r = 0xC8;
						else if (r == 0x60) r = 0x50;
						if (g == 0xE0) r = 0xC8;
						if (b == 0xE0) b = 0xC8;
						else if (b == 0x60) b = 0x50;
						sb.Color = Color.FromArgb(r, g, b);
						g3.FillRectangle(sb, wpX - 15, wpY - 16, 31, 28);
						g3.FillRectangle(sb2, wpX - 14, wpY - 13, 29, 26);
						break;
					default:
						// 12 or greater, just the box
						g3.FillRectangle(sb, wpX - 17, wpY - 16, 35, 32);
						g3.FillRectangle(sb2, wpX - 16, wpY - 15, 33, 30);
						break;
				}
			}
			#endregion // FG tags
			#region text tags
			for (int i=0;i<8;i++)
			{
				if (_textTags[i, 0] == -1) continue;
				sb = new SolidBrush(Color.FromArgb(0xE7, 0, 0));	// default to red
				int clr = _textTags[i, 3];
				if (clr == 0) sb.Color = Color.FromArgb(0, 0xE3, 0);	// green
				//else if (clr == 1) sb.Color = Color.FromArgb(0xE7, 0, 0);	// red
				else if (clr == 2) sb.Color = Color.FromArgb(0xE7, 0xE3, 0);	// yellow
				else if (clr == 3) sb.Color = Color.FromArgb(0x63, 0x61, 0xE7);	// purple
				else if (clr == 4) sb.Color = Color.FromArgb(0xDE, 0, 0xDE);	// pink
				else if (clr == 5) sb.Color = Color.FromArgb(0, 4, 0xA5);	// blue
				g3.DrawString(_tags[_textTags[i, 0]], new Font("Arial", 9, FontStyle.Bold), sb, (int)Math.Round((double)zoomX*_textTags[i, 1]/256, 0) + X, (int)Math.Round((double)zoomY*_textTags[i, 2]/256, 0) + Y);
			}
			#endregion
			for (int i=0;i<_briefData.Length;i++)
			{
				if (_briefData[i].Waypoint == null || _briefData[i].Waypoint[3] != 1) continue;
				bmptemp = new Bitmap(imgCraft.Images[_briefData[i].Craft]);
				bmptemp = XWAMask(bmptemp, _briefData[i].IFF);
				if (_briefData[i].Waypoint[2] == 1) bmptemp.RotateFlip(RotateFlipType.Rotate270FlipNone);
				else if (_briefData[i].Waypoint[2] == 2) bmptemp.RotateFlip(RotateFlipType.Rotate180FlipNone);
				else if (_briefData[i].Waypoint[2] == 3) bmptemp.RotateFlip(RotateFlipType.Rotate90FlipNone);
				else if (_briefData[i].Waypoint[2] == 4) bmptemp.RotateFlip(RotateFlipType.RotateNoneFlipX);
				// simple base-256 grid coords * zoom to get pixel location, + map offset, - pic size/2 to center
				g3.DrawImageUnscaled(bmptemp, (int)Math.Round((double)zoomX*_briefData[i].Waypoint[0]/256, 0) + X - 28, (int)Math.Round((double)zoomX*_briefData[i].Waypoint[1]/256, 0) + Y - 28);
			}
			g3.DrawString("#" + _page, new Font("Arial", 8), new SolidBrush(Color.White), w-20, 4);
			pctBrief.Invalidate();		// since it's drawing to memory, this refreshes the pct.  Removes the flicker when zooming
			g3.Dispose();
		}

		void cboColorTag_SelectedIndexChanged(object sender, EventArgs e)
		{
			_textTags[(int)numText.Value-1, 3] = cboColorTag.SelectedIndex;
			MapPaint();
		}
		void cboIconIff_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_briefData[_icon].IFF = (byte)cboIconIff.SelectedIndex;
			MapPaint();
		}
		void cboMoveIcon_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			if (tempX != -621 && tempY != -621)
			{
				try
				{
					_briefData[cboMoveIcon.SelectedIndex].Waypoint[0] = _briefData[_icon].Waypoint[0];
					_briefData[cboMoveIcon.SelectedIndex].Waypoint[1] = _briefData[_icon].Waypoint[1];
				}
				catch (NullReferenceException) { /* do nothing*/ }
				finally
				{
					try
					{
						_briefData[_icon].Waypoint[0] = tempX;
						_briefData[_icon].Waypoint[1] = tempY;
					}
					catch (NullReferenceException) { /* do nothing*/ }
					_icon = (short)cboMoveIcon.SelectedIndex;
				}
			}
			MapPaint();
		}
		void cboNCraft_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_briefData[_icon].Craft = cboNCraft.SelectedIndex-1;
			if (cboNCraft.SelectedIndex == 0) _briefData[_icon].Waypoint[3] = 0;
			MapPaint();
		}
		void cboNewIcon_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_briefData[_icon] = tempBD;
			_icon = (short)cboNewIcon.SelectedIndex;
			tempBD = _briefData[_icon];
			_briefData[_icon].Craft = cboNCraft.SelectedIndex-1;
			_briefData[_icon].IFF = (byte)cboIconIff.SelectedIndex;
			if (tempX != -621 && tempY != -621)
			{
				_briefData[_icon].Waypoint = new short[4];
				_briefData[_icon].Waypoint[0] = tempX;
				_briefData[_icon].Waypoint[1] = tempY;
				if (cboNCraft.SelectedIndex != 0) _briefData[_icon].Waypoint[3] = 1;
			}
			MapPaint();
		}
		void cboRCraft_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			try { _briefData[_icon].Waypoint[2] = tempX; }
			catch (NullReferenceException) { /* do nothing */ }
			_icon = (short)cboRCraft.SelectedIndex;
			try
			{
				tempX = _briefData[_icon].Waypoint[2];
				_briefData[_icon].Waypoint[2] = (short)cboRotateAmount.SelectedIndex;
			}
			catch (NullReferenceException) { tempX = 0; }
			MapPaint();
		}
		void cboRotateAmount_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			try { _briefData[_icon].Waypoint[2] = (short)cboRotateAmount.SelectedIndex; }
			catch (NullReferenceException) { /* do nothing*/ }
			MapPaint();
		}
		void cboTextTag_SelectedIndexChanged(object sender, EventArgs e)
		{
			_textTags[(int)numText.Value-1, 0] = cboTextTag.SelectedIndex;
			MapPaint();
		}

		void cmdBreak_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.PageBreak;
			_page++;
			EnableOkCancel(true);
		}
		void cmdCancel_Click(object sender, EventArgs e)
		{
			cboText.Enabled = false;
			optFG.Enabled = false;
			optText.Enabled = false;
			lblTitle.Visible = true;
			lblCaption.Visible = true;
			hsbBRF.Visible = false;
			vsbBRF.Visible = false;
			lblInstruction.Visible = false;
			if (_eventType == BaseBriefing.EventType.PageBreak && sender.ToString() != "OK") { _page--; }
			else if (_eventType == BaseBriefing.EventType.TextTag1 && sender.ToString() != "OK") { _textTags = tempTags; }
			else if (_eventType == BaseBriefing.EventType.MoveMap && sender.ToString() != "OK")
			{
				mapX = tempX;
				mapY = tempY;
			}
			else if (_eventType == BaseBriefing.EventType.ZoomMap && sender.ToString() != "OK")
			{
				zoomX = tempX;
				zoomY = tempY;
			}
			else if (_eventType == BaseBriefing.EventType.XwaRotateIcon && sender.ToString() != "OK")
			{
				try { _briefData[_icon].Waypoint[2] = tempX; }
				catch (NullReferenceException) { /* do nothing */ }
			}
			else if (_eventType == BaseBriefing.EventType.XwaMoveIcon && sender.ToString() != "OK")
			{
				try
				{
					_briefData[_icon].Waypoint[0] = tempX;
					_briefData[_icon].Waypoint[1] = tempY;
				}
				catch (NullReferenceException) { /* do nothing */ }
			}
			_eventType = 0;
			EnableOkCancel(false);
			MapPaint();
		}
		void cmdCaption_Click(object sender, EventArgs e)
		{
			cboText.Enabled = true;
			_eventType = BaseBriefing.EventType.CaptionText;
			EnableOkCancel(true);
		}
		void cmdClear_Click(object sender, EventArgs e)
		{
			optFG.Enabled = true;
			optText.Enabled = true;
			_eventType = BaseBriefing.EventType.ClearFGTags;
			EnableOkCancel(true);
		}
		void cmdFG_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.FGTag1;
			pnlShipTag.Visible = true;
			EnableOkCancel(true);
		}
		void cmdOk_Click(object sender, EventArgs e)
		{
			if (_eventType == BaseBriefing.EventType.ClearFGTags) if (optText.Checked) _eventType = BaseBriefing.EventType.ClearTextTags;
			int i = -1;
			switch (_eventType)
			{
				case BaseBriefing.EventType.PageBreak:
					#region page break
					i = FindExisting(_eventType);
					if (i < 10000) { _page--; break; }	// no further action, existing break found
					i -= 10000;
					try
					{
						lstEvents.SelectedIndex = i;	// this will throw for last event
						InsertEvent();
					}
					catch (ArgumentOutOfRangeException)
					{
						lstEvents.Items.Add("");
						for (int n=i+2;n>i;n--)
						{
							if (_events[n-1, 1] == 0) continue;
							for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					for (int n=2;n<6;n++) _events[i, n] = 0;
					if (_platform == Settings.Platform.TIE) lblTitle.Text = "";
					lblCaption.Text = "";
					break;
					#endregion
				case BaseBriefing.EventType.TitleText:
					#region title
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)cboText.SelectedIndex;
					for (int n=3;n<6;n++) _events[i, n] = 0;
					if (_strings[_events[i, 2]].StartsWith(">"))
					{
						lblTitle.TextAlign = ContentAlignment.TopCenter;
						lblTitle.ForeColor = _titleColor;
						lblTitle.Text = _strings[_events[i, 2]].Replace(">", "");
					}
					else
					{
						lblTitle.TextAlign = ContentAlignment.TopLeft;
						lblTitle.ForeColor = _normalColor;
						lblTitle.Text = _strings[_events[i, 2]];
					}
					break;
					#endregion
				case BaseBriefing.EventType.CaptionText:
					#region caption
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)cboText.SelectedIndex;
					for (int n=3;n<6;n++) _events[i, n] = 0;
					if (_strings[_events[i, 2]].StartsWith(">"))
					{
						lblCaption.TextAlign = ContentAlignment.TopCenter;
						lblCaption.ForeColor = _titleColor;
						lblCaption.Text = _strings[_events[i, 2]].Replace(">", "");
					}
					else
					{
						lblCaption.TextAlign = ContentAlignment.TopLeft;
						lblCaption.ForeColor = _normalColor;
						lblCaption.Text = _strings[_events[i, 2]];
					}
					break;
					#endregion
				case BaseBriefing.EventType.MoveMap:
					#region move
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = mapX;
					_events[i, 3] = mapY;
					// don't need to repaint, done while adjusting values
					break;
					#endregion
				case BaseBriefing.EventType.ZoomMap:
					#region zoom
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = zoomX;
					_events[i, 3] = zoomY;
					// don't need to repaint, done while adjusting values
					break;
					#endregion
				case BaseBriefing.EventType.ClearFGTags:
					#region clear FG
					i = FindExisting(_eventType);
					if (i < 10000) break;	// no further action, existing break found
					i -= 10000;
					try
					{
						lstEvents.SelectedIndex = i;	// this will throw for last event
						InsertEvent();
					}
					catch (ArgumentOutOfRangeException)
					{
						lstEvents.Items.Add("");
						for (int n=i+2;n>i;n--)
						{
							if (_events[n-1, 1] == 0) continue;
							for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					for (int n=2;n<6;n++) _events[i, n] = 0;
					for (int n=0;n<8;n++)
					{
						_fgTags[n, 0] = -1;
						_fgTags[n, 1] = 0;
					}
					break;
					#endregion
				case BaseBriefing.EventType.FGTag1:
					#region FG
					_eventType = (BaseBriefing.EventType)((int)_eventType + numFG.Value - 1);
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)cboFGTag.SelectedIndex;
					for (int n=3;n<6;n++) _events[i, n] = 0;
					_fgTags[(int)_eventType-9, 0] = _events[i, 2];
					_fgTags[(int)_eventType-9, 1] = _events[i, 0];
					MapPaint();
					break;
					#endregion
				case BaseBriefing.EventType.ClearTextTags:
					#region clear text
					i = FindExisting(_eventType);
					if (i < 10000) break;	// no further action, existing break found
					i -= 10000;
					try
					{
						lstEvents.SelectedIndex = i;	// this will throw for last event
						InsertEvent();
					}
					catch (ArgumentOutOfRangeException)
					{
						lstEvents.Items.Add("");
						for (int n=i+2;n>i;n--)
						{
							if (_events[n-1, 1] == 0) continue;
							for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					for (int n=2;n<6;n++) _events[i, n] = 0;
					for (int n=0;n<8;n++)
					{
						_textTags[n, 0] = -1;
						_textTags[n, 1] = 0;
					}
					break;
					#endregion
				case BaseBriefing.EventType.TextTag1:
					#region text
					_eventType = (BaseBriefing.EventType)((int)_eventType + numText.Value - 1);
					// can't use FindExisting, due to extra parameter
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						if (tempX == -621 && tempY == -621)
						{
							MessageBox.Show("No tag location selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							i = 0;
							break;
						}
						i -= 10000;	// if one wasn't found, remove marker, create it.
						try
						{
							lstEvents.SelectedIndex = i;
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					else
					{
						// found existing, just see if we change location or not
						if (tempX == -621 && tempY == -621)
						{
							tempX = _events[i, 3];
							tempY = _events[i, 4];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)cboTextTag.SelectedIndex;
					_events[i, 3] = tempX;
					_events[i, 4] = tempY;
					_events[i, 5] = (short)cboColorTag.SelectedIndex;
					// don't need to repaint or restore/edit from backup, as it's taken care of during placement
					break;
					#endregion
				case BaseBriefing.EventType.XwaNewIcon:
					#region new icon
					if (tempX == -621 && tempY == -621 && cboNCraft.SelectedIndex == 0)
					{
						MessageBox.Show("No craft location selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						i = 0;
						break;
					}
					// start with the NewIcon command
					i = FindNext();
					try
					{
						lstEvents.SelectedIndex = i;	// this will throw for last event
						InsertEvent();
					}
					catch (ArgumentOutOfRangeException)
					{
						lstEvents.Items.Add("");
						for (int n=i+2;n>i;n--)
						{
							if (_events[n-1, 1] == 0) continue;
							for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = _icon;
					_events[i, 3] = (short)cboNCraft.SelectedIndex;
					_events[i, 4] = (short)cboIconIff.SelectedIndex;
					_events[i, 5] = 0;
					ListUpdate(i);
					// and now the MoveIcon 
					if (cboNCraft.SelectedIndex != 0)
					{
						i = FindNext();
						try
						{
							lstEvents.SelectedIndex = i;	// this will throw for last event
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
						_events[i, 0] = (short)hsbTimer.Value;
						_events[i, 1] = (short)BaseBriefing.EventType.XwaMoveIcon;
						_events[i, 2] = _icon;
						_events[i, 3] = tempX;
						_events[i, 4] = tempY;
						_events[i, 5] = 0;
					}
					break;
					#endregion
				case BaseBriefing.EventType.XwaShipInfo:
					#region info
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;
						try
						{
							lstEvents.SelectedIndex = i;	// this will throw for last event
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)(optInfoOn.Checked ? 1 : 0);
					_events[i, 3] = (short)cboInfoCraft.SelectedIndex;
					for (int n=4;n<6;n++) _events[i, n] = 0;
					break;
					#endregion
				case BaseBriefing.EventType.XwaMoveIcon:
					#region move icon
					if (tempX == -621 && tempY == -621)
					{
						MessageBox.Show("No craft location or valid icon selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						i = 0;
						break;
					}
					if (numMoveTime.Value == 0)
					{
						i = FindNext();		// could be lots of Moves at one time
						try
						{
							lstEvents.SelectedIndex = i;	// this will throw for last event
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
						_events[i, 0] = (short)hsbTimer.Value;
						_events[i, 1] = (short)_eventType;
						_events[i, 2] = _icon;
						_events[i, 3] = _briefData[_icon].Waypoint[0];
						_events[i, 4] = _briefData[_icon].Waypoint[1];
						_events[i, 5] = 0;
					}
					else
					{
						int t0 = hsbTimer.Value, x = _briefData[_icon].Waypoint[0], y = _briefData[_icon].Waypoint[1];
						int total = (int)Math.Round(numMoveTime.Value * _timerInterval);
						for (int j=0;j<=total;j++)
						{
							i = FindNext(j + t0);
							try
							{
								lstEvents.SelectedIndex = i;	// this will throw for last event
								InsertEvent();
							}
							catch (ArgumentOutOfRangeException)
							{
								lstEvents.Items.Add("");
								for (int n=i+2;n>i;n--)
								{
									if (_events[n-1, 1] == 0) continue;
									for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
								}
							}
							_events[i, 0] = (short)(j + t0);
							_events[i, 1] = (short)_eventType;
							_events[i, 2] = _icon;
							_events[i, 3] = (short)((x-tempX) * j / total + tempX);
							_events[i, 4] = (short)((y-tempY) * j / total + tempY);
							_events[i, 5] = 0;
							ListUpdate(i);
						}
					}
					break;
					#endregion
				case BaseBriefing.EventType.XwaRotateIcon:
					#region rotate
					i = FindNext();		// could be lots of Rotates at one time
					try
					{
						lstEvents.SelectedIndex = i;	// this will throw for last event
						InsertEvent();
					}
					catch (ArgumentOutOfRangeException)
					{
						lstEvents.Items.Add("");
						for (int n=i+2;n>i;n--)
						{
							if (_events[n-1, 1] == 0) continue;
							for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = _icon;
					_events[i, 3] = (short)cboRotateAmount.SelectedIndex;
					for (int n=4;n<6;n++) _events[i, n] = 0;
					break;
					#endregion
				case BaseBriefing.EventType.XwaChangeRegion:
					#region region
					i = FindExisting(_eventType);
					if (i > 10000)
					{
						i -= 10000;
						try
						{
							lstEvents.SelectedIndex = i;	// this will throw for last event
							InsertEvent();
						}
						catch (ArgumentOutOfRangeException)
						{
							lstEvents.Items.Add("");
							for (int n=i+2;n>i;n--)
							{
								if (_events[n-1, 1] == 0) continue;
								for (int h=0;h<6;h++) _events[n, h] = _events[n-1, h];
							}
						}
					}
					_events[i, 0] = (short)hsbTimer.Value;
					_events[i, 1] = (short)_eventType;
					_events[i, 2] = (short)(numNewRegion.Value - 1);
					for (int n=3;n<6;n++) _events[i, n] = 0;
					break;
					#endregion
				default:	// this shouldn't be possible
					break;
			}
			lstEvents.SelectedIndex = i;
			ListUpdate(i);
			cmdCancel_Click("OK", new System.EventArgs());
		}
		void cmdMove_Click(object sender, EventArgs e)
		{
			lblTitle.Visible = false;
			lblCaption.Visible = false;
			hsbBRF.Maximum = 32768;
			hsbBRF.Minimum = -32767;
			hsbBRF.Value = mapX;
			hsbBRF.Visible = true;
			vsbBRF.Maximum = 32768;
			vsbBRF.Minimum = -32767;
			vsbBRF.Value = mapY;
			vsbBRF.Visible = true;
			tempX = mapX;
			tempY = mapY;
			_eventType = BaseBriefing.EventType.MoveMap;
			EnableOkCancel(true);
		}
		void cmdMoveShip_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.XwaMoveIcon;
			EnableOkCancel(true);
			pnlMove.Visible = true;
			lblTitle.Visible = false;
			lblCaption.Visible = false;
			lblInstruction.Visible = true;
			tempX = -621;
			tempY = -621;
			_icon = (short)cboMoveIcon.SelectedIndex;
		}
		void cmdNewShip_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.XwaNewIcon;
			EnableOkCancel(true);
			pnlNew.Visible = true;
			lblTitle.Visible = false;
			lblCaption.Visible = false;
			lblInstruction.Visible = true;
			tempX = -621;
			tempY = -621;
			_icon = (short)cboNewIcon.SelectedIndex;
			tempBD = _briefData[_icon];
		}
		void cmdRegion_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.XwaChangeRegion;
			EnableOkCancel(true);
			pnlRegion.Visible = true;
		}
		void cmdRotate_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.XwaRotateIcon;
			EnableOkCancel(true);
			pnlRotate.Visible = true;
			_icon = (short)cboRCraft.SelectedIndex;
			try { cboRotateAmount.SelectedIndex = _briefData[_icon].Waypoint[2]; }
			catch (NullReferenceException) { cboRotateAmount.SelectedIndex = 0; }
			tempX = (short)cboRotateAmount.SelectedIndex;
		}
		void cmdShipInfo_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.XwaShipInfo;
			EnableOkCancel(true);
			pnlShipInfo.Visible = true;
		}
		void cmdText_Click(object sender, EventArgs e)
		{
			_eventType = BaseBriefing.EventType.TextTag1;
			pnlTextTag.Visible = true;
			lblTitle.Visible = false;
			lblCaption.Visible = false;
			lblInstruction.Visible = true;
			tempTags = _textTags;
			tempX = -621;
			tempY = -621;
			EnableOkCancel(true);
		}
		void cmdTitle_Click(object sender, EventArgs e)
		{
			cboText.Enabled = true;
			_eventType = BaseBriefing.EventType.TitleText;
			EnableOkCancel(true);
		}
		void cmdZoom_Click(object sender, EventArgs e)
		{
			lblTitle.Visible = false;
			lblCaption.Visible = false;
			hsbBRF.Value = zoomX;
			hsbBRF.Minimum = 1;
			hsbBRF.Maximum = 300;
			hsbBRF.Visible = true;
			vsbBRF.Value = zoomY;
			vsbBRF.Minimum = 1;
			vsbBRF.Maximum = 300;
			vsbBRF.Visible = true;
			tempX = zoomX;
			tempY = zoomY;
			_eventType = BaseBriefing.EventType.ZoomMap;
			EnableOkCancel(true);
		}

		void hsbBRF_ValueChanged(object sender, EventArgs e)
		{
			if (_eventType == BaseBriefing.EventType.MoveMap) mapX = (short)hsbBRF.Value;
			if (_eventType == BaseBriefing.EventType.ZoomMap) zoomX = (short)hsbBRF.Value;
			MapPaint();
		}

		void numText_ValueChanged(object sender, EventArgs e)
		{
			_textTags = tempTags;	// restore and re-edit
			_textTags[(int)numText.Value-1, 0] = cboTextTag.SelectedIndex;
			_textTags[(int)numText.Value-1, 1] = tempX;
			_textTags[(int)numText.Value-1, 2] = tempY;
			_textTags[(int)numText.Value-1, 3] = cboColorTag.SelectedIndex;
			MapPaint();
		}

		void pctBrief_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button.ToString() != "Left") return;
			if (_eventType == BaseBriefing.EventType.TextTag1)
			{
				int mod = (_platform != Settings.Platform.TIE ? 2 : 1);
				_textTags = tempTags;	// restore backup before messing with it again
				tempX = (short)(128 * e.X / zoomX * mod - 64 * w / zoomX * mod + mapX);
				tempY = (short)(128 * e.Y / zoomY * mod - 64 * h / zoomY * mod + mapY);
				_textTags[(int)numText.Value-1, 0] = cboTextTag.SelectedIndex;
				_textTags[(int)numText.Value-1, 1] = tempX;
				_textTags[(int)numText.Value-1, 2] = tempY;
				_textTags[(int)numText.Value-1, 3] = cboColorTag.SelectedIndex;
				MapPaint();
			}
			else if (_eventType == BaseBriefing.EventType.XwaNewIcon)
			{
				_briefData[_icon].Waypoint = new short[4];
				tempX = (short)(256 * e.X / zoomX - 128 * w / zoomX + mapX);
				tempY = (short)(256 * e.Y / zoomY - 128 * h / zoomY + mapY);
				_briefData[_icon].Waypoint[0] = tempX;
				_briefData[_icon].Waypoint[1] = tempY;
				if (cboNCraft.SelectedIndex != 0) _briefData[_icon].Waypoint[3] = 1;
				else _briefData[_icon].Waypoint[3] = 0;
				MapPaint();
			}
			else if (_eventType == BaseBriefing.EventType.XwaMoveIcon)
			{
				try
				{
					if (tempX == -621 && tempY == -621)
					{
						tempX = _briefData[_icon].Waypoint[0];
						tempY = _briefData[_icon].Waypoint[1];
					}
					_briefData[_icon].Waypoint[0] = (short)(256 * e.X / zoomX - 128 * w / zoomX + mapX);
					_briefData[_icon].Waypoint[1] = (short)(256 * e.Y / zoomY - 128 * h / zoomY + mapY);
				}
				catch (NullReferenceException) { /* do nothing*/ }
				MapPaint();
			}
		}
		void pctBrief_Paint(object sender, PaintEventArgs e)
		{
			Graphics objGraphics;
			//You can't modify e.Graphics directly.
			objGraphics = e.Graphics;
			// Draw the contents of the bitmap on the form.
			objGraphics.DrawImage(_map, 0, 0, _map.Width, _map.Height);
		}

		void txtLength_TextChanged(object sender, EventArgs e)
		{
			short t_Length;
			switch (_platform)
			{
				case Settings.Platform.TIE:
					try
					{
						t_Length = (short)Math.Round(Convert.ToDecimal(txtLength.Text) * _timerInterval,0);	// this is the line that could throw
						_tieBriefing.Length = t_Length;
						hsbTimer.Maximum = _tieBriefing.Length + 11;
						if (Math.Round(((decimal)_tieBriefing.Length / _timerInterval), 2) != Convert.ToDecimal(txtLength.Text))	// so things like .51 become .5, without
							txtLength.Text = Convert.ToString(Math.Round(((decimal)_tieBriefing.Length / _timerInterval), 2));	// wiping out just a decimal
					}
					catch  { txtLength.Text = Convert.ToString(Math.Round(((decimal)_tieBriefing.Length / _timerInterval), 2)); }
					break;
				case Settings.Platform.XvT:
					try
					{
						t_Length = (short)Math.Round(Convert.ToDecimal(txtLength.Text) * _timerInterval, 0);	// this is the line that could throw
						_xvtBriefing.Length = t_Length;
						hsbTimer.Maximum = _xvtBriefing.Length + 11;
						if (Math.Round(((decimal)_xvtBriefing.Length / _timerInterval), 2) != Convert.ToDecimal(txtLength.Text))	// so things like .51 become .5, without
							txtLength.Text = Convert.ToString(Math.Round(((decimal)_xvtBriefing.Length / _timerInterval), 2));	// wiping out just a decimal
					}
					catch { txtLength.Text = Convert.ToString(Math.Round(((decimal)_xvtBriefing.Length / _timerInterval), 2)); }
					break;
				case Settings.Platform.XWA:
					try
					{
						t_Length = (short)Math.Round(Convert.ToDecimal(txtLength.Text) * _timerInterval, 0);	// this is the line that could throw
						_xwaBriefing.Length = t_Length;
						hsbTimer.Maximum = _xwaBriefing.Length + 11;
						if (Math.Round(((decimal)_xwaBriefing.Length / _timerInterval), 2) != Convert.ToDecimal(txtLength.Text))	// so things like .51 become .5, without
							txtLength.Text = Convert.ToString(Math.Round(((decimal)_xwaBriefing.Length / _timerInterval), 2));	// wiping out just a decimal
					}
					catch { txtLength.Text = Convert.ToString(Math.Round(((decimal)_xwaBriefing.Length / _timerInterval), 2)); }
					break;
			}
		}

		void vsbBRF_ValueChanged(object sender, EventArgs e)
		{
			if (_eventType == BaseBriefing.EventType.MoveMap) mapY = (short)vsbBRF.Value;
			if (_eventType == BaseBriefing.EventType.ZoomMap) zoomY = (short)vsbBRF.Value;
			MapPaint();
		}
		#endregion	tabDisplay
		#region tabStrings
		void LoadStrings()
		{
			cboString.Items.Clear();
			cboText.Items.Clear();
			for (int i=0;i<_strings.Length;i++)
			{
				cboString.Items.Add(_strings[i]);
				cboText.Items.Add(_strings[i]);
			}
		}
		void LoadTags()
		{
			cboTag.Items.Clear();
			cboTextTag.Items.Clear();
			for (int i=0;i<_tags.Length;i++)
			{
				cboTag.Items.Add(_tags[i]);
				cboTextTag.Items.Add(_tags[i]);
			}
		}

		void tableStrings_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			int i=0;
			for (int j=0;j<_strings.Length;j++)
			{
				if (tableStrings.Rows[j].Equals(e.Row))
				{
					i = j;
					break;
				}
			}
			_strings[i] = tableStrings.Rows[i][0].ToString();
			LoadStrings();
		}
		void tableTags_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			int i=0;
			for(int j=0;j<_tags.Length;j++)
			{
				if (tableTags.Rows[j].Equals(e.Row))
				{
					i = j;
					break;
				}
			}
			_tags[i] = tableTags.Rows[i][0].ToString();
			LoadTags();
		}
		#endregion	tabStrings
		#region tabEvents
		void InsertEvent()
		{
			// create a new item @ SelectedIndex, 
			int i = lstEvents.SelectedIndex;
			if (i == -1) i = 0;
			lstEvents.Items.Insert(i, "");
			for (int j=_maxEvents-1;j>i;j--)
			{
				if (_events[j-1, 1] == 0) continue;
				for (int h=0;h<6;h++) _events[j, h] = _events[j-1, h];
			}
			_events[i, 0] = _events[i+1, 0];
			if (_events[i, 0] == 9999) _events[i, 0] = 0;
			_events[i, 1] = 3;
			for (int j=2;j<6;j++) _events[i, j] = 0;
			lstEvents.SelectedIndex = i;
		}
		void ListUpdate(int index)
		{
			if (index == -1) return;
			string temp = String.Format("{0,-8:0.00}", (decimal)_events[index, 0] / _timerInterval);
			temp += cboEvent.Items[_events[index, 1]-3].ToString();
			if (_events[index, 1] == (int)BaseBriefing.EventType.TitleText || _events[index, 1] == (int)BaseBriefing.EventType.CaptionText)
			{
				if (_strings[_events[index, 2]].Length > 30) temp += ": \"" + _strings[_events[index, 2]].Substring(0, 30) + "...\"";
				else temp += ": \"" + _strings[_events[index, 2]] + '\"';
			}
			else if (_events[index, 1] == (int)BaseBriefing.EventType.MoveMap || _events[index, 1] == (int)BaseBriefing.EventType.ZoomMap) { temp += ": X:" + _events[index, 2] + " Y:" + _events[index, 3]; }
			else if (_events[index, 1] >= (int)BaseBriefing.EventType.FGTag1 && _events[index, 1] <= (int)BaseBriefing.EventType.FGTag8) { temp += ": " + _briefData[_events[index, 2]].Name; }
			else if (_events[index, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[index, 1] <= (int)BaseBriefing.EventType.TextTag8)
			{
				if (_tags[_events[index, 2]].Length > 30) temp += ": \"" + _tags[_events[index, 2]].Substring(0, 30) + "...\"";
				else temp += ": \"" + _tags[_events[index, 2]] + '\"';
			}
			else if (_events[index, 1] == (int)BaseBriefing.EventType.XwaNewIcon) { temp += " #" + _events[index, 2] + ": Craft: " + Platform.Xwa.Strings.CraftType[_events[index, 3]] + " IFF: " + cboIFF.Items[_events[index, 4]].ToString(); }
			else if (_events[index, 1] == (int)BaseBriefing.EventType.XwaShipInfo)
			{
				if (_events[index, 2] == 1) temp += ": Icon # " + _events[index, 3] + " State: On";
				else temp += ": Icon # " + _events[index, 3] + " State: Off";
			}
			else if (_events[index, 1] == (int)BaseBriefing.EventType.XwaMoveIcon) { temp += " #" + _events[index, 2] + ": X:" + _events[index, 3] + " Y:" + _events[index, 4]; }
			else if (_events[index, 1] == (int)BaseBriefing.EventType.XwaRotateIcon) { temp += " #" + _events[index, 2] + ": " + cboRotate.Items[_events[index, 3]]; }
			else if (_events[index, 1] == (int)BaseBriefing.EventType.XwaChangeRegion) { temp += " #" + (_events[index, 2]+1); }
			lstEvents.Items[index] = temp;
			if (!_loading)
			{
				_loading = true;
				lstEvents.SelectedIndex = index;
				_loading = false;
				MapPaint();		// to update things like tags, strings, etc
			}
		}
		void UpdateParameters()
		{
			int i = lstEvents.SelectedIndex;
			_loading = true;
			cboIFF.Enabled = false;
			cboString.Enabled = false;
			cboTag.Enabled = false;
			cboFG.Enabled = false;
			cboColor.Enabled = false;
			numX.Enabled = false;
			numY.Enabled = false;
			optOff.Enabled = false;
			optOn.Enabled = false;
			numRegion.Enabled = false;
			cboCraft.Enabled = false;
			cboRotate.Enabled = false;
			if (_events[i, 1] == (int)BaseBriefing.EventType.TitleText || _events[i, 1] == (int)BaseBriefing.EventType.CaptionText)
			{
				try { cboString.SelectedIndex = _events[i, 2]; }
				catch
				{
					cboString.SelectedIndex = 0;
					_events[i, 2] = 0;
				}
				_events[i, 3] = 0;
				_events[i, 4] = 0;
				_events[i, 5] = 0;
				cboString.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.MoveMap || _events[i, 1] == (int)BaseBriefing.EventType.ZoomMap)
			{
				numX.Value = _events[i, 2];
				numY.Value = _events[i, 3];
				_events[i, 4] = 0;
				_events[i, 5] = 0;
				numX.Enabled = true;
				numY.Enabled = true;
			}
			else if (_events[i, 1] >= (int)BaseBriefing.EventType.FGTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.FGTag8)
			{
				try { cboFG.SelectedIndex = _events[i, 2]; }
				catch
				{
					cboFG.SelectedIndex = 0;
					_events[i, 2] = 0;
				}
				_events[i, 3] = 0;
				_events[i, 4] = 0;
				_events[i, 5] = 0;
				cboFG.Enabled = true;
			}
			else if (_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8)
			{
				try
				{
					cboTag.SelectedIndex = _events[i, 2];
					cboColor.SelectedIndex = _events[i, 5];
				}
				catch
				{
					cboTag.SelectedIndex = 0;
					cboColor.SelectedIndex = 0;
					_events[i, 2] = 0;
					_events[i, 3] = 0;
					_events[i, 4] = 0;
					_events[i, 5] = 0;
				}
				numX.Value = _events[i, 3];
				numY.Value = _events[i, 4];
				cboTag.Enabled = true;
				cboColor.Enabled = true;
				numX.Enabled = true;
				numY.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaNewIcon)
			{
				cboFG.SelectedIndex = _events[i, 2];
				cboCraft.SelectedIndex = _events[i, 3];
				cboIFF.SelectedIndex = _events[i, 4];
				cboCraft.Enabled = true;
				cboIFF.Enabled = true;
				cboFG.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaShipInfo)
			{
				optOn.Checked = Convert.ToBoolean(_events[i, 2]);
				optOff.Checked = !optOn.Checked;
				cboFG.SelectedIndex = _events[i, 3];
				cboFG.Enabled = true;
				optOff.Enabled = true;
				optOn.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaMoveIcon)
			{
				cboFG.SelectedIndex = _events[i, 2];
				numX.Value = _events[i, 3];
				numY.Value = _events[i, 4];
				cboFG.Enabled = true;
				numX.Enabled = true;
				numY.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaRotateIcon)
			{
				cboFG.SelectedIndex = _events[i, 2];
				cboRotate.SelectedIndex = _events[i, 3];
				cboFG.Enabled = true;
				cboRotate.Enabled = true;
			}
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaChangeRegion)
			{
				numRegion.Value = _events[i, 2]+1;
				numRegion.Enabled = true;
			}
			_loading = false;
		}

		void cboColor_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8) _events[i, 5] = (short)cboColor.SelectedIndex;
			ListUpdate(i);
		}
		void cboCraft_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.XwaNewIcon) _events[i, 3] = (short)cboCraft.SelectedIndex;
			ListUpdate(i);
		}
		void cboEvent_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (_loading || i == -1 || cboEvent.SelectedIndex == -1) return;
			_events[i, 1] = (short)(cboEvent.SelectedIndex + 3);
			UpdateParameters();
			ListUpdate(i);
		}
		void cboFG_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if ((_events[i, 1] >= (int)BaseBriefing.EventType.FGTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.FGTag8) || _events[i, 1] == (int)BaseBriefing.EventType.XwaNewIcon
				|| _events[i, 1] == (int)BaseBriefing.EventType.XwaMoveIcon || _events[i, 1] == (int)BaseBriefing.EventType.XwaRotateIcon) _events[i, 2] = (short)cboFG.SelectedIndex;
			else if (_events[i, 1] == (int)BaseBriefing.EventType.XwaShipInfo) _events[i, 3] = (short)cboFG.SelectedIndex;
			ListUpdate(i);
		}
		void cboIFF_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.XwaNewIcon) _events[i, 4] = (short)cboIFF.SelectedIndex;
			ListUpdate(i);
		}
		void cboRotate_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.XwaRotateIcon) _events[i, 3] = (short)cboRotate.SelectedIndex;
			ListUpdate(i);
		}
		void cboString_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.TitleText || _events[i, 1] == (int)BaseBriefing.EventType.CaptionText) _events[i, 2] = (short)cboString.SelectedIndex;
			ListUpdate(i);
		}
		void cboTag_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8) _events[i, 2] = (short)cboTag.SelectedIndex;
			ListUpdate(i);
		}

		void cmdDelete_Click(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1) return;
			lstEvents.Items.RemoveAt(i);
			for (int j=i;j<_maxEvents-1;j++)
			{
				if (_events[j, 1] == 0) break;
				for (int h=0;h<6;h++) _events[j, h] = _events[j+1, h];
			}
			try { lstEvents.SelectedIndex = i; }
			catch { lstEvents.SelectedIndex = i-1; }
		}
		void cmdDown_Click(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == lstEvents.Items.Count-1) return;
			short[] t = new short[6];
			for (int j=0;j<6;j++) t[j] = _events[i+1, j];
			for (int j=0;j<6;j++) _events[i+1, j] = _events[i, j];
			for (int j=0;j<6;j++) _events[i, j] = t[j];
			string item = lstEvents.Items[i].ToString();
			lstEvents.Items[i] = lstEvents.Items[i+1];
			lstEvents.Items[i+1] = item;
			lstEvents.SelectedIndex = i+1;
		}
		void cmdNew_Click(object sender, EventArgs e)
		{
			InsertEvent();
			ListUpdate(lstEvents.SelectedIndex);
		}
		void cmdUp_Click(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1) return;
			short[] t = new short[6];
			for (int j=0;j<6;j++) t[j] = _events[i-1, j];
			for (int j=0;j<6;j++) _events[i-1, j] = _events[i, j];
			for (int j=0;j<6;j++) _events[i, j] = t[j];
			string item = lstEvents.Items[i].ToString();
			lstEvents.Items[i] = lstEvents.Items[i-1];
			lstEvents.Items[i-1] = item;
			lstEvents.SelectedIndex = i-1;
		}

		void lstEvents_SelectedIndexChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			_loading = true;
			numTime.Value = _events[i, 0];
			cboEvent.SelectedIndex = _events[i, 1] - 3;
			_loading = false;
			UpdateParameters();
			try
			{
				if (_events[i-1, 0] == _events[i, 0]) cmdUp.Enabled = true;
				else cmdUp.Enabled = false;
			}
			catch { cmdUp.Enabled = false; }
			try
			{
				if (_events[i+1, 0] == _events[i, 0]) cmdDown.Enabled = true;
				else cmdDown.Enabled = false;
			}
			catch { cmdDown.Enabled = false; }
		}

		void numRegion_ValueChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.XwaChangeRegion) _events[i, 2] = (short)(numRegion.Value - 1);
			ListUpdate(i);
		}
		void numTime_ValueChanged(object sender, EventArgs e)
		{
			lblEventTime.Text = String.Format("{0:= 0.00 seconds}", numTime.Value / _timerInterval);
			int i = lstEvents.SelectedIndex;
			int p = 0;
			if (_loading || i == -1) return;
			_loading = true;
			_events[i, 0] = (short)numTime.Value;
			if (_events[i+1,0] < _events[i,0])	// time was increased past the next instruction, must be moved
			{
				while(_events[i+p,0] <= _events[i,0]) p++;	// get the offset to next acceptable place
				short[] t = new short[6];
				for(int j=0;j<6;j++) t[j] = _events[i,j];
				for(int j=0;j<p;j++) { for(int h=0;h<6;h++) _events[i+j,h] = _events[i+j+1,h]; }
				for(int j=0;j<6;j++) _events[i+p-1,j] = t[j];
				lstEvents.Items.Insert(i+p,"");
				lstEvents.Items.RemoveAt(i);
				lstEvents.SelectedIndex = i+p-1;
			}
			else if (_events[i-1,0] > _events[i,0])		// time was decreased past the previous instruction
			{
				while(_events[i+p,0] >= _events[i,0]) p--;
				short[] t = new short[6];
				for(int j=0;j<6;j++) t[j] = _events[i,j];
				for(int j=0;j>p;j--) { for(int h=0;h<6;h++) _events[i+j,h] = _events[i+j-1,h]; }
				for(int j=0;j<6;j++) _events[i+p+1,j] = t[j];
				lstEvents.Items.RemoveAt(i);
				lstEvents.Items.Insert(i+p+1,"");
				lstEvents.SelectedIndex = i+p+1;
			}
			_loading = false;
			try
			{
				if (_events[i-1,0] == _events[i,0]) cmdUp.Enabled = true;
				else cmdUp.Enabled = false;
			}
			catch { cmdUp.Enabled = false; }
			try
			{
				if (_events[i+1,0] == _events[i,0]) cmdDown.Enabled = true;
				else cmdDown.Enabled = false;
			}
			catch { cmdDown.Enabled = false; }
			ListUpdate(i);
		}
		void numX_ValueChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.MoveMap || _events[i, 1] == (int)BaseBriefing.EventType.ZoomMap) _events[i, 2] = (short)numX.Value;
			else if ((_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8)
				|| _events[i, 1] == (int)BaseBriefing.EventType.XwaMoveIcon) _events[i, 3] = (short)numX.Value;
			ListUpdate(i);
		}
		void numY_ValueChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.MoveMap || _events[i, 1] == (int)BaseBriefing.EventType.ZoomMap) _events[i, 3] = (short)numY.Value;
			else if ((_events[i, 1] >= (int)BaseBriefing.EventType.TextTag1 && _events[i, 1] <= (int)BaseBriefing.EventType.TextTag8)
				|| _events[i, 1] == (int)BaseBriefing.EventType.XwaMoveIcon) _events[i, 4] = (short)numY.Value;
			ListUpdate(i);
		}

		void optOn_CheckedChanged(object sender, EventArgs e)
		{
			int i = lstEvents.SelectedIndex;
			if (i == -1 || _loading) return;
			if (_events[i, 1] == (int)BaseBriefing.EventType.XwaShipInfo) _events[i, 2] = (short)(optOn.Checked ? 1 : 0);
			ListUpdate(i);
		}
		#endregion tabEvents
	}

	public struct BriefData
	{
		public int Craft;
		public short[] Waypoint;
		public byte IFF;
		public string Name;
	}
	/*
	 * Okay, here's how the BRF.dat files work:
	 * first SHORT is number of entries
	 * header section is SHORT offset values to craft images,
	 * lets me define multiple craft to the same image.
	 * at the image locations:
	 * BYTE width, BYTE height
	 * BITFIELD; bottom 4 are left px, top 4 are right px, always in pairs even for odd sizes
	 * reads left to right, top to bottom
	 */
}