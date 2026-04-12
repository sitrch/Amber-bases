-- Миграция: добавление полей связи для названия и описания профилей

-- UP
-- Добавление полей IsTitleLinked и IsDescriptionLinked в таблицу Profiles
ALTER TABLE Profiles ADD COLUMN IsTitleLinked INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Profiles ADD COLUMN IsDescriptionLinked INTEGER NOT NULL DEFAULT 0;

-- Обновление существующих записей: если Title или Description пустые, устанавливаем связь в 0 (false)
UPDATE Profiles SET IsTitleLinked = 0 WHERE Title IS NULL OR Title = '';
UPDATE Profiles SET IsDescriptionLinked = 0 WHERE Description IS NULL OR Description = '';

-- DOWN
-- Удаление полей IsTitleLinked и IsDescriptionLinked из таблицы Profiles
-- SQLite не поддерживает DROP COLUMN, поэтому нужно создать новую таблицу
-- Вместо этого создадим временную таблицу и скопируем данные без новых полей
BEGIN TRANSACTION;

-- Создаем временную таблицу со старой структурой
CREATE TABLE Profiles_temp (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Position INTEGER NOT NULL DEFAULT 0,
    Info TEXT,
    ArticleId INTEGER,
    Title TEXT,
    Description TEXT,
    StandartBarLengthId INTEGER,
    CustomBarLength REAL,
    ProfileTypeId INTEGER,
    
    FOREIGN KEY (ArticleId) REFERENCES ProfileArticles(Id),
    FOREIGN KEY (StandartBarLengthId) REFERENCES StandartBarLengths(Id),
    FOREIGN KEY (ProfileTypeId) REFERENCES ProfileTypes(Id)
);

-- Копируем данные из старой таблицы во временную (без новых полей)
INSERT INTO Profiles_temp (Id, Position, Info, ArticleId, Title, Description, StandartBarLengthId, CustomBarLength, ProfileTypeId)
SELECT Id, Position, Info, ArticleId, Title, Description, StandartBarLengthId, CustomBarLength, ProfileTypeId
FROM Profiles;

-- Удаляем старую таблицу
DROP TABLE Profiles;

-- Переименовываем временную таблицу в Profiles
ALTER TABLE Profiles_temp RENAME TO Profiles;

-- Восстанавливаем индексы
CREATE INDEX IF NOT EXISTS idx_profiles_articleid ON Profiles(ArticleId);
CREATE INDEX IF NOT EXISTS idx_profiles_standartbarlengthid ON Profiles(StandartBarLengthId);
CREATE INDEX IF NOT EXISTS idx_profiles_profiletypeid ON Profiles(ProfileTypeId);

COMMIT;