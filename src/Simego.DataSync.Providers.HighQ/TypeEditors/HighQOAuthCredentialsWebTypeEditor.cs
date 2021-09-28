using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Net;

namespace Simego.DataSync.Providers.HighQ.TypeEditors
{
    class HighQOAuthCredentialsWebTypeEditor : UITypeEditor, IDisposable
    {        
        private static readonly Random Random = new Random(Environment.TickCount);
        private readonly WebServer Server;
        private readonly int Port;

        private string _CallbackUrl => $"http://localhost:{Port}/";
        private OAuthWebConnection _ConnectionDialog;
        private HighQOAuthConfiguration _Configuration;

        public HighQOAuthCredentialsWebTypeEditor()
        {
            Port = Random.Next(40000, 60000);

            Server = new WebServer(Port)
            {
                PageRequestCallback = ProcessRequest
            };
        }

        private WebMessage ProcessRequest(HttpListenerContext context)
        {
            var code = context.Request.QueryString["code"];
            if (!string.IsNullOrEmpty(code))
            {
                ValidateToken(code);
                if (!string.IsNullOrEmpty(_Configuration.AccessToken) && !string.IsNullOrEmpty(_Configuration.RefreshToken))
                {
                    //Close the Connection Dialog on the UI thread!
                    _ConnectionDialog.Invoke(new Action(_ConnectionDialog.Close));
                    context.Response.Redirect(_Configuration.ServiceUrl);
                    return null;
                }                           
            }
            
            return new WebMessage { StatusCode = 200, Message = "NOT_OK", ContentType = "text/html" };
        }

        protected void ValidateToken(string code)
        {
            var helper = new HttpWebRequestHelper();
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _Configuration.ClientID,
                ["client_secret"] = _Configuration.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _CallbackUrl
            };

            var tokenResult = helper.PostRequestAsString(parameters, HttpWebRequestHelper.MimeTypeApplicationWwwFormUrlEncoded, Utility.CombineWebPath(_Configuration.ServiceUrl, "/api/oauth2/token"));

            var result = HttpWebRequestHelper.FromJson(tokenResult);

            _Configuration.AccessToken = result["access_token"]?.ToObject<string>();
            _Configuration.RefreshToken = result["refresh_token"]?.ToObject<string>();
            _Configuration.TokenExpires = DateTime.UtcNow.AddSeconds(result["expires_in"]?.ToObject<int>() ?? 0);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _Configuration = ((IHighQOAuthConfiguration)context.Instance).GetOauthConfiguration();

            using (_ConnectionDialog = new OAuthWebConnection())
            {               
                var authoriseUrl = Utility.CombineWebPath(_Configuration.ServiceUrl, $"authorize.action?client_id={Uri.EscapeDataString(_Configuration.ClientID)}&response_type=code&redirect_uri={Uri.EscapeDataString(_CallbackUrl)}");

                //Open External Browser
                Process.Start(authoriseUrl);

                //Wait for the OAuth dance to be over....
                _ConnectionDialog.ShowDialog();

                //Update the Oauth Authentication Settings
                ((IHighQOAuthConfiguration)context.Instance).UpdateOauthConfiguration(_Configuration);
            }

            return !string.IsNullOrEmpty(_Configuration.AccessToken) && !string.IsNullOrEmpty(_Configuration.RefreshToken) ? "Connected" : string.Empty;
        }

        private bool disposed = false;

        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);            
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    Server?.Stop();                    
                }

                disposed = true;
            }
        }
    }
}
