using Newtonsoft.Json.Linq;
using Simego.DataSync.Interfaces;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;
using Simego.DataSync.Providers.HighQ.TypeEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;

namespace Simego.DataSync.Providers.HighQ
{
    [ProviderInfo(Name = "HighQ iSheet", Description = "HighQ iSheet Datasource")]
    public class HighQDataSourceReader : DataReaderProviderBase, IDataSourceSetup, IDataSourceRegistry, IHighQOAuthConfiguration
    {
        private ConnectionInterface _connectionIf;
        private IDataSourceRegistryProvider _registryProvider;
        private readonly HttpWebRequestHelper _requestHelper = new HttpWebRequestHelper();

        [Category("Settings")]
        public string ServiceUrl { get; set; }

        [Category("Settings")]
        public int ApiVersion { get; set; } = 5;

        [Category("Settings")]
        public bool EnableTrace { get; set; }

        [Category("Connection")]
        [ReadOnly(true)]
        public int SiteID { get; set; }

        [Category("Connection")]
        [Editor(typeof(HighQSiteListTypeEditor), typeof(UITypeEditor))]
        public string Site { get; set; }

        [Category("Connection")]
        [ReadOnly(true)]
        public int SheetID { get; set; }

        [Category("Connection")]
        [Editor(typeof(HighQSheetListTypeEditor), typeof(UITypeEditor))]
        public string Sheet { get; set; }

        [Category("Authentication")]
        public string ClientID { get; set; }

        [Category("Authentication")]
        [PasswordPropertyText(true)]
        public string ClientSecret { get; set; }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public string AccessToken { get; set; }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public string RefreshToken { get; set; }

        [Category("Authentication")]
        [ReadOnly(true)]
        [Browsable(false)]
        public DateTime TokenExpires { get; set; } = new DateTime(1970, 1, 1);

        [Category("Authentication")]
        [Editor(typeof(HighQOAuthCredentialsWebTypeEditor), typeof(UITypeEditor))]
        public string Authorize { get; set; }
        
        public override DataTableStore GetDataTable(DataTableStore dt)
        {
            ApplyOAuthAccessToken();

            // store itemid against each row
            dt.AddIdentifierColumn(typeof(int));

            var mapping = new DataSchemaMapping(SchemaMap, Side);
            var columns = SchemaMap.GetIncludedColumns();
            
            
            // create a index on columns to map ids to names
            var columnIndex = GetSheetColumns().ToDictionary(k => k.Name, StringComparer.OrdinalIgnoreCase);

            var limit = 100;
            var offset = 0;
            var total = 0;
            var continue_load = true;

            do
            {
                var dataRequest = _requestHelper.GetRequestAsJson(_requestHelper.CombinePaths(ServiceUrl, $"api/{ApiVersion}/isheet/{SheetID}/items?limit={limit}&offset={offset}"));

                // if only 1 result then isheet.data.item is not an array.
                var data = dataRequest["isheet"]?["data"]?["item"];
                
                if (data != null)
                {
                    total = dataRequest["isheet"]?["totalrecordcount"]?.ToObject<int>() ?? 0;
                
                    if (data is JArray)
                    {
                        foreach (var item_row in data)
                        {
                            offset++;

                            if (AddSheetRow(dt, mapping, columns, columnIndex, item_row) == DataTableStore.ABORT)
                            {
                                continue_load = false;
                                break;
                            }
                        }
                    }
                    else if (data is JToken)
                    {
                        offset++;
                        
                        AddSheetRow(dt, mapping, columns, columnIndex, data);
                    }                    
                }
            
            } while (continue_load && offset < total);

            return dt;
        }

        private static int AddSheetRow(DataTableStore dt, DataSchemaMapping mapping, IList<DataSchemaItem> columns, Dictionary<string, HighQDataSchemaItem> columnIndex, JToken data)
        {
            return dt.Rows.AddWithIdentifier(mapping, columns,
                        (item, columnName) =>
                        {
                            if (columnIndex.ContainsKey(columnName))
                            {
                                foreach (var column in data["column"])
                                {
                                    var columnInfo = columnIndex[columnName];

                                    if (column["attributecolumnid"].ToObject<int>() == columnInfo.Id)
                                    {
                                        return columnInfo.HighQValueParser.ParseValue(column);
                                    }
                                }
                            }

                            return data[columnName]?.ToObject<object>();
                        }
                        , data["itemid"]?.ToObject<int>());
        }

