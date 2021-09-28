using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    public class ChoiceHighQValueParser : IHighQValueParser
    {

        public object ConvertValue(object value)
        {
            return new HighQSheetItemDefaultValueModel()
            {
                Text = DataSchemaTypeConverter.ConvertTo<string[]>(value)
            };
        }
        
        //"rawdata": {
        //    "choices": {
        //      "choice": {
        //        "style": "#000000",
        //        "label": "Finance"
        //      }
        //    }
        //  }
        public object ParseValue(JToken token)
        {
            var choices = token?["rawdata"]?["choices"]?["choice"];
            if (choices == null) return null;
            
            if (choices is JArray array)
            {
                var list = new List<string>();
                foreach (var item in array)
                {
                    var choice = item?["label"]?.ToObject<string>();
                    if (choice != null)
                    {
                        list.Add(choice);
                    }
                }

                list.Sort();
                return list.ToArray();
            }

            return choices["label"]?.ToObject<string>();

        }
    }
}