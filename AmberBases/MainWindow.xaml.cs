using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using Microsoft.Win32;
using AmberBases.Core;
using AmberBases.Services;
using AmberBases.UI;
using AmberBases.UI.Tracking;

namespace AmberBases
{
    public partial class MainWindow : RibbonWindow
    {
        private readonly IExcelDataService _excelDataService;
        private readonly ISqliteDataService _sqliteDataService;
        private readonly InterfaceSettingsTracker _settingsTracker;
        private DataSet _currentDataSet;

        // Словарь для хранения состояния свёрнутости ленты по табам
        // Ключ: имя таба, Значение: true если лента свёрнута
        private Dictionary<string, bool> _tabMinimizedStates;

        public MainWindow()
        {
            InitializeComponent();

            _excelDataService = new ExcelDataService();
            _sqliteDataService = new SqliteDataService();
            _settingsTracker = new InterfaceSettingsTracker();
            _tabMinimizedStates = new Dictionary<string, bool>();

            Loaded += MainWindow_Loaded;
        }

        private DependencyPropertyDescriptor _ribbonMinimizedDescriptor;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFileList();
            DictionaryEditor.InitControl();
            
            // Инициализируем состояния табов
            _tabMinimizedStates["MaterialsTab"] = false;
            _tabMinimizedStates["ExcelTab"] = false;
            _tabMinimizedStates["DictionariesTab"] = false;
            
            // Восстанавливаем сохранённое состояние IsMinimized для текущего таба
            if (MainRibbon.SelectedItem is RibbonTab initialTab)
            {
                RestoreRibbonMinimizedState(initialTab);
            }
            
            // Подписываемся на изменение IsMinimized в реальном времени через DependencyPropertyDescriptor
            // Это позволяет отслеживать сворачивание/разворачивание Ribbon по двойному клику на заголовок таба
            _ribbonMinimizedDescriptor = DependencyPropertyDescriptor.FromProperty(
                Ribbon.IsMinimizedProperty, typeof(Ribbon));
            _ribbonMinimizedDescriptor.AddValueChanged(MainRibbon, Ribbon_IsMinimizedChanged);
        }

        private void Ribbon_IsMinimizedChanged(object sender, EventArgs e)
        {
            // Мгновенно сохраняем состояние для текущего выбранного таба
            if (MainRibbon.SelectedItem is RibbonTab currentTab)
            {
                SaveRibbonMinimizedState(currentTab);
            }
        }
        
        private void RestoreRibbonMinimizedState(RibbonTab tab)
        {
            string tabName = tab.Name;
            bool isMinimized = _settingsTracker.LoadRibbonMinimizedState(tabName);
            MainRibbon.IsMinimized = isMinimized;
        }
        
        private void SaveRibbonMinimizedState(RibbonTab tab)
        {
            string tabName = tab.Name;
            _tabMinimizedStates[tabName] = MainRibbon.IsMinimized;
            _settingsTracker.SaveRibbonMinimizedState(tabName, MainRibbon.IsMinimized);
        }

        /// <summary>
        /// Переключает состояние свёрнутости ленты для указанного таба.
        /// </summary>
        private void ToggleRibbonState(string tabName, bool isMinimized)
        {
            _tabMinimizedStates[tabName] = isMinimized;
            
            // Если это текущая активная вкладка — применяем немедленно
            if (MainRibbon.SelectedItem is RibbonTab currentTab && currentTab.Name == tabName)
            {
                MainRibbon.IsMinimized = isMinimized;
            }
        }

