using System;

namespace Simego.DataSync.Providers.HighQ.Models
{
    class HighQOAuthConfiguration
    {
        public string ServiceUrl { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpires { get; set; }
    }
}
