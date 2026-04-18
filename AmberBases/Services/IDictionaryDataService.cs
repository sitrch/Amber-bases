using System;
using System.Collections.Generic;
using AmberBases.Core.Models.Dictionaries;

namespace AmberBases.Services;

public interface IDictionaryDataService
{
    void InitializeDatabase(string dbPath);
    List<T> GetItems<T>(string dbPath) where T : BaseDictionaryModel, new();
    void AddItem<T>(T item, string dbPath) where T : BaseDictionaryModel;
    void UpdateItem<T>(T item, string dbPath) where T : BaseDictionaryModel;
    void DeleteItem<T>(int id, string dbPath) where T : BaseDictionaryModel;
}
