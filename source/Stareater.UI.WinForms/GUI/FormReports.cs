﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Stareater.Controllers.Views;
using Stareater.AppData;
using Stareater.GUI.Reports;

namespace Stareater.GUI
{
	public partial class FormReports : Form
	{
		private IEnumerable<IReportInfo> reports;
		
		public IReportInfo Result { get; private set; }
		
		public FormReports()
		{
			InitializeComponent();
		}
		
		public FormReports(IEnumerable<IReportInfo> reports) : this()
		{
			this.reports = reports.ToList();
			
			this.Text = SettingsWinforms.Get.Language["FormReports"]["FormTitle"].Text();
			this.fillList();
		}
		
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape) 
				this.Close();
			return base.ProcessCmdKey(ref msg, keyData);
		}
		
		private void fillList()
		{
			reportList.Controls.Clear();
			
			var filter = new FilterRepotVisitor();
			foreach (var report in reports) {
				report.Accept(filter);
				if (!filter.ShowItem)
					continue;
				
				var reportItem = new ReportItem();
				reportItem.Data = report;
				
				reportList.Controls.Add(reportItem);
			}
		}
		
		private void openButton_Click(object sender, EventArgs e)
		{
			var reportItem = reportList.SelectedItem as ReportItem;
			
			if (reportItem == null)
				return;
			
			this.Result = reportItem.Data;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
		
		private void filterButton_Click(object sender, EventArgs e)
		{
			using(var form = new FormReportFilter())
				form.ShowDialog();
			
			this.fillList();
		}
	}
}