        public override DataSchema GetDefaultDataSchema()
        {
            //Return the SheetData source default Schema.
            var schema = new DataSchema();
            schema.Map.AddRange(GetSheetColumns().Select(p => p.ToDataSchemaItem()));
            return schema;
        }

        internal IEnumerable<HighQDataSchemaItem> GetSheetColumns()
        {
            //Return the SheetData source default Schema.

            ApplyOAuthAccessToken();
            var schemaRequest = _requestHelper.GetRequestAsJson(_requestHelper.CombinePaths(ServiceUrl, $"api/{ApiVersion}/isheet/{SheetID}/items?limit=1"));

            yield return new HighQDataSchemaItem
            { Name = "itemid", Key = true, AllowNull = false, FieldType = HighQDataSchemaItemType.FieldInteger, ReadOnly = true };

            yield return new HighQDataSchemaItem
            { Name = "externalid", AllowNull = true, FieldType = HighQDataSchemaItemType.FieldString };

            foreach (var column in schemaRequest["isheet"]["head"]["headcolumn"])
            {
                var columnType = HighQDataSchemaItem.GetTypeFromHighQDescription(column["columntypealias"].ToObject<string>());
                var parser = HighQDataSchemaItem.GetValueParserFromType(columnType);

                yield return new HighQDataSchemaItem()
                {
                    Name = column["columnvalue"].ToObject<string>(),
                    Id = column["columnid"].ToObject<int>(),
                    Sequence = column["sequence"].ToObject<int>(),
                    FieldType = columnType,
                    HighQValueParser = parser
                };
            }
        }

        public IEnumerable<HighQSite> GetSites()
        {
            ApplyOAuthAccessToken();
            var request = _requestHelper.GetRequestAsJson(_requestHelper.CombinePaths(ServiceUrl, $"api/{ApiVersion}/sites"));

            foreach (var site in request["site"])
            {
                yield return new HighQSite()
                {
                    ID = site["id"].ToObject<int>(),
                    Name = site["sitename"].ToObject<string>()
                };
            }
        }

        public IEnumerable<HighQSheet> GetSheets(int siteId)
        {
            ApplyOAuthAccessToken();
            var request = _requestHelper.GetRequestAsJson(_requestHelper.CombinePaths(ServiceUrl, $"api/{ApiVersion}/isheets?siteid={siteId}"));

            foreach (var site in request["isheet"])
            {
                yield return new HighQSheet()
                {
                    ID = site["id"].ToObject<int>(),
                    Name = site["name"].ToObject<string>()
                };
            }
        }

        public HttpWebRequestHelper GetRequestHelper()
        {
            ApplyOAuthAccessToken();
            return _requestHelper.Copy();
        }

        public override List<ProviderParameter> GetInitializationParameters()
        {
            //Return the Provider Settings so we can save the Project File.
            return new List<ProviderParameter>
                       {
                            new ProviderParameter("RegistryKey", RegistryKey),
                            new ProviderParameter("ServiceUrl", ServiceUrl),
                            new ProviderParameter("ApiVersion", ApiVersion.ToString()),
                            new ProviderParameter("OAuth.ClientID", ClientID),
                            new ProviderParameter("OAuth.ClientSecret", SecurityService.EncryptValue(ClientSecret)),
                            new ProviderParameter("OAuth.RefreshToken", SecurityService.EncryptValue(RefreshToken)),
                            new ProviderParameter("OAuth.AccessToken", SecurityService.EncryptValue(AccessToken)),
                            new ProviderParameter("OAuth.TokenExpires", TokenExpires.ToString("o")),
                            new ProviderParameter("OAuth.Authorize", Authorize),
                            new ProviderParameter("SiteID", SiteID.ToString()),
                            new ProviderParameter("SheetID", SheetID.ToString()),
                            new ProviderParameter("Site", Site),
                            new ProviderParameter("Sheet", Sheet)
                       };
        }

