using Newtonsoft.Json;
using System.Collections.Generic;

// Data Models used to serialise Json to HighQ iSheet API

namespace Simego.DataSync.Providers.HighQ.Models
{
    class HighQSheetModel
    {
        [JsonProperty("metaData")]
        public HighQSheetMetaDataModel SheetMetaData { get; set; } = new HighQSheetMetaDataModel();
        
        [JsonProperty("head")]
        public HighQSheetHeadModel SheetHead { get; set; } = new HighQSheetHeadModel();

        [JsonProperty("data")]
        public HighQSheetDataModel SheetData { get; set; } = new HighQSheetDataModel();
    }

    class HighQSheetMetaDataModel
    {
        [JsonProperty("siteid")]
        public int SiteID { get; set; }
        
        [JsonProperty("sheetid")]
        public int SheetID { get; set; }
    }

    class HighQSheetHeadModel
    {
        [JsonProperty("headColumn")]
        public IList<HighQSheetHeadColumnModel> Columns { get; set; } = new List<HighQSheetHeadColumnModel>();
    }
    class HighQSheetHeadColumnModel
    {
        [JsonProperty("columnValue")]
        public string Column { get; set; }

        [JsonProperty("sequence")]
        public int Sequence { get; set; }
    }


    class HighQSheetDataModel
    {
        [JsonProperty("item")]
        public IList<HighQSheetItemModel> Items { get; set; } = new List<HighQSheetItemModel>();
    }
    class HighQSheetItemModel
    {
        [JsonProperty("externalID")]
        public string ExternalID { get; set; }

        [JsonProperty("itemID")]
        public string ItemID { get; set; }

        [JsonProperty("column")]
        public IList<HighQSheetItemColumnModel> Items { get; set; } = new List<HighQSheetItemColumnModel>();
    }

    class HighQSheetItemColumnModel
    {
        [JsonProperty("sequence")]
        public int Sequence { get; set; }

        [JsonProperty("rawData")]
        public object Data { get; set; }
    }

    class HighQSheetItemDefaultValueModel
    {
        [JsonProperty("text")]
        public string [] Text { get; set; }
    }
}
