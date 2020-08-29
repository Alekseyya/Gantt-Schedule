using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using Core.Data;
using Core.Extensions;
using Core.Model;
using DayPilot.Web.Ui;
using DayPilot.Web.Ui.Events.Scheduler;
using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;

namespace WebApp.Controllers
{
    [AllowAnonymous]
    public class ReportsController : Controller
    {
        private readonly IWPPlanRepository _wpPlanRepository;
        public int ReportWidth { get; set; }
        public int ReportHeight { get; set; }
        public int ReportRowHeadersWeight { get; set; }

        public ReportsController(IWPPlanRepository wpPlanRepository)
        {
            _wpPlanRepository = wpPlanRepository;
        }
        
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SavePlan(string submit, int planVersion, string user)
        {
            if (!string.IsNullOrEmpty(submit) && !string.IsNullOrEmpty(user))
            {
                switch (submit)
                {
                    case "Draft":
                        _wpPlanRepository.UpdateTemporaryPlan(planVersion, StatusPlan.Draft, user);
                        return Json(new { success = "Версия плана в черновике" });
                        break;
                    case "Approve":
                        _wpPlanRepository.UpdateTemporaryPlan(planVersion, StatusPlan.Approved, user);
                        return Json(new { success = "Версия плана в согласовании" });
                        break;
                    default:
                        Response.StatusCode = 500;
                        return Json(new { error = "Не выбрана кнопка" });
                }
            }
            else
            {
                Response.StatusCode = 500;
                return Json(new { error = "Ничего не указано" });
            }
        }
        