        private void MainRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Сохраняем состояние для предыдущего таба
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is RibbonTab prevTab)
            {
                SaveRibbonMinimizedState(prevTab);
            }
            
            if (MainRibbon.SelectedItem is RibbonTab selectedTab)
            {
                // Применяем сохранённое состояние для выбранной вкладки
                if (_tabMinimizedStates.ContainsKey(selectedTab.Name))
                {
                    MainRibbon.IsMinimized = _tabMinimizedStates[selectedTab.Name];
                }
                else
                {
                    // Если состояния нет, загружаем из настроек
                    RestoreRibbonMinimizedState(selectedTab);
                }
                
                // ЕСЛИ вкладка должна быть свернута, сбрасываем фокус
                if (MainRibbon.IsMinimized)
                {
                    Keyboard.ClearFocus();
                }
                
                if (selectedTab == ExcelTab)
                {
                    ExcelView.Visibility = Visibility.Visible;
                    DictionaryEditor.Visibility = Visibility.Collapsed;
                }
                else if (selectedTab == MaterialsTab)
                {
                    ExcelView.Visibility = Visibility.Collapsed;
                    DictionaryEditor.Visibility = Visibility.Visible;
                    DictionaryEditor.InitControl();
                    DictionaryEditor.SetMaterialsMode(true);
                }
                else if (selectedTab == DictionariesTab)
                {
                    ExcelView.Visibility = Visibility.Collapsed;
                    DictionaryEditor.Visibility = Visibility.Visible;
                    DictionaryEditor.InitControl();
                    DictionaryEditor.SetMaterialsMode(false);
                }
            }
        }

        /// <summary>
        /// Обработчик пункта контекстного меню "Свернуть".
        /// </summary>
        private void RibbonGroup_Collapse(object sender, RoutedEventArgs e)
        {
            if (MainRibbon.SelectedItem is RibbonTab selectedTab)
            {
                ToggleRibbonState(selectedTab.Name, true);
            }
        }

        /// <summary>
        /// Обработчик пункта контекстного меню "Развернуть".
        /// </summary>
        private void RibbonGroup_Expand(object sender, RoutedEventArgs e)
        {
            if (MainRibbon.SelectedItem is RibbonTab selectedTab)
            {
                ToggleRibbonState(selectedTab.Name, false);
            }
        }

        private void LoadFileList()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string basesDir = Path.Combine(appDir, "Bases");

            if (!Directory.Exists(basesDir))
            {
                string parentDir = appDir;
                while (parentDir != null && !Directory.Exists(Path.Combine(parentDir, "Bases")))
                {
                    parentDir = Directory.GetParent(parentDir)?.FullName;
                }
                if (parentDir != null)
                {
                    basesDir = Path.Combine(parentDir, "Bases");
                }
            }

            filesGalleryCategory.Items.Clear();
            if (Directory.Exists(basesDir))
            {
                var files = Directory.GetFiles(basesDir, "*.xls*")
                    .Where(f => !Path.GetFileName(f).StartsWith("~$") && !Path.GetFileName(f).StartsWith(".~lock"));

                foreach (var file in files)
                {
                    filesGalleryCategory.Items.Add(new FileItem { Name = Path.GetFileName(file), Path = file });
                }
                
                if (filesGalleryCategory.Items.Count > 0)
                {
                    galleryFiles.SelectedItem = filesGalleryCategory.Items[0];
                }
                else
                {
                    lblStatus.Text = "No database files found in Bases folder.";
                }
            }
        }

        private void LoadExcelFile(string filePath)
        {
            try
            {
                lblStatus.Text = "Loading...";

                _currentDataSet = _excelDataService.LoadData(filePath);

                tablesGalleryCategory.Items.Clear();
                foreach (DataTable table in _currentDataSet.Tables)
                {
                    tablesGalleryCategory.Items.Add(table.TableName);
                }

                if (tablesGalleryCategory.Items.Count > 0)
                {
                    cbTables.IsEnabled = true;
                    btnSaveSqlite.IsEnabled = true;
                    galleryTables.SelectedItem = tablesGalleryCategory.Items[0];
                    lblStatus.Text = $"Loaded {tablesGalleryCategory.Items.Count} tables.";
                }
                else
                {
                    cbTables.IsEnabled = false;
                    btnSaveSqlite.IsEnabled = false;
                    dataGrid.ItemsSource = null;
                    lblStatus.Text = "No tables found in Excel.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Error loading Excel.";
            }
        }

        private void CbFiles_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (galleryFiles.SelectedItem is FileItem selectedFile)
            {
                LoadExcelFile(selectedFile.Path);
            }
        }

        private void BtnLoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var newItem = new FileItem { Name = Path.GetFileName(openFileDialog.FileName), Path = openFileDialog.FileName };
                filesGalleryCategory.Items.Add(newItem);
                galleryFiles.SelectedItem = newItem;
            }
        }

        private void CbTables_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (galleryTables.SelectedItem != null && _currentDataSet != null)
            {
                string selectedTableName = galleryTables.SelectedItem.ToString();
                if (_currentDataSet.Tables.Contains(selectedTableName))
                {
                    dataGrid.ItemsSource = _currentDataSet.Tables[selectedTableName].DefaultView;
                }
            }
        }

        private void BtnSaveSqlite_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDataSet == null || _currentDataSet.Tables.Count == 0)
            {
                MessageBox.Show("No data to save. Please load an Excel file first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                lblStatus.Text = "Saving to SQLite...";

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string dbPath = Path.Combine(appDir, DatabaseConfig.SqliteDatabaseFile);

                _sqliteDataService.SaveDataSet(_currentDataSet, dbPath);

                lblStatus.Text = $"Data saved to {DatabaseConfig.SqliteDatabaseFile}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to SQLite: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Error saving to SQLite.";
            }
        }

        private void BtnShowProfileArticles_Click(object sender, RoutedEventArgs e)
        {
            MainRibbon.SelectedItem = MaterialsTab;
        }

        private void BtnShowDictionaries_Click(object sender, RoutedEventArgs e)
        {
            MainRibbon.SelectedItem = DictionariesTab;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuMaterials_Click(object sender, RoutedEventArgs e)
        {
            MainRibbon.SelectedItem = MaterialsTab;
        }

        private void MenuExcelLoader_Click(object sender, RoutedEventArgs e)
        {
            MainRibbon.SelectedItem = ExcelTab;
        }

        private void MenuDictionaries_Click(object sender, RoutedEventArgs e)
        {
            MainRibbon.SelectedItem = DictionariesTab;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DictionaryEditor.SaveAllColumnSettings();

            // Проверяем несохранённые изменения в справочниках
            if (DictionaryEditor.HasAnyUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "Есть несохранённые изменения.\n\nСохранить изменения перед закрытием?",
                    "Несохранённые изменения",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Сохраняем и закрываем
                        DictionaryEditor.SaveAllChanges();
                        break;
                    case MessageBoxResult.No:
                        // Закрываем без сохранения
                        break;
                    case MessageBoxResult.Cancel:
                        // Отменяем закрытие
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}
