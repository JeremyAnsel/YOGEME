/*
 * YOGEME.exe, All-in-one Mission Editor for the X-wing series, TIE through XWA
 * Copyright (C) 2007-2017 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the MPL v2.0 or later
 * 
 * VERSION: 1.4.1+
 */

/* CHANGELOG
 * [UPD] changed how Strings.OrderDesc gets split
 * v1.4.1, 171118
 * [UPD] added Exclamation icon to FG delete confirmation
 * v1.4, 171016
 * [NEW #10] Custom ship list loading
 * v1.3, 170107
 * [FIX] various crashes [JB]
 * [NEW] MRU capability [JB]
 * [NEW] Delete menu item, delete key capture [JB]
 * [FIX] Redo opnXvT procedure [JB]
 * [NEW] Craft reference adjustment when deleting FGs [JB]
 * [UPD] newFG() now BOOL [JB]
 * [FIX] SpecialCargo assignment [JB]
 * [FIX] various label refresh issues [JB]
 * [NEW] FG Goal Summary [JB]
 * v1.2.8, 160606
 * [FIX] WaitForExit in Test replaced with named process check loop (Steam's fault)
 * v1.2.7, 150405
 * [FIX] Team copy/paste
 * [FIX] FG Goal copy/paste now gets entire goal with strings and points, not just trigger
 * [FIX] FG Goal strings were saving in the wrong order
 * [UPD] new Globals.Goal.Trigger implementation
 * [ADD] copy/paste mouse functions to Team listing
 * v1.2.5, 150110
 * [FIX] some type corrections near Update calls [JeremyAnsel]
 * [UPD] modified Common.Update calls for generics
 * v1.2.3, 141214
 * [UPD] change to MPL
 * [FIX] blank form when trying to use LstForm when TIE isn't installed
 * v1.2, 121006
 * - removed try{} in opnXvT_FileOk
 * - Settings passed in and out
 * [NEW] Test menu
 * [FIX] Global Goal text boxes saving contents of Completed for all
 * - txtGlobal_Leave now single function instead of Comp/Inc/Fail
 * [UPD] lblStarting now only applies to Normal difficulty
 * [UPD] opn/sav dialogs default to \Train
 * [NEW] Open Recent menu
 * v1.1.1, 120814
 * [UPD] chkWPArr_Leave to chkWPArr_CheckedChanged
 * [FIX] FG list display
 * - renamed a ton of stuff
 * - class renamed
 * v1.0, 110921
 * - Release
 */

using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Idmr.Platform.Xvt;

namespace Idmr.Yogeme
{
	/// <summary>XvT Mission Editor GUI</summary>
	public partial class XvtForm : Form
	{
		#region vars and stuff
		Settings _config;
		Mission _mission;
		bool _applicationExit;
		int _activeFG = 0;
		int _startingShips = 1;
		bool _loading;
		int _activeMessage = 0;
		DataTable _table = new DataTable("Waypoints");
		DataTable _tableRaw = new DataTable("Waypoints_Raw");
		MapForm _fMap;
		BriefingForm _fBrief;
		LstForm _fLST;
		byte _activeMessageTrigger = 0;
		byte _activeGlobalTrigger = 0;
		byte _activeTeam = 0;
		byte _activeArrDepTrigger = 0;
		byte _activeFGGoal = 0;
		byte _activeOrder = 0;
		byte _activeOptionCraft = 0;
		#endregion
		#region Control Arrays
		RadioButton[] optADAndOr = new RadioButton[8];
		CheckBox[] chkSendTo = new CheckBox[10];
		Label[] lblMessTrig = new Label[4];
		Label[] lblGlobTrig = new Label[12];
		RadioButton[] optGlobAndOr = new RadioButton[18];
		Label[] lblTeam = new Label[10];
		CheckBox[] chkAllies = new CheckBox[10];
		Label[] lblADTrig = new Label[6];
		Label[] lblGoal = new Label[8];
		Label[] lblOrder = new Label[4];
		CheckBox[] chkWP = new CheckBox[22];
		CheckBox[] chkOpt = new CheckBox[15];
		Label[] lblOptCraft = new Label[10];
		ComboBox[] cboEoMColor = new ComboBox[6];
		TextBox[] txtEoM = new TextBox[6];
		MenuItem[] menuRecentMissions = new MenuItem[6];
		#endregion

		public XvtForm(Settings settings, bool bBoP)
		{
			_config = settings;
			InitializeComponent();
			_loading = true;
			initializeMission();
			setBop(bBoP);
			startup();
			lstFG.SelectedIndex = 0;
			_loading = false;
		}
		public XvtForm(Settings settings, string path)
		{
			_config = settings;
			InitializeComponent();
			_loading = true;
			initializeMission();
			startup();
			if(!loadMission(path)) return;
			lstFG.SelectedIndex = 0;
			if (_mission.Messages.Count != 0) lstMessages.SelectedIndex = 0;
			_loading = false;
		}

