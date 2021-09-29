using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;
using Simego.DataSync.Providers.HighQ.TypeEditors;

namespace Simego.DataSync.Providers.HighQ
{
    class ConnectionProperties : IHighQOAuthConfiguration
    {
        private readonly HighQDataSourceReader _reader;
        
        [Category("Settings")]
        public string ServiceUrl 
        { 
            get => _reader.ServiceUrl;
            set => _reader.ServiceUrl = value;
        }
        
        [Category("Settings")]
        public int ApiVersion 
        { 
            get => _reader.ApiVersion;
            set => _reader.ApiVersion = value;
        }
        
        [Category("Connection")]
        [ReadOnly(true)]
        public int SiteID 
        { 
            get => _reader.SiteID;
            set => _reader.SiteID = value;
        }

        [Category("Connection")]
        [Editor(typeof(HighQSiteListTypeEditor), typeof(UITypeEditor))]
        public string Site
        {
            get => _reader.Site;
            set => _reader.Site = value;
        }

        [Category("Connection")]
        [ReadOnly(true)]
        public int SheetID
        {
            get => _reader.SheetID;
            set => _reader.SheetID = value;
        }

        [Category("Connection")]
        [Editor(typeof(HighQSheetListTypeEditor), typeof(UITypeEditor))]
        public string Sheet
        {
            get => _reader.Sheet;
            set => _reader.Sheet = value;
        }

        [Category("Authentication")]
        public string ClientID
        {
            get => _reader.ClientID;
            set => _reader.ClientID = value;
        }

        [Category("Authentication")]
        [PasswordPropertyText(true)]
        public string ClientSecret
        {
            get => _reader.ClientSecret;
            set => _reader.ClientSecret = value;
        }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public string AccessToken
        {
            get => _reader.AccessToken;
            set => _reader.AccessToken = value;
        }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public string RefreshToken
        {
            get => _reader.RefreshToken;
            set => _reader.RefreshToken = value;
        }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public DateTime TokenExpires
        {
            get => _reader.TokenExpires;
            set => _reader.TokenExpires = value;
        }

        [Category("Authentication")]
        [Editor(typeof(HighQOAuthCredentialsWebTypeEditor), typeof(UITypeEditor))]
        public string Authorize
        {
            get => _reader.Authorize;
            set => _reader.Authorize = value;
        }

        public ConnectionProperties(HighQDataSourceReader reader)
        {
            _reader = reader;
        }

        public IEnumerable<HighQSite> GetSites() => _reader.GetSites();
        public IEnumerable<HighQSheet> GetSheets(int siteId) => _reader.GetSheets(siteId);

        HighQOAuthConfiguration IHighQOAuthConfiguration.GetOauthConfiguration() => ((IHighQOAuthConfiguration)_reader).GetOauthConfiguration();
        void IHighQOAuthConfiguration.UpdateOauthConfiguration(HighQOAuthConfiguration configuration) => ((IHighQOAuthConfiguration)_reader).UpdateOauthConfiguration(configuration);
    }
}
