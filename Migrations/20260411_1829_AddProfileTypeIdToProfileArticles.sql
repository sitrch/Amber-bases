-- Migration: Add ProfileTypeId column to ProfileArticles table
-- Date: 2026-04-11 18:29
-- Description: Добавлена недостающая колонка ProfileTypeId (FK на ProfileTypes)

-- UP: Apply migration
ALTER TABLE ProfileArticles ADD COLUMN ProfileTypeId INTEGER;

-- DOWN: Rollback migration
-- Примечание: SQLite не поддерживает DROP COLUMN напрямую до версии 3.35.0
-- Для отката необходимо создать временную таблицу без этой колонки и восстановить данные
-- CREATE TABLE ProfileArticles_backup AS SELECT Id, ManufacturerId, SystemId, Code, BOMArticle, Article, Title, Description, ColorId, CutWisibleWidth, StandartBarLength FROM ProfileArticles;
-- DROP TABLE ProfileArticles;
-- CREATE TABLE ProfileArticles (Id INTEGER PRIMARY KEY AUTOINCREMENT, ManufacturerId INTEGER, SystemId INTEGER, Code TEXT, BOMArticle TEXT, Article TEXT, Title TEXT, Description TEXT, ColorId INTEGER, CutWisibleWidth REAL, StandartBarLength REAL);
-- INSERT INTO ProfileArticles (Id, ManufacturerId, SystemId, Code, BOMArticle, Article, Title, Description, ColorId, CutWisibleWidth, StandartBarLength) SELECT Id, ManufacturerId, SystemId, Code, BOMArticle, Article, Title, Description, ColorId, CutWisibleWidth, StandartBarLength FROM ProfileArticles_backup;
-- DROP TABLE ProfileArticles_backup;