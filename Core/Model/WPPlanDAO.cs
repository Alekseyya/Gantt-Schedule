using System;
using System.ComponentModel.DataAnnotations;
using Core.Extensions;

namespace Core.Model
{
    public enum DaysOfWeek
    {
        [Display(Name = "ВСК")]
        Sunday,
        [Display(Name = "ПНД")]
        Monday,
        [Display(Name = "ВТР")]
        Tuesday,
        [Display(Name = "СРД")]
        Wednesday,
        [Display(Name = "ЧТВ")]
        Thursday,
        [Display(Name = "ПТН")]
        Friday,
        [Display(Name = "СББ")]
        Saturday,
    }

    public enum WorkType
    {
        Primary,
        Optional,
        Sleep
    }

    public enum ACType
    {
        [Display(Name = "Все")]
        All,
        [Display(Name = "RRJ")]
        RRJ,
        [Display(Name = "A32S")]
        A32S,
        [Display(Name = "B777")]
        B777,
        [Display(Name = "A330")]
        A330,
        [Display(Name = "A350")]
        A350,
        [Display(Name = "B737")]
        B737,
        //[Display(Name = "A320")]
        //A320,
        //[Display(Name = "A321")]
        //A321
    }

    public enum ACModel
    {
        [Display(Name = "RRJ")]
        RRJ,
        [Display(Name = "B77W")]
        B77W,
        [Display(Name = "B738")]
        B738,
        [Display(Name = "A320")]
        A320,
        [Display(Name = "A321")]
        A321,
        [Display(Name = "A332")]
        A332,
        [Display(Name = "A333")]
        A333,
        [Display(Name = "A359")]
        A359
    }

    public enum StatusPlan
    {
        [Display(Name="d", Description = "Черновик")]
        Draft,
        [Display(Name="a", Description = "Согласовано")]
        Approved
    }

    public enum TypeReport
    {
        [Display(Name = "План")]
        Plan,
        [Display(Name = "Факт")]
        Fact
    }

    public enum Tab
    {
        PTO,
        OTO,
        W_OUT,
        TO_OUT,
        A_T,
        WDTM
    }

    public class WPPlanDAO
    {
        public Int64 ParentWpno { get; set; }
        /// <summary>
        /// Уникальный номер сохраненной версии плана
        /// </summary>
        public int PLAN_VERSION_ID { get; set; }
        /// <summary>
        /// Уникальный идинтификатор WP
        /// </summary>
        public Int64 WPNO_I { get; set; }
        /// <summary>
        /// Номер пакета работ
        /// </summary>
        public string WPNO { get; set; }
        /// <summary>
        /// Пользовательские ремарки
        /// </summary>
        public string INTERNAL_REMARKS { get; set; }

        public WorkType WorkType { get; set; }

        public enum WorkTypeColor
        {
            [Display(Name = "#A52A2A")]
            Brown,
            [Display(Name = "#FFA500")]
            Orange,
            [Display(Name = "#FF0000")]
            Red,
            [Display(Name = "#0000FF")]
            Blue,
            [Display(Name = "#DB7093")]
            PaleVioletRed,
            [Display(Name = "#C71585")]
            MediumVioletRed,
            [Display(Name = "#00FFFF")]
            Aqua,
            [Display(Name = "#ADFF2F")]
            GreenYellow,
            [Display(Name = "#000000")]
            Black,
        }

        public string HEXColorWorkColor
        {
            get
            {
                switch (WorkType)
                {
                    case WorkType.Primary:
                        return WorkTypeColor.Red.GetAttributeOfType<DisplayAttribute>().Name;
                    case WorkType.Optional:
                        return WorkTypeColor.Orange.GetAttributeOfType<DisplayAttribute>().Name;
                    case WorkType.Sleep:
                        return WorkTypeColor.GreenYellow.GetAttributeOfType<DisplayAttribute>().Name;
                    default:
                        return WorkTypeColor.Black.GetAttributeOfType<DisplayAttribute>().Name;
                }
            }
        }

