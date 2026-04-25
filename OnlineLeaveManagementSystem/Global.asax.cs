using System;
using System.Web;
using OnlineLeaveManagementSystem.Infrastructure;

namespace OnlineLeaveManagementSystem
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            DatabaseInitializer.EnsureDatabase();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Context == null || Response == null)
            {
                return;
            }

            Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // ASP.NET Web Forms emits inline client script for some server controls, so keep
            // script-src compatible while still locking execution down to our own pages.
            Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; font-src 'self'; script-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'self'; frame-ancestors 'self'; form-action 'self';";

            if (Request.IsSecureConnection)
            {
                Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }
        }
    }
}
