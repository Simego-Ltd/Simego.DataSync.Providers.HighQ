using Simego.DataSync.Providers.HighQ.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Simego.DataSync.Providers.HighQ.TypeEditors
{
    class HighQSiteListTypeEditor : UITypeEditor
    {
        private const BindingFlags DefaultPropertyBinding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null) return value;
            if (provider == null) return value;

            var fSiteIdProperty = context.Instance.GetType().GetProperty("SiteID", DefaultPropertyBinding);

            var grid = context.GetType().InvokeMember(
                "OwnerGrid",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public,
                null,
                context,
                null) as PropertyGrid;

            var service = ((IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService)));

            if (grid != null && service != null && fSiteIdProperty != null)
            {
                try
                {
                    grid.Cursor = Cursors.WaitCursor;

                    dynamic reader = context.Instance;
                    if (reader != null)
                    {
                        var sites = ((IEnumerable<HighQSite>) reader.GetSites()).ToList();

                        var list = new ListBox();
                        list.SelectedValueChanged += (sender, e) => { service.CloseDropDown(); };

                        list.Items.AddRange(sites.Select(p => (object) p.Name).ToArray());

                        // Drop the list control.
                        service.DropDownControl(list);

                        if (list.SelectedItem != null)
                        {
                            fSiteIdProperty.SetValue(context.Instance, sites[list.SelectedIndex].ID, null);
                            value = list.Text;
                        }
                    }
                }
                catch (ArgumentException)
                {

                }
                finally
                {
                    grid.Cursor = Cursors.Default;
                }
            }

            return value;
        }
    }
}
