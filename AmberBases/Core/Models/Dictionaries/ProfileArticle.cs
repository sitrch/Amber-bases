using System;
using System.Collections.Generic;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Артикул профиля. Структура соответствует CBaseArticle с FK-pссылками на справочники.
    /// </summary>
    public class ProfileArticle : BaseDictionaryModel
    {
        #region Основные свойства

        /// <summary>
        /// Код заказа.
        /// </summary>
        [ColumnDisplayName("Код")]
        public string Code { get; set; }

        /// <summary>
        /// Артикул полный, для заказа.
        /// </summary>
        [ColumnDisplayName("Код BOM", false)]
        public string BOMArticle { get; set; }

        /// <summary>
        /// Артикул.
        /// </summary>
        [ColumnDisplayName("Артикул")]
        public string Article { get; set; }

        /// <summary>
        /// Название.
        /// </summary>
        [ColumnDisplayName("Название")]
        public string Title { get; set; }

        /// <summary>
        /// Описание.
        /// </summary>
        [ColumnDisplayName("Описание")]
        public string Description { get; set; }

        /// <summary>
        /// Ширина отображения в раскрое.
        /// </summary>
        [ColumnDisplayName("Ширина раскроя")]
        public double CutWisibleWidth { get; set; }

        /// <summary>
        /// Computed-свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        [ColumnDisplayName("Name", false)]
        public string Name => Article;

        /// <summary>
        /// Профильная система (FK на ProfileSystem).
        /// </summary>
        [ColumnDisplayName("Система", false)]
        public ProfileSystem System { get; set; }

        /// <summary>
        /// Цвет (FK на Color).
        /// </summary>
        [ColumnDisplayName("Цвет", false)]
        public Color Color { get; set; }

        /// <summary>
        /// Стандартная длина хлыста (FK на StandartBarLength).
        /// </summary>
        [ColumnDisplayName("Длина хлыста", false)]
        public StandartBarLength StandartBarLength { get; set; }

        /// <summary>
        /// Тип профиля (FK на ProfileType).
        /// </summary>
        [ColumnDisplayName("Тип профиля", false)]
        public ProfileType ProfileType { get; set; }

        #endregion

        #region Служебные свойства

        /// <summary>
        /// ID производителя (FK на SystemProvider).
        /// </summary>
        [ColumnDisplayName("Производитель")]
        public int? ManufacturerId { get; set; }

        /// <summary>
        /// ID профильной системы (FK на ProfileSystem).
        /// </summary>
        [ColumnDisplayName("Система")]
        public int? SystemId { get; set; }

        /// <summary>
        /// ID цвета (FK на Color).
        /// </summary>
        [ColumnDisplayName("Цвет")]
        public int? ColorId { get; set; }

        /// <summary>
        /// ID стандартной длины хлыста (FK на StandartBarLength).
        /// </summary>
        [ColumnDisplayName("Длина хлыста")]
        public int? StandartBarLengthId { get; set; }

        /// <summary>
        /// ID типа профиля (FK на ProfileType).
        /// </summary>
        [ColumnDisplayName("Тип профиля")]
        public int? ProfileTypeId { get; set; }

        #endregion
    }
}
