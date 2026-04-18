using System;
using AmberBases.Core.Models;

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
        [ColumnDisplayName("Название профиля")]
        public string Title { get; set; }

        /// <summary>
        /// Описание профиля
        /// </summary>
        [ColumnDisplayName("Описание")]
        public string Description { get; set; }

        /// <summary>
        /// Пользовательская длина хлыста (если не используется стандартная)
        /// </summary>
        [ColumnDisplayName("Длина хлыста")]
        public double? CustomBarLength { get; set; }

        /// <summary>
        /// Артикул профиля (навигационное свойство)
        /// </summary>
        [ColumnDisplayName("Артикул", false)]
        public ProfileArticle Article { get; set; }

        /// <summary>
        /// Стандартная длина хлыста (навигационное свойство)
        /// </summary>
        [ColumnDisplayName("��лина хлыста", false)]
        public StandartBarLength StandartBarLength { get; set; }

        /// <summary>
        /// Тип профиля (навигационное свойство)
        /// </summary>
        [ColumnDisplayName("Тип профиля", false)]
        public ProfileType ProfileType { get; set; }

        #endregion

        #region Служебные свойства

        /// <summary>
        /// ID артикула профиля (FK на ProfileArticle)
        /// </summary>
        [ColumnDisplayName("Артикул", false)]
        public int? ArticleId { get; set; }

        /// <summary>
        /// ID стандартной длины хлыста (FK на StandartBarLength)
        /// </summary>
        [ColumnDisplayName("Длина хлыста", false)]
        public int? StandartBarLengthId { get; set; }

        /// <summary>
        /// ID типа профиля (FK на ProfileType)
        /// </summary>
        [ColumnDisplayName("Тип профиля", false)]
        public int? ProfileTypeId { get; set; }

        #endregion
    }
}