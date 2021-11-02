using System;
using Newtonsoft.Json.Linq;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;

namespace Simego.DataSync.Providers.HighQ.Parsers
{
    class DateTimeHighQValueParser : IHighQValueParser
    {
        public object ConvertValue(HighQDataSchemaItem column, object value)
        {
            return new HighQSheetItemDefaultValueModel()
            {
                Text = new[] { DataSchemaTypeConverter.ConvertTo<DateTime>(value).ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        //"rawdata": {
        //    "date": "2021-09-27",
        //    "time": "11:56:50"
        //}
        public object ParseValue(HighQDataSchemaItem column, JToken token)
        {
            var date = token["rawdata"]?["date"]?.ToObject<string>();
            var time = token["rawdata"]?["time"]?.ToObject<string>();
            
            if (date != null && time != null)
            {
                return DateTime.Parse(date).Add(TimeSpan.Parse(time));
            }
            
            if (date != null)
            {
                return DateTime.Parse(date);
            }

            return null;
        }
    }
}