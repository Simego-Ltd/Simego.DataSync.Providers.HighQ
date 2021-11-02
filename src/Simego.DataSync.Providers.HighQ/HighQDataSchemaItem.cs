using System;
using Simego.DataSync.Providers.HighQ.Interfaces;
using Simego.DataSync.Providers.HighQ.Parsers;

namespace Simego.DataSync.Providers.HighQ
{
    enum HighQDataSchemaItemType
    {
        FieldString,
        FieldInteger,           
        FieldDouble,            
        FieldDecimal,           
        FieldDateTime,              
        FieldChoice,            
        FieldBoolean,
        FieldLookup,
        FieldSheetLookup
    }

    class HighQDataSchemaItem
    {
        public int Id { get; set; }
        public int Sequence { get; set; }
        public bool Key { get; set; }
        public bool AllowNull { get; set; } = true;
        public string Name { get; set; }
        public HighQDataSchemaItemType FieldType { get; set; } = HighQDataSchemaItemType.FieldString;
        public bool ReadOnly { get; set; }
        public IHighQValueParser HighQValueParser { get; set; } = new DefaultHighQValueParser();

        public DataSchemaItem ToDataSchemaItem()
        {
            switch (FieldType)
            {
                case HighQDataSchemaItemType.FieldInteger:
                    return new DataSchemaItem(Name, Name, typeof(int), Key, ReadOnly, AllowNull, -1);

                case HighQDataSchemaItemType.FieldDouble:
                    return new DataSchemaItem(Name, Name, typeof(double), Key, ReadOnly, AllowNull, -1);

                case HighQDataSchemaItemType.FieldDecimal:
                    return new DataSchemaItem(Name, Name, typeof(decimal), Key, ReadOnly, AllowNull, -1);

                case HighQDataSchemaItemType.FieldDateTime:
                    return new DataSchemaItem(Name, Name, typeof(DateTime), Key, ReadOnly, AllowNull, -1);

                case HighQDataSchemaItemType.FieldBoolean:
                    return new DataSchemaItem(Name, Name, typeof(bool), Key, ReadOnly, AllowNull, -1);

                case HighQDataSchemaItemType.FieldSheetLookup:
                    return new DataSchemaItem(Name, Name, typeof(string[]), Key, ReadOnly, AllowNull, -1);

                default:
                    return new DataSchemaItem(Name, Name, typeof(string), Key, ReadOnly, AllowNull, -1);

            }
        }

        public static HighQDataSchemaItemType GetTypeFromHighQDescription(string desc)
        {
            switch (desc)
            {
                case "SHEET_COLUMN_TYPE_LOOKUP": return HighQDataSchemaItemType.FieldLookup;
                case "SHEET_COLUMN_TYPE_SHEET_LOOKUP": return HighQDataSchemaItemType.FieldSheetLookup;
                case "SHEET_COLUMN_TYPE_DATE_AND_TIME": return HighQDataSchemaItemType.FieldDateTime;
                case "SHEET_COLUMN_TYPE_SINGLE_LINE_TEXT": return HighQDataSchemaItemType.FieldString;
                case "SHEET_COLUMN_TYPE_CHOICE": return HighQDataSchemaItemType.FieldChoice;
                case "SHEET_COLUMN_TYPE_AUTO_INCREMENT": return HighQDataSchemaItemType.FieldString;
                case "SHEET_COLUMN_TYPE_NUMBER": return HighQDataSchemaItemType.FieldDecimal;

                default: return HighQDataSchemaItemType.FieldString;
            }
        }

        public static IHighQValueParser GetValueParserFromType(HighQDataSchemaItemType type)
        {
            switch (type)
            {
                case HighQDataSchemaItemType.FieldDecimal: return new NumberHighQValueParser();
                case HighQDataSchemaItemType.FieldChoice: return new ChoiceHighQValueParser();
                case HighQDataSchemaItemType.FieldDateTime: return new DateTimeHighQValueParser();
                case HighQDataSchemaItemType.FieldLookup: return new LookupHighQValueParser();
                case HighQDataSchemaItemType.FieldSheetLookup: return new LookupISheetHighQValueParser();

                default: return new DefaultHighQValueParser();
            }
        }
    }
}
