using Newtonsoft.Json.Linq;

namespace Simego.DataSync.Providers.HighQ.Interfaces
{
    interface IHighQValueParser
    {
        object ConvertValue(object value);
        object ParseValue(JToken token);
    }
}
