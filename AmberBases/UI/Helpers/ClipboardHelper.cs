using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AmberBases.Core.Models.Dictionaries;
using AmberBases.Helpers;

namespace AmberBases.UI;

public static class ClipboardHelper
{
    public static void CopySelectedCells(DataGrid dataGrid)
    {
        if (dataGrid.SelectedCells.Count == 0) return;

        var cells = dataGrid.SelectedCells.OrderBy(c => c.Column.DisplayIndex).ToList();
        var text = new System.Text.StringBuilder();
        
        int currentRowIndex = -1;
        foreach (var cell in cells)
        {
            if (cell.Item != null)
            {
                int rowIndex = dataGrid.Items.IndexOf(cell.Item);
                if (currentRowIndex != -1 && rowIndex != currentRowIndex)
                {
                    text.AppendLine();
                }
                
                var value = GetCellValue(cell);
                text.Append(value?.ToString() ?? "");
                if (cells.IndexOf(cell) < cells.Count - 1 && 
                    (cells.IndexOf(cell) == cells.Count - 1 || 
                     dataGrid.Items.IndexOf(cells[cells.IndexOf(cell) + 1].Item) == rowIndex))
                {
                    text.Append("\t");
                }
                
                currentRowIndex = rowIndex;
            }
        }
        
        Clipboard.SetText(text.ToString());
    }

    public static void PasteFromClipboard(DataGrid dataGrid, Type entityType)
    {
        if (!Clipboard.ContainsText()) return;
        
        var text = Clipboard.GetText();
        var rows = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var properties = ReflectionHelper.GetSimpleProperties(entityType);

        foreach (var row in rows)
        {
            var values = row.Split('\t');
            try
            {
                var item = Activator.CreateInstance(entityType) as BaseDictionaryModel;
                if (item == null) continue;

                for (int i = 0; i < Math.Min(values.Length, properties.Length); i++)
                {
                    var prop = properties[i];
                    if (!prop.CanWrite) continue;

                    var value = values[i];
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        if (targetType == typeof(string))
                            prop.SetValue(item, value);
                        else if (targetType == typeof(int))
                            prop.SetValue(item, int.Parse(value));
                        else if (targetType == typeof(double))
                            prop.SetValue(item, double.Parse(value.Replace(",", ".")));
                        else if (targetType == typeof(decimal))
                            prop.SetValue(item, decimal.Parse(value.Replace(",", ".")));
                    }
                    catch { }
                }

                if (dataGrid.ItemsSource is IList list)
                {
                    list.Add(item);
                }
            }
            catch { }
        }
    }

    private static object GetCellValue(DataGridCellInfo cell)
    {
        if (cell.Column?.GetCellContent(cell.Item) is FrameworkElement element)
        {
            if (element is TextBlock tb) return tb.Text;
            if (element is CheckBox cb) return cb.IsChecked;
            if (element is ComboBox combobox) return combobox.SelectedValue;
        }
        if (cell.Column?.GetCellContent(cell.Item) is ContentControl cc)
            return cc.Content;
        return null;
    }
}
