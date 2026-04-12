using System;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Применяется для разграничения ролей профилей в разных сценариях
    /// </summary>
    public class CProfile : BaseDictionaryModel
    {
        #region Основные свойства
        
        /// <summary>
        /// Название профиля
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Описание профиля
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Пользовательская длина хлыста (если не используется стандартная)
        /// </summary>
        public double? CustomBarLength { get; set; }

        /// <summary>
        /// Артикул профиля (навигационное свойство)
        /// </summary>
        public ProfileArticle Article { get; set; }

        /// <summary>
        /// Стандартная длина хлыста (навигационное свойство)
        /// </summary>
        public StandartBarLength StandartBarLength { get; set; }

        /// <summary>
        /// Тип профиля (навигационное свойство)
        /// </summary>
        public ProfileType ProfileType { get; set; }
        
        #endregion

        #region Служебные свойства
        
        /// <summary>
        /// ID артикула профиля (FK на ProfileArticle)
        /// </summary>
        public int? ArticleId { get; set; }

        /// <summary>
        /// ID стандартной длины хлыста (FK на StandartBarLength)
        /// </summary>
        public int? StandartBarLengthId { get; set; }

        /// <summary>
        /// ID типа профиля (FK на ProfileType)
        /// </summary>
        public int? ProfileTypeId { get; set; }
        
        #endregion
    }
}