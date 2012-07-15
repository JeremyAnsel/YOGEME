﻿/*
 * YOGEME.exe, All-in-one Mission Editor for the X-wing series, TIE through XWA
 * Copyright (C) 2007-2012 Michael Gaisser (mjgaisser@gmail.com)
 * Licensed under the GPL v3.0 or later
 * 
 * VERSION: 1.1
 */

/* CHANGELOG
 * v1.1, 120715
 * - Created, currently disabled
 */

using System;
using System.Windows.Forms;

namespace Idmr.Yogeme
{
	public partial class dlgTest : Form
	{
		Settings _config;

		public dlgTest(Settings settings)
		{
			_config = settings;
			InitializeComponent();
			chkVerify.Checked = _config.VerifyTest;
			chkDelete.Checked = _config.DeleteTestPilots;
			// can't check DoNotShow, otherwise this wouldn't have been launched in the first place :P
			if (!_config.Verify) chkVerify.Enabled = true;
		}

		void cmdCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		void cmdTest_Click(object sender, EventArgs e)
		{
			_config.VerifyTest = chkVerify.Checked;
			_config.DeleteTestPilots = chkDelete.Checked;
			_config.ConfirmTest = !chkDoNotShow.Checked;
			// TODO: Test Dialog
			Close();
		}
	}
}
