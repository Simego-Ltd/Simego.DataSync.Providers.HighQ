using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    class LookupISheetHighQValueParser : IHighQValueParser
    {
        public object ConvertValue(HighQDataSchemaItem column, object value)
        {       
            // Trying to implment writing back to a lookup item
            // can't seem to get it to work tried matching the format of the returned data, simple text etc nothing works.

            return new HighQSheetLookupModel()
            {
                Items = new HighQSheetLookupValueModel { Items = new[] { new HighQSheetLookupItemValueModel { Item = DataSchemaTypeConverter.ConvertTo<int>(value) } } }
            };
        }

        public object ParseValue(HighQDataSchemaItem column, JToken token)
        {
            var parts = column.Name.Split('|');
            var isheetItems = token?["rawdata"]?["isheetitems"]?.ToObject<JToken>();

            try
            {
                if (isheetItems != null && isheetItems["isheetitem"] is JArray arr)
                {
                    var list = new List<string>();
                    foreach (var isheetItem in arr)
                    {
                        list.Add(isheetItem[parts[1]]?.ToObject<string>());
                    }
                    return list.OrderBy(p => p).ToArray();
                }

                if (isheetItems != null && isheetItems["isheetitem"] is JToken item)
                {
                    return item?[parts[1]]?.ToObject<string>();
                }
            }
            catch(ArgumentException)
            {
                //Ignore - this happens when the API returns an empty object rather than a string value.
            }

            return null;
        }
    }
}
