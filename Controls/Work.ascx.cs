﻿//
// ADefwebserver.com
// Copyright (c) 2009
// by Michael Washington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using Microsoft.VisualBasic;

namespace ITIL.Modules.ServiceDesk.Controls
{
    public partial class Work : ITILServiceDeskModuleBase
    {

        #region Properties
        public int TaskID
        {
            get { return Convert.ToInt32(ViewState["TaskID"]); }
            set { ViewState["TaskID"] = value; }
        }

        public int ModuleID
        {
            get { return Convert.ToInt32(ViewState["ModuleID"]); }
            set { ViewState["ModuleID"] = value; }
        }

        public bool ViewOnly
        {
            get { return Convert.ToBoolean(ViewState["ViewOnly"]); }
            set { ViewState["ViewOnly"] = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                cmdtxtStartCalendar1.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStartDay);
                cmdtxtStartCalendar2.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStopDay);
                cmdtxtStartCalendar3.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStartDayEdit);
                cmdtxtStartCalendar4.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStopDayEdit);

                pnlInsertWork.GroupingText = Localization.GetString("pnlInsertWork.Text", LocalResourceFile);

                if (!Page.IsPostBack)
                {
                    // Insert Default dates and times
                    txtStartDay.Text = DateTime.Now.ToShortDateString();
                    txtStopDay.Text = DateTime.Now.ToShortDateString();
                    txtStartTime.Text = DateTime.Now.AddHours(-1).ToShortTimeString();
                    txtStopTime.Text = DateTime.Now.ToShortTimeString();

                    SetView("Default");

                    if (ViewOnly)
                    {
                        SetViewOnlyMode();
                    }
                }
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        #region SetView
        public void SetView(string ViewMode)
        {
            if (ViewMode == "Default")
            {
                pnlInsertWork.Visible = true;
                //pnlTableHeader.Visible = true;
                pnlExistingComments.Visible = true;
                pnlEditWork.Visible = false;
            }

            if (ViewMode == "Edit")
            {
                pnlInsertWork.Visible = false;
                //pnlTableHeader.Visible = false;
                pnlExistingComments.Visible = false;
                pnlEditWork.Visible = true;
            }
        }
        #endregion

        #region SetViewOnlyMode
        private void SetViewOnlyMode()
        {
            lnkDelete.Visible = false;
            lnkUpdate.Visible = false;
        }
        #endregion

        // Insert Comment

        #region btnInsertComment_Click
        protected void btnInsertComment_Click(object sender, EventArgs e)
        {
            InsertComment();
        }
        #endregion

        #region btnInsertCommentAndEmail_Click
        protected void btnInsertCommentAndEmail_Click(object sender, EventArgs e)
        {
            string strComment = txtComment.Text;
            InsertComment();
        }
        #endregion

        #region InsertComment
        private void InsertComment()
        {
            if (txtComment.Text.Trim().Length > 0)
            {
                try
                {
                    // Try to Make Start and Stop Time
                    DateTime StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDay.Text, txtStartTime.Text));
                    DateTime StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDay.Text, txtStopTime.Text));
                }
                catch
                {
                    lblError.Text = Localization.GetString("MustProvideValidStarAndStopTimes.Text", LocalResourceFile);
                    return;
                }

                ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

                string strComment = txtComment.Text.Trim();

                // Save Task Details
                ITILServiceDesk_TaskDetail objITILServiceDesk_TaskDetail = new ITILServiceDesk_TaskDetail();

                objITILServiceDesk_TaskDetail.TaskID = TaskID;
                objITILServiceDesk_TaskDetail.Description = txtComment.Text.Trim();
                objITILServiceDesk_TaskDetail.InsertDate = DateTime.Now;
                objITILServiceDesk_TaskDetail.UserID = UserId;
                objITILServiceDesk_TaskDetail.DetailType = "Work";
                objITILServiceDesk_TaskDetail.StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDay.Text, txtStartTime.Text));
                objITILServiceDesk_TaskDetail.StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDay.Text, txtStopTime.Text));

                objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails.InsertOnSubmit(objITILServiceDesk_TaskDetail);
                objServiceDeskDALDataContext.SubmitChanges();
                txtComment.Text = "";

                // Insert Log
                Log.InsertLog(TaskID, UserId, String.Format("{0} inserted Work comment.", GetUserName()));

                gvComments.DataBind();
            }
            else
            {
                lblError.Text = Localization.GetString("MustProvideADescription.Text", LocalResourceFile);
            }
        }
        #endregion

        #region LDSComments_Selecting
        protected void LDSComments_Selecting(object sender, LinqDataSourceSelectEventArgs e)
        {
            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();
            var result = from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails
                         where ITILServiceDesk_TaskDetails.TaskID == TaskID
                         where (ITILServiceDesk_TaskDetails.DetailType == "Work")
                         select ITILServiceDesk_TaskDetails;

            e.Result = result;
        }
        #endregion

        #region gvComments_RowDataBound
        protected void gvComments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                GridViewRow objGridViewRow = (GridViewRow)e.Row;

                // Comment
                Label lblComment = (Label)objGridViewRow.FindControl("lblComment");
                if (lblComment.Text.Trim().Length > 100)
                {
                    string mylblComment = lblComment.Text;
                    lblComment.Text = String.Format("{0}...", mylblComment.Substring(0,100));
                }

                // User
                Label gvlblUser = (Label)objGridViewRow.FindControl("gvlblUser");
                if (gvlblUser.Text != "-1")
                {
                    UserInfo objUser = UserController.GetUser(PortalId, Convert.ToInt32(gvlblUser.Text), false);

                    if (objUser != null)
                    {
                        string strDisplayName = objUser.DisplayName;

                        if (strDisplayName.Length > 25)
                        {
                            gvlblUser.Text = String.Format("{0}...", strDisplayName.Substring(0, 25));
                        }
                        else
                        {
                            gvlblUser.Text = strDisplayName;
                        }
                    }
                    else
                    {
                        gvlblUser.Text = "[User Deleted]";
                    }
                }
                else
                {
                    gvlblUser.Text = Localization.GetString("Requestor.Text", LocalResourceFile);
                }


                // Time
                Label lblTimeSpan = (Label)objGridViewRow.FindControl("lblTimeSpan");
                try
                {

                    Label lblStartTime = (Label)objGridViewRow.FindControl("lblStartTime");
                    Label lblStopTime = (Label)objGridViewRow.FindControl("lblStopTime");

                    DateTime StartDate = Convert.ToDateTime(lblStartTime.Text);
                    DateTime StopDate = Convert.ToDateTime(lblStopTime.Text);
                    TimeSpan TimeDifference = StopDate.Subtract(StartDate);

                    // if no Days
                    if (TimeDifference.Days == 0)
                    {
                        if (TimeDifference.Hours == 0)
                        {
                            lblTimeSpan.Text = String.Format(Localization.GetString("Minute.Text", LocalResourceFile), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                        }
                        else
                        {
                            lblTimeSpan.Text = String.Format(Localization.GetString("HoursandMinute.Text", LocalResourceFile), TimeDifference.Hours.ToString(), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                        }
                    }
                    else
                    {
                        lblTimeSpan.Text = String.Format(Localization.GetString("DaysHoursMinutes.Text", LocalResourceFile), TimeDifference.Days.ToString(), ((TimeDifference.Days > 1) ? "s" : ""), TimeDifference.Hours.ToString(), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                    }
                }
                catch (Exception ex)
                {
                    lblTimeSpan.Text = ex.Message;
                    Exceptions.ProcessModuleLoadException(this, ex);
                }
            }
        }
        #endregion

        #region GetRandomPassword
        public string GetRandomPassword()
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            int intElements = random.Next(10, 26);

            for (int i = 0; i < intElements; i++)
            {
                int intRandomType = random.Next(0, 2);
                if (intRandomType == 1)
                {
                    char ch;
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                else
                {
                    builder.Append(random.Next(0, 9));
                }
            }
            return builder.ToString();
        }
        #endregion

        #region GetUserName
        private string GetUserName()
        {
            string strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);

            if (UserId > -1)
            {
                strUserName = UserInfo.DisplayName;
            }

            return strUserName;
        }

        private string GetUserName(int intUserID)
        {
            string strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);

            if (intUserID > -1)
            {
                UserInfo objUser = UserController.GetUser(PortalId, intUserID, false);

                if (objUser != null)
                {
                    strUserName = objUser.DisplayName;
                }
                else
                {
                    strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);
                }
            }

            return strUserName;
        }
        #endregion

        // GridView

        #region gvComments_RowCommand
        protected void gvComments_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Select")
            {
                SetView("Edit");
                lblDetailID.Text = Convert.ToString(e.CommandArgument);
                DisplayComment();
            }
        }
        #endregion

        // Comment Edit

        #region lnkBack_Click
        protected void lnkBack_Click(object sender, EventArgs e)
        {
            SetView("Default");
        }
        #endregion

        #region DisplayComment
        private void DisplayComment()
        {
            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

            var objITILServiceDesk_TaskDetail = (from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails
                                              where ITILServiceDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                              select ITILServiceDesk_TaskDetails).FirstOrDefault();

            if (objITILServiceDesk_TaskDetail != null)
            {
                txtDescription.Text = objITILServiceDesk_TaskDetail.Description;
                lblDisplayUser.Text = GetUserName(objITILServiceDesk_TaskDetail.UserID);
                txtStartDayEdit.Text = objITILServiceDesk_TaskDetail.StartTime.Value.ToShortDateString();
                txtStopDayEdit.Text = objITILServiceDesk_TaskDetail.StopTime.Value.ToShortDateString();
                txtStartTimeEdit.Text = objITILServiceDesk_TaskDetail.StartTime.Value.ToShortTimeString();
                txtStopTimeEdit.Text = objITILServiceDesk_TaskDetail.StopTime.Value.ToShortTimeString();
                lblInsertDate.Text = String.Format("{0} {1}", objITILServiceDesk_TaskDetail.InsertDate.ToLongDateString(), objITILServiceDesk_TaskDetail.InsertDate.ToLongTimeString());
            }
        }
        #endregion

        #region lnkDelete_Click
        protected void lnkDelete_Click(object sender, EventArgs e)
        {
            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

            var objITILServiceDesk_TaskDetail = (from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails
                                              where ITILServiceDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                              select ITILServiceDesk_TaskDetails).FirstOrDefault();

            // Delete the Record
            objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails.DeleteOnSubmit(objITILServiceDesk_TaskDetail);
            objServiceDeskDALDataContext.SubmitChanges();

            // Insert Log
            Log.InsertLog(TaskID, UserId, String.Format("{0} deleted Work comment: {1}", GetUserName(), txtDescription.Text));

            SetView("Default");
            gvComments.DataBind();
        }
        #endregion

        #region lnkUpdate_Click
        protected void lnkUpdate_Click(object sender, EventArgs e)
        {
            UpdateComment();
        }
        #endregion

        #region UpdateComment
        private void UpdateComment()
        {
            try
            {
                // Try to Make Start and Stop Time
                DateTime StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDayEdit.Text, txtStartTimeEdit.Text));
                DateTime StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDayEdit.Text, txtStopTimeEdit.Text));
            }
            catch
            {
                lblErrorEditComment.Text = Localization.GetString("MustProvideValidStarAndStopTimes.Text", LocalResourceFile);
                return;
            }

            if (txtDescription.Text.Trim().Length > 0)
            {
                ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

                string strComment = txtDescription.Text.Trim();

                // Save Task Details
                var objITILServiceDesk_TaskDetail = (from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_TaskDetails
                                                  where ITILServiceDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                                  select ITILServiceDesk_TaskDetails).FirstOrDefault();

                if (objITILServiceDesk_TaskDetail != null)
                {

                    objITILServiceDesk_TaskDetail.TaskID = TaskID;
                    objITILServiceDesk_TaskDetail.Description = txtDescription.Text.Trim();
                    objITILServiceDesk_TaskDetail.UserID = UserId;
                    objITILServiceDesk_TaskDetail.DetailType = "Work";
                    objITILServiceDesk_TaskDetail.StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDayEdit.Text, txtStartTimeEdit.Text));
                    objITILServiceDesk_TaskDetail.StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDayEdit.Text, txtStopTimeEdit.Text));

                    objServiceDeskDALDataContext.SubmitChanges();
                    txtDescription.Text = "";

                    // Insert Log
                    Log.InsertLog(TaskID, UserId, String.Format("{0} updated Work comment.", GetUserName()));

                    SetView("Default");
                    gvComments.DataBind();
                }
            }
            else
            {
                lblErrorEditComment.Text = Localization.GetString("MustProvideADescription.Text", LocalResourceFile);
            }
        }
        #endregion

        // Utility

        #region GetAssignedRole
        private int GetAssignedRole()
        {
            int intRole = -1;

            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();
            var result = from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_Tasks
                         where ITILServiceDesk_TaskDetails.TaskID == Convert.ToInt32(Request.QueryString["TaskID"])
                         select ITILServiceDesk_TaskDetails;

            if (result != null)
            {
                intRole = result.FirstOrDefault().AssignedRoleID;
            }

            return intRole;
        }
        #endregion

        #region GetDescriptionOfTicket
        private string GetDescriptionOfTicket()
        {
            string strDescription = "";
            int intTaskId = Convert.ToInt32(Request.QueryString["TaskID"]);

            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();
            var result = (from ITILServiceDesk_TaskDetails in objServiceDeskDALDataContext.ITILServiceDesk_Tasks
                          where ITILServiceDesk_TaskDetails.TaskID == Convert.ToInt32(Request.QueryString["TaskID"])
                          select ITILServiceDesk_TaskDetails).FirstOrDefault();

            if (result != null)
            {
                strDescription = result.Description;
            }

            return strDescription;
        }
        #endregion

        #region GetSettings
        private List<ITILServiceDesk_Setting> GetSettings()
        {
            // Get Settings
            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

            List<ITILServiceDesk_Setting> colITILServiceDesk_Setting = (from ITILServiceDesk_Settings in objServiceDeskDALDataContext.ITILServiceDesk_Settings
                                                                  where ITILServiceDesk_Settings.PortalID == PortalId
                                                                  select ITILServiceDesk_Settings).ToList();

            if (colITILServiceDesk_Setting.Count == 0)
            {
                // Create Default vaules
                ITILServiceDesk_Setting objITILServiceDesk_Setting1 = new ITILServiceDesk_Setting();

                objITILServiceDesk_Setting1.PortalID = PortalId;
                objITILServiceDesk_Setting1.SettingName = "AdminRole";
                objITILServiceDesk_Setting1.SettingValue = "Administrators";

                objServiceDeskDALDataContext.ITILServiceDesk_Settings.InsertOnSubmit(objITILServiceDesk_Setting1);
                objServiceDeskDALDataContext.SubmitChanges();

                ITILServiceDesk_Setting objITILServiceDesk_Setting2 = new ITILServiceDesk_Setting();

                objITILServiceDesk_Setting2.PortalID = PortalId;
                objITILServiceDesk_Setting2.SettingName = "UploadefFilesPath";
                objITILServiceDesk_Setting2.SettingValue = Server.MapPath("~/DesktopModules/ITILServiceDesk/Upload");

                objServiceDeskDALDataContext.ITILServiceDesk_Settings.InsertOnSubmit(objITILServiceDesk_Setting2);
                objServiceDeskDALDataContext.SubmitChanges();

                colITILServiceDesk_Setting = (from ITILServiceDesk_Settings in objServiceDeskDALDataContext.ITILServiceDesk_Settings
                                           where ITILServiceDesk_Settings.PortalID == PortalId
                                           select ITILServiceDesk_Settings).ToList();
            }

            // Upload Permission
            ITILServiceDesk_Setting UploadPermissionITILServiceDesk_Setting = (from ITILServiceDesk_Settings in objServiceDeskDALDataContext.ITILServiceDesk_Settings
                                                                         where ITILServiceDesk_Settings.PortalID == PortalId
                                                                         where ITILServiceDesk_Settings.SettingName == "UploadPermission"
                                                                         select ITILServiceDesk_Settings).FirstOrDefault();

            if (UploadPermissionITILServiceDesk_Setting != null)
            {
                // Add to collection
                colITILServiceDesk_Setting.Add(UploadPermissionITILServiceDesk_Setting);
            }
            else
            {
                // Add Default value
                ITILServiceDesk_Setting objITILServiceDesk_Setting = new ITILServiceDesk_Setting();
                objITILServiceDesk_Setting.SettingName = "UploadPermission";
                objITILServiceDesk_Setting.SettingValue = "All";
                objITILServiceDesk_Setting.PortalID = PortalId;
                objServiceDeskDALDataContext.ITILServiceDesk_Settings.InsertOnSubmit(objITILServiceDesk_Setting);
                objServiceDeskDALDataContext.SubmitChanges();

                // Add to collection
                colITILServiceDesk_Setting.Add(objITILServiceDesk_Setting);
            }

            return colITILServiceDesk_Setting;
        }
        #endregion

        #region GetAdminRole
        private string GetAdminRole()
        {
            ServiceDeskDALDataContext objServiceDeskDALDataContext = new ServiceDeskDALDataContext();

            List<ITILServiceDesk_Setting> colITILServiceDesk_Setting = (from ITILServiceDesk_Settings in objServiceDeskDALDataContext.ITILServiceDesk_Settings
                                                                  where ITILServiceDesk_Settings.PortalID == PortalId
                                                                  select ITILServiceDesk_Settings).ToList();

            ITILServiceDesk_Setting objITILServiceDesk_Setting = colITILServiceDesk_Setting.Where(x => x.SettingName == "AdminRole").FirstOrDefault();

            string strAdminRoleID = "Administrators";
            if (objITILServiceDesk_Setting != null)
            {
                strAdminRoleID = objITILServiceDesk_Setting.SettingValue;
            }

            return strAdminRoleID;
        }
        #endregion
    }
}