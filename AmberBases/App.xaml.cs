using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AmberBases.UI;

namespace AmberBases
{
    public partial class App : Application
    {
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    ColumnOrderStore.Load();
    RegisterGlobalInputBindings();
}

        /// <summary>
        /// Регистрирует глобальные горячие клавиши для всех DataGrid в приложении.
        /// </summary>
        private void RegisterGlobalInputBindings()
        {
            // Ctrl+Z -> ApplicationCommands.Undo
            CommandManager.RegisterClassInputBinding(
                typeof(DataGrid),
                new InputBinding(ApplicationCommands.Undo, new KeyGesture(Key.Z, ModifierKeys.Control)));

            // Ctrl+Y -> ApplicationCommands.Redo
            CommandManager.RegisterClassInputBinding(
                typeof(DataGrid),
                new InputBinding(ApplicationCommands.Redo, new KeyGesture(Key.Y, ModifierKeys.Control)));

            // Ctrl+C -> ApplicationCommands.Copy
            CommandManager.RegisterClassInputBinding(
                typeof(DataGrid),
                new InputBinding(ApplicationCommands.Copy, new KeyGesture(Key.C, ModifierKeys.Control)));

            // Ctrl+V -> ApplicationCommands.Paste
            CommandManager.RegisterClassInputBinding(
                typeof(DataGrid),
                new InputBinding(ApplicationCommands.Paste, new KeyGesture(Key.V, ModifierKeys.Control)));

            // Регистрируем CommandBindings для DataGrid
            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(
                    ApplicationCommands.Undo,
                    DataGrid_Undo_Executed,
                    DataGrid_Undo_CanExecute));

            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(
                    ApplicationCommands.Redo,
                    DataGrid_Redo_Executed,
                    DataGrid_Redo_CanExecute));

            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(
                    ApplicationCommands.Copy,
                    DataGrid_Copy_Executed,
                    DataGrid_Copy_CanExecute));

            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(
                    ApplicationCommands.Paste,
                    DataGrid_Paste_Executed,
                    DataGrid_Paste_CanExecute));
        }

        private void DataGrid_Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                e.CanExecute = control?.ActionTracker?.CanUndo == true;
            }
        }

        private void DataGrid_Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                control?.Undo();
                e.Handled = true;
            }
        }

        private void DataGrid_Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                e.CanExecute = control?.ActionTracker?.CanRedo == true;
            }
        }

        private void DataGrid_Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                control?.Redo();
                e.Handled = true;
            }
        }

        private void DataGrid_Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                e.CanExecute = control != null && (dataGrid.SelectedCells.Count > 0 || dataGrid.SelectedItem != null);
            }
        }

        private void DataGrid_Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                control?.CopySelectedCells(dataGrid);
                e.Handled = true;
            }
        }

        private void DataGrid_Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                e.CanExecute = control != null && Clipboard.ContainsText();
            }
        }

        private void DataGrid_Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var control = FindParentDictionaryTableControl(dataGrid);
                control?.PasteFromClipboard(dataGrid);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Ищет родительский DictionaryTableControl для DataGrid.
        /// </summary>
        private DictionaryTableControl FindParentDictionaryTableControl(DependencyObject obj)
        {
            while (obj != null)
            {
                if (obj is DictionaryTableControl control)
                    return control;
                obj = System.Windows.Media.VisualTreeHelper.GetParent(obj);
            }
            return null;
        }
    }
}
