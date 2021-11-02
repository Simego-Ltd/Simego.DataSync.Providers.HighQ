namespace Simego.DataSync.Providers.HighQ.Models
{
    public class HighQColumn
    {
        //{
        //    "rawData": null,
        //    "title": "Number",
        //    "type": "Auto increment",
        //    "mendatory": false,
        //    "columnID": 1875,
        //    "sequence": null
        //}

        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
