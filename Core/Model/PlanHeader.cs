using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Model
{
    public class PlanVersionHeaderDAO
    {
        [Display(Name = "Номер версии")]
        public int ID { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "Начало периода")]
        public DateTime DateFrom { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Конец периода")]
        public DateTime DateTo { get; set; }
        [Display(Name = "Кем зафиксирована ИВП")]
        public string ApprovedBy { get; set; }
        [Display(Name = "Время фиксации ИВП")]
        public DateTime ApprovedDate { get; set; }
        [Display(Name = "Статус")]
        public StatusPlan Status { get; set; }
        [Display(Name = "Создано")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }
        [Display(Name = "Кем создано")]
        public string CreatedBy { get; set; }
    }

    public class PlanVersionHeader
    {
        [Display(Name = "Номер версии")]
        public int PLAN_VERSION_ID { get; set; }
        [Display(Name = "Начало периода")]
        public string DATE_FROM { get; set; }
        [Display(Name = "Конец периода")]
        public string DATE_TO { get; set; }
        [Display(Name = "Кем зафиксирована ИВП")]
        public string APPROVED_BY { get; set; }
        [Display(Name = "Время фиксации ИВП")]
        public string APPROVED_AT { get; set; }
        [Display(Name = "Статус")]
        public string STATUS { get; set; }
        [Display(Name = "Создано")]
        public string CREATED_AT { get; set; }
        public string CREATED_TIME { get; set; }
        [Display(Name = "Кем создано")]
        public string CREATED_BY { get; set; }
    }
}
