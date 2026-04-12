-- Миграция: создание таблицы Profiles для справочника профилей

-- UP
-- Создание таблицы Profiles
CREATE TABLE IF NOT EXISTS Profiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Position INTEGER NOT NULL DEFAULT 0,
    Info TEXT,
    ArticleId INTEGER,
    Title TEXT,
    Description TEXT,
    StandartBarLengthId INTEGER,
    CustomBarLength REAL,
    ProfileTypeId INTEGER,
    
    -- Внешние ключи
    FOREIGN KEY (ArticleId) REFERENCES ProfileArticles(Id),
    FOREIGN KEY (StandartBarLengthId) REFERENCES StandartBarLengths(Id),
    FOREIGN KEY (ProfileTypeId) REFERENCES ProfileTypes(Id)
);

-- Создание индексов для улучшения производительности
CREATE INDEX IF NOT EXISTS idx_profiles_articleid ON Profiles(ArticleId);
CREATE INDEX IF NOT EXISTS idx_profiles_standartbarlengthid ON Profiles(StandartBarLengthId);
CREATE INDEX IF NOT EXISTS idx_profiles_profiletypeid ON Profiles(ProfileTypeId);

-- DOWN
-- Удаление таблицы Profiles
DROP TABLE IF EXISTS Profiles;

-- Удаление индексов
DROP INDEX IF EXISTS idx_profiles_articleid;
DROP INDEX IF EXISTS idx_profiles_standartbarlengthid;
DROP INDEX IF EXISTS idx_profiles_profiletypeid;