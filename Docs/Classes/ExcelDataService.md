# ExcelDataService

**Модуль:** Services  
**Расположение:** `AmberBases/Services/ExcelDataService.cs`

## Назначение
Класс `ExcelDataService` реализует интерфейс `IExcelDataService` и отвечает за извлечение данных из Excel-файлов (формата `.xlsx`) и преобразование их во внутренний объект `System.Data.DataSet`. 
Для работы с Excel используется библиотека **ExcelDataReader** (вместо ClosedXML, для обхода строгих ограничений парсинга OpenXML).

## Методы
- `DataSet LoadData(string filePath)`:
  Главный публичный метод. Открывает Excel-книгу по указанному пути `filePath` и динамически перебирает все листы, доступные в файле, добавляя их в результирующий `DataSet`. Для предотвращения ошибок блокировки файл открывается через `FileStream` с правами `FileAccess.Read` и разрешением совместного доступа `FileShare.ReadWrite`. 
  Чтение осуществляется через `ExcelReaderFactory.CreateReader`, результаты конвертируются в `DataSet` с помощью метода `AsDataSet` (используя первую строку в качестве заголовков). Обработка каждого листа обернута в блок `try-catch`, чтобы ошибка на одном листе не прерывала общую загрузку.
- `DataTable ProcessTable(DataTable sourceTable, string tableName)` (private):
  Вспомогательный метод. Обрабатывает исходную таблицу, полученную от `ExcelDataReader` (очищает полностью пустые строки, уникализирует названия колонок, присваивает имя `ColumnN` пустым колонкам). 
  - Осуществляется проверка на уникальность имен колонок, чтобы избежать исключений при совпадении заголовков в Excel.

## Зависимости
- `ExcelDataReader` и `ExcelDataReader.DataSet` (для парсинга Excel).
- `System.Data` (для DataSet и DataTable).
- `System.IO` (для работы с файловыми потоками).
- Реализует `IExcelDataService`.