        [HttpGet]
        public ActionResult WorkPackagePlanReport(string dateStartTO, string dateEndTO, ACType? acType, TypeReport? typeReport,
            int? planVersion, bool? createPlan)
        {
            if (string.IsNullOrEmpty(dateStartTO) && string.IsNullOrEmpty(dateEndTO))
                return RedirectToAction("WorkPackagePlanReport", new
                {
                    dateStartTO = DateTime.Today.ToShortDateString(),
                    dateEndTO = DateTime.Today.AddDays(9).ToShortDateString(),
                    typeReport = TypeReport.Plan,
                    createPlan = false
                });
            var currentTypeReports = (TypeReport) (typeReport.HasValue ? typeReport : TypeReport.Plan);
            DateTime.TryParse(dateStartTO, out DateTime dateTimeStartTO);
            DateTime.TryParse(dateEndTO, out DateTime dateTimeEndTO);
            var wcType = acType.HasValue ? acType.Value : ACType.All;
            
            ViewBag.CreatePlan = createPlan.HasValue && createPlan.Value;
            ViewBag.PlanVersion = null;
            if (createPlan == true || (planVersion.HasValue && planVersion != 0))
            {
                var userName = (User.Identity.Name.IndexOf("\\") != -1) ? User.Identity.Name.Substring(User.Identity.Name.IndexOf("\\") + 1) : User.Identity.Name;
                var currentPlanVersion = planVersion.HasValue && planVersion != 0 ? planVersion.Value : _wpPlanRepository.InsertTemporaryPlan(DateTime.Today.DifferenceBetweenTwoDates(dateTimeStartTO), DateTime.Today.DifferenceBetweenTwoDates(dateTimeEndTO), userName);
                ViewBag.PlanVersion = currentPlanVersion;
                if (dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) <= 10)
                {
                    dateTimeStartTO = dateTimeStartTO.StartOfDay();
                    dateTimeEndTO = dateTimeEndTO.EndOfDay();
                    ViewBag.WorkPackagePlanReportPTO = GetWorkPackagePlanReportPTO(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                    ViewBag.WorkPackagePlanReportOTO = GetWorkPackageReportOTO(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                    ////ViewBag.WorkPackagePlanReportWOUT = GetWorkPackagePlanReportWOUT(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                    ViewBag.WorkPackagePlanReportTOOUT = GetWorkPackagePlanReportTOOUT(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                    ViewBag.WorkPackagePlanReportAT = GetWorkPackagePlanReportAT(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                    ViewBag.WorkPackagePlanReportWDTM = GetWorkPackagePlanReportWDTM(dateTimeStartTO, dateTimeEndTO, wcType, currentTypeReports, currentPlanVersion);
                }
            }
            ViewBag.DateStartTO = dateTimeStartTO.ToShortDateString();
            ViewBag.DateEndTO = dateTimeEndTO.ToShortDateString();
            ViewBag.ВС = wcType;
            ViewBag.ArrayBC = new SelectList(Enum.GetValues(typeof(ACType)).Cast<ACType>()
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = x.ToString(),
                        Selected = acType != null && (x == (ACType)acType ? true : false)
                    };
                }), "Value", "Text");
            ViewBag.TypeReport = typeReport.HasValue ? typeReport.Value : TypeReport.Plan;
            ViewBag.TypeReports = new SelectList(Enum.GetValues(typeof(TypeReport)).Cast<TypeReport>()
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = x.ToString(),
                        Selected = acType != null && (x == (TypeReport)acType ? true : false)
                    };
                }), "Value", "Text");

            return View();
        }

        protected string GetWorkPackagePlanReportWDTM(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion, 
            bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.StationNameField = "stationName";


            //отображение по дням без вертикальных линий по два часа
            /*scheduler.CellDuration = 1440;
            scheduler.CellWidth = 120;
            scheduler.EventHeight = 45;
            scheduler.RowHeaderWidth = 120;
            scheduler.TwoHeaders = false;*/

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;


            scheduler.RowHeaderWidth = 90;
            if (!oneHeader)
                scheduler.TwoHeaders = true;



            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;

            var listResources = new List<Resource>()
            {
                new Resource("Стоянка", "A", new List<string>(){"PL07", "PL070"}, new List<ACType>() {ACType.A330, ACType.A350, ACType.B777, ACType.B737, ACType.A32S, ACType.RRJ}),
                new Resource("Хранение", "B", new List<string>(){"PL09", "PL090"}, new List<ACType>() {ACType.A330, ACType.A350, ACType.B777, ACType.B737, ACType.A32S, ACType.RRJ})
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }

            scheduler.StartDate = dateTimeStartTO;
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion, listResources);

            scheduler.DataSource = dataTable;

            //scheduler.DataSource = GetSchedulerData();
            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                //2 - чтобы влезало
                ReportWidth = scheduler.GridWidth + scheduler.RowHeaderWidth + 5;
                //+ подпись
                ReportHeight = scheduler.GridHeight + 40 + 52/*scheduler.HeaderHeight * 2 + 1*/;
                ReportRowHeadersWeight = scheduler.RowHeaderWidth;
                html = sb.ToString();
            }

            return html;
        }

        protected string GetWorkPackagePlanReportAT(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion,
        bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.StationNameField = "stationName";
            //отображение по дням без вертикальных линий по два часа
            /*scheduler.CellDuration = 1440;
            scheduler.CellWidth = 120;
            scheduler.EventHeight = 45;
            scheduler.RowHeaderWidth = 120;
            scheduler.TwoHeaders = false;*/

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;


            scheduler.RowHeaderWidth = 90;
            if (!oneHeader)
                scheduler.TwoHeaders = true;



            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;

            var listResources = new List<Resource>()
            {
                new Resource("Перрон", "A", new List<string>() {"PL08", "PL080"}, new List<ACType>() {ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777} ),
                new Resource("Ангар", "B", new List<string>(){"PL10", "PL13", "PL100", "PL130"}, new List<ACType>() {ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777} )
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }

            scheduler.StartDate = dateTimeStartTO;
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion, listResources);

            scheduler.DataSource = dataTable;

            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                //2 - чтобы влезало
                ReportWidth = scheduler.GridWidth + scheduler.RowHeaderWidth + 5;
                //+ подпись
                ReportHeight = scheduler.GridHeight + 40 + 52/*scheduler.HeaderHeight * 2 + 1*/;
                ReportRowHeadersWeight = scheduler.RowHeaderWidth;
                html = sb.ToString();
            }

            return html;
        }


        protected string GetWorkPackagePlanReportTOOUT(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion, 
            bool oneHeader =false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.StationNameField = "stationName";
            //отображение по дням без вертикальных линий по два часа
            /*scheduler.CellDuration = 1440;
            scheduler.CellWidth = 120;
            scheduler.EventHeight = 45;
            scheduler.RowHeaderWidth = 120;
            scheduler.TwoHeaders = false;*/

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;


            scheduler.RowHeaderWidth = 90;
            if (!oneHeader)
                scheduler.TwoHeaders = true;



            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;


            var listResources = new List<Resource>()
            {
                new Resource("TO OUT", "A", new List<string>(){"PL05", "PL050"}, new List<ACType>() {ACType.B777, ACType.A330, ACType.B737, ACType.A32S, ACType.RRJ})
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }
            
            scheduler.StartDate = dateTimeStartTO;
            //scheduler.Days = 5;// DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion, listResources);

            scheduler.DataSource = dataTable;

            //scheduler.DataSource = GetSchedulerData();
            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                //2 - чтобы влезало
                ReportWidth = scheduler.GridWidth + scheduler.RowHeaderWidth + 5;
                //+ подпись
                ReportHeight = scheduler.GridHeight + 40 + 52/*scheduler.HeaderHeight * 2 + 1*/;
                html = sb.ToString();
            }

            return html;
        }

        protected string GetWorkPackagePlanReportWOUT(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion, bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            //отображение по дням без вертикальных линий по два часа
            /*scheduler.CellDuration = 1440;
            scheduler.CellWidth = 120;
            scheduler.EventHeight = 45;
            scheduler.RowHeaderWidth = 120;
            scheduler.TwoHeaders = false;*/

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;


            scheduler.RowHeaderWidth = 90;
            if (!oneHeader)
                scheduler.TwoHeaders = true;



            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;

            scheduler.Resources.Add(new Resource("Weekly OUT", "A"));

            scheduler.StartDate = dateTimeStartTO;
            //scheduler.Days = 5;// DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateWOUT(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion);

            scheduler.DataSource = dataTable;

            //scheduler.DataSource = GetSchedulerData();
            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                html = sb.ToString();
            }

            return html;
        }

        protected string GetWorkPackageReportOTO(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion, bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.StationNameField = "stationName";
            //отображение по дням без вертикальных линий по два часа
            /*scheduler.CellDuration = 1440;
            scheduler.CellWidth = 120;
            scheduler.EventHeight = 45;
            scheduler.RowHeaderWidth = 120;
            scheduler.TwoHeaders = false;*/

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;
            
            scheduler.RowHeaderWidth = 90;
            if (!oneHeader)
                scheduler.TwoHeaders = true;


            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;

            var listResources = new List<Resource>()
            {
                new Resource("Ангар", "A", new List<string>(){"PL06", "PL110"}, new List<ACType>() {ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777} ),
                new Resource("Оперативное ТО", "B", new List<string>(){"PL01", "PL010"}, new List<ACType>() {ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777}),
                new Resource("Outsource/<br/>Внебазовое ТО", "D", new List<string>(){"PL04", "PL040"},new List<ACType>() {ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777} )
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }

            

            scheduler.StartDate = dateTimeStartTO;
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion, listResources);

            scheduler.DataSource = dataTable;
            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            


            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                //2 - чтобы влезало
                ReportWidth = scheduler.GridWidth + scheduler.RowHeaderWidth + 5;
                //+ подпись
                ReportHeight = scheduler.GridHeight + 40 + 52/*scheduler.HeaderHeight * 2 + 1*/;
                ReportRowHeadersWeight = scheduler.RowHeaderWidth;
                html = sb.ToString();
            }

            return html;
        }


        protected string GetWorkPackagePlanReportPTO(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, TypeReport typeReports, int planVersion, bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.ArrivalDifDaysField = "arrivalDifDays";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DepartureDifDaysField = "departureDifDays";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.WPNOIField = "wpnoi";
            scheduler.WPNOField = "wpno";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.StationNameField = "stationName";

            //отображение по дням без вертикальных линий по два часа
            //scheduler.CellDuration = 1440;
            //scheduler.CellWidth = 120;
            //scheduler.EventHeight = 45;
            //scheduler.RowHeaderWidth = 120;
            //scheduler.TwoHeaders = false;

            //отображение по два часа с дополнительным заголовком
            scheduler.CellDuration = 120;
            scheduler.CellWidth = 15;
            scheduler.TimeSpendingHeight = 14;
            scheduler.HeightBar = 4;
            scheduler.EventHeight = 50 + scheduler.TimeSpendingHeight;
            scheduler.RowHeaderWidth = 90;
             
            if(!oneHeader)
                scheduler.TwoHeaders = true;


            scheduler.BeforeEventRender += new BeforeEventRenderEventHandler(DayPilotScheduler_BeforeEventRender);
            scheduler.EventClickHandling = DayPilot.Web.Ui.Enums.EventClickHandlingEnum.JavaScript;
            scheduler.TimeRangeSelectedHandling = DayPilot.Web.Ui.Enums.TimeRangeSelectedHandling.Disabled;


            scheduler.CssOnly = true;
            scheduler.CssClassPrefix = "scheduler_transparent";


            scheduler.TimeFormat = DayPilot.Web.Ui.Enums.TimeFormat.Clock24Hours;

            var listResources = new List<Resource>()
            {
                new Resource("C-ch", "A", new List<string>{"PL03", "PL30"}, new List<ACType>{ ACType.RRJ,ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777}),
                new Resource("ПТО силами А-Т", "B", new List<string> { "PL10", "PL100" }, new List<ACType> { ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777}),
                new Resource("ОТО ангар", "C", new List<string> { "PL06", "PL060" }, new List<ACType> { ACType.RRJ, ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777}),
                new Resource("A-ch, AOG", "D", new List<string> { "PL02", "PL020", "PL012", "PL0120" }, new List<ACType> { ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777, ACType.RRJ }),
                new Resource("Мойки ВС", "E", new List<string> { "PL11", "PL110" }, new List<ACType> { ACType.A32S, ACType.B737, ACType.A330, ACType.A350, ACType.B777, ACType.RRJ })
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }

            scheduler.StartDate = dateTimeStartTO;
            //scheduler.Days = 5;// DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, typeReports, planVersion, listResources);

            scheduler.DataSource = dataTable;

            //scheduler.DataSource = GetSchedulerData();
            scheduler.DataBind();

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            using (var htmlWriter = new HtmlTextWriter(sw))
            {
                scheduler.RenderControl(htmlWriter);
                //2 - чтобы влезало
                ReportWidth = scheduler.GridWidth + scheduler.RowHeaderWidth + 5;
                //+ подпись
                ReportHeight = scheduler.GridHeight + 40 + 52/*scheduler.HeaderHeight * 2 + 1*/;
                ReportRowHeadersWeight = scheduler.RowHeaderWidth;
                html = sb.ToString();
            }

            return html;
        }
        
        [HttpGet]
        public FileContentResult ExportWorkPackagePlanReportToPDF(Tab? tab, string dateStart, string dateEnd, ACType? acType, TypeReport? typeReport, int? planVersion)
        {
            //см.: https://stackoverflow.com/questions/50232070/unable-to-generate-readable-pdf-using-itext-7s-htmlconverter-converttodocument
            //см.: https://stackoverflow.com/questions/46707630/how-to-create-a-document-with-unequal-page-sizes-in-itext-7
            byte[] binData = null;
            DateTime.TryParse(dateStart, out DateTime dateTimeStart);
            DateTime.TryParse(dateEnd, out DateTime dateTimeEnd);
            dateTimeStart = dateTimeStart.StartOfDay();
            dateTimeEnd = dateTimeEnd.EndOfDay();
            var currentTypeReports = (TypeReport)(typeReport.HasValue ? typeReport : TypeReport.Plan);
            var html = "";// @"<p>This <em>is </em><span class=""headline"" style=""text-decoration: underline;"">some</span> <strong>sample <em> text</em></strong><span style=""color: red;"">!!!</span></p>";
            var css = Properties.Resources.scheduler_transparent + "@page {margin: 0;}";// @".headline{font-size:200%}";
            var wcType = acType.HasValue ? acType.Value : ACType.All;
            string tabHtmlString;
            var userName = (User.Identity.Name.IndexOf("\\") != -1) ? User.Identity.Name.Substring(User.Identity.Name.IndexOf("\\") + 1) : User.Identity.Name;
            var currentPlanVersion = planVersion.HasValue && planVersion.Value != 0 ? planVersion.Value : _wpPlanRepository.InsertTemporaryPlan(DateTime.Today.DifferenceBetweenTwoDates(dateTimeStart), DateTime.Today.DifferenceBetweenTwoDates(dateTimeEnd), userName);
            //Enum.TryParse(tabString, out Tab tab);
            var signature = string.Empty;
            var placeSignaturePTO = "<div style='float: right;font-size: 0.6em;'>" +
                                 "<span style = 'display:block;'>Согласовано</span>" +
                                 "<span style = 'display:block;'>Начальник службы ПТО ВС</span>" +
                                 "<div style = 'margin-left:35px'>" +
                                 "<span style = 'margin-left:150px'>О.А.Бригадинский </span>" +
                                 "<div>" +
                                 "<span style = 'width: 238px;display: inline-block;border-bottom-color: black;border-bottom: 1px solid black;'>" +
                                 "<span> \"</span>" + "<span style = margin-left:100px> \"</span>" + "</span> " +
                                 "<span style = 'margin-left:40px'>2020</span> " +
                                 "</div>" +
                                 "</div>" +
                                 "</div>";
            var placeSignatureOTO = "<div style='float: right;font-size: 0.6em;'>" +
                                    "<span style = 'display:block;'>Согласовано</span>" +
                                    "<span style = 'display:block;'>Начальник службы ПТО ВС</span>" +
                                    "<div style = 'margin-left:35px'>" +
                                    "<span style = 'margin-left:150px'>А.В.Горячев</span>" +
                                    "<div>" +
                                    "<span style = 'width: 210px;display: inline-block;border-bottom-color: black;border-bottom: 1px solid black;'>" +
                                    "<span> \"</span>" + "<span style = margin-left:100px> \"</span>" + "</span> " +
                                    "<span style = 'margin-left:40px'>2020</span> " +
                                    "</div>" +
                                    "</div>" +
                                    "</div>";
            switch (tab)
            {
                case Tab.PTO:
                    tabHtmlString = GetWorkPackagePlanReportPTO(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    signature = placeSignaturePTO;
                    break;
                case Tab.OTO:
                    tabHtmlString = GetWorkPackageReportOTO(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    signature = placeSignatureOTO;
                    break;
                //case Tab.W_OUT:
                //    tabHtmlString = GetWorkPackagePlanReportWOUT(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion);
                //    break;
                case Tab.TO_OUT:
                    tabHtmlString = GetWorkPackagePlanReportTOOUT(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    break;
                case Tab.A_T:
                    tabHtmlString = GetWorkPackagePlanReportAT(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    break;
                case Tab.WDTM:
                    tabHtmlString = GetWorkPackagePlanReportWDTM(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    break;
                default:
                    tabHtmlString = GetWorkPackagePlanReportPTO(dateTimeStart, dateTimeEnd, wcType, currentTypeReports, currentPlanVersion, true);
                    break;
            }

            

            html = "<html>"
                + "<head>"
                + "<meta http-equiv='content-type' content='application/xhtml+xml; charset=UTF-8;' />"
                + "</head>"
                + "<body><style type='text/css'>" + css + "</style>"
                + "<table style='border-spacing: 0; border-collapse: collapse; width: 100%;'> <tr><td>"
                + tabHtmlString + "</td></tr></table>" + signature
                + "</body></html>";
            binData = CreatePDF(html);
            return File(binData, "application / pdf", "Report" + DateTime.Now.ToString("ddMMyyHHmmss") + ".pdf");
        }

        private byte[] CreateDocument(string html)
        {
            byte[] binData;
            using (var workStream = new MemoryStream())
            {
                using (var pdfWriter = new PdfWriter(workStream))
                {
                    using (var pdfDoc = new PdfDocument(pdfWriter))
                    {
                        //pdfDoc.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A2.Rotate());
                        pdfDoc.SetDefaultPageSize(new PageSize(ReportWidth *0.75f, ReportHeight * 0.75f));
                        ConverterProperties props = new ConverterProperties();
                        using (var document = HtmlConverter.ConvertToDocument(html, pdfDoc, props))
                        {
                            //document.SetFont(PdfFontFactory.GetRegisteredFamilies())
                            //document.SetMargins(0f, 0f, 0f, 0f);
                        }
                    }
                    binData = workStream.ToArray();
                }
            }
            return binData;
        }
        private byte[] CreatePDF(string html)
        {
            byte[] binData;

            //Пиксели в pt = 1*0.75
            //Парамеры: ширина блока ресурсов, ширина 5 дней(влезает в a4), высота с двойной шапкой

            #region Оставить

            using (var workStream = new MemoryStream())
            {
                var pdfWriter = new PdfWriter(workStream);
                var pdf = new PdfDocument(pdfWriter);
                ////создали один pdf документ
                //pdf.SetDefaultPageSize(new PageSize(ReportWidth * 0.75f, ReportHeight * 0.75f));
                ////основной документ
                //HtmlConverter.ConvertToDocument(html, pdf, new ConverterProperties());


                #region Рабочий код для листов после первого
                PdfDocument sourcePdf = new PdfDocument(new PdfReader(new MemoryStream(CreateDocument(html))));
                //Original page
                PdfPage origPage = sourcePdf.GetPage(1);
                PdfFormXObject pageCopy = origPage.CopyAsFormXObject(pdf);
                //Original page size
                Rectangle orig = origPage.GetPageSizeWithRotation();
                
                //Просто сжимание в A4
                Rectangle tileSize = PageSize.A4.Rotate();

                // Transformation matrix//30 пунктов - это 20px справа и слева, 40 * 0,75
                AffineTransform transformationMatrix =  new AffineTransform();
                if (tileSize.GetHeight() / orig.GetHeight() > 0.5)
                    transformationMatrix = AffineTransform.GetScaleInstance((tileSize.GetWidth() -30) / orig.GetWidth(), 0.5);
                else
                    transformationMatrix = AffineTransform.GetScaleInstance((tileSize.GetWidth() -30) / orig.GetWidth(), (tileSize.GetHeight() -30) / orig.GetHeight());
                PdfPage transformPage = pdf.AddNewPage(new PageSize(tileSize));
                PdfCanvas transformCanvas = new PdfCanvas(transformPage);
                transformCanvas.ConcatMatrix(transformationMatrix);
                if(tileSize.GetHeight() / orig.GetHeight() > 0.5)
                //20 пунктов сверху отступ
                    transformCanvas.AddXObject(pageCopy, 30, tileSize.GetHeight() + (tileSize.GetHeight() - orig.GetHeight()) -20);
                else
                    transformCanvas.AddXObject(pageCopy, 30, 20);

                ////Две страницы, на первой 5 дней, на второй все остальные
                ////The first tile
                //var widthFiveDays = 990;
                //Rectangle toMoveFirstPage = new Rectangle(0, 0, widthFiveDays * 0.75f, ReportHeight * 0.75f);
                //PdfPage page = pdf.AddNewPage(PageSize.A4.Rotate());
                //PdfCanvas canvas = new PdfCanvas(page);
                //canvas.Rectangle(0, 0, toMoveFirstPage.GetWidth(), toMoveFirstPage.GetHeight());
                //canvas.Clip();
                //canvas.NewPath();
                ////по x и по Y
                //canvas.AddXObject(pageCopy, 0, -200);

                ////The second tile
                ////100 - чтобы поднять снизу от конца листа
                //Rectangle toMoveSecondPage = new Rectangle((ReportWidth - widthFiveDays) * 0.75f, ReportHeight * 0.75f);
                //page = pdf.AddNewPage(PageSize.A4.Rotate());
                //canvas = new PdfCanvas(page);
                //canvas.Rectangle(0, 0, toMoveSecondPage.GetWidth(), toMoveSecondPage.GetHeight());
                //canvas.Clip();
                //canvas.NewPath();
                //canvas.AddXObject(pageCopy, -widthFiveDays * 0.75f, 0);


                ////Для разбивки по горизонтали
                ////The third tile
                //page = pdf.AddNewPage(PageSize.A4.Rotate());
                //canvas = new PdfCanvas(page);
                //canvas.ConcatMatrix(transformationMatrix);
                //canvas.AddXObject(pageCopy, 0, 0);
                ////The fourth tile
                //page = pdf.AddNewPage(PageSize.A4.Rotate());
                //canvas = new PdfCanvas(page);
                //canvas.ConcatMatrix(transformationMatrix);
                //canvas.AddXObject(pageCopy, -orig.GetWidth() / 2f, 0);
                #endregion
                pdf.Close();
                pdfWriter.Close();
                binData = workStream.ToArray();
            }

            #endregion

            return binData;
        }

        protected (DataRow dr, int counter) CreateRowByPL(DataTable dt, DataRow dr, DateTime start, DateTime dateTimeStartTO, 
            DateTime dateTimeEndTO, string pl, int day, int counter, string resource, ACType acType, TypeReport typeReports, int planVersion)
        {
            if (typeReports == TypeReport.Plan)
            {
                var entriesPlanPL = _wpPlanRepository.GetPlanByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType);
                foreach (var entryPL in entriesPlanPL)
                {
                    dr = dt.NewRow();
                    dr["id"] = counter;
                    //var differenceBetweenTwoStartDates = entryPL.PL_START_DATE.DifferenceBetweenTwoDatesAbs(dateTimeStartTO);
                    var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.PL_START_DATE);
                    var differenceBetweenTwoStartDatesInHours = entryPL.PL_START_DATE.Date.DifferenceBetweenTwoDatesInHours(entryPL.PL_START_DATE);
                    if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                    {
                        dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                        dr["arrival"] = entryPL.Arrival;
                    }
                    else
                    {
                        dr["start"] = entryPL.PL_START_DATE;
                        dr["arrival"] = entryPL.Arrival;
                    }
                    //var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.PL_END_DATE);
                    var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.PL_END_DATE);
                    var differenceBetweenTwoEndDatesInHours = entryPL.PL_END_DATE.Date.DifferenceBetweenTwoDatesInHours(entryPL.PL_END_DATE);
                    if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                    {
                        dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                        dr["departure"] = entryPL.Departure;
                    }
                    else
                    {
                        dr["end"] = entryPL.PL_END_DATE;
                        dr["departure"] = entryPL.Departure;
                    }

                    var mhrPlan = entryPL.MHR != 0 ? $" - {entryPL.MHR} MH" : "";
                    var readiness = entryPL.CONTAINS_C_CHECK ? $"<br/>(готовность {entryPL.PL_END_DATE.ToShortDateString()})" : "";
                    var remarks = !string.IsNullOrEmpty(entryPL.INTERNAL_REMARKS) ? entryPL.INTERNAL_REMARKS + "<br/>" : "";
                    var name = $"{remarks}{entryPL.AC_TYP} {entryPL.AC_REGISTR} {entryPL.Description}{mhrPlan} {readiness}";
                    dr["name"] = name;
                    dr["resource"] = resource;
                    dr["color"] = entryPL.HEXColorACType;
                    dr["wpnoi"] = entryPL.WPNO_I;
                    dr["wpno"] = entryPL.WPNO;
                    dr["mhr"] = entryPL.MHR;
                    dr["arrivalDifDays"] = entryPL.ArrivalDifDays;
                    dr["departureDifDays"] = entryPL.DepartureDifDays;
                    dt.Rows.Add(dr);
                    counter++;
                }
            }
            else
            {
                var entriesFactPL = _wpPlanRepository.GetFactByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType);
                foreach (var entryPL in entriesFactPL)
                {
                    dr = dt.NewRow();
                    dr["id"] = counter;
                    var startDate = entryPL.ACT_START_DATE;
                    var endDate = entryPL.ACT_END_DATE;

                    if (entryPL.ACT_START_DATE == DateTime.MinValue)
                    {
                        startDate = entryPL.PL_START_DATE;
                    }

                    if (entryPL.ACT_END_DATE == DateTime.MinValue)
                    {
                        endDate = entryPL.PL_END_DATE;
                    }

                    var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(startDate);
                    var differenceBetweenTwoStartDatesInHours = startDate.Date.DifferenceBetweenTwoDatesInHours(startDate);
                    if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                    {
                        dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                        dr["arrival"] = entryPL.Arrival;
                    }
                    else
                    {
                        dr["start"] = startDate;
                        dr["arrival"] = entryPL.Arrival;
                    }
                    var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(endDate);
                    var differenceBetweenTwoEndDatesInHours = endDate.Date.DifferenceBetweenTwoDatesInHours(endDate);
                    if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                    {
                        dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                        dr["departure"] = entryPL.Departure;
                    }
                    else
                    {
                        dr["end"] = endDate;
                        dr["departure"] = entryPL.Departure;
                    }
                    var mhrFact = entryPL.BOOKED_MHR != 0 ? $" - {entryPL.BOOKED_MHR} MH" : "";
                    var readiness = entryPL.CONTAINS_C_CHECK ? $"<br/>(готовность {endDate.ToShortDateString()})" : "";
                    var remarks = !string.IsNullOrEmpty(entryPL.INTERNAL_REMARKS) ? entryPL.INTERNAL_REMARKS + "<br/>" : "<br/>";
                    var name = $"{remarks}{entryPL.AC_TYP} {entryPL.AC_REGISTR} {entryPL.Description}{mhrFact} {readiness}";
                    dr["name"] = name;
                    dr["resource"] = resource;
                    dr["color"] = entryPL.HEXColorACType;
                    dr["wpnoi"] = entryPL.WPNO_I;
                    dr["wpno"] = entryPL.WPNO;
                    dr["mhr"] = entryPL.BOOKED_MHR;
                    dr["arrivalDifDays"] = entryPL.ArrivalDifDays;
                    dr["departureDifDays"] = entryPL.DepartureDifDays;
                    dt.Rows.Add(dr);
                    counter++;
                }
            }
            
            return (dr, counter);
        }

        protected void CreateWOUT(DateTime start, DateTime dateTimeStartTO, DateTime dateTimeEndTO, DataRow dr, DataTable dt, int day, ACType acType,
            TypeReport typeReport, int planVersion)
        {
            var counter = 0;
            var createPL04 = CreateRowByPL(dt, dr, start, dateTimeStartTO, dateTimeEndTO, "PL04", day, counter, "A", acType, typeReport, planVersion);
            counter = createPL04.counter;
            dr = createPL04.dr;
            CreateRowByPL(dt, dr, start, dateTimeStartTO, dateTimeEndTO, "PL040", day, counter, "A", acType, typeReport, planVersion);
        }

        //Поубирать лишние параметры
        protected (DataRow dr, int counter) CreateRowByPL(DataTable dt, DataRow dr, DateTime start, DateTime dateTimeStartTO,
            DateTime dateTimeEndTO, int counter, string resource, string resourceName, ACType acType, TypeReport typeReports, List<WPPlanDAO> entriesPL)
        {
            if (typeReports == TypeReport.Plan)
            {
                foreach (var entryPL in entriesPL)
                {
                    dr = dt.NewRow();
                    dr["id"] = counter;
                    var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.PL_START_DATE);
                    var differenceBetweenTwoStartDatesInHours = entryPL.PL_START_DATE.Date.DifferenceBetweenTwoDatesInHours(entryPL.PL_START_DATE);
                    if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                    {
                        dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                        dr["arrival"] = entryPL.Arrival;
                    }
                    else
                    {
                        dr["start"] = entryPL.PL_START_DATE;
                        dr["arrival"] = entryPL.Arrival;
                    }
                    var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.PL_END_DATE);
                    var differenceBetweenTwoEndDatesInHours = entryPL.PL_END_DATE.Date.DifferenceBetweenTwoDatesInHours(entryPL.PL_END_DATE);
                    if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                    {
                        dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                        dr["departure"] = entryPL.Departure;
                    }
                    else
                    {
                        dr["end"] = entryPL.PL_END_DATE;
                        dr["departure"] = entryPL.Departure;
                    }

                    var mhrPlan = entryPL.MHR != 0 ? $" - {entryPL.MHR} MH" : "";
                    var readiness = entryPL.CONTAINS_C_CHECK ? $"<br/>(готовность {entryPL.PL_END_DATE.ToShortDateString()})" : "";
                    var remarks = !string.IsNullOrEmpty(entryPL.INTERNAL_REMARKS) ? entryPL.INTERNAL_REMARKS + "<br/>" : "";
                    var name = $"{remarks}{entryPL.AC_MODEL} {entryPL.AC_REGISTR} {entryPL.Description}{mhrPlan} {readiness}";
                    dr["name"] = name;
                    dr["resource"] = resource;
                    dr["color"] = entryPL.HEXColorACType;
                    dr["wpnoi"] = entryPL.WPNO_I;
                    dr["wpno"] = entryPL.WPNO;
                    dr["mhr"] = entryPL.MHR;
                    dr["bookedMHR"] = entryPL.BOOKED_MHR;
                    dr["arrivalDifDays"] = entryPL.ArrivalDifDays;
                    dr["departureDifDays"] = entryPL.DepartureDifDays;
                    dr["resourceName"] = resourceName;
                    dr["acType"] = entryPL.AC_TYP;
                    dr["stationName"] = entryPL.STATION_NAME;
                    dt.Rows.Add(dr);
                    counter++;
                }
            }
            else
            {
                foreach (var entryPL in entriesPL)
                {
                    dr = dt.NewRow();
                    dr["id"] = counter;
                    var startDate = entryPL.ACT_START_DATE;
                    var endDate = entryPL.ACT_END_DATE;

                    if (entryPL.ACT_START_DATE == DateTime.MinValue)
                    {
                        startDate = entryPL.PL_START_DATE;
                    }

                    if (entryPL.ACT_END_DATE == DateTime.MinValue)
                    {
                        endDate = entryPL.PL_END_DATE;
                    }

                    var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(startDate);
                    var differenceBetweenTwoStartDatesInHours = startDate.Date.DifferenceBetweenTwoDatesInHours(startDate);
                    if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                    {
                        dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                        dr["arrival"] = entryPL.Arrival;
                    }
                    else
                    {
                        dr["start"] = startDate;
                        dr["arrival"] = entryPL.Arrival;
                    }
                    var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(endDate);
                    var differenceBetweenTwoEndDatesInHours = endDate.Date.DifferenceBetweenTwoDatesInHours(endDate);
                    if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                    {
                        dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                        dr["departure"] = entryPL.Departure;
                    }
                    else
                    {
                        dr["end"] = endDate;
                        dr["departure"] = entryPL.Departure;
                    }
                    var mhrFact = entryPL.BOOKED_MHR != 0 ? $" - {entryPL.BOOKED_MHR} MH" : "";
                    var readiness = entryPL.CONTAINS_C_CHECK ? $"<br/>(готовность {endDate.ToShortDateString()})" : "";
                    var remarks = !string.IsNullOrEmpty(entryPL.INTERNAL_REMARKS) ? entryPL.INTERNAL_REMARKS + "<br/>" : "<br/>";
                    var name = $"{remarks}{entryPL.AC_MODEL} {entryPL.AC_REGISTR} {entryPL.Description}{mhrFact} {readiness}";
                    dr["name"] = name;
                    dr["resource"] = resource;
                    dr["color"] = entryPL.HEXColorACType;
                    dr["wpnoi"] = entryPL.WPNO_I;
                    dr["wpno"] = entryPL.WPNO;
                    dr["mhr"] = entryPL.BOOKED_MHR;
                    dr["bookedMHR"] = entryPL.BOOKED_MHR;
                    dr["arrivalDifDays"] = entryPL.ArrivalDifDays;
                    dr["departureDifDays"] = entryPL.DepartureDifDays;
                    dr["resourceName"] = resourceName;
                    dr["acType"] = entryPL.AC_TYP;
                    dr["stationName"] = entryPL.STATION_NAME;
                    dt.Rows.Add(dr);
                    counter++;
                }
            }
            return (dr, counter);
        }

        protected List<WPPlanDAO> SortResourcesByWC(List<WPPlanDAO> entriesPL, List<ACType> sortWC, TypeReport typeReport)
        {
            var tmpWpPlan = new List<WPPlanDAO>();
            //по каждому критерию сортировки
            foreach (var sort in sortWC)
            {

                if (typeReport == TypeReport.Plan)
                    tmpWpPlan.AddRange(entriesPL.Where(x => x.AC_TYP == sort).OrderByDescending(x=>x.PL_START_DATE));
                else
                    tmpWpPlan.AddRange(entriesPL.Where(x => x.AC_TYP == sort).OrderByDescending(x => x.ACT_START_DATE));
            }
            return tmpWpPlan;
        }

        protected void CreateDataTable(DateTime start, DateTime dateTimeStartTO, DateTime dateTimeEndTO, DataRow dr, DataTable dt, int day, ACType acType,
            TypeReport typeReport, int planVersion, List<Resource> resources)
        {
            var counter = 0;
            //По каждому ресурсу
            foreach (var resource in resources)
            {
                var entriesPL = new List<WPPlanDAO>();
                foreach (var pl in resource.PL)
                {
                    if(typeReport == TypeReport.Plan)
                        entriesPL.AddRange(_wpPlanRepository.GetPlanByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType));
                    else
                        entriesPL.AddRange(_wpPlanRepository.GetFactByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType));
                }
                //Сортировка в ресурсе
                entriesPL = SortResourcesByWC(entriesPL, resource.Filter, typeReport);

                var createResource = CreateRowByPL(dt, dr, start, dateTimeStartTO,
                    dateTimeEndTO, counter, resource.Value, resource.Name, acType, typeReport, entriesPL);
                counter = createResource.counter;
                dr = createResource.dr;
            }
        }

        protected DataTable CreateDataTableColumns()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("start", typeof(DateTime));
            dt.Columns.Add("arrival", typeof(DateTime));
            dt.Columns.Add("arrivalDifDays", typeof(string));
            dt.Columns.Add("end", typeof(DateTime));
            dt.Columns.Add("departure", typeof(DateTime));
            dt.Columns.Add("departureDifDays", typeof(string));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(string));
            dt.Columns.Add("resource", typeof(string));
            dt.Columns.Add("color", typeof(string));
            dt.Columns.Add("wpno", typeof(string));
            dt.Columns.Add("wpnoi", typeof(Int64));
            dt.Columns.Add("mhr", typeof(string));
            dt.Columns.Add("bookedMHR", typeof(string));
            dt.Columns.Add("stationName", typeof(string));
            dt.Columns.Add("resourceName", typeof(string));
            dt.Columns.Add("acType", typeof(ACType));
            return dt;
        }

        protected void DayPilotScheduler_BeforeEventRender(object sender, BeforeEventRenderEventArgs e)
        {
            string color = e.DataItem["color"] as string;
            if (!String.IsNullOrEmpty(color))
            {
                e.DurationBarColor = color;
            }
        }
    }
}