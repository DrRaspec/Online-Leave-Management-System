using System;
using System.IO;
using System.Web;
using OnlineLeaveManagementSystem.Data;
using OnlineLeaveManagementSystem.Security;

namespace OnlineLeaveManagementSystem
{
    public partial class SecureAttachmentDownload : AuthenticatedPage
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string file = Request.QueryString["file"];
            if (string.IsNullOrWhiteSpace(file))
            {
                Response.StatusCode = 404;
                Response.End();
                return;
            }

            string safeFile = Path.GetFileName(file);
            string fullPath = Server.MapPath("~/App_Data/LeaveAttachments/" + safeFile);
            if (!File.Exists(fullPath))
            {
                Response.StatusCode = 404;
                Response.End();
                return;
            }

            if (!LeaveManagementRepository.CanAccessAttachment(CurrentUser, safeFile))
            {
                Response.StatusCode = 403;
                Response.End();
                return;
            }

            Response.Clear();
            Response.ContentType = MimeMapping.GetMimeMapping(safeFile);
            Response.AddHeader("X-Content-Type-Options", "nosniff");
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + safeFile.Replace("\"", string.Empty) + "\"");
            Response.TransmitFile(fullPath);
            Response.End();
        }
    }
}
