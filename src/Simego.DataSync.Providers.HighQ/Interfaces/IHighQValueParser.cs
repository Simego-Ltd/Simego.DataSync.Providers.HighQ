using Newtonsoft.Json.Linq;

namespace Simego.DataSync.Providers.HighQ.Interfaces
{
    interface IHighQValueParser
    {
        object ConvertValue(HighQDataSchemaItem column, object value);
        object ParseValue(HighQDataSchemaItem column, JToken token);
    }
}