        /// <summary>
        /// Тип BC(Список работ WP)
        /// </summary>
        public ACType AC_TYP { get; set; }
        public ACModel AC_MODEL { get; set; }
        public enum ACTypeColor
        {
            [Display(Name = "#A52A2A")]
            Brown,
            [Display(Name = "#FFA500")]
            Orange,
            [Display(Name = "#FF0000")]
            Red,
            [Display(Name = "#0000FF")]
            Blue,
            [Display(Name = "#DB7093")]
            PaleVioletRed,
            [Display(Name = "#C71585")]
            MediumVioletRed,
            [Display(Name = "#00FFFF")]
            Aqua,
            [Display(Name = "#ADFF2F")]
            GreenYellow,
            [Display(Name = "#000000")]
            Black,
        }

         
        public string HEXColorACType
        {
            get
            {//https://colorscheme.ru/html-colors.html
                if (PROJECTNO == "PL12" || PROJECTNO == "PL120")
                    return ACTypeColor.Red.GetAttributeOfType<DisplayAttribute>().Name;
                else
                {
                    switch (AC_MODEL)
                    {
                        case ACModel.A320:
                            return ACTypeColor.Brown.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.A321:
                            return ACTypeColor.Orange.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.A332:
                            return ACTypeColor.Blue.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.A333:
                            return ACTypeColor.Blue.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.A359:
                            return ACTypeColor.PaleVioletRed.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.B738:
                            return ACTypeColor.MediumVioletRed.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.B77W:
                            return ACTypeColor.Aqua.GetAttributeOfType<DisplayAttribute>().Name;
                        case ACModel.RRJ:
                            return ACTypeColor.GreenYellow.GetAttributeOfType<DisplayAttribute>().Name;
                        default:
                            return ACTypeColor.Black.GetAttributeOfType<DisplayAttribute>().Name;
                    }
                }
            }
        }

        /// <summary>
        /// время прилета до начала ТО
        /// </summary>
        public DateTime Arrival { get; set; }

        /// <summary>
        /// Количество дней на которые отличаются даты прибытия
        /// </summary>
        //public int ArrivalDifDays { get; set; }

        /// <summary>
        /// время вылета после TO
        /// </summary>
        public DateTime Departure { get; set; }

        /// <summary>
        /// Количество дне йн которые отличаются даты отправления
        /// </summary>
        //public int DepartureDifDays { get; set; }

        /// <summary>
        /// Список и конфигурация работ
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// номер ВС(номер Борта)
        /// </summary>
        public string AC_REGISTR { get; set; }

        /// <summary>
        /// Место проведения ТО
        /// </summary>
        public string STATION { get; set; }
        /// <summary>
        /// Название станции ТО
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
        /// Дата начала ТО факт
        /// </summary>
        public DateTime FactStartDate { get; set; }

        /// <summary>
        /// Дата начала проведения ТО(Дата начала ТО план)
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// Готовность ВС к эксплуатации(Должно быть в DateTime)
        /// </summary>
        public DateTime FactEndDate { get; set; }

        /// <summary>
        /// Дата окончания ТО план
        /// </summary>
        public DateTime EndDate { get; set; }


        /// <summary>
        /// Дата создания WP
        /// </summary>
        public DateTime CREATED_DATE { get; set; }

        /// <summary>
        /// Y - если содержит C-ch (флаг для отображениея "Готовность")
        /// </summary>
        public bool CONTAINS_C_CHECK { get; set; }

        /// <summary>
        /// Человеко-часы на техническое обслуживани плановая(ч/ч)
        /// </summary>
        public int MHR { get; set; }

        /// <summary>
        /// Фактические трудозатраты (ч/ч)
        /// </summary>
        public int BOOKED_MHR { get; set; }
    }
}
