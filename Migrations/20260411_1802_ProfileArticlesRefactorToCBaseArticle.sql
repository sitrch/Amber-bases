-- Migration: Restructure ProfileArticles table to match CBaseArticle structure
-- Date: 2026-04-11 18:02
-- Description: Добавлены новые колонки для соответствия структуре CBaseArticle с FK на справочники

-- UP: Apply migration
-- Новые FK колонки
ALTER TABLE ProfileArticles ADD COLUMN ManufacturerId INTEGER;
ALTER TABLE ProfileArticles ADD COLUMN SystemId INTEGER;
ALTER TABLE ProfileArticles ADD COLUMN Code TEXT;
ALTER TABLE ProfileArticles ADD COLUMN BOMArticle TEXT;
ALTER TABLE ProfileArticles ADD COLUMN Title TEXT;
ALTER TABLE ProfileArticles ADD COLUMN Description TEXT;
ALTER TABLE ProfileArticles ADD COLUMN ColorId INTEGER;
ALTER TABLE ProfileArticles ADD COLUMN CutWisibleWidth REAL;
ALTER TABLE ProfileArticles ADD COLUMN StandartBarLength REAL;

-- DOWN: Rollback migration (SQLite не поддерживает DROP COLUMN напрямую, поэтому создаём временную таблицу)
-- Примечание: данные из удалённых колонок будут потеряны

-- CREATE TABLE ProfileArticles_backup AS SELECT Id, Article, ProfileSystemId, ProfileTypeId, ApplicabilityId, FileName, Size, StepHeight FROM ProfileArticles;
-- DROP TABLE ProfileArticles;
-- CREATE TABLE ProfileArticles (Id INTEGER PRIMARY KEY AUTOINCREMENT, Article TEXT, ProfileSystemId INTEGER, ProfileTypeId INTEGER, ApplicabilityId INTEGER, FileName TEXT, Size REAL, StepHeight REAL);
-- INSERT INTO ProfileArticles (Id, Article, ProfileSystemId, ProfileTypeId, ApplicabilityId, FileName, Size, StepHeight) SELECT Id, Article, ProfileSystemId, ProfileTypeId, ApplicabilityId, FileName, Size, StepHeight FROM ProfileArticles_backup;
-- DROP TABLE ProfileArticles_backup;