using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    public class LookupHighQValueParser : IHighQValueParser
    {
        public object ConvertValue(object value)
        {
            return new HighQSheetItemDefaultValueModel()
            {
                Text = DataSchemaTypeConverter.ConvertTo<string[]>(value)
            };
        }

        //"rawdata": {
        //    "lookups": {
        //      "lookup": {
        //        "id": "1407",
        //        "userlookuptype": "1",
        //        "email": "support@simego.com"
        //      }
        //    }
        //}

        public object ParseValue(JToken token)
        {
            var lookups = token?["rawdata"]?["lookups"]?["lookup"];
            if (lookups == null) return null;

            if (lookups is JArray array)
            {
                var list = new List<string>();
                foreach (var item in array)
                {
                    var choice = item?["id"]?.ToObject<string>();
                    if (choice != null)
                    {
                        list.Add(choice);
                    }
                }

                list.Sort();
                return list.ToArray();
            }

            return lookups["id"]?.ToObject<string>();

        }
    }
}