        public override void Initialize(List<ProviderParameter> parameters)
        {
            //Load the Provider Settings from the File.
            foreach (ProviderParameter p in parameters)
            {
                AddConfigKey(p.Name, p.ConfigKey);

                switch (p.Name)
                {
                    case "RegistryKey":
                        {
                            RegistryKey = p.Value;
                            break;
                        }
                    case "ServiceUrl":
                        {
                            ServiceUrl = p.Value;
                            break;
                        }
                    case "OAuth.ClientID":
                        {
                            ClientID = p.Value;
                            break;
                        }
                    case "OAuth.ClientSecret":
                        {
                            ClientSecret = p.Value;
                            break;
                        }
                    case "OAuth.RefreshToken":
                        {
                            RefreshToken = p.Value;
                            break;
                        }
                    case "OAuth.AccessToken":
                        {
                            AccessToken = p.Value;
                            break;
                        }
                    case "OAuth.TokenExpires":
                        {
                            if (DateTime.TryParse(p.Value, out var val))
                            {
                                TokenExpires = val.ToUniversalTime();
                            }
                            
                            break;
                        }
                    case "OAuth.Authorize":
                        {
                            Authorize = p.Value;
                            break;
                        }
                    case "ApiVersion":
                    {
                        if (int.TryParse(p.Value, out var val))
                        {
                            ApiVersion = val;
                        }

                        break;
                    }
                    case "SiteID":
                    {
                        if (int.TryParse(p.Value, out var val))
                        {
                            SiteID = val;
                        }

                        break;
                    }
                    case "Site":
                    {
                        Site = p.Value;
                        break;
                    }
                    case "SheetID":
                    {
                        if (int.TryParse(p.Value, out var val))
                        {
                            SheetID = val;
                        }

                        break;
                    }
                    case "Sheet":
                    {
                        Sheet = p.Value;
                        break;
                    }
                }
            }
        }

        public override IDataSourceWriter GetWriter() => new HighQDataSourceWriter { SchemaMap = SchemaMap };

        public void DisplayConfigurationUI(Control parent)
        {
            if (_connectionIf == null)
            {
                _connectionIf = new ConnectionInterface();
                _connectionIf.PropertyGrid.SelectedObject = new ConnectionProperties(this);
            }

            _connectionIf.Font = parent.Font;
            _connectionIf.Size = new Size(parent.Width, parent.Height);
            _connectionIf.Location = new Point(0, 0);
            _connectionIf.Dock = System.Windows.Forms.DockStyle.Fill;

            parent.Controls.Add(_connectionIf);
        }

        public bool Validate()
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "HighQ Datasource", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false;

        }

        public IDataSourceReader GetReader() => this;

        private void ApplyOAuthAccessToken()
        {
            //Set the Http Helper Trace
            _requestHelper.TraceEnabled = EnableTrace;

            if (DateTime.UtcNow > TokenExpires.AddHours(-1))
            {
                // Time to Refresh our Access Token
                var helper = new HttpWebRequestHelper();
                var parameters = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = ClientID,
                    ["client_secret"] = SecurityService.DecyptValue(ClientSecret),
                    ["refresh_token"] = SecurityService.DecyptValue(RefreshToken)
                };

                var tokenResult = helper.PostRequestAsString(parameters, HttpWebRequestHelper.MimeTypeApplicationWwwFormUrlEncoded, Utility.CombineWebPath(ServiceUrl, "/api/oauth2/token"));

                var result = HttpWebRequestHelper.FromJson(tokenResult);

                AccessToken = SecurityService.EncryptValue(result["access_token"]?.ToObject<string>());
                RefreshToken = SecurityService.EncryptValue(result["refresh_token"]?.ToObject<string>());
                TokenExpires = DateTime.UtcNow.AddSeconds(result["expires_in"]?.ToObject<int>() ?? 0);

                // Update Registry to apply updated Access and Refresh Token
                UpdateRegistry(_registryProvider, RegistryKey, GetRegistryInitializationParameters());
            }

