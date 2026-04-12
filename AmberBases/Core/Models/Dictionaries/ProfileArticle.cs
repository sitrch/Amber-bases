using System;
using System.Collections.Generic;

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
        public string Code { get; set; }

        /// <summary>
        /// Артикул полный, для заказа.
        /// </summary>
        public string BOMArticle { get; set; }

        /// <summary>
        /// Артикул.
        /// </summary>
        public string Article { get; set; }

        /// <summary>
        /// Название.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Описание.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Ширина отображения в раскрое.
        /// </summary>
        public double CutWisibleWidth { get; set; }

        /// <summary>
        /// Computed-свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        public string Name => Article;

        /// <summary>
        /// Профильная система (FK на ProfileSystem).
        /// </summary>
        public ProfileSystem System { get; set; }

        /// <summary>
        /// Цвет (FK на Color).
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Стандартная длина хлыста (FK на StandartBarLength).
        /// </summary>
        public StandartBarLength StandartBarLength { get; set; }

        /// <summary>
        /// Тип профиля (FK на ProfileType).
        /// </summary>
        public ProfileType ProfileType { get; set; }
        
        #endregion

        #region Служебные свойства
        
        /// <summary>
        /// ID производителя (FK на SystemProvider).
        /// </summary>
        public int? ManufacturerId { get; set; }

        /// <summary>
        /// ID профильной системы (FK на ProfileSystem).
        /// </summary>
        public int? SystemId { get; set; }

        /// <summary>
        /// ID цвета (FK на Color).
        /// </summary>
        public int? ColorId { get; set; }

        /// <summary>
        /// ID стандартной длины хлыста (FK на StandartBarLength).
        /// </summary>
        public int? StandartBarLengthId { get; set; }

        /// <summary>
        /// ID типа профиля (FK на ProfileType).
        /// </summary>
        public int? ProfileTypeId { get; set; }
        
        #endregion
    }
}
