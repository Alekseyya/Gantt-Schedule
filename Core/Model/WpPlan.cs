using System;

namespace Core.Model
{
    public class WpPlan
    {
        /// <summary>
        /// Уникальный номер сохраненной версии плана
        /// </summary>
        public int PLAN_VERSION_ID { get; set; }
        /// <summary>
        /// Время прилета до начала ТО
        /// </summary>
        public string ARRIVAL_AT { get; set; }
        /// <summary>
        /// Количество дней на которые отличаются даты прибытия
        /// </summary>
        public int ARRIVAL_DIF_DAYS { get; set; }
        /// <summary>
        /// время вылета после TO
        /// </summary>
        public string DEPARTURE_AT { get; set; }
        /// <summary>
        /// Количество дне йн которые отличаются даты отправления
        /// </summary>
        public int DEPARTURE_DIF_DAYS { get; set; }
        /// <summary>
        /// Уникальный идинтификатор WP
        /// </summary>
        public Int64 WPNO_I { get; set; }
        /// <summary>
        /// Номер пакета работ
        /// </summary>
        public string WPNO { get; set; }

        /// <summary>
        /// Y - если содержит C-ch (флаг для отображениея "Готовность")
        /// </summary>
        public string CONTAINS_C_CHECK { get; set; }

        /// <summary>
        /// Пользовательские ремарки
        /// </summary>
        public string INTERNAL_REMARKS { get; set; }
        /// <summary>
        /// Модель ВС. В очтете: отображается над баром ТО
        /// </summary>
        public string AC_MODEL { get; set; }
        /// <summary>
        /// Тип BC
        /// </summary>
        public string AC_TYP { get; set; }

        /// <summary>
        /// Список и конфигурация работ(Список работ WP)
        /// </summary>
        public string DESCRIPTION { get; set; }

        /// <summary>
        /// номер Борта
        /// </summary>
        public string AC_REGISTR { get; set; }

        /// <summary>
        /// Место проведения ТО
        /// </summary>
        public string STATION { get; set; }
        /// <summary>
        /// Место проведения ТО
        /// </summary>
        public string STATION_NAME { get; set; }
        /// <summary>
        /// PL
        /// </summary>
        public string PROJECTNO { get; set; }

        /// <summary>
        /// Провайдер ТО
        /// </summary>
        public string MAINTPROV_ADDRESS { get; set; }

        /// <summary>
        /// Тип формы ТО
        /// </summary>
        public string HIDDEN { get; set; }

        /// <summary>
        /// Плановое время начала ТО
        /// </summary>
        public string PL_START_AT { get; set; }
        /// <summary>
        /// Фактическое время начала ТО
        /// </summary>
        public string ACT_START_AT { get; set; }

        /// <summary>
        /// Плановое время окончания ТО
        /// </summary>
        public string PL_END_AT { get; set; }
        /// <summary>
        /// Фактическое время окончания ТО
        /// </summary>
        public string ACT_END_AT { get; set; }

        /// <summary>
        /// Дата создания WP
        /// </summary>
        public string CREATED_DATE { get; set; }
        /// <summary>
        /// ИНдетификатор родительского пакета
        /// </summary>
        public Int64 PARENT_WPNO_I { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public int MUTATION { get; set; }

        /// <summary>
        /// Время последнего изменения
        /// </summary>
        public int MUTATION_TIME { get; set; }

        /// <summary>
        /// Человеко-часы на техническое обслуживани плановые
        /// </summary>
        public int MHR { get; set; }

        public int BOOKED_MHR { get; set; }

    }
}
