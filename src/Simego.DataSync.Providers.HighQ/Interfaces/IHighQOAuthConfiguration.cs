using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Interfaces
{
    internal interface IHighQOAuthConfiguration
    {
        HighQOAuthConfiguration GetOauthConfiguration();
        void UpdateOauthConfiguration(HighQOAuthConfiguration configuration);
    }
}
