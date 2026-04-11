-- Миграция: Изменение типа связи CoatingType в таблице Colors
-- Описание: Переход от хранения строкового CoatingType к хранению CoatingTypeId (внешний ключ на CoatingTypes)

-- ==========================================
-- UP СЕКЦИЯ: Применяется к БД
-- ==========================================

-- В SQLite нет прямой команды DROP COLUMN, поэтому мы пересоздаем таблицу

-- 1. Добавляем колонку CoatingTypeId (если ее еще нет через инициализацию)
-- ALTER TABLE Colors ADD COLUMN CoatingTypeId INTEGER; 
-- (SQLite Data Service штатно делает это через try-catch при старте)

BEGIN TRANSACTION;

-- 2. Переименовываем старую таблицу
ALTER TABLE Colors RENAME TO Colors_OLD;

-- 3. Создаем новую таблицу с правильной схемой
CREATE TABLE Colors (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Position INTEGER NOT NULL,
    ColorName TEXT,
    RAL INTEGER,
    CoatingTypeId INTEGER
);

-- 4. Переносим данные, пытаясь сопоставить старые строки с ID из таблицы CoatingTypes
INSERT INTO Colors (Id, Position, ColorName, RAL, CoatingTypeId)
SELECT 
    c.Id, 
    c.Position, 
    c.ColorName, 
    c.RAL, 
    ct.Id 
FROM Colors_OLD c
LEFT JOIN CoatingTypes ct ON c.CoatingType = ct.Name;

-- 5. Удаляем старую таблицу
DROP TABLE Colors_OLD;

COMMIT;

-- ==========================================
-- DOWN СЕКЦИЯ: Откат миграции
-- ==========================================
/*
BEGIN TRANSACTION;

ALTER TABLE Colors RENAME TO Colors_OLD;

CREATE TABLE Colors (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Position INTEGER NOT NULL,
    ColorName TEXT,
    RAL INTEGER,
    CoatingType TEXT
);

INSERT INTO Colors (Id, Position, ColorName, RAL, CoatingType)
SELECT 
    c.Id, 
    c.Position, 
    c.ColorName, 
    c.RAL, 
    ct.Name 
FROM Colors_OLD c
LEFT JOIN CoatingTypes ct ON c.CoatingTypeId = ct.Id;

DROP TABLE Colors_OLD;

COMMIT;
*/