# Модели справочников (Dictionary Models)

**Модуль:** Core  
**Расположение:** `AmberBases/Core/Models/Dictionaries/`

## Назначение
Набор классов, представляющих собой модели данных для таблиц-справочников в базе данных `AmberDictionaries.sqlite`. Используются для маппинга данных между базой данных SQLite и пользовательским интерфейсом.

## Сущности
1. **SystemProvider** (Поставщик системы)
   - `Id`: Идентификатор
   - `Name`: Название поставщика

2. **ProfileSystem** (Профильная система)
   - `Id`: Идентификатор
   - `ProviderId`: ID поставщика (внешний ключ к SystemProvider)
   - `Name`: Название системы

3. **Color** (Цвет)
   - `Id`: Идентификатор
   - `ColorName`: Название цвета
   - `RAL`: Код RAL
   - `CoatingTypeId`: ID типа покрытия (внешний ключ к CoatingType)

 4. **StandartBarLength** (Длина хлыста)
    - `Id`: Идентификатор
    - `Length`: Длина в мм
    - `Name`: Вычисляемое свойство для совместимости с FK lookup (возвращает `Length.ToString()`)

5. **ProfileType** (Тип профиля)
   - `Id`: Идентификатор
   - `Name`: Название типа профиля

6. **Applicability** (Применимость)
   - `Id`: Идентификатор
   - `Name`: Название применимости

    7. **ProfileArticle** (Артикул профиля)
       Структура соответствует `CBaseArticle` с FK-ссылками на справочники:
       - `Id`: Идентификатор
       - `ManufacturerId`: ID производителя (FK на SystemProvider)
       - `SystemId`: ID профильной системы (FK на ProfileSystem)
       - `Code`: Код заказа
       - `BOMArticle`: Артикул полный, для заказа
       - `Article`: Артикул
       - `Title`: Название
       - `Description`: Описание
       - `ColorId`: ID цвета (FK на Color)
       - `CutWisibleWidth`: Ширина отображения в раскрое
       - `StandartBarLengthId`: ID стандартной длины хлыста (FK на StandartBarLength)
       - `ProfileTypeId`: ID типа профиля (FK на ProfileType)
       - Навигационные свойства (расположены в регионе "Основные свойства"):
         - `Manufacturer`: SystemProvider (ссылка на производителя)
         - `System`: ProfileSystem (ссылка на профильную систему)
         - `Color`: Color (ссылка на цвет)
         - `StandartBarLength`: StandartBarLength (ссылка на длину хлыста)
         - `ProfileType`: ProfileType (ссылка на тип профиля)

## Примечание
- Класс `StandartBarLength` ранее использовался под именем `StandartBarLength` как алиас типа. Теперь используется единое имя `StandartBarLength` для всех ссылок в коде.

## Связи
Классы предназначены для использования в UI через `ObservableCollection<T>`, и связаны внешними ключами друг с другом. База данных содержит аналогичные таблицы с настроенными `FOREIGN KEY` и правилом `ON DELETE CASCADE`.

### FK-связи ProfileArticle
- `ManufacturerId` → `SystemProvider.Id` (Производитель)
- `SystemId` → `ProfileSystem.Id` (Профильная система)
- `ProfileTypeId` → `ProfileType.Id` (Тип профиля)
- `ColorId` → `Color.Id` (Цвет)
- `StandartBarLengthId` → `StandartBarLength.Id` (Стандартная длина хлыста)

## Зависимости
Нет внешних зависимостей. Используются только встроенные типы C#.