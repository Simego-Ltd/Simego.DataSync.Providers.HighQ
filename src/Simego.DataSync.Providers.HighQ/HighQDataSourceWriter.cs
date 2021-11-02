using Newtonsoft.Json;
using Simego.DataSync.Engine;
using Simego.DataSync.Interfaces;
using Simego.DataSync.Providers.HighQ.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simego.DataSync.Providers.HighQ
{
    public class HighQDataSourceWriter : DataWriterProviderBase
    {
        private HighQDataSourceReader DataSourceReader { get; set; }
        private DataSchemaMapping Mapping { get; set; }
        private IDictionary<string, HighQDataSchemaItem> HighQSchema { get; set; }
        private HttpWebRequestHelper RequestHelper { get; set; }
        public override void AddItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);

                        //Call the Automation BeforeAddItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeAddItem(this, itemInvariant, null);

                        if (itemInvariant.Sync)
                        {
                            #region Add Item

                            //Get the Target Item SheetData
                            Dictionary<string, object> targetItem = AddItemToDictionary(Mapping, itemInvariant);

                            var model = new HighQSheetModel
                            {
                                SheetMetaData =
                                {
                                    SiteID = DataSourceReader.SiteID,
                                    SheetID = DataSourceReader.SheetID
                                }
                            };

                            foreach (var column in targetItem.Keys)
                            {
                                if (column == "itemid") continue;
                                if (column == "externalid") continue;
                                
                                model.SheetHead.Columns.Add(new HighQSheetHeadColumnModel() { Column = HighQSchema[column].Name, Sequence = HighQSchema[column].Sequence });
                            }

                            var dataitem = new HighQSheetItemModel();
                            
                            foreach (var column in targetItem.Keys)
                            {
                                if (column == "itemid") continue;
                                if (column == "externalid")
                                {
                                    dataitem.ExternalID = DataSchemaTypeConverter.ConvertTo<string>(targetItem[column]);
                                    continue;
                                }

                                dataitem.Items.Add(
                                    new HighQSheetItemColumnModel()
                                    {
                                        Sequence = HighQSchema[column].Sequence, 
                                        Data = HighQSchema[column].HighQValueParser.ConvertValue(HighQSchema[column], targetItem[column])
                                    });
                            }

                            model.SheetData.Items.Add(dataitem);

                            var json = JsonConvert.SerializeObject(model, Formatting.Indented);

                            RequestHelper.PostRequestAsJson(json, RequestHelper.CombinePaths(DataSourceReader.ServiceUrl, $"api/{DataSourceReader.ApiVersion}/isheet/item/create"));
                            
                            //Call the Automation AfterAddItem (pass the created item identifier if possible)
                            Automation?.AfterAddItem(this, itemInvariant, null);
                        }

                        #endregion

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows

                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void UpdateItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);

                        //Example: Get the item ID from the Target Identifier Store 
                        var item_id = itemInvariant.GetTargetIdentifier<int>();

                        //Call the Automation BeforeUpdateItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeUpdateItem(this, itemInvariant, item_id);

                        if (itemInvariant.Sync)
                        {
                            #region Update Item

                            //Get the Target Item SheetData
                            Dictionary<string, object> targetItem = UpdateItemToDictionary(Mapping, itemInvariant);

                            var model = new HighQSheetModel
                            {
                                SheetMetaData =
                                {
                                    SiteID = DataSourceReader.SiteID,
                                    SheetID = DataSourceReader.SheetID
                                }
                            };

                            foreach (var column in targetItem.Keys)
                            {
                                if (column == "itemid") continue;
                                if (column == "externalid") continue;

                                model.SheetHead.Columns.Add(new HighQSheetHeadColumnModel() { Column = HighQSchema[column].Name, Sequence = HighQSchema[column].Sequence });
                            }

                            var dataitem = new HighQSheetItemModel() { ItemID = item_id.ToString() };

                            foreach (var column in targetItem.Keys)
                            {
                                if (column == "itemid") continue;
                                if (column == "externalid") continue;
                                
                                dataitem.Items.Add(
                                    new HighQSheetItemColumnModel()
                                    {
                                        Sequence = HighQSchema[column].Sequence,
                                        Data = HighQSchema[column].HighQValueParser.ConvertValue(HighQSchema[column], targetItem[column])
                                    });
                            }

                            model.SheetData.Items.Add(dataitem);

                            var json = JsonConvert.SerializeObject(model, Formatting.Indented);

                            RequestHelper.PostRequestAsJson(json, RequestHelper.CombinePaths(DataSourceReader.ServiceUrl, $"api/{DataSourceReader.ApiVersion}/isheet/item/create"));

                            //Call the Automation AfterUpdateItem 
                            Automation?.AfterUpdateItem(this, itemInvariant, item_id);

                            #endregion
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void DeleteItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        var itemInvariant = new DataCompareItemInvariant(item);

                        //Example: Get the item ID from the Target Identifier Store 
                        var item_id = itemInvariant.GetTargetIdentifier<int>();

                        //Call the Automation BeforeDeleteItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeDeleteItem(this, itemInvariant, item_id);

                        if (itemInvariant.Sync)
                        {                            
                            RequestHelper.DeleteRequestAsJson(null, RequestHelper.CombinePaths(DataSourceReader.ServiceUrl, $"api/{DataSourceReader.ApiVersion}/isheet/{DataSourceReader.SheetID}/items?itemids={item_id}"));

                            //Call the Automation AfterDeleteItem 
                            Automation?.AfterDeleteItem(this, itemInvariant, item_id);
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (SystemException e)
                    {
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void Execute(List<DataCompareItem> addItems, List<DataCompareItem> updateItems, List<DataCompareItem> deleteItems, IDataSourceReader reader, IDataSynchronizationStatus status)
        {
            DataSourceReader = reader as HighQDataSourceReader;

            if (DataSourceReader != null)
            {
                Mapping = new DataSchemaMapping(SchemaMap, DataCompare);

                RequestHelper = DataSourceReader.GetRequestHelper();

                HighQSchema = DataSourceReader.GetSheetColumns()
                    .ToDictionary(k => k.Name, StringComparer.OrdinalIgnoreCase);
                
                //Process the Changed Items
                if (addItems != null && status.ContinueProcessing) AddItems(addItems, status);
                if (updateItems != null && status.ContinueProcessing) UpdateItems(updateItems, status);
                if (deleteItems != null && status.ContinueProcessing) DeleteItems(deleteItems, status);

            }
        }

        private static void HandleError(IDataSynchronizationStatus status, Exception e)
        {
            if (!status.FailOnError)
            {
                status.LogMessage(e.Message);
            }
            if (status.FailOnError)
            {
                throw e;
            }
        }
    }
}
