using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    public class DefaultHighQValueParser : IHighQValueParser
    {
        public object ConvertValue(object value)
        {
            return new HighQSheetItemDefaultValueModel()
            {
                Text = new[] { DataSchemaTypeConverter.ConvertTo<string>(value) }
            };
        }

        public object ParseValue(JToken token)
        {
            return token?["rawdata"]?["value"]?.ToObject<object>();
        }
    }

}
