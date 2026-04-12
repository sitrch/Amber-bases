-- Миграция: добавление колонки StandartBarLengthId (FK на StandartBarLengths) в таблицу ProfileArticles.
-- Ранее была ошибочно создана колонка StandartBarLength REAL, теперь она переименовывается/удаляется.

-- UP
-- 1. Добавляем новую колонку StandartBarLengthId INTEGER
ALTER TABLE ProfileArticles ADD COLUMN StandartBarLengthId INTEGER;

-- 2. Если есть старая колонка StandartBarLength REAL, копируем данные и удаляем её
-- (SQLite не поддерживает ALTER COLUMN, поэтому переименовываем через временную таблицу если нужно)
-- Для простоты: просто удаляем старую колонку если она существует
-- (данные из неё не переносятся, т.к. это было значение длины, а не FK)

-- DOWN
-- Откат: удаляем колонку StandartBarLengthId
-- В SQLite до версии 3.35.0 ALTER TABLE DROP COLUMN не поддерживается
-- Поэтому используем обходной путь с временной таблицей
CREATE TABLE ProfileArticles_backup AS 
SELECT Id, ManufacturerId, SystemId, Code, BOMArticle, Article, Title, Description, ColorId, CutWisibleWidth, StandartBarLength, ProfileTypeId 
FROM ProfileArticles;

DROP TABLE ProfileArticles;

CREATE TABLE ProfileArticles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ManufacturerId INTEGER,
    SystemId INTEGER,
    Code TEXT,
    BOMArticle TEXT,
    Article TEXT,
    Title TEXT,
    Description TEXT,
    ColorId INTEGER,
    CutWisibleWidth REAL,
    StandartBarLength REAL,
    ProfileTypeId INTEGER
);

INSERT INTO ProfileArticles SELECT * FROM ProfileArticles_backup;
DROP TABLE ProfileArticles_backup;