		void closeForms()
		{
			if (_fMap != null) _fMap.Close();
			if (_fBrief != null) _fBrief.Close();
			if (_fLST != null) _fLST.Close();
		}
		void comboVarRefresh(int index, ComboBox cbo)
		{	//index is usually cboTrigType.SelectedIndex, cbo = cboTrigVar
			if (index == -1) return;
			cbo.Items.Clear();
			switch (index)		//switch (VariableType)
			{
				case 0:
					cbo.Items.Add("None");
					break;
				case 1: //FlightGroup
					cbo.Items.AddRange(_mission.FlightGroups.GetList());
					break;
				case 2:
					cbo.Items.AddRange(Strings.CraftType);
					cbo.Items.RemoveAt(0);
					break;
				case 3:
					cbo.Items.AddRange(Strings.ShipClass);
					break;
				case 4:
					cbo.Items.AddRange(Strings.ObjectType);
					break;
				case 5:
					cbo.Items.AddRange(Strings.IFF);
					break;
				case 6:
					cbo.Items.AddRange(Strings.Orders);
					break;
				case 7:
					cbo.Items.AddRange(Strings.CraftWhen);
					break;
				//case 8: Global Group
				//since it's just numbers, same as default, left out for specifics
				case 12:	// Teams
					cbo.Items.AddRange(_mission.Teams.GetList());
					break;
				case 0x15:	// All Teams except
					cbo.Items.AddRange(_mission.Teams.GetList());
					break;
				// case 0x17: Global Unit
				default:
					string[] temp = new string[256];
					for (int i = 0;i <= 255;i++) temp[i] = i.ToString();
					cbo.Items.AddRange(temp);
					break;
			}
		}
		void comboReset(ComboBox cbo, string[] items, int index)
		{
			cbo.Items.Clear();
			cbo.Items.AddRange(items);
			cbo.SelectedIndex = index;
		}
		void craftStart(FlightGroup fg, bool bAdd)
		{
			if (fg.Difficulty == 1 || fg.Difficulty == 3 || !fg.ArrivesIn30Seconds) return;
			if (bAdd) _startingShips += fg.NumberOfCraft;
			else _startingShips -= fg.NumberOfCraft;
			lblStarting.Text = _startingShips.ToString() + " craft at 30 seconds";
			if (_startingShips > Mission.CraftLimit) lblStarting.ForeColor = Color.Red;
			else lblStarting.ForeColor = SystemColors.ControlText;
		}
		void initializeMission()
		{
			tabMain.Focus(); //[JB] Exit focus from any form controls.  Fixes some crashes when Leave() events are processed after mission data has been cleared (notably from within the Messages tab).
			_mission = new Mission();
			_config.LastMission = "";
			_activeFG = 0;
			_activeMessage = 0;
			_mission.FlightGroups[0].CraftType = Convert.ToByte(_config.XvtCraft);
			_mission.FlightGroups[0].IFF = Convert.ToByte(_config.XvtIff);
			string[] fgList = _mission.FlightGroups.GetList();
			comboReset(cboArrMS, fgList, 0);
			comboReset(cboArrMSAlt, fgList, 0);
			comboReset(cboDepMS, fgList, 0);
			comboReset(cboDepMSAlt, fgList, 0);
			lstFG.Items.Clear();
			lstFG.Items.Add(_mission.FlightGroups[_activeFG].ToString(true));
			tabMain.SelectedIndex = 0;
			tabFGMinor.SelectedIndex = 0;
			this.Text = "Ye Olde Galactic Empire Mission Editor - XvT - New Mission.tie";
		}
		void labelRefresh(Mission.Trigger trigger, Label lbl)
		{	// lbl is the affected label
			string triggerText = trigger.ToString();
			triggerText = replaceTargetText(triggerText);
			lbl.Text = triggerText;
		}
		string replaceTargetText(string text)
		{
			while (text.Contains("FG:"))
			{
				int index = text.IndexOf("FG:") + 3;
				int length = text.IndexOfAny(new char[] { ' ', ',', '\0' }, index) - index;
				int fg;
				if (length > 0) fg = Int32.Parse(text.Substring(index, length));
				else fg = Int32.Parse(text.Substring(index));
				text = text.Replace("FG:" + fg, _mission.FlightGroups[fg].ToString());
			}
			while (text.Contains("TM:"))
			{
				int index = text.IndexOf("TM:") + 3;
				int length = text.IndexOfAny(new char[] { ' ', ',', '\0' }, index) - index;
				int team;
				if (length > 0) team = Int32.Parse(text.Substring(index, length));
				else team = Int32.Parse(text.Substring(index));
				string n = (team < 10) ? (_mission.Teams[team].Name == "" ? "Team " + team : _mission.Teams[team].Name) : team.ToString(); //[JB] Test if team index is within bounds.  This fixes a crash when loading certain BoP melee files which have a global goal for Team#11.
				text = text.Replace("TM:" + team, n);
			}
			return text;
		}
		bool loadMission(string fileMission)
		{
			string remove = _config.RecentMissions[0];
			closeForms();
			lstFG.SelectedIndex = 0;  //[JB] Prevents sporadic index out of range exceptions (such as when opening mission with fewer FGs than the current selected index, or opening missions of a different platform)
			lstFG.Items.Clear();
			lstMessages.Items.Clear();
			_startingShips = 0;
			byte[] buffer = new byte[64];
			try
			{
				FileStream fs = File.OpenRead(fileMission);
				try
				{
					#region determine platform
					switch (Platform.MissionFile.GetPlatform(fs))
					{
						case Platform.MissionFile.Platform.TIE:
							_applicationExit = false;
							new TieForm(_config, fileMission).Show();
							Close();
							return false;
						case Platform.MissionFile.Platform.XvT:
							setBop(false);
							break;
						case Platform.MissionFile.Platform.BoP:
							setBop(true);
							break;
						case Platform.MissionFile.Platform.XWA:
							_applicationExit = false;
							new XwaForm(_config, fileMission).Show();
							Close();
							return false;
						default:
							throw new Exception("File is not a valid mission file for any platform, please select an appropriate *.tie file.");
					}
					#endregion
					_mission.LoadFromStream(fs);
					fs.Close();
				}
				catch (Exception x)
				{
					fs.Close();
					MessageBox.Show(x.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
			}
			catch (Exception x)
			{
				MessageBox.Show(x.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			for (int i=0;i<_mission.FlightGroups.Count;i++)
			{
				lstFG.Items.Add(_mission.FlightGroups[i].ToString(true));
				if (_mission.FlightGroups[i].ArrivesIn30Seconds) craftStart(_mission.FlightGroups[i], true);
			}
			updateFGList();
			if (_mission.Messages.Count == 0) enableMessages(false);
			else
			{
				enableMessages(true);
				for (int i=0;i<_mission.Messages.Count;i++) lstMessages.Items.Add(_mission.Messages[i].MessageString);
			}
			updateMissionTabs();
			cboGlobalTeam.SelectedIndex = -1;	// otherwise it doesn't trigger an index change
			cboGlobalTeam.SelectedIndex = 0;
			for (_activeTeam=0;_activeTeam<10;_activeTeam++) teamRefresh();
			lblTeamArr_Click(0, new EventArgs());
			this.Text = "Ye Olde Galactic Empire Mission Editor - " + (_mission.IsBop ? "BoP" : "XvT") + " - " + _mission.MissionFileName;
			_config.LastMission = fileMission;
			refreshRecent(); //[JB] Setting _config.LastMission modifies the Recent list.  Need to refresh the menu to match.
			return true;
		}
		void promptSave()
		{
			_config.SaveSettings();
			if (_config.ConfirmSave && (this.Text.IndexOf("*") != -1))
			{
				DialogResult res = MessageBox.Show("Mission has been edited without saving, would you like to save?", "Confirm", MessageBoxButtons.YesNo);
				if (res == DialogResult.Yes)
				{
					if (_mission.MissionPath == "\\NewMission.tie") savXvT.ShowDialog();
					else saveMission(_mission.MissionPath);
				}
			}
		}
		void refreshRecent()
		{
			for (int i = 1; i < 6; i++)
			{
				menuRecentMissions[i].Text = "&" + i + ": " + _config.RecentMissions[i] + " (" + _config.RecentPlatforms[i].ToString() + ")";
				menuRecentMissions[i].Visible = (_config.RecentMissions[i] != "");
			}
			menuRecentMissions[0].Enabled = menuRecentMissions[1].Visible;
		}
		void saveMission(string fileMission)
		{
			try { _fBrief.Save(); }
			catch { /* do nothing */ }
			lblTeamArr_Click(lblTeam[_activeTeam], new EventArgs());
			try { _mission.Save(fileMission); }
			catch (Exception x) { MessageBox.Show(x.Message, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			this.Text = "Ye Olde Galactic Empire Mission Editor - " + (_mission.IsBop ? "BoP" : "XvT") + " - " + _mission.MissionFileName;
			_config.LastMission = fileMission;
   			refreshRecent();  //[JB] Setting _config.LastMission modifies the Recent list.  Need to refresh the menu to match.
			//Verify the mission after it's been saved
			if (_config.Verify) Common.RunVerify(_mission.MissionPath, _config.VerifyLocation);
		}
		void setBop(bool bop)
		{
			_mission.IsBop = bop;
			if (_mission.IsBop)
			{
				menuNewXvT.Shortcut = Shortcut.None;
				menuNewBoP.Shortcut = Shortcut.CtrlN;
				_config.LastPlatform = Settings.Platform.BoP;
				Text = Text.Substring(0, 41) + "BoP" + Text.Substring(44);
				txtMissDesc.MaxLength = 0x1000;
				opnXvT.InitialDirectory = _config.GetWorkingPath();  //[JB] Updated for MRU access.  Defaults to installation and mission folder if not enabled.
				savXvT.InitialDirectory = _config.GetWorkingPath();
				menuTest.Enabled = _config.BopInstalled;
			}
			else
			{
				if (txtMissDesc.Text.Length > 0x400 || txtMissSucc.Text.Length != 0 || txtMissFail.Text.Length != 0)
				{
					DialogResult r = MessageBox.Show("Changing platforms will result in loss of Mission Description and Debriefing text", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
					if (r == DialogResult.Cancel) { optBoP.Checked = true; return; }
					if (txtMissDesc.Text.Length > 0x400) txtMissDesc.Text = txtMissDesc.Text.Substring(0, 0x400);
				}
				menuTest.Enabled = _config.XvtInstalled;
				menuNewXvT.Shortcut = Shortcut.CtrlN;
				menuNewBoP.Shortcut = Shortcut.None;
				_config.LastPlatform = Settings.Platform.XvT;
				Text = Text.Substring(0, 41) + "XvT" + Text.Substring(44);
				txtMissDesc.MaxLength = 0x400;
				txtMissSucc.Text = "";
				txtMissFail.Text = "";

				opnXvT.InitialDirectory = _config.GetWorkingPath();  //[JB] Updated for MRU access.  Defaults to installation and mission folder if not enabled.
				savXvT.InitialDirectory = _config.GetWorkingPath();
			}
			optBoP.Checked = _mission.IsBop;
			optXvT.Checked = !optBoP.Checked;
			txtMissSucc.Visible = _mission.IsBop;
			txtMissFail.Visible = _mission.IsBop;
			txtMissSucc.Enabled = _mission.IsBop;  //[JB] Fix to make these fields editable for BoP
			txtMissFail.Enabled = _mission.IsBop;
			menuNewXvT.ShowShortcut = !_mission.IsBop;
			menuNewBoP.ShowShortcut = _mission.IsBop;
			menuSaveAsBoP.Visible = !_mission.IsBop;
			menuSaveAsXvT.Visible = _mission.IsBop;
			numBackdrop.Maximum = (_mission.IsBop ? 16 : 7);  //[JB] Need to update backdrop maximums for BoP, otherwise it was staying at 7 and causing index
		}
		void startup()
		{
			if (File.Exists(Application.StartupPath + "\\xvt_shiplist.txt"))
			{
				System.Diagnostics.Debug.WriteLine("custom XvT list found");
				string[] crafts;
				string[] abbrvs;
				try
				{
					Common.ProcessCraftList(Application.StartupPath + "\\xvt_shiplist.txt", out crafts, out abbrvs);
					Strings.OverrideShipList(crafts, abbrvs);
					initializeMission();    // have to re-init since lstFG is already populated
				}
				catch { MessageBox.Show("Error processing custom XvT ship list, using defaults.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			}
			this.Height = 600;	// since VS tends to slowly shrink the damn thing
			tabMain.SelectedIndex = 0;
			tabFGMinor.SelectedIndex = 0;
			_config.LastMission = "";
			_applicationExit = true;	//becomes false if selecting "New Mission" from menu
			#region Menu
			// menuTest has already been taken care of
			if (_config.RestrictPlatforms)
			{
				if (!_config.TieInstalled) { menuNewTIE.Enabled = false; }
				if (!_config.BopInstalled) { menuNewBoP.Enabled = false; }
				if (!_config.XwaInstalled) { menuNewXWA.Enabled = false; }
			}
			menuRecentMissions[0] = menuRecent;
			menuRecentMissions[1] = menuRec1;
			menuRecentMissions[2] = menuRec2;
			menuRecentMissions[3] = menuRec3;
			menuRecentMissions[4] = menuRec4;
			menuRecentMissions[5] = menuRec5;
			for (int i = 1; i < 6; i++)
			{
				menuRecentMissions[i].Click += new EventHandler(menuRecentMissions_Click);
				menuRecentMissions[i].Tag = i;
			}
			refreshRecent();
			#endregion
			#region FlightGroups
			#region Craft
			cboCraft.Items.AddRange(Strings.CraftType); cboCraft.SelectedIndex = _mission.FlightGroups[0].CraftType;
			cboIFF.Items.AddRange(Strings.IFF); cboIFF.SelectedIndex = _mission.FlightGroups[0].IFF;
			cboTeam.Items.AddRange(_mission.Teams.GetList()); cboTeam.SelectedIndex = _mission.FlightGroups[0].Team;
			cboAI.Items.AddRange(Strings.Rating); cboAI.SelectedIndex = _mission.FlightGroups[0].AI;
			cboMarkings.Items.AddRange(Strings.Color); cboMarkings.SelectedIndex = _mission.FlightGroups[0].Markings;
			cboPlayer.SelectedIndex = 0;
			cboPosition.SelectedIndex = 0;
			cboFormation.Items.AddRange(Strings.Formation); cboFormation.SelectedIndex = 0;
			cboRadio.Items.AddRange(Strings.Radio); cboRadio.SelectedIndex = _mission.FlightGroups[0].Radio;
			cboStatus.Items.AddRange(Strings.Status); cboStatus.SelectedIndex = 0;
			cboStatus2.Items.AddRange(Strings.Status); cboStatus2.SelectedIndex = 0;
			cboWarheads.Items.AddRange(Strings.Warheads); cboWarheads.SelectedIndex = 0;
			cboBeam.Items.AddRange(Strings.Beam); cboBeam.SelectedIndex = 0;
			cboCounter.SelectedIndex = 0;
			#endregion
			#region Arr/Dep
			cboADTrig.Items.AddRange(Strings.Trigger);
			cboADTrigAmount.Items.AddRange(Strings.Amount);
			cboADTrigType.Items.AddRange(Strings.VariableType);
			cboAbort.Items.AddRange(Strings.Abort); cboAbort.SelectedIndex = 0;
			cboDiff.Items.AddRange(Strings.Difficulty); cboDiff.SelectedIndex = 0;
			string[] fgList = _mission.FlightGroups.GetList();
			cboArrMS.Items.AddRange(fgList); cboArrMS.SelectedIndex = 0;
			cboArrMSAlt.Items.AddRange(fgList); cboArrMSAlt.SelectedIndex = 0;
			cboDepMS.Items.AddRange(fgList); cboDepMS.SelectedIndex = 0;
			cboDepMSAlt.Items.AddRange(fgList); cboDepMSAlt.SelectedIndex = 0;
			lblADTrig[0] = lblArr1;
			lblADTrig[1] = lblArr2;
			lblADTrig[2] = lblArr3;
			lblADTrig[3] = lblArr4;
			lblADTrig[4] = lblDep1;
			lblADTrig[5] = lblDep2;
			for (int i=0;i<6;i++)
			{
				lblADTrig[i].Click += new EventHandler(lblADTrigArr_Click);
				lblADTrig[i].MouseUp += new MouseEventHandler(lblADTrigArr_MouseUp);
				lblADTrig[i].DoubleClick += new EventHandler(lblADTrigArr_DoubleClick);
				lblADTrig[i].Tag = i;
			}
			lblADTrigArr_Click(0, new EventArgs());
			optADAndOr[0] = optArr1OR2;
			optADAndOr[1] = optArr3OR4;
			optADAndOr[2] = optArr12OR34;
			optADAndOr[3] = optDepOR;
			optADAndOr[4] = optArr1AND2;
			optADAndOr[5] = optArr3AND4;
			optADAndOr[6] = optArr12AND34;
			optADAndOr[7] = optDepAND;
			for (int i=0;i<4;i++)
			{
				optADAndOr[i].CheckedChanged += new EventHandler(optADAndOrArr_CheckedChanged);
				optADAndOr[i].Tag = i;
			}
			#endregion
			#region Orders
			cboOrders.Items.AddRange(Strings.Orders);
			cboOT1Type.Items.AddRange(Strings.VariableType);
			cboOT2Type.Items.AddRange(Strings.VariableType);
			cboOT3Type.Items.AddRange(Strings.VariableType);
			cboOT4Type.Items.AddRange(Strings.VariableType);
			lblOrder[0] = lblOrder1;
			lblOrder[1] = lblOrder2;
			lblOrder[2] = lblOrder3;
			lblOrder[3] = lblOrder4;
			for (int i=0;i<4;i++)
			{
				lblOrder[i].Click += new EventHandler(lblOrderArr_Click);
				lblOrder[i].DoubleClick += new EventHandler(lblOrderArr_DoubleClick);
				lblOrder[i].MouseUp += new MouseEventHandler(lblOrderArr_MouseUp);
				lblOrder[i].Tag = i;
			}
			lblOrderArr_Click(0, new EventArgs());
			#endregion
			#region Waypoints
			_table.Columns.Add("X"); _table.Columns.Add("Y"); _table.Columns.Add("Z");
			_tableRaw.Columns.Add("X"); _tableRaw.Columns.Add("Y"); _tableRaw.Columns.Add("Z");
			for (int i=0;i<22;i++)	//initialize WPs
			{
				DataRow dr = _table.NewRow();
				int j;
				for(j=0;j<3;j++) dr[j] = 0;	//set X Y Z to zero
				_table.Rows.Add(dr);
				dr = _tableRaw.NewRow();
				for(j=0;j<3;j++) dr[j] = 0;	//mirror in raw table
				_tableRaw.Rows.Add(dr);
			}
			dataWaypoints.Table = _table;
			dataWaypoints_Raw.Table = _tableRaw;
			dataWP.DataSource = dataWaypoints;
			dataWP_Raw.DataSource = dataWaypoints_Raw;
			this._table.RowChanged += new DataRowChangeEventHandler(table_RowChanged);
			this._tableRaw.RowChanged += new DataRowChangeEventHandler(tableRaw_RowChanged);
			chkWP[0] = chkSP1;
			chkWP[1] = chkSP2;
			chkWP[2] = chkSP3;
			chkWP[3] = chkSP4;
			chkWP[4] = chkWP1;
			chkWP[5] = chkWP2;
			chkWP[6] = chkWP3;
			chkWP[7] = chkWP4;
			chkWP[8] = chkWP5;
			chkWP[9] = chkWP6;
			chkWP[10] = chkWP7;
			chkWP[11] = chkWP8;
			chkWP[12] = chkWPRend;
			chkWP[13] = chkWPHyp;
			chkWP[14] = chkWPBrief1;
			chkWP[15] = chkWPBrief2;
			chkWP[16] = chkWPBrief3;
			chkWP[17] = chkWPBrief4;
			chkWP[18] = chkWPBrief5;
			chkWP[19] = chkWPBrief6;
			chkWP[20] = chkWPBrief7;
			chkWP[21] = chkWPBrief8;
			for (int i=0;i<22;i++)
			{
				chkWP[i].CheckedChanged += new EventHandler(chkWPArr_CheckedChanged);
				chkWP[i].Tag = i;
			}
			#endregion
			#region FG Goals
			cboGoalAmount.Items.AddRange(Strings.Amount);
			cboGoalTrigger.Items.AddRange(Strings.Trigger);
			lblGoal[0] = lblGoal1;
			lblGoal[1] = lblGoal2;
			lblGoal[2] = lblGoal3;
			lblGoal[3] = lblGoal4;
			lblGoal[4] = lblGoal5;
			lblGoal[5] = lblGoal6;
			lblGoal[6] = lblGoal7;
			lblGoal[7] = lblGoal8;
			for (int i=0;i<8;i++)
			{
				lblGoal[i].Click += new EventHandler(lblGoalArr_Click);
				lblGoal[i].DoubleClick += new EventHandler(lblGoalArr_DoubleClick);
				lblGoal[i].MouseUp += new MouseEventHandler(lblGoalArr_MouseUp);
				lblGoal[i].Tag = i;
			}
			lblGoalArr_Click(0, new EventArgs());
			#endregion
			#region Options
			cboOptCraft.Items.AddRange(Strings.CraftType);
			cboSkipAmount.Items.AddRange(Strings.Amount); cboSkipAmount.SelectedIndex = 0;
			cboSkipTrig.Items.AddRange(Strings.Trigger); cboSkipTrig.SelectedIndex = 0;
			cboSkipType.Items.AddRange(Strings.VariableType); cboSkipType.SelectedIndex = 0;
			cboRoleImp.Items.AddRange(Strings.Roles); cboRoleImp.SelectedIndex = 0;
			cboRoleReb.Items.AddRange(Strings.Roles); cboRoleReb.SelectedIndex = 0;
			cboRole3.Items.AddRange(Strings.Roles); cboRole3.SelectedIndex = 0;
			cboRole4.Items.AddRange(Strings.Roles); cboRole4.SelectedIndex = 0;
			cboRoleAll.Items.AddRange(Strings.Roles); cboRoleAll.SelectedIndex = 0;
			chkOpt[0] = chkOptWNone;
			chkOpt[1] = chkOptWBomb;
			chkOpt[2] = chkOptWRocket;
			chkOpt[3] = chkOptWMissile;
			chkOpt[4] = chkOptWTorp;
			chkOpt[5] = chkOptWAdvMissile;
			chkOpt[6] = chkOptWAdvTorp;
			chkOpt[7] = chkOptWMagPulse;
			chkOpt[8] = chkOptBNone;
			chkOpt[9] = chkOptBTractor;
			chkOpt[10] = chkOptBJamming;
			chkOpt[11] = chkOptBDecoy;
			chkOpt[12] = chkOptCNone;
			chkOpt[13] = chkOptCChaff;
			chkOpt[14] = chkOptCFlare;
			for (int i=0;i<15;i++)
			{
				chkOpt[i].CheckedChanged += new EventHandler(chkOptArr_CheckedChanged);
				chkOpt[i].Tag = i;
			}
			lblOptCraft[0] = lblOptCraft1;
			lblOptCraft[1] = lblOptCraft2;
			lblOptCraft[2] = lblOptCraft3;
			lblOptCraft[3] = lblOptCraft4;
			lblOptCraft[4] = lblOptCraft5;
			lblOptCraft[5] = lblOptCraft6;
			lblOptCraft[6] = lblOptCraft7;
			lblOptCraft[7] = lblOptCraft8;
			lblOptCraft[8] = lblOptCraft9;
			lblOptCraft[9] = lblOptCraft10;
			for (int i=0;i<10;i++)
			{
				lblOptCraft[i].Click += new EventHandler(lblOptCraftArr_Click);
				lblOptCraft[i].Tag = i;
			}
			#endregion
			#endregion
			#region Messages
			cboMessAmount.Items.AddRange(Strings.Amount);
			cboMessTrig.Items.AddRange(Strings.Trigger);
			cboMessType.Items.AddRange(Strings.VariableType);
			chkSendTo[0] = chkMess1;
			chkSendTo[1] = chkMess2;
			chkSendTo[2] = chkMess3;
			chkSendTo[3] = chkMess4;
			chkSendTo[4] = chkMess5;
			chkSendTo[5] = chkMess6;
			chkSendTo[6] = chkMess7;
			chkSendTo[7] = chkMess8;
			chkSendTo[8] = chkMess9;
			chkSendTo[9] = chkMess10;
			for (int i=0;i<10;i++)
			{
				chkSendTo[i].Leave += new EventHandler(chkSendToArr_Leave);
				chkSendTo[i].Tag = i;
			}
			lblMessTrig[0] = lblMess1;
			lblMessTrig[1] = lblMess2;
			lblMessTrig[2] = lblMess3;
			lblMessTrig[3] = lblMess4;
			for (int i=0;i<4;i++)
			{
				lblMessTrig[i].Click += new EventHandler(lblMessTrigArr_Click);
				lblMessTrig[i].DoubleClick += new EventHandler(lblMessTrigArr_DoubleClick);
				lblMessTrig[i].MouseUp += new MouseEventHandler(lblMessTrigArr_MouseUp);
				lblMessTrig[i].Tag = i;
			}
			#endregion
			#region Globals
			cboGlobalAmount.Items.AddRange(Strings.Amount);
			cboGlobalTrig.Items.AddRange(Strings.Trigger);
			cboGlobalType.Items.AddRange(Strings.VariableType);
			cboGlobalTeam.Items.AddRange(_mission.Teams.GetList());
			lblGlobTrig[0] = lblPrim1;
			lblGlobTrig[1] = lblPrim2;
			lblGlobTrig[2] = lblPrim3;
			lblGlobTrig[3] = lblPrim4;
			lblGlobTrig[4] = lblPrev1;
			lblGlobTrig[5] = lblPrev2;
			lblGlobTrig[6] = lblPrev3;
			lblGlobTrig[7] = lblPrev4;
			lblGlobTrig[8] = lblSec1;
			lblGlobTrig[9] = lblSec2;
			lblGlobTrig[10] = lblSec3;
			lblGlobTrig[11] = lblSec4;
			for (int i=0;i<12;i++)
			{
				lblGlobTrig[i].Click += new EventHandler(lblGlobTrigArr_Click);
				lblGlobTrig[i].DoubleClick += new EventHandler(lblGlobTrigArr_DoubleClick);
				lblGlobTrig[i].MouseUp += new MouseEventHandler(lblGlobTrigArr_MouseUp);
				lblGlobTrig[i].Tag = i;
			}
			optGlobAndOr[0] = optPrim1OR2;
			optGlobAndOr[1] = optPrim3OR4;
			optGlobAndOr[2] = optPrim12OR34;
			optGlobAndOr[3] = optPrev1OR2;
			optGlobAndOr[4] = optPrev3OR4;
			optGlobAndOr[5] = optPrev12OR34;
			optGlobAndOr[6] = optSec1OR2;
			optGlobAndOr[7] = optSec3OR4;
			optGlobAndOr[8] = optSec12OR34;
			optGlobAndOr[9] = optPrim1AND2;
			optGlobAndOr[10] = optPrim3AND4;
			optGlobAndOr[11] = optPrim12AND34;
			optGlobAndOr[12] = optPrev1AND2;
			optGlobAndOr[13] = optPrev3AND4;
			optGlobAndOr[14] = optPrev12AND34;
			optGlobAndOr[15] = optSec1AND2;
			optGlobAndOr[16] = optSec3AND4;
			optGlobAndOr[17] = optSec12AND34;
			for (int i=0;i<9;i++)
			{
				optGlobAndOr[i].CheckedChanged += new EventHandler(optGlobAndOrArr_CheckedChanged);
				optGlobAndOr[i].Tag = i;
			}
			txtGlobalComp.Tag = Globals.GoalState.Complete;
			txtGlobalFail.Tag = Globals.GoalState.Failed;
			txtGlobalInc.Tag = Globals.GoalState.Incomplete;
			#endregion
			#region Teams
			lblTeam[0] = lblTeam1;
			lblTeam[1] = lblTeam2;
			lblTeam[2] = lblTeam3;
			lblTeam[3] = lblTeam4;
			lblTeam[4] = lblTeam5;
			lblTeam[5] = lblTeam6;
			lblTeam[6] = lblTeam7;
			lblTeam[7] = lblTeam8;
			lblTeam[8] = lblTeam9;
			lblTeam[9] = lblTeam10;
			chkAllies[0] = chkTeam1;
			chkAllies[1] = chkTeam2;
			chkAllies[2] = chkTeam3;
			chkAllies[3] = chkTeam4;
			chkAllies[4] = chkTeam5;
			chkAllies[5] = chkTeam6;
			chkAllies[6] = chkTeam7;
			chkAllies[7] = chkTeam8;
			chkAllies[8] = chkTeam9;
			chkAllies[9] = chkTeam10;
			for (int i=0;i<10;i++)
			{
				lblTeam[i].Click += new EventHandler(lblTeamArr_Click);
				lblTeam[i].DoubleClick += new EventHandler(lblTeamArr_DoubleClick);
				lblTeam[i].MouseUp += new MouseEventHandler(lblTeamArr_MouseUp);
				lblTeam[i].Tag = i;
				chkAllies[i].Leave += new EventHandler(chkAlliesArr_Leave);
				chkAllies[i].Tag = i;
			}
			cboEoMColor[0] = cboPC1Color;
			cboEoMColor[1] = cboPC2Color;
			cboEoMColor[2] = cboPF1Color;
			cboEoMColor[3] = cboPF2Color;
			cboEoMColor[4] = cboSC1Color;
			cboEoMColor[5] = cboSC2Color;
			txtEoM[0] = txtPrimComp1;
			txtEoM[1] = txtPrimComp2;
			txtEoM[2] = txtPrimFail1;
			txtEoM[3] = txtPrimFail2;
			txtEoM[4] = txtSecComp1;
			txtEoM[5] = txtSecComp2;
			for (int i=0;i<6;i++)
			{
				cboEoMColor[i].SelectedIndexChanged += new EventHandler(cboEoMColorArr_SelectedIndexChanged);
				cboEoMColor[i].Tag = i;
				txtEoM[i].Leave += new EventHandler(txtEoMArr_Leave);
				txtEoM[i].Tag = i;
			}
			for (_activeTeam=0;_activeTeam<10;_activeTeam++) teamRefresh();
			#endregion
			cboGlobalTeam.SelectedIndex = 0;
			cboMissType.Items.AddRange(Enum.GetNames(typeof(Mission.MissionTypeEnum)));
			cboMissType.SelectedIndex = 0;

			//[JB] Added
			//this.KeyPreview = true;  //Property is already set in .designer
			this.KeyDown += new KeyEventHandler(XvtForm_KeyDown);
		}
		void updateMissionTabs()
		{
			numMissUnk1.Value = _mission.Unknown1;
			numMissUnk2.Value = _mission.Unknown2;
			chkMissUnk3.Checked = _mission.Unknown3;
			txtMissUnk4.Text = _mission.Unknown4;
			txtMissUnk5.Text = _mission.Unknown5;
			cboMissType.SelectedIndex = (int)_mission.MissionType;
			chkMissUnk6.Checked = _mission.Unknown6;
			numMissTimeMin.Value = _mission.TimeLimitMin;
			numMissTimeSec.Value = _mission.TimeLimitSec;
			txtMissDesc.Text = _mission.MissionDescription;
			txtMissSucc.Text = _mission.MissionSuccessful;
			txtMissFail.Text = _mission.MissionFailed;
		}

		void frmXvT_Activated(object sender, EventArgs e)
		{
			if (_fMap != null)
			{
				lstFG.SelectedIndex = -1;
				lstFG.SelectedIndex = _activeFG;
			}
		}
		void frmXvT_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			promptSave();
			if (_config.ConfirmExit && _applicationExit)
			{
				DialogResult res = MessageBox.Show("Are you sure you wish to exit?", "Confirm", MessageBoxButtons.YesNo);
				if (res == DialogResult.No) { e.Cancel = true; return; }
			}
			closeForms();
			if (_applicationExit) Application.Exit();
		}
		void XvtForm_KeyDown(object sender, KeyEventArgs e)
		{
			//Instead of assigning this in designer.cs
			//  this.menuDelete.Shortcut = System.Windows.Forms.Shortcut.Del;
			//Trap and process the key here, with few changes to the original code
			//while allowing the delete key to remain functional in other areas like text boxes.

			if (e.KeyCode == Keys.Delete)
			{
				if (lstFG.Focused || lstMessages.Focused)
				{
					menuDelete_Click(0, new EventArgs());
					e.Handled = true;
				}
			}
		}


		//[JB] No longer an event.  Calling this function after Open Dialog closure fixes a bug where the Open Dialog could stick open.
		//void opnXvT_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		void opnXvT_FileOk()
		{
			_loading = true;
			if (loadMission(opnXvT.FileName))
			{
				tabMain.SelectedIndex = 0;
				tabFGMinor.SelectedIndex = 0;
				lstFG.SelectedIndex = 0;
				if (_mission.Messages.Count != 0) lstMessages.SelectedIndex = 0;
			}
			_loading = false;
		}

		void savXvT_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_mission.MissionPath = savXvT.FileName;
			saveMission(_mission.MissionPath);
		}

		void tabMain_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (tabMain.SelectedIndex)
			{
				case 0:
					toolNewItem.ToolTipText = "New FlightGroup";
					toolNewItem.Enabled = true;
					toolDeleteItem.ToolTipText = "Delete FlightGroup";
					toolDeleteItem.Enabled = true;
					toolCopy.ToolTipText = "Copy FlightGroup";
					toolPaste.ToolTipText = "Paste FlightGroup";
					break;
				case 1:
					toolNewItem.ToolTipText = "New Message";
					toolNewItem.Enabled = true;
					toolDeleteItem.ToolTipText = "Delete Message";
					toolDeleteItem.Enabled = true;
					toolCopy.ToolTipText = "Copy Message";
					toolPaste.ToolTipText = "Paste Message";
					break;
				case 2:
					toolCopy.ToolTipText = "Copy selected Global Goal";
					toolPaste.ToolTipText = "Paste into selected Global Goal";
					toolNewItem.Enabled = false;
					toolDeleteItem.Enabled = false;
					toolNewItem.ToolTipText = "";
					toolDeleteItem.ToolTipText = "";
					break;
				case 3:
					toolCopy.ToolTipText = "Copy selected Team";
					toolPaste.ToolTipText = "Paste into selected Team";
					toolNewItem.Enabled = false;
					toolDeleteItem.Enabled = false;
					toolNewItem.ToolTipText = "";
					toolDeleteItem.ToolTipText = "";
					break;
				default:
					toolNewItem.Enabled = false;
					toolDeleteItem.Enabled = false;
					toolNewItem.ToolTipText = "";
					toolDeleteItem.ToolTipText = "";
					toolCopy.ToolTipText = "Copy Item";
					toolPaste.ToolTipText = "Paste Item";
					break;
			}
		}

		void toolXvT_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
		{
			toolXvT.Focus();	// clicking the toolbar doesn't remove focus, so last change may not be saved
			switch (toolXvT.Buttons.IndexOf(e.Button))
			{
				case 0:		//New Mission
					menuNewXvT_Click("toolbar", new EventArgs());
					_loading = false;
					break;
				case 1:		//Open Mission
					menuOpen_Click("toolbar", new EventArgs());
					_loading = false;
					break;
				case 2:		//Save Mission
					menuSave_Click("toolbar", new EventArgs());
					break;
				case 3:		//Save As
					savXvT.ShowDialog();
					break;
				case 5:		//New Item
					if (tabMain.SelectedIndex == 0) newFG();
					else if (tabMain.SelectedIndex == 1) newMess();
					break;
				case 6:		//Delete Item
					menuDelete_Click("toolbar", new EventArgs());  //[JB] Changed to call the function directly since that function now does more conditional checks.
					break;
				case 7:		//Copy Item
					menuCopy_Click("toolbar", new EventArgs());
					break;
				case 8:		//Paste Item
					menuPaste_Click("toolbar", new EventArgs());
					break;
				case 10:	//Map
					menuMap_Click("toolbar", new EventArgs());
					break;
				case 11:	//Briefing
					menuBrief_Click("toolbar", new EventArgs());
					break;
				case 12:	//Verify
					menuVerify_Click("toolbar", new EventArgs());
					break;
				case 14:	//Options
					menuOptions_Click("toolbar", new EventArgs());
					break;
				case 15:	//LST
					menuLST_Click("toolbar", new EventArgs());
					break;
				case 16:	//Help
					menuHelpInfo_Click("toolbar", new EventArgs());
					break;
			}
		}

		#region Menu
		void menuAbout_Click(object sender, EventArgs e)
		{
			new AboutDialog().ShowDialog();
		}
		void menuBrief_Click(object sender, EventArgs e)
		{
			Common.Title(this, false);
			try { _fBrief.Close(); }
			catch { /* do nothing */ }
			_fBrief = new BriefingForm(_mission.FlightGroups, _mission.Briefings);
			_fBrief.Show();
		}
		void menuCopy_Click(object sender, EventArgs e)
		{
			System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			Stream stream = new FileStream("YOGEME.bin", FileMode.Create, FileAccess.Write, FileShare.None);
			#region ArrDep
			if (sender.ToString() == "AD")
			{
				formatter.Serialize(stream, _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger]);
				stream.Close();
				return;
			}
			#endregion
			#region FG Goal
			if (sender.ToString() == "Goal")
			{
				formatter.Serialize(stream, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal]);
				stream.Close();
				return;
			}
			#endregion
			#region Orders
			if (sender.ToString() == "Order")
			{
				formatter.Serialize(stream, _mission.FlightGroups[_activeFG].Orders[_activeOrder]);
				stream.Close();
				return;
			}
			#endregion
			#region Skip to O4
			if (sender.ToString() == "Skip")
			{
				formatter.Serialize(stream, _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[(lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1)]);
				stream.Close();
				return;
			}
			#endregion
			#region generic TextBox
			if (this.ActiveControl.GetType().ToString() == "System.Windows.Forms.TextBox")
			{
				System.Windows.Forms.TextBox txt_t = (System.Windows.Forms.TextBox)ActiveControl;
				if (txt_t.SelectedText != "")
				{
					formatter.Serialize(stream, txt_t.SelectedText);
					stream.Close();
					return;
				}
			}
			#endregion
			#region MessTrig
			if (sender.ToString() == "MessTrig")
			{
				formatter.Serialize(stream, _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger]);
				stream.Close();
				return;
			}
			#endregion

			switch (tabMain.SelectedIndex)
			{
				case 0:
					formatter.Serialize(stream, _mission.FlightGroups[_activeFG]);
					break;
				case 1:
					if (_mission.Messages.Count != 0) formatter.Serialize(stream, _mission.Messages[_activeMessage]);
					break;
				case 2:
					formatter.Serialize(stream, _mission.Globals[_activeTeam].Goals[_activeGlobalTrigger / 4].Triggers[_activeGlobalTrigger % 4]);
					break;
				case 3:
					formatter.Serialize(stream, _mission.Teams[_activeTeam]);
					break;
				default:
					break;
			}
			stream.Close();
		}
		void menuER_Click(object sender, EventArgs e)
		{
			Common.LaunchER();
		}
		void menuExit_Click(object sender, EventArgs e)
		{
			Close();
		}
		void menuGoalSummary_Click(object sender, EventArgs e)
		{
			string output = "(global goals not included):\r\n----------\r\n" + generateGoalSummary();
			new GoalSummaryDialog(output).Show();
		}
		void menuHelpInfo_Click(object sender, EventArgs e)
		{
			Common.LaunchHelp();
		}
		void menuIDMR_Click(object sender, EventArgs e)
		{
			Common.LaunchIdmr();
		}
		void menuLST_Click(object sender, EventArgs e)
		{
			try { _fLST.Close(); }
			catch { /* do nothing */ }
			if (_mission.IsBop) _fLST = new LstForm(Settings.Platform.BoP);
			else _fLST = new LstForm(Settings.Platform.XvT);
			_fLST.Show();
		}
		void menuMap_Click(object sender, EventArgs e)
		{
			try { _fMap.Close(); }
			catch { /* do nothing */ }
			_fMap = new MapForm(_config, _mission.FlightGroups);
			_fMap.Show();
		}
		void menuNewBoP_Click(object sender, EventArgs e)
		{
			menuNewXvT_Click("BoP", new EventArgs());
		}
		void menuNewTIE_Click(object sender, EventArgs e)
		{
			promptSave();
			closeForms();
			_applicationExit = false;
			new TieForm(_config).Show();
			Close();
		}
		void menuNewXvT_Click(object sender, EventArgs e)
		{
			promptSave();
			closeForms();
			_loading = true;
			initializeMission();
			updateMissionTabs();
			lstMessages.Items.Clear();
			enableMessages(false);
			lblMessage.Text = "Message #0 of 0";
			lstFG.SelectedIndex = 0;
			for (_activeTeam=0;_activeTeam<10;_activeTeam++) teamRefresh();
			lblTeamArr_Click(0, new EventArgs());
			setBop(sender.ToString() == "BoP");
			if (this.Text.EndsWith("*")) this.Text = this.Text.Substring(0, this.Text.Length-1);
			_loading = false;  //[JB] Fix loading state when creating a new mission
		}
		void menuNewXWA_Click(object sender, EventArgs e)
		{
			promptSave();
			closeForms();
			_applicationExit = false;
			new XwaForm(_config).Show();
			Close();
		}
		void menuOpen_Click(object sender, EventArgs e)
		{
			promptSave();
			opnXvT.FileName = _mission.MissionFileName;
			if (opnXvT.ShowDialog() == DialogResult.OK)  //[JB] Wait until dialog is closed before loading.  Fixes bug where dialog could be stuck open.  Also update paths for last-opened folder. 
			{
				opnXvT_FileOk();
				_config.SetWorkingPath(Path.GetDirectoryName(opnXvT.FileName));
				opnXvT.InitialDirectory = _config.GetWorkingPath(); //Update since folder may have changed
			}
		}
		void menuOptions_Click(object sender, EventArgs e)
		{
			new OptionsDialog(_config).ShowDialog();
		}
		//[JB] Added function for menu item and modified for extra safety checks to prevent deleting when the list controls don't have focus.
		void menuDelete_Click(object sender, EventArgs e)
		{
			if (tabMain.SelectedIndex == 0)
			{
				if((sender.ToString() == "toolbar") || (lstFG.Focused)) deleteFG();
			}
			else if (tabMain.SelectedIndex == 1)
			{
				if((sender.ToString() == "toolbar") || (lstMessages.Focused)) deleteMess();
			}
		}
		void menuPaste_Click(object sender, EventArgs e)
		{
			System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			Stream stream;
			try { stream = new FileStream("YOGEME.bin", FileMode.Open, FileAccess.Read, FileShare.Read); }
			catch { return; }
			#region ArrDep
			if (sender.ToString()== "AD")
			{
				try
				{
					Mission.Trigger t = (Mission.Trigger)formatter.Deserialize(stream);
					_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger] = t;
					lblADTrigArr_Click(_activeArrDepTrigger, new EventArgs());
					labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger], lblADTrig[_activeArrDepTrigger]);
					Common.Title(this, false);
				}
				catch { /* do nothing */ }
				stream.Close();
				return;
			}
			#endregion
			#region Orders
			if (sender.ToString() == "Order")
			{
				try
				{
					FlightGroup.Order o = (FlightGroup.Order)formatter.Deserialize(stream);
					_mission.FlightGroups[_activeFG].Orders[_activeOrder] = o;
					lblOrderArr_Click(_activeOrder, new EventArgs());
					orderLabelRefresh();
					Common.Title(this, false);
				}
				catch { /* do nothing */ }
				stream.Close();
				return;
			}
			#endregion
			#region FG Goals
			if (sender.ToString() == "Goal")
			{
				try
				{
					FlightGroup.Goal g = (FlightGroup.Goal)formatter.Deserialize(stream);
					if (g == null) throw new Exception();
					_mission.FlightGroups[_activeFG].Goals[_activeFGGoal] = g;
					lblGoalArr_Click(_activeFGGoal, new EventArgs());
					goalLabelRefresh();
					Common.Title(this, false);
				}
				catch { /* do nothing */ }
				stream.Close();
				return;
			}
			#endregion
			#region Skip to O4
			if (sender.ToString() == "Skip")
			{
				try
				{
					Mission.Trigger t = (Mission.Trigger)formatter.Deserialize(stream);
					int j = (lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1);
					_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[j] = t;
					lblSkipTrigArr_Click(j, new EventArgs());
					labelRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[j], (j==0 ? lblSkipTrig1 : lblSkipTrig2));	// no array, hence explicit naming
					Common.Title(this, false);
				}
				catch { /* do nothing */ }
				stream.Close();
				return;
			}
			#endregion
			#region generic TextBox
			try
			{
				if (ActiveControl.GetType().ToString() == "System.Windows.Forms.TextBox")
				{
					string s = formatter.Deserialize(stream).ToString();
					if (s.IndexOf("", 0) != -1) throw new Exception();		// bypass FGs, Mess, Teams, etc
					if (s.IndexOf("System.", 0) != -1) throw new Exception();	// bypass byte[]
					System.Windows.Forms.TextBox t = (System.Windows.Forms.TextBox)ActiveControl;
					t.SelectedText = s;
					stream.Close();
					Common.Title(this, false);
					return;
				}
			}
			catch { /* do nothing*/ }
			#endregion
			#region MessTrig
			if (sender.ToString() == "MessTrig")
			{
				try
				{
					Mission.Trigger t = (Mission.Trigger)formatter.Deserialize(stream);
					_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger]= t;
					lblMessTrigArr_Click(_activeMessageTrigger, new EventArgs());
					labelRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger], lblMessTrig[_activeMessageTrigger]);
					Common.Title(this, false);
				}
				catch { /* do nothing */ }
				stream.Close();
				return;
			}
			#endregion
			#region overalls by tab
			switch (tabMain.SelectedIndex)
			{
				case 0:
					try
					{
						FlightGroup fg = (FlightGroup)formatter.Deserialize(stream);
						if (fg == null) throw new Exception();
						//[JB] Need to test copy to make sure it worked
						if(newFG() == false)
							break;
						_mission.FlightGroups[_activeFG] = fg;
						listRefresh();
						_startingShips--;
						lstFG.SelectedIndex = _activeFG;
						craftStart(fg, true);
					}
					catch { /* do nothing */ }
					break;
				case 1:
					try
					{
						Platform.Xvt.Message m = (Platform.Xvt.Message)formatter.Deserialize(stream);
						if (m == null) throw new Exception();
						newMess();
						_mission.Messages[_activeMessage] = m;
						messListRefresh();
						lstMessages.SelectedIndex = _activeMessage;
					}
					catch { /* do nothing */ }
					break;
				case 2:
					try
					{
						Globals.Goal.Trigger t = (Globals.Goal.Trigger)formatter.Deserialize(stream);
						_mission.Globals[_activeTeam].Goals[_activeGlobalTrigger / 4].Triggers[_activeGlobalTrigger % 4] = t;
						lblGlobTrigArr_Click(_activeGlobalTrigger, new EventArgs());
						Common.Title(this, false);
					}
					catch { /* do nothing */ }
					break;
				case 3:
					try
					{
						Team t = (Team)formatter.Deserialize(stream);
						if (t == null) throw new Exception();
						_mission.Teams[_activeTeam] = t;
						teamRefresh();
						lblTeamArr_Click(_activeTeam, new EventArgs());
						Common.Title(this, false);
					}
					catch { /* do nothing */ }
					break;
			}
			#endregion
			stream.Close();
		}
		void menuRecentMissions_Click(object sender, EventArgs e)
		{
			string mission = _config.RecentMissions[(int)((MenuItem)sender).Tag];
			promptSave();
			initializeMission();
			if (loadMission(mission))
			{
				tabMain.SelectedIndex = 0;
				tabFGMinor.SelectedIndex = 0;
				_activeFG = 0;
				lstFG.SelectedIndex = 0;
				_loading = true;		//turned false in previous line
				if (_mission.Messages.Count != 0) lstMessages.SelectedIndex = 0;
			}
			_config.SetWorkingPath(Path.GetDirectoryName(mission)); //[JB] Update last-accessed
			opnXvT.InitialDirectory = _config.GetWorkingPath();
			savXvT.InitialDirectory = _config.GetWorkingPath();
			_loading = false;
		}
		void menuSave_Click(object sender, EventArgs e)
		{
			if (_mission.MissionPath == "\\NewMission.tie") savXvT.ShowDialog();
			else saveMission(_mission.MissionPath);
		}
		void menuSaveAsBoP_Click(object sender, EventArgs e)
		{
			setBop(true);
			savXvT.ShowDialog();
		}
		void menuSaveAsTIE_Click(object sender, EventArgs e)
		{
			promptSave();
			try
			{
				Platform.Tie.Mission converted = Platform.Converter.XvtBopToTie(_mission);
				converted.Save();
			}
			catch (ArgumentException x)
			{
				MessageBox.Show(x.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		void menuSaveAsXvT_Click(object sender, EventArgs e)
		{
			setBop(false);
			savXvT.ShowDialog();
		}
		void menuSaveAsXWA_Click(object sender, EventArgs e)
		{
			menuSave_Click("SaveAsXWA", new System.EventArgs());
			Common.RunConverter(_mission.MissionPath, 2);
		}
		void menuTest_Click(object sender, EventArgs e)
		{
			if (_config.ConfirmTest)
			{
				DialogResult res = new TestDialog(_config).ShowDialog();
				if (res == DialogResult.Cancel) return;
			}
			// prep stuff
			menuSave_Click("menuTest_Click", new EventArgs());
			if (_config.VerifyTest && !_config.Verify) Common.RunVerify(_mission.MissionPath, _config.VerifyLocation);
			/*Version os = Environment.OSVersion.Version;
			bool isWin7 = (os.Major == 6 && os.Minor == 1);
			System.Diagnostics.Process explorer = null;
			int restart = 1;
			Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", true);*/

			// configure XvT/BoP
			int index = 0;
			string path = (_mission.IsBop ? _config.BopPath : _config.XvtPath);
			while (File.Exists(path + "\\test" + index + "0.plt")) index++;
			string pilot = "\\test" + index + "0.plt";
			string bopPilot = "\\test" + index + "0.pl2";
			string lst = "\\Train\\IMPERIAL.LST";
			string backup = "\\Train\\IMPERIAL_" + index + ".bak";

			File.Copy(Application.StartupPath + "\\xvttest0.plt", _config.XvtPath + pilot);
			if (_config.BopInstalled) File.Copy(Application.StartupPath + "\\xvttest0.pl2", _config.BopPath + bopPilot, true);
			// XvT pilot edit
			FileStream pilotFile = File.OpenWrite(_config.XvtPath + pilot);
			pilotFile.Position = 4;
			char[] indexBytes = index.ToString().ToCharArray();
			new BinaryWriter(pilotFile).Write(indexBytes);
			for (int i = (int)pilotFile.Position; i < 0xC; i++) pilotFile.WriteByte(0);
			pilotFile.Close();
			// BoP pilot edit
			if (_config.BopInstalled)
			{
				pilotFile = File.OpenWrite(_config.BopPath + bopPilot);
				pilotFile.Position = 4;
				indexBytes = index.ToString().ToCharArray();
				new BinaryWriter(pilotFile).Write(indexBytes);
				for (int i = (int)pilotFile.Position; i < 0xC; i++) pilotFile.WriteByte(0);
				pilotFile.Close();
			}

			// configure XvT
			System.Diagnostics.Process xvt = new System.Diagnostics.Process();
			xvt.StartInfo.FileName = path + "\\Z_XVT__.exe";
			xvt.StartInfo.Arguments = "/skipintro";
			xvt.StartInfo.UseShellExecute = false;
			xvt.StartInfo.WorkingDirectory = path;
			File.Copy(path + lst, path + backup, true);
			StreamReader sr = File.OpenText(_config.XvtPath + "\\Config.cfg");
			string contents = sr.ReadToEnd();
			sr.Close();
			int lastpilot = contents.IndexOf("lastpilot ") + 10;
			int nextline = contents.IndexOf("\r\n", lastpilot);
			string modified = contents.Substring(0, lastpilot) + "test" + index + contents.Substring(nextline);
			StreamWriter sw = new FileInfo(_config.XvtPath + "\\Config.cfg").CreateText();
			sw.Write(modified);
			sw.Close();
			if (_config.BopInstalled)
			{
				sr = File.OpenText(_config.BopPath + "\\config2.cfg");
				contents = sr.ReadToEnd();
				sr.Close();
				lastpilot = contents.IndexOf("lastpilot ") + 10;
				nextline = contents.IndexOf("\r\n", lastpilot);
				modified = contents.Substring(0, lastpilot) + "test" + index + contents.Substring(nextline);
				sw = new FileInfo(_config.BopPath + "\\config2.cfg").CreateText();
				sw.Write(modified);
				sw.Close();
			}
			sr = File.OpenText(path + lst);
			contents = sr.ReadToEnd();
			sr.Close();
			string[] expanded = contents.Replace("\r\n", "\0").Split('\0');
			expanded[4] = _mission.MissionFileName;
			expanded[5] = "YOGEME: " + expanded[4];
			modified = String.Join("\r\n", expanded);
			sw = new FileInfo(path + lst).CreateText();
			sw.Write(modified);
			sw.Close();

			/*if (isWin7)	// explorer kill so colors work right
			{
				restart = (int)key.GetValue("AutoRestartShell", 1);
				key.SetValue("AutoRestartShell", 0, Microsoft.Win32.RegistryValueKind.DWord);
				explorer = System.Diagnostics.Process.GetProcessesByName("explorer")[0];
				explorer.Kill();
				explorer.WaitForExit();
			}*/

			xvt.Start();
			System.Threading.Thread.Sleep(1000);
			System.Diagnostics.Process[] runningXvts = System.Diagnostics.Process.GetProcessesByName("Z_XVT__");
			while (runningXvts.Length > 0)
			{
				Application.DoEvents();
				System.Diagnostics.Debug.WriteLine("sleeping...");
				System.Threading.Thread.Sleep(1000);
				runningXvts = System.Diagnostics.Process.GetProcessesByName("Z_XVT__");
			}

			/*if (isWin7)	// restart
			{
				key.SetValue("AutoRestartShell", restart, Microsoft.Win32.RegistryValueKind.DWord);
				explorer.StartInfo.UseShellExecute = false;
				explorer.StartInfo.FileName = "explorer.exe";
				explorer.Start();
			}*/
			if (_config.DeleteTestPilots)
			{
				File.Delete(_config.XvtPath + pilot);
				File.Delete(_config.BopPath + bopPilot);
			}
			File.Copy(path + backup, path + lst, true);
			File.Delete(path + backup);
		}
		void menuVerify_Click(object sender, EventArgs e)
		{
			menuSave_Click("Verify", new System.EventArgs());
			if (!_config.Verify) Common.RunVerify(_mission.MissionPath, _config.VerifyLocation);	//prevents from doing this twice due to Save
		}
		#endregion
		#region Flight Groups
		//[JB] Counts all trigger and parameter references of a flight group.  Used to populate the counters in the confirm deletion dialog.
		int[] countFlightGroupReferences(int fgIndex)
		{
			int[] count = new int[8];
			const int cMothership = 1, cArrDep = 2, cOrder = 3, cSkip = 4, cGoal = 5, cMessage = 6, cBrief = 7;
			for(int i = 0; i < _mission.FlightGroups.Count; i++)
			{
				if(fgIndex == i)
					continue;

				FlightGroup fg = _mission.FlightGroups[i];
				if(fg.ArrivalMethod1 == true && fg.ArrivalCraft1 == fgIndex) count[cMothership]++;
				if(fg.ArrivalMethod2 == true && fg.ArrivalCraft2 == fgIndex) count[cMothership]++;
				if(fg.DepartureMethod1 == true && fg.DepartureCraft1 == fgIndex) count[cMothership]++;
				if(fg.DepartureMethod2 == true && fg.DepartureCraft2 == fgIndex) count[cMothership]++;
				foreach(Mission.Trigger adt in fg.ArrDepTriggers)
				{
					//Note: many FGs initially present in battle use the first FG for Arr/Dep condition, even though the FG isn't actually used (condition is TRUE or FALSE). In which case no need to warn.
					if(adt.VariableType == 1 && adt.Variable == fgIndex && adt.Condition != 0 && adt.Condition != 10) count[cArrDep]++;
				}
				foreach(FlightGroup.Order order in fg.Orders)
				{
					if (order.Target1Type == 1 && order.Target1 == fgIndex) count[cOrder]++;
					if (order.Target2Type == 1 && order.Target2 == fgIndex) count[cOrder]++;
					if (order.Target3Type == 1 && order.Target3 == fgIndex) count[cOrder]++;
					if (order.Target4Type == 1 && order.Target4 == fgIndex) count[cOrder]++;
				}
				foreach (Mission.Trigger sk in fg.SkipToOrder4Trigger)
					if (sk.VariableType == 1 && sk.Variable == fgIndex) count[cSkip]++;
			}

			foreach (Globals global in _mission.Globals)
				foreach(Globals.Goal goal in global.Goals)
					foreach(Globals.Goal.Trigger trig in goal.Triggers)
						if (trig.GoalTrigger.VariableType == 1 && trig.GoalTrigger.Variable == fgIndex)
							count[cGoal]++;

			foreach (Idmr.Platform.Xvt.Message msg in _mission.Messages)
				foreach(Mission.Trigger trig in msg.Triggers)
					if (trig.VariableType == 1 && trig.Variable == fgIndex)
						count[cMessage]++;

			foreach(var br in _mission.Briefings)
			{
				int p = 0;
				while(p < br.EventsLength)
				{
					if(br.Events[p+1] >= (int)Briefing.EventType.FGTag1 && br.Events[p+1] <= (int)Briefing.EventType.FGTag8)
						if(br.Events[p+2] == fgIndex)
							count[cBrief]++;

					p += 2 + br.EventParameterCount[br.Events[p+1]];
				}
			}

			for(int i = 1; i < 8; i++)
				count[0] += count[i];
			return count;
		}
		//[JB] Removes all references and triggers to a flight group and replaces with null triggers.
		void scrubFG(int fgIndex)
		{
			for (int i = 0; i < _mission.FlightGroups.Count; i++)
			{
				if (fgIndex == i)
					continue;

				FlightGroup fg = _mission.FlightGroups[i];
				bool check = fg.ArrivesIn30Seconds;

				//Don't check method, always scrub if referenced
				if (fg.ArrivalCraft1 == fgIndex) { fg.ArrivalMethod1 = false; fg.ArrivalCraft1 = 0; }
				if (fg.ArrivalCraft2 == fgIndex) { fg.ArrivalMethod2 = false; fg.ArrivalCraft2 = 0; }
				if (fg.DepartureCraft1 == fgIndex) { fg.DepartureMethod1 = false; fg.DepartureCraft1 = 0; }
				if (fg.DepartureCraft2 == fgIndex) { fg.DepartureMethod2 = false; fg.DepartureCraft2 = 0; }
				for(int j = 0; j < fg.ArrDepTriggers.Length; j++)
				{
					Mission.Trigger adt = fg.ArrDepTriggers[j];
					if(adt.VariableType == 1 && adt.Variable == fgIndex)
					{
						adt.Amount = 0;
						adt.VariableType = 0;
						adt.Variable = 0;
						adt.Condition = (byte)((j < 4) ? 0 : 10); //  //First 4 are arrival.  Set those to true.  Departure set to _trigger[10] = "none (FALSE)"
					}
				}
				foreach (FlightGroup.Order order in fg.Orders)
				{
					if (order.Target1Type == 1 && order.Target1 == fgIndex) { order.Target1Type = 0; order.Target1 = 0; }
					if (order.Target2Type == 1 && order.Target2 == fgIndex) { order.Target2Type = 0; order.Target2 = 0; }
					if (order.Target3Type == 1 && order.Target3 == fgIndex) { order.Target3Type = 0; order.Target3 = 0; }
					if (order.Target4Type == 1 && order.Target4 == fgIndex) { order.Target4Type = 0; order.Target4 = 0; }
				}
				foreach (Mission.Trigger sk in fg.SkipToOrder4Trigger)
				{
					if (sk.VariableType == 1 && sk.Variable == fgIndex)
					{
						sk.Amount = 0;
						sk.VariableType = 0;
						sk.Variable = 0;
						sk.Condition = 10;  //Set to false.
					}
				}
				if (check != fg.ArrivesIn30Seconds)
					craftStart(fg, fg.ArrivesIn30Seconds);  //Add or delete based on change

			}
			foreach (Globals global in _mission.Globals)
			{
				foreach (Globals.Goal goal in global.Goals)
				{
					foreach (Globals.Goal.Trigger trig in goal.Triggers)
					{
						if (trig.GoalTrigger.VariableType == 1 && trig.GoalTrigger.Variable == fgIndex)
						{
							trig.GoalTrigger.Amount = 0;
							trig.GoalTrigger.VariableType = 0;
							trig.GoalTrigger.Variable = 0;
							trig.GoalTrigger.Condition = 10; //_trigger[10] = "none (FALSE)"
						}
					}
				}
			}
			foreach (Idmr.Platform.Xvt.Message msg in _mission.Messages)
			{
				foreach (Mission.Trigger trig in msg.Triggers)
				{
					if (trig.VariableType == 1 && trig.Variable == fgIndex)
					{
						trig.Amount = 0;
						trig.VariableType = 0;
						trig.Variable = 0;
						trig.Condition = 0; //_trigger[0] = "always (TRUE)"
					}
				}
			}
			//XvT has multiple briefings. Walk through the raw data of each briefing and count.
			foreach(var br in _mission.Briefings)
			{
				int p = 0;
				int paramCount = 0;
				while(p < br.EventsLength)
				{
					if(br.Events[p+1] == (int)Briefing.EventType.EndBriefing)
						break;
					bool delete = false;
					if(br.Events[p+1] >= (int)Briefing.EventType.FGTag1 && br.Events[p+1] <= (int)Briefing.EventType.FGTag8)
					{
						if(br.Events[p+2] == fgIndex)
						{
							int len = br.EventsLength; //get() walks the event list, so cache the current value as the modifications will temporarily corrupt it
							paramCount = 2 + br.EventParameterCount[br.Events[p+1]];
							for(int i = p; i < len - paramCount; i++)
								br.Events[i] = br.Events[i + paramCount];  //Drop everything down
							for(int i = len - paramCount; i < len; i++)
								br.Events[i] = 0;  //Erase the final space
							delete = true;
						}
					}
					if(delete == false)  //If we didn't delete, advance, otherwise recheck the new event that dropped into this position
						p += 2 + br.EventParameterCount[br.Events[p+1]];
				}
			}
		}
		//[JB] When a FG is deleted, all further FGs drop down one index.  This function decrements all craft references to compensate.
		void scrubFGMove(int fgIndex)
		{
			for (int i = 0; i < _mission.FlightGroups.Count; i++)
			{
				if (fgIndex == i)
					continue;

				FlightGroup fg = _mission.FlightGroups[i];

				if (fg.ArrivalCraft1 > fgIndex) { fg.ArrivalCraft1--; }
				if (fg.ArrivalCraft2 > fgIndex) { fg.ArrivalCraft2--; }
				if (fg.DepartureCraft1 > fgIndex) { fg.DepartureCraft1--; }
				if (fg.DepartureCraft2 > fgIndex) { fg.DepartureCraft2--; }
				foreach (Mission.Trigger adt in fg.ArrDepTriggers)
					if (adt.VariableType == 1 && adt.Variable > fgIndex)
						adt.Variable--;

				foreach (FlightGroup.Order order in fg.Orders)
				{
					if (order.Target1Type == 1 && order.Target1 > fgIndex) { order.Target1--; }
					if (order.Target2Type == 1 && order.Target2 > fgIndex) { order.Target2--; }
					if (order.Target3Type == 1 && order.Target3 > fgIndex) { order.Target3--; }
					if (order.Target4Type == 1 && order.Target4 > fgIndex) { order.Target4--; }
				}

				foreach (Mission.Trigger sk in fg.SkipToOrder4Trigger)
					if (sk.VariableType == 1 && sk.Variable > fgIndex)
						sk.Variable--;
			}
			foreach (Globals global in _mission.Globals)
				foreach (Globals.Goal goal in global.Goals)
					foreach (Globals.Goal.Trigger trig in goal.Triggers)
						if (trig.GoalTrigger.VariableType == 1 && trig.GoalTrigger.Variable > fgIndex)
							trig.GoalTrigger.Variable--;

			foreach (Idmr.Platform.Xvt.Message msg in _mission.Messages)
				foreach (Mission.Trigger trig in msg.Triggers)
					if (trig.VariableType == 1 && trig.Variable > fgIndex)
						trig.Variable--;

			foreach(var br in _mission.Briefings)
			{
				int p = 0;
				while(p < br.EventsLength)
				{
					if(br.Events[p+1] == (int)Briefing.EventType.EndBriefing)
						break;
					if(br.Events[p+1] >= (int)Briefing.EventType.FGTag1 && br.Events[p+1] <= (int)Briefing.EventType.FGTag8)
						if(br.Events[p+2] > fgIndex)
							br.Events[p+2]--;
					p += 2 + br.EventParameterCount[br.Events[p+1]];
				}
			}
		}
		void deleteFG()
		{
			if(_fBrief != null)  //Need to be able to examine the current briefing data if open and modified
			{
				_fBrief.Save();
				_fBrief.Close();
			}
			//[JB] Confirm delete
			if(_config.ConfirmFGDelete)
			{
				int[] count = countFlightGroupReferences(_activeFG);
				if (count[0] > 0)
				{
					string[] Reasons = new string[8] { "", "Mothership reference", "Arrival/Departure trigger", "Order target reference", "'Skip to Order 4' trigger", "Global Goal trigger", "Message trigger", "Briefing FG Tag" };
					string breakdown = "";
					for(int i = 1; i < 8; i++)
						if(count[i] > 0) breakdown += "    " + count[i] + " " + Reasons[i] + ((count[i]>1)?"s":"") + "\n";

					string s = "This Flight Group is referenced " + count[0] + " time" + ((count[1]>1)?"s":"") + " in these cases:\n" + breakdown + "\nAll references targeting this flight group will be reset to default.";
					if(count[7] > 0) s += "\nAssociated Briefing FG Tag events will be deleted.";
					s += "\n\nAre you sure you want to delete this Flight Group?";
					DialogResult res = MessageBox.Show(s, "WARNING: Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
					if (res == DialogResult.No)
						return;
				}
			}
			scrubFG(_activeFG);
			scrubFGMove(_activeFG);

			if (_mission.FlightGroups.Count != 1) lstFG.Items.RemoveAt(_activeFG);
			craftStart(_mission.FlightGroups[_activeFG], false);
			if (_mission.FlightGroups.Count == 1)
			{
				_mission.FlightGroups.Clear();
				_activeFG = 0;
				_mission.FlightGroups[0].CraftType = _config.XvtCraft;
				_mission.FlightGroups[0].IFF = _config.XvtIff;
				craftStart(_mission.FlightGroups[0], true);
			}
			else _activeFG = _mission.FlightGroups.RemoveAt(_activeFG);
			updateFGList();
			lstFG.SelectedIndex = _activeFG;
			try
			{
				_fMap.Import(_mission.FlightGroups);
				_fMap.MapPaint(true);
			}
			catch { /* do nothing */ }
			try
			{
				_fBrief.Import(_mission.FlightGroups);
				_fBrief.MapPaint();
			}
			catch { /* do nothing */ }
			Common.Title(this, _loading);

			//[JB] Need to force these tabs to refresh, otherwise the trigger controls might display outdated information
			lstMessages_SelectedIndexChanged(0, new EventArgs());
			cboGlobalTeam_SelectedIndexChanged(0, new EventArgs());
			lstFG_SelectedIndexChanged(0, new EventArgs());

			if(!lstFG.Focused) lstFG.Focus();  //[JB] Return control back to the list (helpful to maintain navigation using the arrow keys when certain tabs are open)
		}
		void listRefresh()
		{
			lstFG.Items[_activeFG] = _mission.FlightGroups[_activeFG].ToString(true);
			lstFG.Items[_activeFG] += " ";	// force refresh, otherwise solo IFF change wouldn't show
		}
		//[JB] Added return type so copy function can test if it worked.
		bool newFG()
		{
			if (_mission.FlightGroups.Count >= Mission.FlightGroupLimit)
			{
				MessageBox.Show("Mission contains maximum number of Flight Groups", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			_activeFG = _mission.FlightGroups.Add();
			_mission.FlightGroups[_activeFG].CraftType = _config.XvtCraft;
			_mission.FlightGroups[_activeFG].IFF = _config.XvtIff;
			craftStart(_mission.FlightGroups[_activeFG], true);
			lstFG.Items.Add("");	// only need to create the item, listRefresh() will fill it in
			updateFGList();
			lstFG.SelectedIndex = _activeFG;
			_loading = false;
			try
			{
				_fMap.Import(_mission.FlightGroups);
				_fMap.MapPaint(true);
			}
			catch { /* do nothing */ }
			try
			{
				_fBrief.Import(_mission.FlightGroups);
				_fBrief.MapPaint();
			}
			catch { /* do nothing */ }
			Common.Title(this, _loading);
			return true;
		}
		void updateFGList()
		{
			string[] fgList = _mission.FlightGroups.GetList();
			bool temp = _loading;
			_loading = true;
			comboReset(cboArrMS, fgList, 0);
			comboReset(cboArrMSAlt, fgList, 0);
			comboReset(cboDepMS, fgList, 0);
			comboReset(cboDepMSAlt, fgList, 0);
			_loading = temp;
			listRefresh();
		}
		string generateGoalSummary()
		{
			//30 elements:  Primary,Prevent,Bonus, ... (repeat for each team)
			//Each element contains a list of strings for each line of text.
			//Was going to try and summarize global goals but the output was ugly, so removed it. Easy for the user to view those anyway.
			System.Collections.Generic.List<string>[] goalList = new System.Collections.Generic.List<string>[30];

			for (int i = 0; i < 30; i++)
				goalList[i] = new System.Collections.Generic.List<string>();

			//Iterate FGs and their goals, adding them to the proper list
			for (int i = 0; i < _mission.FlightGroups.Count; i++)
			{
				FlightGroup fg = _mission.FlightGroups[i];
				foreach (FlightGroup.Goal goal in fg.Goals)
				{
					if (goal.Condition == 0 || goal.Condition == 10 || goal.Team > 9)  //Avoid True, False conditions and make sure team is valid.
						continue;
					string c = Strings.CraftAbbrv[fg.CraftType] + " " + fg.Name;
					string n = goal.ToString().Replace("Flight Group", c);
					int category = (goal.Argument <= 1) ? 0 : 2;  //0 = primary, 1 = prevent, 2 = bonus
					goalList[(goal.Team * 3) + category].Add(n);
				}
			}

			//Compose the output by going through the teams and writing the results from each goal category
			string output = "";
			for (int i = 0; i < 10; i++) //Each team
			{
				if (goalList[(i * 3) + 0].Count == 0 && goalList[(i * 3) + 2].Count == 0)  //No primary or bonus goals
					continue;

				if (output.Length > 0)
					output += "\r\n----------\r\n";
				output += "TEAM #" + (i + 1) + ": " + _mission.Teams[i].Name + "\r\n";
				output += "PRIMARY:\r\n";
				foreach (string s in goalList[(i * 3) + 0])
					output += s + "\r\n";

				if (goalList[(i * 3) + 1].Count > 0)
				{
					output += "\r\n";
					output += "SECONDARY:\r\n";
					foreach (string s in goalList[(i * 3) + 1])
						output += s + "\r\n";
				}

				if (goalList[(i * 3) + 2].Count > 0)
				{
					output += "\r\n";
					output += "BONUS:\r\n";
					foreach (string s in goalList[(i * 3) + 2])
						output += s + "\r\n";
				}
			}
			if (output == "") output = "Nothing here.";
			output += "\r\n";

			return output;
		}

		void lstFG_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (e.Index == -1 || _mission.FlightGroups[e.Index] == null) return;
			e.DrawBackground();
			Brush brText = SystemBrushes.ControlText;
			switch(_mission.FlightGroups[e.Index].IFF)
			{
				case 0:
					brText = Brushes.LimeGreen;
					break;
				case 1:
					brText = Brushes.Crimson;
					break;
				case 2:
					brText = Brushes.RoyalBlue;
					break;
				case 3:
					brText = Brushes.Yellow;
					break;
				case 4:
					brText = Brushes.Red;
					break;
				case 5:
					brText = Brushes.DarkOrchid;
					break;
			}
			e.Graphics.DrawString(lstFG.Items[e.Index].ToString(), e.Font, brText, e.Bounds, StringFormat.GenericDefault);
		}
		void lstFG_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstFG.SelectedIndex == -1) return;
			_activeFG = lstFG.SelectedIndex;
			lblFG.Text = "Flight Group #" + (_activeFG+1).ToString() + " of " + _mission.FlightGroups.Count.ToString();
			bool btemp = _loading;
			_loading = true;
			#region Craft
			txtName.Text = _mission.FlightGroups[_activeFG].Name;
			txtCargo.Text = _mission.FlightGroups[_activeFG].Cargo;
			txtSpecCargo.Text = _mission.FlightGroups[_activeFG].SpecialCargo;  //[JB] Fixed, was setting to Cargo
			numSC.Value = _mission.FlightGroups[_activeFG].SpecialCargoCraft;
			chkRandSC.Checked = _mission.FlightGroups[_activeFG].RandSpecCargo;
			cboCraft.SelectedIndex = _mission.FlightGroups[_activeFG].CraftType;
			cboIFF.SelectedIndex = _mission.FlightGroups[_activeFG].IFF;
			cboTeam.SelectedIndex = _mission.FlightGroups[_activeFG].Team;
			try { cboAI.SelectedIndex = _mission.FlightGroups[_activeFG].AI; }	// for some reason, some custom missions have this as -1
			catch { cboAI.SelectedIndex = 2; _mission.FlightGroups[_activeFG].AI = 2; }
			cboMarkings.SelectedIndex = _mission.FlightGroups[_activeFG].Markings;
			cboPlayer.SelectedIndex = _mission.FlightGroups[_activeFG].PlayerNumber;
			cboPosition.SelectedIndex = _mission.FlightGroups[_activeFG].PlayerCraft;
			cboFormation.SelectedIndex = _mission.FlightGroups[_activeFG].Formation;
			cboRadio.SelectedIndex = _mission.FlightGroups[_activeFG].Radio;
			numLead.Value = _mission.FlightGroups[_activeFG].FormLeaderDist;
			numSpacing.Value = _mission.FlightGroups[_activeFG].FormDistance;
			numWaves.Value = _mission.FlightGroups[_activeFG].NumberOfWaves;
			numCraft.Value = _mission.FlightGroups[_activeFG].NumberOfCraft;
			numGG.Value = _mission.FlightGroups[_activeFG].GlobalGroup;
			numGU.Value = _mission.FlightGroups[_activeFG].GlobalUnit;
			cboStatus.SelectedIndex = _mission.FlightGroups[_activeFG].Status1;
			cboStatus2.SelectedIndex = _mission.FlightGroups[_activeFG].Status2;
			cboWarheads.SelectedIndex = _mission.FlightGroups[_activeFG].Missile;
			cboBeam.SelectedIndex = _mission.FlightGroups[_activeFG].Beam;
			cboCounter.SelectedIndex = _mission.FlightGroups[_activeFG].Countermeasures;
			numExplode.Value = _mission.FlightGroups[_activeFG].ExplosionTime;
			#endregion
			#region Arr/Dep
			optArrMS.Checked = _mission.FlightGroups[_activeFG].ArrivalMethod1;
			optArrHyp.Checked = !optArrMS.Checked;
			try { cboArrMS.SelectedIndex = _mission.FlightGroups[_activeFG].ArrivalCraft1; }
			catch { cboArrMS.SelectedIndex = 0; _mission.FlightGroups[_activeFG].ArrivalCraft1 = 0; optArrHyp.Checked = true; }
			optArrMSAlt.Checked = _mission.FlightGroups[_activeFG].ArrivalMethod2;
			optArrHypAlt.Checked = !optArrMSAlt.Checked;
			try { cboArrMSAlt.SelectedIndex = _mission.FlightGroups[_activeFG].ArrivalCraft2; }
			catch { cboArrMSAlt.SelectedIndex = 0; _mission.FlightGroups[_activeFG].ArrivalCraft2 = 0; optArrHypAlt.Checked = true; }
			optDepMS.Checked = _mission.FlightGroups[_activeFG].DepartureMethod1;
			optDepHyp.Checked = !optDepMS.Checked;
			try { cboDepMS.SelectedIndex = _mission.FlightGroups[_activeFG].DepartureCraft1; }
			catch { cboDepMS.SelectedIndex = 0; _mission.FlightGroups[_activeFG].DepartureCraft1 = 0; optDepHyp.Checked = true; }
			optDepMSAlt.Checked = _mission.FlightGroups[_activeFG].DepartureMethod2;
			optDepHypAlt.Checked = !optDepMSAlt.Checked;
			try { cboDepMSAlt.SelectedIndex = _mission.FlightGroups[_activeFG].DepartureCraft2; }
			catch { cboDepMSAlt.SelectedIndex = 0; _mission.FlightGroups[_activeFG].DepartureCraft2 = 0; optDepHypAlt.Checked = true; }
			for (int i=0;i<4;i++)
			{
				optADAndOr[i].Checked = _mission.FlightGroups[_activeFG].ArrDepAO[i];
				optADAndOr[i+4].Checked = !optADAndOr[i].Checked;
			}
			numArrMin.Value = _mission.FlightGroups[_activeFG].ArrivalDelayMinutes;
			numArrSec.Value = _mission.FlightGroups[_activeFG].ArrivalDelaySeconds;
			numDepMin.Value = _mission.FlightGroups[_activeFG].DepartureTimerMinutes;
			numDepSec.Value = _mission.FlightGroups[_activeFG].DepartureTimerSeconds;
			cboAbort.SelectedIndex = _mission.FlightGroups[_activeFG].AbortTrigger;
			cboDiff.SelectedIndex = _mission.FlightGroups[_activeFG].Difficulty;
			chkArrHuman.Checked = _mission.FlightGroups[_activeFG].ArriveOnlyIfHuman;
			for (int i=0;i<6;i++) labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[i], lblADTrig[i]);
			lblADTrigArr_Click(0, new EventArgs());
			#endregion
			for (_activeFGGoal=0;_activeFGGoal<8;_activeFGGoal++) goalLabelRefresh();
			lblGoalArr_Click(0, new EventArgs());
			#region Waypoints
			for (int i=0;i<22;i++)
			{
				for (int j=0;j<3;j++)
				{
					_tableRaw.Rows[i][j] = _mission.FlightGroups[_activeFG].Waypoints[i][j];
					_table.Rows[i][j] = Math.Round((double)_mission.FlightGroups[_activeFG].Waypoints[i][j] / 160, 2);
				}
				chkWP[i].Checked = _mission.FlightGroups[_activeFG].Waypoints[i].Enabled;
			}
			_tableRaw.AcceptChanges();
			_table.AcceptChanges();
			numYaw.Value = _mission.FlightGroups[_activeFG].Yaw;
			numPitch.Value = _mission.FlightGroups[_activeFG].Pitch;
			numRoll.Value = _mission.FlightGroups[_activeFG].Roll;
			if (_mission.FlightGroups[_activeFG].CraftType <= 0x45) enableRot(false);
			else enableRot(true);
			#endregion
			for (_activeOrder=0;_activeOrder<4;_activeOrder++) orderLabelRefresh();
			lblOrderArr_Click(0, new EventArgs());
			#region Options
			cboRoleImp.SelectedIndex = 0;
			cboRoleReb.SelectedIndex = 0;
			cboRole3.SelectedIndex = 0;
			cboRole4.SelectedIndex = 0;
			cboRoleAll.SelectedIndex = 0;
			string[] str_role = new string[Strings.Roles.Length];
			for (int i=0;i<str_role.Length;i++) str_role[i] = Strings.Roles[i].Substring(0, 3).ToUpper();
			for (int i=0;i<4;i++)
			{
				try
				{
					string str_abbrv = _mission.FlightGroups[_activeFG].Roles[i].Substring(1);
					int j;
					for (j=(str_role.Length-1);j>0;j--) if (str_abbrv == str_role[j]) break;	// reversed to default to zero
					switch (_mission.FlightGroups[_activeFG].Roles[i].Substring(0, 1))
					{
						case "1":
							cboRoleImp.SelectedIndex = j;
							break;
						case "2":
							cboRoleReb.SelectedIndex = j;
							break;
						case "3":
							cboRole3.SelectedIndex = j;
							break;
						case "4":
							cboRole4.SelectedIndex = j;
							break;
						case "a":
							cboRoleAll.SelectedIndex = j;
							break;
						default:
							break;
					}
				}
				catch { /* do nothing */ }
			}
			for (int i=0;i<15;i++) chkOpt[i].Checked = _mission.FlightGroups[_activeFG].OptLoadout[i];
			lblSkipTrigArr_Click(lblSkipTrig1, new EventArgs());  //[JB] Fixed
			lblSkipTrigArr_Click(lblSkipTrig2, new EventArgs());
			for (_activeOptionCraft = 0; _activeOptionCraft < 10; _activeOptionCraft++) optCraftLabelRefresh();
			lblOptCraftArr_Click(0, new EventArgs());
			cboOptCat.SelectedIndex = (int)_mission.FlightGroups[_activeFG].OptCraftCategory;
			optSkipOR.Checked = _mission.FlightGroups[_activeFG].SkipToO4T1AndOrT2;
			optSkipAND.Checked = !optSkipOR.Checked;
			#endregion
			#region Unknowns
			numUnk1.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown1;
			chkUnk2.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown2;
			numUnk3.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown3;
			numUnk4.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown4;
			numUnk5.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown5;
			numUnkOrder.Value = 1;
			numUnkOrder_ValueChanged(0, new EventArgs()); //[JB] Force refresh of associated checkboxes
			numUnkGoal.Value = 1;
			numUnkGoal_ValueChanged(0, new EventArgs()); 
			chkUnk17.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown17;
			chkUnk18.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown18;
			chkUnk19.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown19;
			numUnk20.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown20;
			numUnk21.Value = _mission.FlightGroups[_activeFG].Unknowns.Unknown21;
			chkUnk22.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown22;
			chkUnk23.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown23;
			chkUnk24.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown24;
			chkUnk25.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown25;
			chkUnk26.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown26;
			chkUnk27.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown27;
			chkUnk28.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown28;
			chkUnk29.Checked = _mission.FlightGroups[_activeFG].Unknowns.Unknown29;
			#endregion
			//[JB] Need to check special cargo to restore edit box if necessary.
			bool sc = _mission.FlightGroups[_activeFG].RandSpecCargo | (_mission.FlightGroups[_activeFG].SpecialCargoCraft > 0);
			lblNotUsed.Visible = sc ? false : true;
			txtSpecCargo.Visible = sc ? true : false;
			if(numBackdrop.Enabled) //[JB] Need to update the backdrop edit box
			{
				int v = _mission.FlightGroups[_activeFG].Status1;
				if(v > numBackdrop.Maximum)
					v = (int)numBackdrop.Maximum;
				numBackdrop.Value = v;  //[JB] Need to enforce limit since some XvT missions have values out of range which throw an exception when trying to select that FG
			}
			_loading = btemp;
		}

		#region Craft
		void enableBackdrop(bool state)
		{
			cboStatus2.Enabled = !state;
			cboWarheads.Enabled = !state;
			cboCounter.Enabled = !state;
			cboBeam.Enabled = !state;
			numCraft.Enabled = !state;
			numWaves.Enabled = !state;
			txtCargo.Enabled = !state;
			txtSpecCargo.Enabled = !state;
			numSC.Enabled = !state;
			chkRandSC.Enabled = !state;
			cboAI.Enabled = !state;
			cboMarkings.Enabled = !state;
			cboPlayer.Enabled = !state;
			cboPosition.Enabled = !state;
			cboRadio.Enabled = !state;
			cboFormation.Enabled = !state;
			numSpacing.Enabled = !state;
			numLead.Enabled = !state;
			cmdForms.Enabled = !state;
			cmdBackdrop.Enabled = state;
			numBackdrop.Enabled = state;
			cboStatus.Enabled = !state;
			if (state) lblStatus.Text = "Backdrop";
			else lblStatus.Text = "Status";
		}

		void cboCraft_SelectedIndexChanged(object sender, EventArgs e)
		{
			enableBackdrop((cboCraft.SelectedIndex == 0x57 ? true : false));
			if (_loading) return;
			_mission.FlightGroups[_activeFG].CraftType = Common.Update(this, _mission.FlightGroups[_activeFG].CraftType, Convert.ToByte(cboCraft.SelectedIndex));
			updateFGList();
		}
		void cboFormation_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.FlightGroups[_activeFG].Formation = (byte)cboFormation.SelectedIndex;
			Common.Title(this, false);
		}
		void cboStatus_Leave(object sender, EventArgs e)
		{
			//[JB] Added try/catch since there are more Status effects than Backdrops
			try { numBackdrop.Value = cboStatus.SelectedIndex; }
			catch { numBackdrop.Value = numBackdrop.Maximum; }
			_mission.FlightGroups[_activeFG].Status1 = Common.Update(this, _mission.FlightGroups[_activeFG].Status1, Convert.ToByte(cboStatus.SelectedIndex));
		}

		void chkRandSC_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.FlightGroups[_activeFG].RandSpecCargo = chkRandSC.Checked;
			if (chkRandSC.Checked)
			{
				numSC.Value = 0;
				lblNotUsed.Visible = false;
				txtSpecCargo.Visible = true;
			}
			else if (numSC.Value == 0)
			{
				lblNotUsed.Visible = true;
				txtSpecCargo.Visible = false;
			}
			Common.Title(this, false);
		}

		void cmdBackdrop_Click(object sender, EventArgs e)
		{
			try
			{
				BackdropDialog dlg = new BackdropDialog((_mission.IsBop ? Platform.MissionFile.Platform.BoP : Idmr.Platform.MissionFile.Platform.XvT), _mission.FlightGroups[_activeFG].Status1);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					numBackdrop.Value = dlg.BackdropIndex;	// simply GUI
					cboStatus.SelectedIndex = (int)numBackdrop.Value;	// drives stored value
				}
			}
			catch (ArgumentException x)
			{
				MessageBox.Show(x.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		void cmdForms_Click(object sender, EventArgs e)
		{
			try  //[JB] Added try/catch
			{
				FormationDialog dlg = new FormationDialog(_mission.FlightGroups[_activeFG].Formation);
				if (dlg.ShowDialog() == DialogResult.OK)
					cboFormation.SelectedIndex = Common.Update(this, cboFormation.SelectedIndex, dlg.Formation);
			}
			catch { MessageBox.Show("Could not load the Formations browser.", "Error"); }
		}

		void grpCraft2_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].IFF = Common.Update(this, _mission.FlightGroups[_activeFG].IFF, Convert.ToByte(cboIFF.SelectedIndex));
			_mission.FlightGroups[_activeFG].Team = Common.Update(this, _mission.FlightGroups[_activeFG].Team, Convert.ToByte(cboTeam.SelectedIndex));
			_mission.FlightGroups[_activeFG].AI = Common.Update(this, _mission.FlightGroups[_activeFG].AI, Convert.ToByte(cboAI.SelectedIndex));
			_mission.FlightGroups[_activeFG].Markings = Common.Update(this, _mission.FlightGroups[_activeFG].Markings, Convert.ToByte(cboMarkings.SelectedIndex));
			_mission.FlightGroups[_activeFG].PlayerNumber = Common.Update(this, _mission.FlightGroups[_activeFG].PlayerNumber, Convert.ToByte(cboPlayer.SelectedIndex));
			_mission.FlightGroups[_activeFG].PlayerCraft = Common.Update(this, _mission.FlightGroups[_activeFG].PlayerCraft, Convert.ToByte(cboPosition.SelectedIndex));
			_mission.FlightGroups[_activeFG].Formation = Common.Update(this, _mission.FlightGroups[_activeFG].Formation, Convert.ToByte(cboFormation.SelectedIndex));
			_mission.FlightGroups[_activeFG].Radio = Common.Update(this, _mission.FlightGroups[_activeFG].Radio, Convert.ToByte(cboRadio.SelectedIndex));
			_mission.FlightGroups[_activeFG].FormDistance = Common.Update(this, _mission.FlightGroups[_activeFG].FormDistance, Convert.ToByte(numSpacing.Value));
			_mission.FlightGroups[_activeFG].FormLeaderDist = Common.Update(this, _mission.FlightGroups[_activeFG].FormLeaderDist, Convert.ToByte(numLead.Value));
			listRefresh();
		}
		void grpCraft3_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].NumberOfWaves = Common.Update(this, _mission.FlightGroups[_activeFG].NumberOfWaves, Convert.ToByte(numWaves.Value));
			craftStart(_mission.FlightGroups[_activeFG], false);
			_mission.FlightGroups[_activeFG].NumberOfCraft = Common.Update(this, _mission.FlightGroups[_activeFG].NumberOfCraft, Convert.ToByte(numCraft.Value));
			craftStart(_mission.FlightGroups[_activeFG], true);
			if (_mission.FlightGroups[_activeFG].SpecialCargoCraft > _mission.FlightGroups[_activeFG].NumberOfCraft) numSC.Value = 0;
			_mission.FlightGroups[_activeFG].GlobalGroup = Common.Update(this, _mission.FlightGroups[_activeFG].GlobalGroup, Convert.ToByte(numGG.Value));
			_mission.FlightGroups[_activeFG].GlobalUnit = Common.Update(this, _mission.FlightGroups[_activeFG].GlobalUnit, Convert.ToByte(numGU.Value));
			listRefresh();
		}
		void grpCraft4_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Status2 = Common.Update(this, _mission.FlightGroups[_activeFG].Status2, Convert.ToByte(cboStatus2.SelectedIndex));
			_mission.FlightGroups[_activeFG].Missile = Common.Update(this, _mission.FlightGroups[_activeFG].Missile, Convert.ToByte(cboWarheads.SelectedIndex));
			_mission.FlightGroups[_activeFG].Beam = Common.Update(this, _mission.FlightGroups[_activeFG].Beam, Convert.ToByte(cboBeam.SelectedIndex));
			_mission.FlightGroups[_activeFG].Countermeasures = Common.Update(this, _mission.FlightGroups[_activeFG].Countermeasures, Convert.ToByte(cboCounter.SelectedIndex));
			//XvtMission.FlightGroups[FG].ExplosionTime = Convert.ToByte(Common.Title(this, false, XvtMission.FlightGroups[activeFG].ExplosionTime, numExplode.Value));
		}

		void numBackdrop_Leave(object sender, EventArgs e)
		{
			cboStatus.SelectedIndex = (int)numBackdrop.Value;
			_mission.FlightGroups[_activeFG].Status1 = Common.Update(this, _mission.FlightGroups[_activeFG].Status1, Convert.ToByte(cboStatus.SelectedIndex));
		}
		void numSC_ValueChanged(object sender, EventArgs e)
		{
			Common.Title(this, _loading);
			if (_mission.FlightGroups[_activeFG].RandSpecCargo) { numSC.Value = 0; return;  }
			if (numSC.Value == 0 || numSC.Value > _mission.FlightGroups[_activeFG].NumberOfCraft)
			{
				_mission.FlightGroups[_activeFG].SpecialCargoCraft = 0;
				txtSpecCargo.Visible = false;
				lblNotUsed.Visible = true;
			}
			else
			{
				_mission.FlightGroups[_activeFG].SpecialCargoCraft = (byte)numSC.Value;
				txtSpecCargo.Visible = true;
				lblNotUsed.Visible = false;
			}
		}

		void txtCargo_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Cargo = Common.Update(this, _mission.FlightGroups[_activeFG].Cargo, txtCargo.Text);
		}
		void txtName_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Name = Common.Update(this, _mission.FlightGroups[_activeFG].Name, txtName.Text);
			updateFGList();
		}
		void txtSpecCargo_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].SpecialCargo = Common.Update(this, _mission.FlightGroups[_activeFG].SpecialCargo, txtSpecCargo.Text);
		}
		#endregion
		#region Arr/Dep
		void lblADTrigArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeArrDepTrigger = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeArrDepTrigger = Convert.ToByte(sender); l = lblADTrig[_activeArrDepTrigger]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<6;i++) if (i != _activeArrDepTrigger) lblADTrig[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboADTrig.SelectedIndex = _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Condition;
			cboADTrigType.SelectedIndex = -1;
			cboADTrigType.SelectedIndex = _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].VariableType;
			cboADTrigAmount.SelectedIndex = _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Amount;
			_loading = btemp;
		}
		void lblADTrigArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("AD", new EventArgs());
		}
		void lblADTrigArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("AD", new EventArgs());
		}
		void optADAndOrArr_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			RadioButton r = (RadioButton)sender;
			_mission.FlightGroups[_activeFG].ArrDepAO[(int)r.Tag] = Common.Update(this, _mission.FlightGroups[_activeFG].ArrDepAO[(int)r.Tag], r.Checked);
		}

		void cboADTrig_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Condition = Common.Update(this, _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Condition, Convert.ToByte(cboADTrig.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger], lblADTrig[_activeArrDepTrigger]);
		}
		void cboADTrigAmount_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Amount = Common.Update(this, _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Amount, Convert.ToByte(cboADTrigAmount.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger], lblADTrig[_activeArrDepTrigger]);
		}
		void cboADTrigType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboADTrigType.SelectedIndex == -1) return;
			if (!_loading)
				_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].VariableType = Common.Update(this, _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].VariableType, Convert.ToByte(cboADTrigType.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].VariableType, cboADTrigVar);
			try { cboADTrigVar.SelectedIndex = _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Variable; }
			catch { cboADTrigVar.SelectedIndex = 0; _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Variable = 0; }
			labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger], lblADTrig[_activeArrDepTrigger]);
		}
		void cboADTrigVar_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Variable = Common.Update(this, _mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger].Variable, Convert.ToByte(cboADTrigVar.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].ArrDepTriggers[_activeArrDepTrigger], lblADTrig[_activeArrDepTrigger]);
		}
		void cboArrMS_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArrivalCraft1 = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalCraft1, Convert.ToByte(cboArrMS.SelectedIndex));
		}
		void cboArrMSAlt_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArrivalCraft2 = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalCraft2, Convert.ToByte(cboArrMSAlt.SelectedIndex));
		}
		void cboDiff_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Difficulty = Common.Update(this, _mission.FlightGroups[_activeFG].Difficulty, Convert.ToByte(cboDiff.SelectedIndex));
		}

		void chkArrHuman_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ArriveOnlyIfHuman = Common.Update(this, _mission.FlightGroups[_activeFG].ArriveOnlyIfHuman, chkArrHuman.Checked);
		}

		void cmdCopyAD_Click(object sender, EventArgs e)
		{
			menuCopy_Click("AD", new EventArgs());
		}
		void cmdPasteAD_Click(object sender, EventArgs e)
		{
			menuPaste_Click("AD", new EventArgs());
		}

		void grpDep_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].DepartureCraft1 = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureCraft1, Convert.ToByte(cboDepMS.SelectedIndex));
			_mission.FlightGroups[_activeFG].DepartureCraft2 = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureCraft2, Convert.ToByte(cboDepMSAlt.SelectedIndex));
			_mission.FlightGroups[_activeFG].AbortTrigger = Common.Update(this, _mission.FlightGroups[_activeFG].AbortTrigger, Convert.ToByte(cboAbort.SelectedIndex));
			_mission.FlightGroups[_activeFG].DepartureTimerMinutes = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureTimerMinutes, Convert.ToByte(numDepMin.Value));
			_mission.FlightGroups[_activeFG].DepartureTimerSeconds = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureTimerSeconds, Convert.ToByte(numDepSec.Value));
		}

		void numArrMin_Leave(object sender, EventArgs e)
		{
			craftStart(_mission.FlightGroups[_activeFG], false);
			_mission.FlightGroups[_activeFG].ArrivalDelayMinutes = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalDelayMinutes, Convert.ToByte(numArrMin.Value));
			craftStart(_mission.FlightGroups[_activeFG], true);
		}
		void numArrSec_Leave(object sender, EventArgs e)
		{
			craftStart(_mission.FlightGroups[_activeFG], false);
			_mission.FlightGroups[_activeFG].ArrivalDelaySeconds = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalDelaySeconds, Convert.ToByte(numArrSec.Value));
			craftStart(_mission.FlightGroups[_activeFG], true);
		}

		void optArrMS_CheckedChanged(object sender, EventArgs e)
		{
			cboArrMS.Enabled = optArrMS.Checked;
			if (!_loading) _mission.FlightGroups[_activeFG].ArrivalMethod1 = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalMethod1, optArrMS.Checked);
		}
		void optArrMSAlt_CheckedChanged(object sender, EventArgs e)
		{
			cboArrMSAlt.Enabled = optArrMSAlt.Checked;
			if (!_loading) _mission.FlightGroups[_activeFG].ArrivalMethod2 = Common.Update(this, _mission.FlightGroups[_activeFG].ArrivalMethod2, optArrMSAlt.Checked);
		}
		void optDepMS_CheckedChanged(object sender, EventArgs e)
		{
			cboDepMS.Enabled = optDepMS.Checked;
			if (!_loading) _mission.FlightGroups[_activeFG].DepartureMethod1 = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureMethod1, optDepMS.Checked);
		}
		void optDepMSAlt_CheckedChanged(object sender, EventArgs e)
		{
			cboDepMSAlt.Enabled = optDepMSAlt.Checked;
			if (!_loading) _mission.FlightGroups[_activeFG].DepartureMethod2 = Common.Update(this, _mission.FlightGroups[_activeFG].DepartureMethod2, optDepMSAlt.Checked);
		}
		#endregion
		#region Orders
		void orderLabelRefresh()
		{
			string orderText = _mission.FlightGroups[_activeFG].Orders[_activeOrder].ToString();
			orderText = replaceTargetText(orderText);
			lblOrder[_activeOrder].Text = "Order " + (_activeOrder + 1) + ": " + orderText;
		}
		
		void lblOrderArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeOrder = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeOrder = Convert.ToByte(sender); l = lblOrder[_activeOrder]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<4;i++) if (i!=_activeOrder) lblOrder[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboOrders.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder][0];
			cboOThrottle.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Throttle;
			numOVar1.Value = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable1;
			numOVar2.Value = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable2;
			cboOT3Type.SelectedIndex = -1;
			cboOT3Type.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3Type;
			cboOT4Type.SelectedIndex = -1;
			cboOT4Type.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4Type;
			optOT3T4OR.Checked = Convert.ToBoolean(_mission.FlightGroups[_activeFG].Orders[_activeOrder].T3AndOrT4);
			optOT3T4AND.Checked = !optOT3T4OR.Checked;
			cboOT1Type.SelectedIndex = -1;
			cboOT1Type.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1Type;
			cboOT2Type.SelectedIndex = -1;
			cboOT2Type.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2Type;
			optOT1T2OR.Checked = Convert.ToBoolean(_mission.FlightGroups[_activeFG].Orders[_activeOrder].T1AndOrT2);
			optOT1T2AND.Checked = !optOT1T2OR.Checked;
			numOSpeed.Value = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Speed;
			txtOString.Text = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Designation;
			_loading = btemp;
		}
		void lblOrderArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("Order", new EventArgs());
		}
		void lblOrderArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("Order", new EventArgs());
		}

		void cboOrders_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.FlightGroups[_activeFG].Orders[_activeOrder].Command = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Command, Convert.ToByte(cboOrders.SelectedIndex));
			orderLabelRefresh();
			string[] s = Strings.OrderDesc[cboOrders.SelectedIndex].Split('|');
			lblODesc.Text = s[0];
			lblOVar1.Text = s[1];
			lblOVar2.Text = s[2];
		}
		void cboOT1_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1, Convert.ToByte(cboOT1.SelectedIndex));
			orderLabelRefresh();
		}
		void cboOT1Type_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboOT1Type.SelectedIndex == -1) return;
			if (!_loading)
				_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1Type = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1Type, Convert.ToByte(cboOT1Type.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1Type, cboOT1);
			try { cboOT1.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1; }
			catch { cboOT1.SelectedIndex = 0; _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target1 = 0; }
			orderLabelRefresh();
		}
		void cboOT2_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2, Convert.ToByte(cboOT2.SelectedIndex));
			orderLabelRefresh();
		}
		void cboOT2Type_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboOT2Type.SelectedIndex == -1) return;
			if (!_loading)
				_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2Type = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2Type, Convert.ToByte(cboOT2Type.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2Type, cboOT2);
			try { cboOT2.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2; }
			catch { cboOT2.SelectedIndex = 0; _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target2 = 0; }
			orderLabelRefresh();
		}
		void cboOT3_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3, Convert.ToByte(cboOT3.SelectedIndex));
			orderLabelRefresh();
		}
		void cboOT3Type_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboOT3Type.SelectedIndex == -1) return;
			if (!_loading)
				_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3Type = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3Type, Convert.ToByte(cboOT3Type.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3Type, cboOT3);
			try { cboOT3.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3; }
			catch { cboOT3.SelectedIndex = 0; _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target3 = 0; }
			orderLabelRefresh();
		}
		void cboOT4_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4, Convert.ToByte(cboOT4.SelectedIndex));
			orderLabelRefresh();
		}
		void cboOT4Type_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboOT4Type.SelectedIndex == -1) return;
			if (!_loading)
				_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4Type = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4Type, Convert.ToByte(cboOT4Type.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4Type, cboOT4);
			try { cboOT4.SelectedIndex = _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4; }
			catch { cboOT4.SelectedIndex = 0; _mission.FlightGroups[_activeFG].Orders[_activeOrder].Target4 = 0; }
			orderLabelRefresh();
		}
		void cboOThrottle_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Throttle = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Throttle, Convert.ToByte(cboOThrottle.SelectedIndex));
		}

		void cmdCopyOrder_Click(object sender, EventArgs e)
		{
			menuCopy_Click("Order", new EventArgs());
		}
		void cmdPasteOrder_Click(object sender, EventArgs e)
		{
			menuPaste_Click("Order", new EventArgs());
		}

		void numOSpeed_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Speed = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Speed, Convert.ToByte(numOSpeed.Value));
		}
		void numOVar1_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable1 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable1, Convert.ToByte(numOVar1.Value));
		}
		void numOVar2_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable2 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Variable2, Convert.ToByte(numOVar2.Value));
		}

		void optOT1T2OR_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].T1AndOrT2 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].T1AndOrT2, optOT1T2OR.Checked);
			orderLabelRefresh();
		}
		void optOT3T4OR_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].T3AndOrT4 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].T3AndOrT4, optOT3T4OR.Checked);
			orderLabelRefresh();
		}
		void optSkipOR_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.FlightGroups[_activeFG].SkipToO4T1AndOrT2 = Common.Update(this, _mission.FlightGroups[_activeFG].SkipToO4T1AndOrT2, optSkipOR.Checked);
		}

		void txtOString_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Orders[_activeOrder].Designation = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[_activeOrder].Designation, txtOString.Text);
		}
		#endregion
		#region Goals
		void goalLabelRefresh()
		{
			lblGoal[_activeFGGoal].Text = "Goal " + (_activeFGGoal+1).ToString() + ": " + _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].ToString();
		}
		
		void lblGoalArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeFGGoal = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeFGGoal = Convert.ToByte(sender); l = lblGoal[_activeFGGoal]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<8;i++) if (i!=_activeFGGoal) lblGoal[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboGoalArgument.SelectedIndex = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Argument;
			cboGoalTrigger.SelectedIndex = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Condition;
			cboGoalAmount.SelectedIndex = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Amount;
			numGoalPoints.Value = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Points;
			chkGoalEnable.Checked = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Enabled;
			numGoalTeam.Value = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Team+1;
			txtGoalInc.Text = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].IncompleteText;
			txtGoalComp.Text = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].CompleteText;
			txtGoalFail.Text = _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].FailedText;
			_loading = btemp;
		}
		void lblGoalArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("Goal", new EventArgs());
		}
		void lblGoalArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("Goal", new EventArgs());
		}

		void chkGoalEnable_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Enabled = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Enabled, chkGoalEnable.Checked);
			goalLabelRefresh();
		}

		void grpGoal_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Argument = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Argument, Convert.ToByte(cboGoalArgument.SelectedIndex));
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Condition = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Condition, Convert.ToByte(cboGoalTrigger.SelectedIndex));
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Amount = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Amount, Convert.ToByte(cboGoalAmount.SelectedIndex));
			goalLabelRefresh();
		}

		void numGoalPoints_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Points = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Points, (short)numGoalPoints.Value);
			goalLabelRefresh();
		}
		void numGoalTeam_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Team = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].Team, Convert.ToByte(numGoalTeam.Value - 1));
		}

		void txtGoalComp_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].CompleteText = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].CompleteText, txtGoalComp.Text);
		}
		void txtGoalFail_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].FailedText = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].FailedText, txtGoalFail.Text);
		}
		void txtGoalInc_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Goals[_activeFGGoal].IncompleteText = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[_activeFGGoal].IncompleteText, txtGoalInc.Text);
		}
		#endregion
		#region Waypoints
		void enableRot(bool state)
		{
			numYaw.Enabled = state;
			numRoll.Enabled = state;
		}

		void chkWPArr_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			CheckBox c = (CheckBox)sender;
			_mission.FlightGroups[_activeFG].Waypoints[(int)c.Tag].Enabled = Common.Update(this, _mission.FlightGroups[_activeFG].Waypoints[(int)c.Tag].Enabled, c.Checked);
		}

		void table_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			int i, j=0;
			if (_loading) return;
			_loading = true;
			for (j=0;j<22;j++) if (_table.Rows[j].Equals(e.Row)) break;	//find the row index that you're changing
			try
			{
				for (i=0;i<3;i++)
				{
					short raw = (short)(Convert.ToDouble(_table.Rows[j][i]) * 160);
					_mission.FlightGroups[_activeFG].Waypoints[j][i] = Common.Update(this, _mission.FlightGroups[_activeFG].Waypoints[j][i], raw);
					_tableRaw.Rows[j][i] = raw;
				}
			}
			catch { for (i=0;i<3;i++) _table.Rows[j][i] = Math.Round((double)(_mission.FlightGroups[_activeFG].Waypoints[j][i]) / 160, 2); }	// reset
			_loading = false;
		}
		void tableRaw_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			int i, j=0;
			if (_loading) return;
			_loading = true;
			for (j=0;j<22;j++) if (_tableRaw.Rows[j].Equals(e.Row)) break;	//find the row index that you're changing
			try
			{
				for (i=0;i<3;i++)
				{
					short raw = Convert.ToInt16(_tableRaw.Rows[j][i]);
					_mission.FlightGroups[_activeFG].Waypoints[j][i] = Common.Update(this, _mission.FlightGroups[_activeFG].Waypoints[j][i], raw);
					_table.Rows[j][i] = Math.Round((double)raw / 160, 2);
				}
			}
			catch { for (i=0;i<3;i++) _tableRaw.Rows[j][i] = _mission.FlightGroups[_activeFG].Waypoints[j][i]; }
			_loading = false;
		}

		void numPitch_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Pitch = Common.Update(this, _mission.FlightGroups[_activeFG].Pitch, (short)numPitch.Value);
		}
		void numRoll_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Roll = Common.Update(this, _mission.FlightGroups[_activeFG].Roll, (short)numRoll.Value);
		}
		void numYaw_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Yaw = Common.Update(this, _mission.FlightGroups[_activeFG].Yaw, (short)numYaw.Value);
		}
		#endregion
		#region Options
		void enableOptCat(bool state)
		{
			numOptCraft.Enabled = state;
			numOptWaves.Enabled = state;
			cboOptCraft.Enabled = state;
			for (int i=0;i<10;i++) lblOptCraft[i].Enabled = state;
		}
		void optCraftLabelRefresh()
		{
			lblOptCraft[_activeOptionCraft].Text = "Craft " + (_activeOptionCraft+1).ToString() + ":";
			if (_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].CraftType != 0) lblOptCraft[_activeOptionCraft].Text += " " + (_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfWaves+1)
				+ " x (" + (_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfCraft) + ") " + Strings.CraftType[_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].CraftType]; //Strings.CraftType[_activeOptionCraft];  //[JB] Fix so all labels properly refresh in load time, getting the craft name directly from the setting rather than the combobox . Also fixed, NumberOfCraft is not +1, should be exactly as is.
		}
		void updateRole(string teamChar)
		{
			ComboBox c = null;
			switch (teamChar)
			{
				case "1":
					c = cboRoleImp;
					break;
				case "2":
					c = cboRoleReb;
					break;
				case "3":
					c = cboRole3;
					break;
				case "4":
					c = cboRole4;
					break;
				case "a":
					c = cboRoleAll;
					break;
				default:
					return;
			}
			string s = teamChar + c.Text.Substring(0, 3).ToUpper();
			for (int i=0;i<4;i++)	// first, try to find existing entry and replace it
			{
				try
				{
					if (_mission.FlightGroups[_activeFG].Roles[i].StartsWith(teamChar))
					{
						_mission.FlightGroups[_activeFG].Roles[i] = Common.Update(this, _mission.FlightGroups[_activeFG].Roles[i], s);
						return;
					}
				}
				catch { /* do nothing */ }	// block is to catch null strings
			}
			// no entry
			for (int i=0;i<4;i++)
			{
				if (_mission.FlightGroups[_activeFG].Roles[i] == "")
				{
					Common.Title(this, false);
					_mission.FlightGroups[_activeFG].Roles[i] = s;
					break;	// find the first unused
				}
			}
			// if this is the fifth role, then tough luck it's not getting saved
		}

		void chkOptArr_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			CheckBox c = (CheckBox)sender;
			int i = (int)c.Tag;
			_mission.FlightGroups[_activeFG].OptLoadout[i] = Common.Update(this, _mission.FlightGroups[_activeFG].OptLoadout[i], c.Checked);
			bool tempLoad = _loading;
			_loading = true;
			if (c.Checked)
			{
				if (i == 0) for (int j=1;j<8;j++) _mission.FlightGroups[_activeFG].OptLoadout[j] = false;	// turn off warheads
				else if (i > 0 && i < 8) _mission.FlightGroups[_activeFG].OptLoadout[0] = false;		// clear warhead None
				else if (i == 8) for (int j=9;j<12;j++) _mission.FlightGroups[_activeFG].OptLoadout[j] = false;	// turn off beams
				else if (i > 8 && i < 12) _mission.FlightGroups[_activeFG].OptLoadout[8] = false;	// clear beam None
				else if (i == 12) { _mission.FlightGroups[_activeFG].OptLoadout[13] = false; _mission.FlightGroups[_activeFG].OptLoadout[14] = false; }	// turn off CMs
				else _mission.FlightGroups[_activeFG].OptLoadout[12] = false;	// clear CM None
			}
			else
			{
				//[JB] Changed variable in for() loop from "i" to "j" to avoid overwriting
				//Fixed grouping for "b" since unchecking would erase other remaining groups.
				bool b = false;
				if (i > 0 && i < 8) {
					for (int j = 1; j < 8; j++) b |= _mission.FlightGroups[_activeFG].OptLoadout[j];
					if (!b && !chkOpt[0].Checked) _mission.FlightGroups[_activeFG].OptLoadout[0] = true; }
				b = false;
				if (i > 8 && i < 12) {
					for (int j = 9; j < 12; j++) b |= _mission.FlightGroups[_activeFG].OptLoadout[j];
					if (!b && !chkOpt[8].Checked) _mission.FlightGroups[_activeFG].OptLoadout[8] = true; }
				b = false;
				if (i > 12 && i < 15) {
					for (int j = 13; j < 15; j++) b |= _mission.FlightGroups[_activeFG].OptLoadout[j];
					if (!b && !chkOpt[12].Checked) _mission.FlightGroups[_activeFG].OptLoadout[12] = true; }
			}
			for (i=0;i<15;i++) chkOpt[i].Checked = _mission.FlightGroups[_activeFG].OptLoadout[i];
			_loading = tempLoad;
		}
		void lblOptCraftArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeOptionCraft = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeOptionCraft = Convert.ToByte(sender); l = lblOptCraft[_activeOptionCraft]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<10;i++) if (i!=_activeOptionCraft) lblOptCraft[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboOptCraft.SelectedIndex = _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].CraftType;
			int numCraft = _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfCraft;
			if(numCraft < 1) numCraft = 1;
			numOptCraft.Value = numCraft; //[JB] This isn't +1 like craft waves is (appears in XvT exactly as its value), but the control only accepts a minimum of 1.
			numOptWaves.Value = _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfWaves+1;
			_loading = btemp;
		}
		void lblSkipTrigArr_Click(object sender, EventArgs e)
		{
			Label l = null; // clicked
			Label ll = null; // other one
			int i = 0;
			try
			{
				l = (Label)sender;
				l.Focus();
				ll = (l==lblSkipTrig1 ? lblSkipTrig2 : lblSkipTrig1);
				i = (l == lblSkipTrig1 ? 0 : 1);	// i = clicked trigger
			}
			catch (InvalidCastException)
			{
				i = (int)sender;	// i = clicked trigger from code
				if (i == 0) { l = lblSkipTrig1; ; ll = lblSkipTrig2; }
				else { l = lblSkipTrig2; ll = lblSkipTrig1; }
			}
			l.ForeColor = SystemColors.Highlight;
			ll.ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboSkipTrig.SelectedIndex = _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Condition;
			cboSkipType.SelectedIndex = -1;
			cboSkipType.SelectedIndex = _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].VariableType;
			cboSkipAmount.SelectedIndex = _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Amount;
			_loading = btemp;
		}
		void lblSkipTrigArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("Skip", new EventArgs());
		}
		void lblSkipTrigArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("Skip", new EventArgs());
		}

		void cboOptCat_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.FlightGroups[_activeFG].OptCraftCategory = Common.Update(this, _mission.FlightGroups[_activeFG].OptCraftCategory, (FlightGroup.OptionalCraftCategory)Convert.ToByte(cboOptCat.SelectedIndex));
			enableOptCat((cboOptCat.SelectedIndex == 4));
		}
		void cboOptCraft_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].CraftType = Common.Update(this, _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].CraftType, Convert.ToByte(cboOptCraft.SelectedIndex));
			optCraftLabelRefresh();
		}
		void cboSkipTrig_Leave(object sender, EventArgs e)
		{
			int i = (lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1);
			_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Condition = Common.Update(this, _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Condition, Convert.ToByte(cboSkipTrig.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i], (i==0 ? lblSkipTrig1 : lblSkipTrig2));
		}
		void cboSkipType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboSkipType.SelectedIndex == -1) return;
			int i = (lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1);
			if (!_loading)
				_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].VariableType = Common.Update(this, _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].VariableType, Convert.ToByte(cboSkipType.SelectedIndex));
			comboVarRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].VariableType, cboSkipVar);
			try { cboSkipVar.SelectedIndex = _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Variable; }
			catch { cboSkipVar.SelectedIndex = 0; _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Variable = 0; }
			labelRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i], (i==0 ? lblSkipTrig1 : lblSkipTrig2));
		}
		void cboSkipVar_Leave(object sender, EventArgs e)
		{
			int i = (lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1);
			_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Variable = Common.Update(this, _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Variable, Convert.ToByte(cboSkipVar.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i], (i==0 ? lblSkipTrig1 : lblSkipTrig2));
		}
		void cboSkipAmount_Leave(object sender, EventArgs e)
		{
			int i = (lblSkipTrig1.ForeColor == SystemColors.Highlight ? 0 : 1);
			_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Amount = Common.Update(this, _mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i].Amount, Convert.ToByte(cboSkipAmount.SelectedIndex));
			labelRefresh(_mission.FlightGroups[_activeFG].SkipToOrder4Trigger[i], (i==0 ? lblSkipTrig1 : lblSkipTrig2));
		}

		void grpRole_Leave(object sender, EventArgs e)
		{
			updateRole("1");
			updateRole("2");
			updateRole("3");
			updateRole("4");
			updateRole("a");
		}

		void numExplode_ValueChanged(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].ExplosionTime = (byte)numExplode.Value;
		}
		void numOptWaves_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfWaves = Common.Update(this, _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfWaves, Convert.ToByte(numOptWaves.Value - 1));
			optCraftLabelRefresh();
		}
		void numOptCraft_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfCraft = Common.Update(this, _mission.FlightGroups[_activeFG].OptCraft[_activeOptionCraft].NumberOfCraft, Convert.ToByte(numOptCraft.Value));  //[JB] Fixed, no -1.  Take value exactly as is.
			optCraftLabelRefresh();
		}
		#endregion
		#region Unknowns
		void grpUnkAD_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Unknowns.Unknown3 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown3, Convert.ToByte(numUnk3.Value));
			_mission.FlightGroups[_activeFG].Unknowns.Unknown4 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown4, Convert.ToByte(numUnk4.Value));
			_mission.FlightGroups[_activeFG].Unknowns.Unknown5 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown5, Convert.ToByte(numUnk5.Value));
		}
		void grpUnkCraft_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Unknowns.Unknown1 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown1, Convert.ToByte(numUnk1.Value));
			_mission.FlightGroups[_activeFG].Unknowns.Unknown2 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown2, chkUnk2.Checked);
		}
		void grpUnkGoal_Leave(object sender, EventArgs e)
		{
			numUnkGoal_Enter("grpUnkGoal_Leave", new EventArgs());
		}
		void grpUnkOrder_Leave(object sender, EventArgs e)
		{
			numUnkOrder_Enter("grpUnkOrder_Leave", new EventArgs());
		}
		void grpUnkOther_Leave(object sender, EventArgs e)
		{
			_mission.FlightGroups[_activeFG].Unknowns.Unknown17 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown17, chkUnk17.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown18 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown18, chkUnk18.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown19 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown19, chkUnk19.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown20 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown20, Convert.ToByte(numUnk20.Value));
			_mission.FlightGroups[_activeFG].Unknowns.Unknown21 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown21, Convert.ToByte(numUnk21.Value));
			_mission.FlightGroups[_activeFG].Unknowns.Unknown22 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown22, chkUnk22.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown23 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown23, chkUnk23.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown24 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown24, chkUnk24.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown25 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown25, chkUnk25.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown26 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown26, chkUnk26.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown27 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown27, chkUnk27.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown28 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown28, chkUnk28.Checked);
			_mission.FlightGroups[_activeFG].Unknowns.Unknown29 = Common.Update(this, _mission.FlightGroups[_activeFG].Unknowns.Unknown29, chkUnk29.Checked);
		}

		void numUnkGoal_Enter(object sender, EventArgs e)
		{
			int i = (int)numUnkGoal.Value - 1;
			_mission.FlightGroups[_activeFG].Goals[i].Unknown10 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown10, chkUnk10.Checked);
			_mission.FlightGroups[_activeFG].Goals[i].Unknown11 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown11, chkUnk11.Checked);
			_mission.FlightGroups[_activeFG].Goals[i].Unknown12 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown12, chkUnk12.Checked);
			_mission.FlightGroups[_activeFG].Goals[i].Unknown13 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown13, Convert.ToByte(numUnk13.Value));
			_mission.FlightGroups[_activeFG].Goals[i].Unknown14 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown14, chkUnk14.Checked);
			_mission.FlightGroups[_activeFG].Goals[i].Unknown16 = Common.Update(this, _mission.FlightGroups[_activeFG].Goals[i].Unknown16, Convert.ToByte(numUnk16.Value));
		}
		void numUnkGoal_ValueChanged(object sender, EventArgs e)
		{
			int i = (int)numUnkGoal.Value - 1;
			chkUnk10.Checked = _mission.FlightGroups[_activeFG].Goals[i].Unknown10;
			chkUnk11.Checked = _mission.FlightGroups[_activeFG].Goals[i].Unknown11;
			chkUnk12.Checked = _mission.FlightGroups[_activeFG].Goals[i].Unknown12;
			numUnk13.Value = _mission.FlightGroups[_activeFG].Goals[i].Unknown13;
			chkUnk14.Checked = _mission.FlightGroups[_activeFG].Goals[i].Unknown14;
			numUnk16.Value = _mission.FlightGroups[_activeFG].Goals[i].Unknown16;
		}
		void numUnkOrder_Enter(object sender, EventArgs e)
		{
			int i = (int)numUnkOrder.Value - 1;
			_mission.FlightGroups[_activeFG].Orders[i].Unknown6 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[i].Unknown6, Convert.ToByte(numUnk6.Value));
			_mission.FlightGroups[_activeFG].Orders[i].Unknown7 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[i].Unknown7, Convert.ToByte(numUnk7.Value));
			_mission.FlightGroups[_activeFG].Orders[i].Unknown8 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[i].Unknown8, Convert.ToByte(numUnk8.Value));
			_mission.FlightGroups[_activeFG].Orders[i].Unknown9 = Common.Update(this, _mission.FlightGroups[_activeFG].Orders[i].Unknown9, Convert.ToByte(numUnk9.Value));
		}
		void numUnkOrder_ValueChanged(object sender, EventArgs e)
		{
			int i = (int)numUnkOrder.Value - 1;
			numUnk6.Value = _mission.FlightGroups[_activeFG].Orders[i].Unknown6;
			numUnk7.Value = _mission.FlightGroups[_activeFG].Orders[i].Unknown7;
			numUnk8.Value = _mission.FlightGroups[_activeFG].Orders[i].Unknown8;
			numUnk9.Value = _mission.FlightGroups[_activeFG].Orders[i].Unknown9;
		}
		#endregion
		#endregion
		#region Messages
		void newMess()
		{
			if (_mission.Messages.Count == Mission.MessageLimit)
			{
				MessageBox.Show("Mission contains maximum number of Messages.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			_activeMessage = _mission.Messages.Add();
			if (_mission.Messages.Count == 1) enableMessages(true);
			lstMessages.Items.Add(_mission.Messages[_activeMessage].MessageString);
			lstMessages.SelectedIndex = _activeMessage;
			Common.Title(this, _loading);
		}
		void deleteMess()
		{
			if(_activeMessage < 0 || _activeMessage >= _mission.Messages.Count)  //[JB] Added check
				return;
			lstMessages.Items.RemoveAt(_activeMessage); //[JB] Need to delete from list before _activeMessage changes, otherwise it may remove the wrong index.
			_activeMessage = _mission.Messages.RemoveAt(_activeMessage);
			if (_mission.Messages.Count == 0)
			{
				lstMessages.Items.Clear();
				enableMessages(false);
				lblMessage.Text = "Message #0 of 0";
				return;
			}
			lstMessages.SelectedIndex = _activeMessage;
			Common.Title(this, _loading);
		}
		void enableMessages(bool state)
		{
			grpMessages.Enabled = state;
			txtMessage.Enabled = state;
			txtShort.Enabled = state;
			grpSend.Enabled = state;
			numMessDelay.Enabled = state;
			cboMessTrig.Enabled = state;
			cboMessType.Enabled = state;
			cboMessVar.Enabled = state;
			cboMessAmount.Enabled = state;
			cboMessColor.Enabled = state;
		}
		void messListRefresh()
		{
			if (_mission.Messages.Count == 0) return;
			lstMessages.Items[_activeMessage] = _mission.Messages[_activeMessage].MessageString;
		}

		void lstMessages_DrawItem(object sender, DrawItemEventArgs e)
		{
			if (_mission.Messages.Count == 0 || _mission.Messages[e.Index] == null) return;
			e.DrawBackground();
			Brush brText = SystemBrushes.ControlText;
			switch (_mission.Messages[e.Index].Color)
			{
				case 0:
					brText = Brushes.Crimson;
					break;
				case 1:
					brText = Brushes.LimeGreen;
					break;
				case 2:
					brText = Brushes.RoyalBlue;
					break;
				case 3:
					brText = Brushes.Yellow;
					break;
			}
			e.Graphics.DrawString(lstMessages.Items[e.Index].ToString(), e.Font, brText, e.Bounds, StringFormat.GenericDefault);
		}
		void lstMessages_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstMessages.SelectedIndex == -1) return;
			_activeMessage = lstMessages.SelectedIndex;
			lblMessage.Text = "Message #" + (_activeMessage+1).ToString() + " of " + _mission.Messages.Count.ToString();
			bool btemp = _loading;
			_loading = true;
			for (int i=0;i<4;i++) labelRefresh(_mission.Messages[_activeMessage].Triggers[i], lblMessTrig[i]);
			txtMessage.Text = _mission.Messages[_activeMessage].MessageString;
			cboMessColor.SelectedIndex = _mission.Messages[_activeMessage].Color;
			optMess1OR2.Checked = _mission.Messages[_activeMessage].T1AndOrT2;
			optMess1AND2.Checked = !optMess1OR2.Checked;
			optMess3OR4.Checked = _mission.Messages[_activeMessage].T3AndOrT4;
			optMess3AND4.Checked = !optMess3OR4.Checked;
			optMess12OR34.Checked = _mission.Messages[_activeMessage].T12AndOrT34;
			optMess12AND34.Checked = !optMess12OR34.Checked;
			txtShort.Text = _mission.Messages[_activeMessage].Note;
			numMessDelay.Value = _mission.Messages[_activeMessage].Delay * 5;
			for (int i=0;i<10;i++) chkSendTo[i].Checked = _mission.Messages[_activeMessage].SentToTeam[i];
			lblMessTrigArr_Click(0, new EventArgs());
			_loading = btemp;
		}

		void chkSendToArr_Leave(object sender, EventArgs e)
		{
			CheckBox c = (CheckBox)sender;
			_mission.Messages[_activeMessage].SentToTeam[(int)c.Tag] = Common.Update(this, _mission.Messages[_activeMessage].SentToTeam[(int)c.Tag], c.Checked);
		}
		void lblMessTrigArr_Click(object sender, EventArgs e)
		{
			Label l=null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeMessageTrigger = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeMessageTrigger = Convert.ToByte(sender); l = lblMessTrig[_activeMessageTrigger]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<4;i++) if (i!=_activeMessageTrigger) lblMessTrig[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			cboMessTrig.SelectedIndex = _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Condition;
			cboMessType.SelectedIndex = -1;
			cboMessType.SelectedIndex = _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].VariableType;
			cboMessAmount.SelectedIndex = _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Amount;
			_loading = btemp;
		}
		void lblMessTrigArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("MessTrig", new EventArgs());
		}
		void lblMessTrigArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("MessTrig", new EventArgs());
		}

		void cboMessAmount_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Amount = Common.Update(this, _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Amount, Convert.ToByte(cboMessAmount.SelectedIndex));
			labelRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger], lblMessTrig[_activeMessageTrigger]);
		}
		void cboMessColor_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			_mission.Messages[_activeMessage].Color = Common.Update(this, _mission.Messages[_activeMessage].Color, Convert.ToByte(cboMessColor.SelectedIndex));
			messListRefresh();
		}
		void cboMessTrig_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Condition = Common.Update(this, _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Condition, Convert.ToByte(cboMessTrig.SelectedIndex));
			labelRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger], lblMessTrig[_activeMessageTrigger]);
		}
		void cboMessType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboMessType.SelectedIndex == -1) return;
			if (!_loading)
				_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].VariableType = Common.Update(this, _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].VariableType, Convert.ToByte(cboMessType.SelectedIndex));
			comboVarRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].VariableType, cboMessVar);
			try { cboMessVar.SelectedIndex = _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Variable; }
			catch { cboMessVar.SelectedIndex = 0; _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Variable = 0; }
			if (!_loading) labelRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger], lblMessTrig[_activeMessageTrigger]);
		}
		void cboMessVar_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Variable = Common.Update(this, _mission.Messages[_activeMessage].Triggers[_activeMessageTrigger].Variable, Convert.ToByte(cboMessVar.SelectedIndex));
			labelRefresh(_mission.Messages[_activeMessage].Triggers[_activeMessageTrigger], lblMessTrig[_activeMessageTrigger]);
		}

		void numMessDelay_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].Delay = Common.Update(this, _mission.Messages[_activeMessage].Delay, Convert.ToByte(numMessDelay.Value / 5));
		}

		void optMess1OR2_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.Messages[_activeMessage].T1AndOrT2 = Common.Update(this, _mission.Messages[_activeMessage].T1AndOrT2, optMess1OR2.Checked);
		}
		void optMess12OR34_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.Messages[_activeMessage].T12AndOrT34 = Common.Update(this, _mission.Messages[_activeMessage].T12AndOrT34, optMess12OR34.Checked);
		}
		void optMess3OR4_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
				_mission.Messages[_activeMessage].T3AndOrT4 = Common.Update(this, _mission.Messages[_activeMessage].T3AndOrT4, optMess3OR4.Checked);
		}

		void txtMessage_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].MessageString = Common.Update(this, _mission.Messages[_activeMessage].MessageString, txtMessage.Text);
			messListRefresh();
		}
		void txtShort_Leave(object sender, EventArgs e)
		{
			_mission.Messages[_activeMessage].Note = Common.Update(this, _mission.Messages[_activeMessage].Note, txtShort.Text);
		}
		#endregion
		#region Globals
		void cboGlobalTeam_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboGlobalTeam.SelectedIndex == -1) return;
			_activeTeam = (byte)cboGlobalTeam.SelectedIndex;
			lblTeamArr_Click(_activeTeam, new EventArgs());	// link the Globals and Team tabs to share GlobTeam
			bool btemp = _loading;
			_loading = true;
			for (int i = 0; i < 12; i++) labelRefresh(_mission.Globals[_activeTeam].Goals[i / 4].Triggers[i % 4].GoalTrigger, lblGlobTrig[i]);
			for (int i=0;i<9;i++)
			{
				optGlobAndOr[i].Checked = _mission.Globals[_activeTeam].Goals[i/3].AndOr[i%3];	// OR
				optGlobAndOr[i+9].Checked = !optGlobAndOr[i].Checked;	// AND
			}
			lblGlobTrigArr_Click(0, new EventArgs());
			_loading = btemp;
		}

		void lblGlobTrigArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_activeGlobalTrigger = Convert.ToByte(l.Tag);
			}
			catch (InvalidCastException) { _activeGlobalTrigger = Convert.ToByte(sender); l = lblGlobTrig[_activeGlobalTrigger]; }
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<12;i++) if (i!=_activeGlobalTrigger) lblGlobTrig[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			int g = _activeGlobalTrigger / 4;
			int t = _activeGlobalTrigger % 4;
			cboGlobalTrig.SelectedIndex = _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Condition;
			cboGlobalType.SelectedIndex = -1;
			cboGlobalType.SelectedIndex = _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.VariableType;
			cboGlobalAmount.SelectedIndex = _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Amount;
			numGlobalPoints.Value = _mission.Globals[_activeTeam].Goals[g].Points;
			txtGlobalInc.Text = _mission.Globals[_activeTeam].Goals[g].Triggers[t][Globals.GoalState.Incomplete];
			txtGlobalComp.Text = _mission.Globals[_activeTeam].Goals[g].Triggers[t][Globals.GoalState.Complete];
			txtGlobalFail.Text = _mission.Globals[_activeTeam].Goals[g].Triggers[t][Globals.GoalState.Failed];
			txtGlobalFail.Visible = (g >= (int)Globals.GoalIndex.Prevent ? false : true);
			txtGlobalInc.Visible = (g >= (int)Globals.GoalIndex.Secondary ? false : true);
			labelRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger, lblGlobTrig[_activeGlobalTrigger]);
			_loading = btemp;
		}
		void lblGlobTrigArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("Glob", new EventArgs());
		}
		void lblGlobTrigArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("Glob", new EventArgs());
		}
		void optGlobAndOrArr_CheckedChanged(object sender, EventArgs e)
		{
			if (_loading) return;
			RadioButton r = (RadioButton)sender;
			int g = (int)r.Tag / 3, i = (int)r.Tag % 3;
			_mission.Globals[_activeTeam].Goals[g].AndOr[i] = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].AndOr[i], r.Checked);
		}

		void cboGlobalAmount_Leave(object sender, EventArgs e)
		{
			int g = _activeGlobalTrigger / 4, t = _activeGlobalTrigger % 4;
			_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Amount = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Amount, Convert.ToByte(cboGlobalAmount.SelectedIndex));
			labelRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger, lblGlobTrig[_activeGlobalTrigger]);
		}
		void cboGlobalTrig_Leave(object sender, EventArgs e)
		{
			int g = _activeGlobalTrigger / 4, t = _activeGlobalTrigger % 4;
			_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Condition = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Condition, Convert.ToByte(cboGlobalTrig.SelectedIndex));
			labelRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger, lblGlobTrig[_activeGlobalTrigger]);
		}
		void cboGlobalType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cboGlobalType.SelectedIndex == -1) return;
			int g = _activeGlobalTrigger / 4, t = _activeGlobalTrigger % 4;
			if (!_loading)
				_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.VariableType = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.VariableType, Convert.ToByte(cboGlobalType.SelectedIndex));
			comboVarRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.VariableType, cboGlobalVar);
			try { cboGlobalVar.SelectedIndex = _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Variable; }
			catch { cboGlobalVar.SelectedIndex = 0; _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Variable = 0; }
			if (!_loading) labelRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger, lblGlobTrig[_activeGlobalTrigger]);
		}
		void cboGlobalVar_Leave(object sender, EventArgs e)
		{
			int g = _activeGlobalTrigger / 4, t = _activeGlobalTrigger % 4;
			_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Variable = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger.Variable, Convert.ToByte(cboGlobalVar.SelectedIndex));
			labelRefresh(_mission.Globals[_activeTeam].Goals[g].Triggers[t].GoalTrigger, lblGlobTrig[_activeGlobalTrigger]);
		}

		void numGlobalPoints_Leave(object sender, EventArgs e)
		{
			_mission.Globals[_activeTeam].Goals[_activeGlobalTrigger/4].Points = Common.Update(this, _mission.Globals[_activeTeam].Goals[_activeGlobalTrigger/4].Points, (short)numGlobalPoints.Value);
		}

		void txtGlobal_Leave(object sender, EventArgs e)
		{
			int g = _activeGlobalTrigger / 4, t = _activeGlobalTrigger % 4;
			Globals.GoalState gs = (Globals.GoalState)((TextBox)sender).Tag;
			_mission.Globals[_activeTeam].Goals[g].Triggers[t][gs] = Common.Update(this, _mission.Globals[_activeTeam].Goals[g].Triggers[t][gs], ((TextBox)(sender)).Text);
		}
		#endregion
		#region Teams
		void teamRefresh()
		{
			string team = _mission.Teams[_activeTeam].Name;
			lblTeam[_activeTeam].Text = "Team " + (_activeTeam+1).ToString() + ": " + team;
			cboGlobalTeam.Items[_activeTeam] = team;
			cboTeam.Items[_activeTeam] = team;
			ComboBox[] cboType = new ComboBox[8];
			cboType[0] = cboADTrigType;
			cboType[1] = cboOT1Type;
			cboType[2] = cboOT2Type;
			cboType[3] = cboOT3Type;
			cboType[4] = cboOT4Type;
			cboType[5] = cboSkipType;
			cboType[6] = cboMessType;
			cboType[7] = cboGlobalType;
			ComboBox[] cbo = new ComboBox[8];
			cbo[0] = cboADTrigVar;
			cbo[1] = cboOT1;
			cbo[2] = cboOT2;
			cbo[3] = cboOT3;
			cbo[4] = cboOT4;
			cbo[5] = cboSkipVar;
			cbo[6] = cboMessVar;
			cbo[7] = cboGlobalVar;
			for (int i=0;i<8;i++)
				if (cboType[i].SelectedIndex == 0xC || cboType[i].SelectedIndex == 0x15)
					cbo[i].Items[_activeTeam] = team;
			chkAllies[_activeTeam].Text = team;

			//[JB] Update the Briefing's list of team names.
			for (int i = 0; i < _mission.Teams.Count; i++)
				BriefingForm.sharedTeamNames[i] = _mission.Teams[i].Name;
		}

		void cboEoMColorArr_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox c = (ComboBox)sender;
			if (!_loading)
				_mission.Teams[_activeTeam].EndOfMissionMessageColor[(int)c.Tag] = Common.Update(this, _mission.Teams[_activeTeam].EndOfMissionMessageColor[(int)c.Tag], Convert.ToByte(c.SelectedIndex));
			Color clr = Color.Crimson;
			if (c.SelectedIndex == 1) clr = Color.LimeGreen;
			else if (c.SelectedIndex == 2) clr = Color.RoyalBlue;
			else if (c.SelectedIndex == 3) clr = Color.Yellow;
			txtEoM[(int)c.Tag].ForeColor = clr;
		}
		void chkAlliesArr_Leave(object sender, EventArgs e)
		{
			CheckBox c = (CheckBox)sender;
			_mission.Teams[_activeTeam].AlliedWithTeam[(int)c.Tag] = Common.Update(this, _mission.Teams[_activeTeam].AlliedWithTeam[(int)c.Tag], c.Checked);
		}

		void lblTeamArr_Click(object sender, EventArgs e)
		{
			Label l = null;
			try
			{
				l = (Label)sender;
				l.Focus();
				_mission.Teams[_activeTeam].Name = txtTeamName.Text;
				for (int i=0;i<6;i++)
				{
					_mission.Teams[_activeTeam].EndOfMissionMessages[i] = txtEoM[i].Text;
					_mission.Teams[_activeTeam].EndOfMissionMessageColor[i] = (byte)cboEoMColor[i].SelectedIndex;
				}
				for (int i=0;i<10;i++) _mission.Teams[_activeTeam].AlliedWithTeam[i] = chkAllies[i].Checked;
				teamRefresh();
				_activeTeam = Convert.ToByte(l.Tag);
			}	// fired by click
			catch (InvalidCastException) { _activeTeam = Convert.ToByte(sender); l = lblTeam[_activeTeam]; }	// fired by code
			if (l == null) return;
			cboGlobalTeam.SelectedIndex = _activeTeam;
			l.ForeColor = SystemColors.Highlight;
			for (int i=0;i<10;i++) if (i!=_activeTeam) lblTeam[i].ForeColor = SystemColors.ControlText;
			bool btemp = _loading;
			_loading = true;
			txtTeamName.Text = _mission.Teams[_activeTeam].Name;
			for (int i=0;i<10;i++) chkAllies[i].Checked = _mission.Teams[_activeTeam].AlliedWithTeam[i];
			for (int i=0;i<6;i++)
			{
				txtEoM[i].Text = _mission.Teams[_activeTeam].EndOfMissionMessages[i];
				cboEoMColor[i].SelectedIndex = _mission.Teams[_activeTeam].EndOfMissionMessageColor[i];
			}
			_loading = btemp;
		}
		void lblTeamArr_DoubleClick(object sender, EventArgs e)
		{
			menuPaste_Click("Team", new EventArgs());
		}
		void lblTeamArr_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) menuCopy_Click("Team", new EventArgs());
		}

		void txtEoMArr_Leave(object sender, EventArgs e)
		{
			TextBox t = (TextBox)sender;
			_mission.Teams[_activeTeam].EndOfMissionMessages[(int)t.Tag] = Common.Update(this, _mission.Teams[_activeTeam].EndOfMissionMessages[(int)t.Tag], t.Text);
		}

		void txtTeamName_Leave(object sender, EventArgs e)
		{
			_mission.Teams[_activeTeam].Name = Common.Update(this, _mission.Teams[_activeTeam].Name, txtTeamName.Text);
			teamRefresh();
		}
		#endregion
		#region Mission
		void cboMissType_Leave(object sender, EventArgs e)
		{
			_mission.MissionType = Common.Update(this, _mission.MissionType, (Mission.MissionTypeEnum)Convert.ToByte(cboMissType.SelectedIndex));
		}

		void chkMissUnk3_Leave(object sender, EventArgs e)
		{
			_mission.Unknown3 = Common.Update(this, _mission.Unknown3, chkMissUnk3.Checked);
		}
		void chkMissUnk6_Leave(object sender, EventArgs e)
		{
			_mission.Unknown6 = Common.Update(this, _mission.Unknown6, chkMissUnk6.Checked);
		}

		void numMissTimeMin_Leave(object sender, EventArgs e)
		{
			_mission.TimeLimitMin = Common.Update(this, _mission.TimeLimitMin, Convert.ToByte(numMissTimeMin.Value));
		}
		void numMissTimeSec_Leave(object sender, EventArgs e)
		{
			_mission.TimeLimitSec = Common.Update(this, _mission.TimeLimitSec, Convert.ToByte(numMissTimeSec.Value));
		}
		void numMissUnk1_Leave(object sender, EventArgs e)
		{
			_mission.Unknown1 = Common.Update(this, _mission.Unknown1, Convert.ToByte(numMissUnk1.Value));
		}
		void numMissUnk2_Leave(object sender, EventArgs e)
		{
			_mission.Unknown2 = Common.Update(this, _mission.Unknown2, Convert.ToByte(numMissUnk2.Value));
		}

		void optXvT_CheckedChanged(object sender, EventArgs e)
		{
			setBop(!optXvT.Checked);
			Common.Title(this, _loading);
		}

		void txtMissDesc_Leave(object sender, EventArgs e)
		{
			_mission.MissionDescription = Common.Update(this, _mission.MissionDescription, txtMissDesc.Text);
		}
		void txtMissFail_Leave(object sender, EventArgs e)
		{
			_mission.MissionFailed = Common.Update(this, _mission.MissionFailed, txtMissFail.Text);
		}
		void txtMissSucc_Leave(object sender, EventArgs e)
		{
			_mission.MissionSuccessful = Common.Update(this, _mission.MissionSuccessful, txtMissSucc.Text);
		}
		void txtMissUnk4_Leave(object sender, EventArgs e)
		{
			_mission.Unknown4 = Common.Update(this, _mission.Unknown4, txtMissUnk4.Text);
		}
		void txtMissUnk5_Leave(object sender, EventArgs e)
		{
			_mission.Unknown5 = Common.Update(this, _mission.Unknown5, txtMissUnk5.Text);
		}
		#endregion
	}
}