            _requestHelper.SetAuthorizationHeader(SecurityService.DecyptValue(AccessToken));
        }  
        
        HighQOAuthConfiguration IHighQOAuthConfiguration.GetOauthConfiguration()
        {
            return new HighQOAuthConfiguration 
            { 
                ServiceUrl = ServiceUrl,
                ClientID = ClientID,
                ClientSecret = SecurityService.DecyptValue(ClientSecret)
            };
        }

        void IHighQOAuthConfiguration.UpdateOauthConfiguration(HighQOAuthConfiguration configuration)
        {
            AccessToken = SecurityService.EncryptValue(configuration.AccessToken);
            RefreshToken = SecurityService.EncryptValue(configuration.RefreshToken);
            TokenExpires = configuration.TokenExpires;
        }

        #region IDataSourceRegistry Members
        
        [Category("Connection.Library")]
        [Description("Key Name of the Item in the Connection Library")]
        [DisplayName("Key")]
        public string RegistryKey { get; set; }
        
        public void InitializeFromRegistry(IDataSourceRegistryProvider provider)
        {
            _registryProvider = provider;
            var registry = provider.Get(RegistryKey);
            if (registry != null)
            {
                foreach (ProviderParameter p in registry.Parameters)
                {
                    switch (p.Name)
                    {
                        case "ServiceUrl":
                            {
                                ServiceUrl = p.Value;
                                break;
                            }
                        case "OAuth.ClientID":
                            {
                                ClientID = p.Value;
                                break;
                            }
                        case "OAuth.ClientSecret":
                            {
                                ClientSecret = p.Value;
                                break;
                            }
                        case "OAuth.RefreshToken":
                            {
                                RefreshToken = p.Value;
                                break;
                            }
                        case "OAuth.AccessToken":
                            {
                                AccessToken = p.Value;
                                break;
                            }
                        case "OAuth.TokenExpires":
                            {
                                if (DateTime.TryParse(p.Value, out var val))
                                {
                                    TokenExpires = val.ToUniversalTime();
                                }

                                break;
                            }
                        case "OAuth.Authorize":
                            {
                                Authorize = p.Value;
                                break;
                            }
                        case "ApiVersion":
                            {
                                if (int.TryParse(p.Value, out var val))
                                {
                                    ApiVersion = val;
                                }

                                break;
                            }
                    }
                }
            }
        }

        public List<ProviderParameter> GetRegistryInitializationParameters()
        {
            return new List<ProviderParameter>
                       {
                            new ProviderParameter("ServiceUrl", ServiceUrl),
                            new ProviderParameter("ApiVersion", ApiVersion.ToString()),
                            new ProviderParameter("OAuth.ClientID", ClientID),
                            new ProviderParameter("OAuth.ClientSecret", SecurityService.EncryptValue(ClientSecret)),
                            new ProviderParameter("OAuth.RefreshToken", SecurityService.EncryptValue(RefreshToken)),
                            new ProviderParameter("OAuth.AccessToken", SecurityService.EncryptValue(AccessToken)),
                            new ProviderParameter("OAuth.TokenExpires", TokenExpires.ToString("o")),
                            new ProviderParameter("OAuth.Authorize", Authorize),
                       };
        }

        public IDataSourceReader ConnectFromRegistry(IDataSourceRegistryProvider provider)
        {
            InitializeFromRegistry(provider);
            return this;
        }

        public object GetRegistryInterface() => string.IsNullOrEmpty(RegistryKey) ? this : (object)new HighQDataSourceReaderWithRegistry(this);

        #endregion
    }

    public class HighQDataSourceReaderWithRegistry : DataReaderRegistryView<HighQDataSourceReader>, IHighQOAuthConfiguration
    {
        [Category("Connection.Library")]
        [Description("Key Name of the Item in the Connection Library")]
        [DisplayName("Key")]
        public string RegistryKey { get { return _reader.RegistryKey; } set { _reader.RegistryKey = value; } }

        [Category("Settings")]
        [ReadOnly(true)]
        public string ServiceUrl
        {
            get => _reader.ServiceUrl;
            set => _reader.ServiceUrl = value;
        }

        [Category("Settings")]
        [ReadOnly(true)]
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
        [ReadOnly(true)]
        public string ClientID
        {
            get => _reader.ClientID;
            set => _reader.ClientID = value;
        }

        [Category("Authentication")]
        [PasswordPropertyText(true)]
        [ReadOnly(true)]
        [Browsable(false)]
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
        [ReadOnly(true)]
        [Browsable(false)]
        public string Authorize
        {
            get => _reader.Authorize;
            set => _reader.Authorize = value;
        }

        public HighQDataSourceReaderWithRegistry(HighQDataSourceReader reader) : base(reader)
        {

        }

        public IEnumerable<HighQSite> GetSites() => _reader.GetSites();
        public IEnumerable<HighQSheet> GetSheets(int siteId) => _reader.GetSheets(siteId);

        HighQOAuthConfiguration IHighQOAuthConfiguration.GetOauthConfiguration() => ((IHighQOAuthConfiguration)_reader).GetOauthConfiguration();

        void IHighQOAuthConfiguration.UpdateOauthConfiguration(HighQOAuthConfiguration configuration) => ((IHighQOAuthConfiguration)_reader).UpdateOauthConfiguration(configuration);
    }
}
