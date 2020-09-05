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
        public ActionResult WorkPackagePlanReport(string dateStartTO, string dateEndTO, ACType? acType, /*TypeReport? typeReport,*/
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
            //var currentTypeReports = (TypeReport) (typeReport.HasValue ? typeReport : TypeReport.Plan);
            DateTime.TryParse(dateStartTO, out DateTime dateTimeStartTO);
            DateTime.TryParse(dateEndTO, out DateTime dateTimeEndTO);
            var wcType = acType.HasValue ? acType.Value : ACType.All;
            
            ViewBag.CreatePlan = createPlan.HasValue && createPlan.Value;
            ViewBag.PlanVersion = null;
            if (createPlan == true || (planVersion.HasValue && planVersion != 0))
            {
                //var userName = (User.Identity.Name.IndexOf("\\") != -1) ? User.Identity.Name.Substring(User.Identity.Name.IndexOf("\\") + 1) : User.Identity.Name;
                //var currentPlanVersion = planVersion.HasValue && planVersion != 0 ? planVersion.Value : _wpPlanRepository.InsertTemporaryPlan(DateTime.Today.DifferenceBetweenTwoDates(dateTimeStartTO), DateTime.Today.DifferenceBetweenTwoDates(dateTimeEndTO), userName);
                //ViewBag.PlanVersion = currentPlanVersion;
                if (dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) <= 10)
                {
                    dateTimeStartTO = dateTimeStartTO.StartOfDay();
                    dateTimeEndTO = dateTimeEndTO.EndOfDay();
                    ViewBag.WorkPackagePlanReportPTO = GetWorkPlanReport(dateTimeStartTO, dateTimeEndTO, wcType /*currentTypeReports,*/ /*currentPlanVersion*/);
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
            //ViewBag.TypeReport = typeReport.HasValue ? typeReport.Value : TypeReport.Plan;
            //ViewBag.TypeReports = new SelectList(Enum.GetValues(typeof(TypeReport)).Cast<TypeReport>()
            //    .Select(x =>
            //    {
            //        return new SelectListItem
            //        {
            //            Text = x.GetAttributeOfType<DisplayAttribute>().Name,
            //            Value = x.ToString(),
            //            Selected = acType != null && (x == (TypeReport)acType ? true : false)
            //        };
            //    }), "Value", "Text");

            return View();
        }

        protected string GetWorkPlanReport(DateTime dateTimeStartTO, DateTime dateTimeEndTO, ACType acType, /*TypeReport typeReports,*/ /*int planVersion,*/ bool oneHeader = false)
        {
            string html = "";

            DayPilotScheduler scheduler = new DayPilotScheduler();

            scheduler.HeaderFontSize = "11px";
            scheduler.HeaderHeight = 16;
            scheduler.DataStartField = "start";
            scheduler.ArrivalField = "arrival";
            scheduler.DataEndField = "end";
            scheduler.DepartureField = "departure";
            scheduler.DataTextField = "name";
            scheduler.DataValueField = "id";
            scheduler.DataResourceField = "resource";
            scheduler.EventFontSize = "10px";
            scheduler.MHRField = "mhr";
            scheduler.BookedMHRField = "bookedMHR";
            scheduler.DataResourceNameField = "resourceName";
            scheduler.ACTypeField = "acType";
            scheduler.WorkTypeField = "workType";
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


            var entriesHome = new List<WPPlanDAO>()
            {
                new WPPlanDAO(){Arrival = new DateTime(2020, 08, 31, 16, 00, 00), Departure = new DateTime(2020, 08, 31, 20, 00, 00), WorkType = WorkType.Optional, Description = "Готовка еды", StartDate = new DateTime(2020, 08, 31, 20, 00, 00),
                    EndDate = new DateTime(2020, 08, 31, 23, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Optional, Description = "Мойка посуды", StartDate = new DateTime(2020, 08, 31, 22, 00, 00), EndDate = new DateTime(2020, 08, 31, 23, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Sleep, Description = "Сон", StartDate = new DateTime(2020, 09, 01, 02, 00, 00), EndDate = new DateTime(2020, 09, 01, 10, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Sleep, Description = "Сон", StartDate = new DateTime(2020, 09, 02, 02, 00, 00), EndDate = new DateTime(2020, 09, 02, 10, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Sleep, Description = "Сон", StartDate = new DateTime(2020, 09, 03, 02, 00, 00), EndDate = new DateTime(2020, 09, 03, 10, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Sleep, Description = "Сон", StartDate = new DateTime(2020, 09, 04, 02, 00, 00), EndDate = new DateTime(2020, 09, 04, 10, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Sleep, Description = "Сон", StartDate = new DateTime(2020, 09, 05, 02, 00, 00), EndDate = new DateTime(2020, 09, 05, 10, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Задача 1 в блокноте", StartDate = new DateTime(2020, 09, 01, 10, 00, 00), EndDate = new DateTime(2020, 09, 01, 12, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Задача 2 в блокноте", StartDate = new DateTime(2020, 09, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 01, 20, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Подготовка к собеседованиям", StartDate = new DateTime(2020, 08, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 20, 20, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Переделать проект", StartDate = new DateTime(2020, 08, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 03, 20, 00, 00)}
            };

            var entriesWork = new List<WPPlanDAO>()
            {
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Задача 789", StartDate = new DateTime(2020, 09, 01, 10, 00, 00), EndDate = new DateTime(2020, 09, 01, 12, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Задача 700", StartDate = new DateTime(2020, 09, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 01, 20, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Primary, Description = "Задача 56", StartDate = new DateTime(2020, 08, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 20, 20, 00, 00)},
                new WPPlanDAO(){WorkType = WorkType.Optional, Description = "Задача 53", StartDate = new DateTime(2020, 08, 01, 12, 00, 00), EndDate = new DateTime(2020, 09, 03, 20, 00, 00)}
            };

            var listResources = new List<Resource>()
            {
                new Resource("Дом", "A", entriesHome,  new List<WorkType> {WorkType.Sleep, WorkType.Primary, WorkType.Optional}),
                new Resource("Работа", "B", entriesWork, new List<WorkType> {WorkType.Sleep, WorkType.Optional, WorkType.Primary}),
            };
            foreach (var resource in listResources)
            {
                scheduler.Resources.Add(resource);
            }

            scheduler.StartDate = dateTimeStartTO;
            //scheduler.Days = 5;// DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            scheduler.Days = (int)dateTimeStartTO.DifferenceBetweenTwoDatesAbs(dateTimeEndTO) + 1;

            var dataTable = CreateDataTableColumns();
            CreateDataTable(scheduler.StartDate, dateTimeStartTO, dateTimeEndTO, null, dataTable, scheduler.Days, acType, /*typeReports, planVersion,*/ listResources);

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
                ReportHeight = scheduler.GridHeight + 40 + 52;
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
            //var currentTypeReports = (TypeReport)(typeReport.HasValue ? typeReport : TypeReport.Plan);
            //var html = "";
            var css = Properties.Resources.scheduler_transparent + "@page {margin: 0;}";// @".headline{font-size:200%}";
            var wcType = acType.HasValue ? acType.Value : ACType.All;
            string tabHtmlString;
            //var userName = (User.Identity.Name.IndexOf("\\") != -1) ? User.Identity.Name.Substring(User.Identity.Name.IndexOf("\\") + 1) : User.Identity.Name;
            //var currentPlanVersion = planVersion.HasValue && planVersion.Value != 0 ? planVersion.Value : _wpPlanRepository.InsertTemporaryPlan(DateTime.Today.DifferenceBetweenTwoDates(dateTimeStart), DateTime.Today.DifferenceBetweenTwoDates(dateTimeEnd), userName);
            
            switch (tab)
            {
                case Tab.PTO:
                    tabHtmlString = GetWorkPlanReport(dateTimeStart, dateTimeEnd, wcType, /*currentTypeReports, currentPlanVersion,*/ true);
                    break;
                default:
                    tabHtmlString = GetWorkPlanReport(dateTimeStart, dateTimeEnd, wcType, /*currentTypeReports, currentPlanVersion,*/ true);
                    break;
            }
            var html = "<html>"
                + "<head>"
                + "<meta http-equiv='content-type' content='application/xhtml+xml; charset=UTF-8;' />"
                + "</head>"
                + "<body><style type='text/css'>" + css + "</style>"
                + "<table style='border-spacing: 0; border-collapse: collapse; width: 100%;'> <tr><td>"
                + tabHtmlString + "</td></tr></table>" + "</body></html>";
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
            using (var workStream = new MemoryStream())
            {
                var pdfWriter = new PdfWriter(workStream);
                var pdf = new PdfDocument(pdfWriter);
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
                pdf.Close();
                pdfWriter.Close();
                binData = workStream.ToArray();
            }

            return binData;
        }

        protected (DataRow dr, int counter) CreateRowByPL(DataTable dt, DataRow dr, DateTime start, DateTime dateTimeStartTO,
            DateTime dateTimeEndTO, int counter, string resource, string resourceName, ACType acType, TypeReport typeReports, List<WPPlanDAO> entriesPL)
        {
            if (typeReports == TypeReport.Plan)
            {
                foreach (var entryPL in entriesPL)
                {
                    dr = dt.NewRow();
                    dr["id"] = counter;
                    var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.StartDate);
                    var differenceBetweenTwoStartDatesInHours = entryPL.StartDate.Date.DifferenceBetweenTwoDatesInHours(entryPL.StartDate);
                    if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                    {
                        dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                        dr["arrival"] = entryPL.Arrival;
                    }
                    else
                    {
                        dr["start"] = entryPL.StartDate;
                        dr["arrival"] = entryPL.Arrival;
                    }
                    var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(entryPL.EndDate);
                    var differenceBetweenTwoEndDatesInHours = entryPL.EndDate.Date.DifferenceBetweenTwoDatesInHours(entryPL.EndDate);
                    if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                    {
                        dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                        dr["departure"] = entryPL.Departure;
                    }
                    else
                    {
                        dr["end"] = entryPL.EndDate;
                        dr["departure"] = entryPL.Departure;
                    }
                    dr["name"] = entryPL.Description;
                    dr["resource"] = resource;
                    dr["color"] = entryPL.HEXColorACType;
                    dr["wpnoi"] = entryPL.WPNO_I;
                    dr["wpno"] = entryPL.WPNO;
                    dr["mhr"] = entryPL.MHR;
                    dr["bookedMHR"] = entryPL.BOOKED_MHR;
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
                    var startDate = entryPL.FactStartDate;
                    var endDate = entryPL.FactEndDate;

                    if (entryPL.FactStartDate == DateTime.MinValue)
                    {
                        startDate = entryPL.StartDate;
                    }

                    if (entryPL.FactEndDate == DateTime.MinValue)
                    {
                        endDate = entryPL.EndDate;
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
                    dr["resourceName"] = resourceName;
                    dr["acType"] = entryPL.AC_TYP;
                    dr["stationName"] = entryPL.STATION_NAME;
                    dt.Rows.Add(dr);
                    counter++;
                }
            }
            return (dr, counter);
        }
        

        protected (DataRow dr, int counter) CreateRow(DataTable dt, DataRow dr, DateTime start, DateTime dateTimeStartTO,
            DateTime dateTimeEndTO, int counter, string resource, string resourceName, List<WPPlanDAO> entries)
        {
            foreach (var entry in entries)
            {
                dr = dt.NewRow();
                dr["id"] = counter;
                var differenceBetweenTwoStartDates = dateTimeStartTO.DifferenceBetweenTwoDates(entry.StartDate);
                var differenceBetweenTwoStartDatesInHours = entry.StartDate.Date.DifferenceBetweenTwoDatesInHours(entry.StartDate);
                if (differenceBetweenTwoStartDates >= 0 && differenceBetweenTwoStartDatesInHours >= 0)
                {
                    dr["start"] = start.AddDays(differenceBetweenTwoStartDates).AddHours(differenceBetweenTwoStartDatesInHours);
                    dr["arrival"] = entry.Arrival;
                }
                else
                {
                    dr["start"] = entry.StartDate;
                    dr["arrival"] = entry.Arrival;
                }
                var differenceBetweenTwoEndDates = dateTimeStartTO.DifferenceBetweenTwoDates(entry.EndDate);
                var differenceBetweenTwoEndDatesInHours = entry.EndDate.Date.DifferenceBetweenTwoDatesInHours(entry.EndDate);
                if (differenceBetweenTwoEndDates >= 0 && differenceBetweenTwoEndDatesInHours >= 0)
                {
                    dr["end"] = start.AddDays(differenceBetweenTwoEndDates).AddHours(differenceBetweenTwoEndDatesInHours);
                    dr["departure"] = entry.Departure;
                }
                else
                {
                    dr["end"] = entry.EndDate;
                    dr["departure"] = entry.Departure;
                }
                dr["name"] = entry.Description;
                dr["resource"] = resource;
                dr["color"] = entry.HEXColorWorkColor;
                dr["mhr"] = entry.MHR;
                dr["bookedMHR"] = entry.BOOKED_MHR;
                dr["resourceName"] = resourceName;
                dr["acType"] = entry.AC_TYP;
                dr["workType"] = entry.WorkType;
                dr["stationName"] = entry.STATION_NAME;
                dt.Rows.Add(dr);
                counter++;
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
                    tmpWpPlan.AddRange(entriesPL.Where(x => x.AC_TYP == sort).OrderByDescending(x=>x.StartDate));
                else
                    tmpWpPlan.AddRange(entriesPL.Where(x => x.AC_TYP == sort).OrderByDescending(x => x.FactStartDate));
            }
            return tmpWpPlan;
        }

        protected List<WPPlanDAO> SortResourcesByWorkType(List<WPPlanDAO> entriesPL, List<WorkType> workTypes)
        {
            var workPlans = new List<WPPlanDAO>();
            //по каждому критерию сортировки
            foreach (var sort in workTypes)
            {
                workPlans.AddRange(entriesPL.Where(x => x.WorkType == sort).OrderBy(x => x.StartDate));
            }
            return workPlans;
        }


        protected void CreateDataTable(DateTime start, DateTime dateTimeStartTO, DateTime dateTimeEndTO, DataRow dr, DataTable dt, int day,
            ACType acType,
            /*TypeReport typeReport, int planVersion,*/ List<Resource> resources)
        {
            var counter = 0;
            //По каждому ресурсу
            foreach (var resource in resources)
            {
                //var entriesPL = new List<WPPlanDAO>();
                //if (resource.PL != null)
                //{
                //    foreach (var pl in resource.PL)
                //    {
                //        if (typeReport == TypeReport.Plan)
                //            entriesPL.AddRange(_wpPlanRepository.GetPlanByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType));
                //        else
                //            entriesPL.AddRange(_wpPlanRepository.GetFactByPLByDates(planVersion, pl, dateTimeStartTO, dateTimeEndTO, acType));
                //    }
                //    //Сортировка в ресурсе
                //    entriesPL = SortResourcesByWC(entriesPL, resource.Filter, typeReport);

                //    var createResource = CreateRowByPL(dt, dr, start, dateTimeStartTO,
                //        dateTimeEndTO, counter, resource.Value, resource.Name, acType, typeReport, entriesPL);
                //    counter = createResource.counter;
                //    dr = createResource.dr;
                //} //Код для вставки моего плана!!!
                //else
                //{
                    var entries = SortResourcesByWorkType(resource.Entries, resource.WorkTypes);
                    var createResource = CreateRow(dt, dr, start, dateTimeStartTO, dateTimeEndTO, counter, resource.Value, resource.Name, entries);
                    counter = createResource.counter;
                    dr = createResource.dr;
                //}
            }
        }

        protected DataTable CreateDataTableColumns()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("start", typeof(DateTime));
            dt.Columns.Add("arrival", typeof(DateTime));
            dt.Columns.Add("end", typeof(DateTime));
            dt.Columns.Add("departure", typeof(DateTime));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(string));
            dt.Columns.Add("resource", typeof(string));
            dt.Columns.Add("color", typeof(string));
            dt.Columns.Add("mhr", typeof(string));
            dt.Columns.Add("bookedMHR", typeof(string));
            dt.Columns.Add("stationName", typeof(string));
            dt.Columns.Add("resourceName", typeof(string));
            dt.Columns.Add("acType", typeof(ACType));
            dt.Columns.Add("workType", typeof(WorkType));
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