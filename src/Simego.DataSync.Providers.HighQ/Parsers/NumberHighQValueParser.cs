using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    class NumberHighQValueParser : IHighQValueParser
    {
        public object ConvertValue(HighQDataSchemaItem column, object value)
        {
            return new HighQSheetItemDefaultValueModel()
            {
                Text = new[] { DataSchemaTypeConverter.ConvertTo<string>(value) }
            };
        }

        public object ParseValue(HighQDataSchemaItem column, JToken token)
        {
            return token?["rawdata"]?["value"]?.ToObject<object>();
        }

    }

}