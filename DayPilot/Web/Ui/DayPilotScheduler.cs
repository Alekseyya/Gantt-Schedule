/*
Copyright © 2005 - 2016 Annpoint, s.r.o.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

-------------------------------------------------------------------------

NOTE: Reuse requires the following acknowledgement (see also NOTICE):
This product includes DayPilot (http://www.daypilot.org) developed by Annpoint, s.r.o.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Core.Extensions;
using Core.Model;
using DayPilot.Utils;
using DayPilot.Web.Ui.Ajax;
using DayPilot.Web.Ui.Data;
using DayPilot.Web.Ui.Enums;
using DayPilot.Web.Ui.Enums.Scheduler;
using DayPilot.Web.Ui.Events;
using DayPilot.Web.Ui.Events.Scheduler;

namespace DayPilot.Web.Ui
{
    /// <summary>
    /// Calendar/scheduler control with hours on the horizontal axis and resources on the vertical axis.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Themeable(true)]
    [PersistChildren(false)]
    [ParseChildren(true)]
    [DefaultProperty(null)]
    [ToolboxBitmap(typeof(Calendar))]
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal), AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public class DayPilotScheduler : DataBoundControl, IPostBackEventHandler
    {
        private List<Day> _days;

        private string _dataStartField;
        private string _arrivalField;
        //private string _arrivalDifDaysField;
        private string _dataEndField;
        private string _departureField;
        //private string _departureDifDaysField;
        private string _dataTextField;
        private string _dataValueField;
        private string _dataResourceField;
        private string _dataResourceNameField;
        private string _acTypeField;
        private string _workTypeField;
        private string _wpnoiField;
        private string _wpnoField;
        private string _mhrField;
        private string _bookedMHRField;
        private string _stationNameField;

        internal List<int> RowHeaderColumns
        {
            get
            {
                List<int> result = new List<int>();
                foreach (RowHeaderColumn r in HeaderColumns)
                {
                    result.Add(r.Width);
                }
                return result;
            }  

        }

        private List<string> RowHeaderTitles
        {
            get { 
                List<string> result = new List<string>();
                foreach (RowHeaderColumn r in HeaderColumns)
                {
                    result.Add(r.Title);
                }
                return result;
            }  
        }

        /// <summary>
        /// Sum of column widths defined in RowHeaderColumnWidths property.
        /// </summary>
        private int RowHeaderColumnWidthTotal
        {
            get { 
                int total = 0;
                foreach(int w in RowHeaderColumns)
                {
                    total += w;
                }
                return total;
            }
        }

        /// <summary>
        /// Event called when the user clicks an event in the calendar. It's only called when EventClickHandling is set to PostBack.
        /// </summary>
        [Category("User actions")]
        [Description("Event called when the user clicks an event in the calendar.")]
        public event EventClickEventHandler EventClick;

        /// <summary>
        /// Event called when the user clicks a free space in the calendar. It's only called when TimeRangeSelectedHandling is set to PostBack.
        /// </summary>
        [Category("User actions")]
        [Description("Event called when the user clicks a free space in the calendar.")]
        public event TimeRangeSelectedEventHandler TimeRangeSelected;

        [Category("User actions")]
        [Description("Event called when the user changes row header column width.")]
        public event RowHeaderColumnWidthChangedEventHandler HeaderColumnWidthChanged;

        /// <summary>
        /// Use this event to modify event properties before rendering.
        /// </summary>
        [Category("Preprocessing")]
        [Description("Use this event to modify event properties before rendering.")]
        public event BeforeEventRenderEventHandler BeforeEventRender;

        /// <summary>
        /// Use this event to modify resource header (Y axis) properties before rendering.
        /// </summary>
        [Category("Rendering")]
        [Description("Use this event to modify resource header (Y axis) properties before rendering.")]
        public event BeforeResHeaderRenderEventHandler BeforeResHeaderRender;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //RowHeaderWidth = RowHeaderColumnWidthTotal;

            string themes = Page.ClientScript.GetWebResourceUrl(typeof(DayPilotCalendar), "DayPilot.Resources.Common.js");
            ScriptManagerHelper.RegisterClientScriptInclude(this, typeof(DayPilotCalendar), "Common.js", themes);

            string splitter = Page.ClientScript.GetWebResourceUrl(typeof(DayPilotScheduler), "DayPilot.Resources.Splitter.js");
            ScriptManagerHelper.RegisterClientScriptInclude(this, typeof(DayPilotScheduler), "Splitter.js", splitter);


        }


        /// <summary>
        /// Renders the component HTML code.
        /// </summary>
        /// <param name="output"></param>
        protected override void Render(HtmlTextWriter output)
        {
            LoadEventsToDays();
            RenderMainTable(output);

            if (RowHeaderColumns.Count > 1)
            {
                ScriptManagerHelper.RegisterStartupScript(this, typeof(DayPilotScheduler), ClientID + "object", JsInitCode(), false);
            }
        }
        public void RenderCore(HtmlTextWriter output)
        {
            LoadEventsToDays();
            RenderMainTable(output);

            if (RowHeaderColumns.Count > 1)
            {
                ScriptManagerHelper.RegisterStartupScript(this, typeof(DayPilotScheduler), ClientID + "object", JsInitCode(), false);
            }
        }


        private string JsInitCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine(String.Format("/* DayPilotLite: {0} */", Assembly.GetExecutingAssembly().GetName().Version));
            sb.AppendLine("(function() {");
            sb.AppendFormat("var splitter = new DayPilot.Splitter('{0}');", ClientID + "_corner"); sb.AppendLine();
            sb.Append("splitter.widths = [");
            for (int i = 0; i < RowHeaderColumns.Count; i++)
            {
                bool last = i == RowHeaderColumns.Count - 1;
                sb.Append(RowHeaderColumns[i]);
                if (!last)
                {
                    sb.Append(",");
                }
            }
            sb.AppendLine("];");
            sb.Append("splitter.titles = [");
            for (int i = 0; i < RowHeaderTitles.Count; i++)
            {
                bool last = i == RowHeaderTitles.Count - 1;
                sb.Append("'");
                sb.Append(RowHeaderTitles[i]);
                sb.Append("'");
                if (!last)
                {
                    sb.Append(",");
                }
            }
            sb.AppendLine("];");
            sb.AppendLine("splitter.updated = function(col) { __doPostBack('" + UniqueID + "', 'COL:' + this.widths.join(',')); };");
            sb.AppendLine("splitter.color = '" + ColorTranslator.ToHtml(BorderColor) + "';");
            sb.AppendLine("splitter.opacity = 30;");
            sb.AppendLine("splitter.height = " + (HeaderHeight - 1) + ";");
            sb.AppendLine("splitter.Init();");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            // make sure __doPostBack is available
            Page.ClientScript.GetPostBackEventReference(this, "");

            return sb.ToString();
        }

        private string PrefixCssClass(string name)
        {
            if (String.IsNullOrEmpty(Theme))
            {
                return String.Empty;
            }
            return Theme + name;
        }


        private void RenderMainTable(HtmlTextWriter output)
        {
            output.AddAttribute("id", ClientID);
            output.AddStyleAttribute("width", Width + "px");
            output.AddStyleAttribute("-khtml-user-select", "none");
            output.AddStyleAttribute("-webkit-user-select", "none");
            output.AddStyleAttribute("-moz-user-select", "none");
            output.AddStyleAttribute("user-select", "none");
            if (CssOnly)
            {
                output.AddStyleAttribute("position", "relative");
                output.AddAttribute("class", PrefixCssClass("_main"));
            }
            else
            {
                output.AddStyleAttribute("line-height", "1.2");
            }
            output.RenderBeginTag("div");

            if (TwoHeaders)
                RenderTwoHeadersBackground(output);
            else
            {
                if (CssOnly)
                {
                    CalculateGridHeight();
                    RenderDividers(output);
                    RenderMatrixLines(output);
                    RenderEvents(output);
                }

                output.AddAttribute("cellspacing", "0");
                output.AddAttribute("cellpadding", "0");
                output.AddAttribute("border", "0");
                if (!CssOnly)
                {
                    output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
                }
                output.RenderBeginTag("table");
                output.RenderBeginTag("tr");
                output.AddStyleAttribute("width", (RowHeaderWidthResolved) + "px");
                output.RenderBeginTag("td");
                RenderCorner(output);
                output.RenderEndTag(); // td
                RenderHeaderColsMonth(output);
                output.RenderEndTag(); // tr
            }


            // SOUTH
            RenderRows(output);

            output.RenderEndTag(); // table


            /*if (CssOnly)
            {
                RenderDividers(output);
                RenderMatrixLines(output);
                RenderEvents(output);
            }*/

            output.RenderEndTag(); // main
        }

        private void CalculateGridHeight()
        {
            ListDividersResources = new List<int>();

            int top = HeaderHeight;
            for (int i = 0; i < _days.Count; i++)
            {
                var countEvent = 0;
                Bottom = 0;
                Day d = _days[i];
                //отрисовка ресурса
                PreviousEventColumnNumber = -1;
                var orderEvents = OrderEventsByFilterWC(d);
                foreach (Event ep in orderEvents)
                {
                    if (ep.Column.Number > 0)
                    {
                        //-1 типа предыдущий
                        CalculateResources(d, ep, d.events.Where(x => x.Column.Number == ep.Column.Number - 1).ToList());
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                    else
                    {
                        Top = 0;
                        CalculateResources(d, ep, null);
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                }

                if (Bottom == 0)
                    Bottom = d.Name.CalculateHeightToPixels(Convert.ToInt32(EventFontSize.Replace("px", "")), 1.5);
                ListDividersResources.Add(Bottom);
                top += Bottom;
            }
        }

        /// <summary>
        /// Сортировка Event с условиями фильтра для каждой вкладки
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private List<Event> OrderEventsByFilterWC(Day d)
        {
            var orderEvents = d.events.OrderBy(x => x.Column.Number);

            for (int j = 0; j < orderEvents.GroupBy(x => x.Column.Number).Select(x => x.First()).Select(x => x.Column.Number).ToList().Count; j++)
            {
                var first = orderEvents.FirstOrDefault(x => x.Column.Number == j);
                if (orderEvents.Where(x => x.Column.Number == j).All(x => first != null && x.ACType == first.ACType))
                    continue;
                else
                {
                    var columnPreviousCollection = orderEvents.First(x => x.Column.Number == j - 1);
                    var numberCollection = orderEvents.Where(x => x.Column.Number == j);
                    //Перед тем как переводить в следующий Column записи не подходящие по ВС(фильтр по ВС)
                    //Надо следующие за ними инкрементировать. Они находятся в группах, по этому надо делать Distinct(т.е номер блока надо менять)
                    foreach (var entryEvent in orderEvents.Where(x => x.Column.Number >= (j + 1)).GroupBy(x => x.Column.Number).Select(x => x.First()))
                    {
                        entryEvent.Column.SetValue(entryEvent.Column.Number + 1);
                    }

                    //Изменить номер column для тех event, которые имеют тип ВС другой.
                    //На следущий column, чтобы типы ВС были в разных колонках
                    foreach (var entryEvent in numberCollection.Where(x => x.ACType != columnPreviousCollection.ACType))
                    {
                        entryEvent.Column.SetValue(j + 1);
                    }
                    break;
                }
            }
            return orderEvents.OrderBy(x => x.Column.Number).ToList();
        }

        private void CalculateTwoHeaderGridHeight()
        {
            ListDividersResources = new List<int>();

            int top = (HeaderHeight * 2);
            for (int i = 0; i < _days.Count; i++)
            {
                var countEvent = 0;
                Bottom = 0;
                Day d = _days[i];
                //отрисовка ресурса
                PreviousEventColumnNumber = -1;

                var orderEvents = OrderEventsByFilterWC(d);
                
                foreach (Event ep in orderEvents)
                {
                    if (ep.Column.Number > 0)
                    {
                        //-1 типа предыдущий
                        CalculateResources(d, ep, d.events.Where(x => x.Column.Number == ep.Column.Number - 1).ToList());
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                    else
                    {
                        Top = 0;
                        CalculateResources(d, ep, null);
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                }

                if (Bottom == 0)
                    Bottom = d.Name.CalculateHeightToPixels(Convert.ToInt32(EventFontSize.Replace("px", "")), 1.5);
                ListDividersResources.Add(Bottom);
                top += Bottom;
            }
        }

        private void RenderTwoHeadersBackground(HtmlTextWriter output)
        {
            if (CssOnly)
            {
                CalculateTwoHeaderGridHeight();

                RenderDividersTwoHeaders(output);
                RenderMatrixLinesTwoHeaders(output);
                RenderEventsTwoHeaders(output);

            }

            output.AddAttribute("cellspacing", "0");
            output.AddAttribute("cellpadding", "0");
            output.AddAttribute("border", "0");
            if (!CssOnly)
            {
                output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
            }
            output.RenderBeginTag("table");
            output.RenderBeginTag("tr");
            output.AddStyleAttribute("width", (RowHeaderWidthResolved) + "px");
            output.RenderBeginTag("td");
            RenderCorner(output);
            output.RenderEndTag(); // td
            RenderHeaderColsMonth(output);
            output.RenderEndTag(); // tr

            //Колонки по 2 часа
            output.RenderBeginTag("tr");
            output.AddStyleAttribute("width", (RowHeaderWidthResolved) + "px");
            output.RenderBeginTag("td");
            RenderCorner(output);
            output.RenderEndTag(); // td
            RenderHeaderCols(output);
            output.RenderEndTag(); // tr

        }


        /// <summary>
        /// Создание горизонтального разделителя, и разделители ресурсов
        /// </summary>
        /// <param name="output"></param>
        private void RenderDividers(HtmlTextWriter output)
        {
            int top = HeaderHeight;
            for (int i = 0; i < _days.Count; i++)
            {
                Day d = _days[i];
                top += ListDividersResources[i];
                //разделитель ресурсов       
                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", "0px");
                output.AddStyleAttribute("height", "1px");
                output.AddStyleAttribute("width", RowHeaderWidthResolved + "px");
                output.AddStyleAttribute("z-index", "1");
                output.AddAttribute("class", PrefixCssClass("_resourcedivider"));
                output.RenderBeginTag("div");
                output.RenderEndTag();

            }

            int totalWidth = RowHeaderWidth + GridWidth;

            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", (HeaderHeight - 1) + "px");
            output.AddStyleAttribute("left", "0px");
            output.AddStyleAttribute("height", "1px");
            output.AddStyleAttribute("width", totalWidth + "px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_divider_horizontal"));
            output.RenderBeginTag("div");
            output.RenderEndTag();

            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", "0px");
            output.AddStyleAttribute("left", (RowHeaderWidthResolved - 1) + "px");
            output.AddStyleAttribute("height", top + "px");
            output.AddStyleAttribute("width", "1px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_divider"));
            output.RenderBeginTag("div");
            output.RenderEndTag();
        }

        /// <summary>
        /// Создать разделители, если есть две шапки(День и часы)
        /// </summary>
        /// <param name="output"></param>
        private void RenderDividersTwoHeaders(HtmlTextWriter output)
        {
            //отрисовка первого ресурса разделителя
            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", HeaderHeight * 2 + "px");
            output.AddStyleAttribute("left", "0px");
            output.AddStyleAttribute("height", "1px");
            output.AddStyleAttribute("width", RowHeaderWidthResolved + "px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_resourcedivider"));
            output.RenderBeginTag("div");
            output.RenderEndTag();

            //Разделитель ресурсов
            int top = HeaderHeight * 2;
            for (int i = 0; i < _days.Count; i++)
            {
                Day d = _days[i];
                top += ListDividersResources[i];
                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", "0px");
                output.AddStyleAttribute("height", "1px");
                output.AddStyleAttribute("width", RowHeaderWidthResolved + "px");
                output.AddStyleAttribute("z-index", "1");
                output.AddAttribute("class", PrefixCssClass("_resourcedivider"));
                output.RenderBeginTag("div");
                output.RenderEndTag();

            }

            //разделитель между шапками
            int totalWidth = GridWidth;
            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", (HeaderHeight - 1) + "px");
            output.AddStyleAttribute("left", RowHeaderWidthResolved +"px");
            output.AddStyleAttribute("height", "1px");
            output.AddStyleAttribute("width", totalWidth + "px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_divider_horizontal"));
            output.RenderBeginTag("div");
            output.RenderEndTag();

            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", "0px");
            output.AddStyleAttribute("left", (RowHeaderWidthResolved - 1) + "px");
            output.AddStyleAttribute("height", top + "px");
            output.AddStyleAttribute("width", "1px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_divider"));
            output.RenderBeginTag("div");
            output.RenderEndTag();

            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", (HeaderHeight * 2) + "px");
            output.AddStyleAttribute("left", RowHeaderWidthResolved + "px");
            output.AddStyleAttribute("height", "1px");
            output.AddStyleAttribute("width", totalWidth + "px");
            output.AddStyleAttribute("z-index", "1");
            output.AddAttribute("class", PrefixCssClass("_divider_horizontal"));
            output.RenderBeginTag("div");
            output.RenderEndTag();
        }

        private void RenderMatrixLines(HtmlTextWriter output)
        {
            int top = HeaderHeight;
            for (int i = 0; i < _days.Count; i++)
            {
                Day d = _days[i];
                top += ListDividersResources[i];

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", RowHeaderWidthResolved + "px");
                output.AddStyleAttribute("height", "1px");
                output.AddStyleAttribute("width", GridWidth + "px");
                output.AddAttribute("class", PrefixCssClass("_matrix_horizontal_line"));
                output.RenderBeginTag("div");
                output.RenderEndTag();
            }
            //отрисовка вертикальных линий по дням
            for (int i = 1; i <= Days; i++)
            {
                int left = CellWidth * i * 12 + RowHeaderWidth - 1;

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", (HeaderHeight + 1) + "px");
                output.AddStyleAttribute("left", left + "px");
                output.AddStyleAttribute("height", GridHeight + "px");
                output.AddStyleAttribute("width", "1px");
                output.AddAttribute("class", PrefixCssClass("_matrix_vertical_line"));
                output.RenderBeginTag("div");
                output.RenderEndTag();
            }

        }

        /// <summary>
        /// Отрисовка вертикальный и горизонтальных линий в сетке
        /// </summary>
        /// <param name="output"></param>
        private void RenderMatrixLinesTwoHeaders(HtmlTextWriter output)
        {
            //отрисовка горизонтальных линий  в сетке
            int top = HeaderHeight * 2;
            for (int i = 0; i < _days.Count; i++)
            {
                Day d = _days[i];
                top += ListDividersResources[i];

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", RowHeaderWidthResolved + "px");
                output.AddStyleAttribute("height", "1px");
                output.AddStyleAttribute("width", GridWidth + "px");
                output.AddAttribute("class", PrefixCssClass("_matrix_horizontal_line"));
                output.RenderBeginTag("div");
                output.RenderEndTag();
            }
            //отрисовка вертакильный линий в сетке
            for (int i = 0; i < CellCount; i++)
            {
                int left = RowHeaderWidthResolved + i * CellWidth + CellWidth - 1;

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("top", ((HeaderHeight * 2) + 1) + "px");
                output.AddStyleAttribute("left", left + "px");
                output.AddStyleAttribute("height", GridHeight + "px");
                output.AddStyleAttribute("width", "1px");
                output.AddAttribute("class", PrefixCssClass("_matrix_vertical_line"));
                output.RenderBeginTag("div");
                output.RenderEndTag();
            }

        }

        private void RenderEvents(HtmlTextWriter output)
        {
            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", "0px");
            output.AddStyleAttribute("left", "0px");
            output.AddStyleAttribute("height", "0px");
            output.AddStyleAttribute("width", "0px");
            output.RenderBeginTag("div");

            ListDividersResources = new List<int>();
            int top = HeaderHeight;
            for (int i = 0; i < _days.Count; i++)
            {
                var countEvent = 0;
                Bottom = 0;
                Day d = _days[i];
                //Див с ресурсом, в который входит бары
                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("border", "none");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", RowHeaderWidthResolved + "px");
                //рабочий код
                //output.AddStyleAttribute("height", (d.MaxColumns() * EventHeight - 1) + "px");
                output.AddStyleAttribute("height", Bottom + "px");

                output.AddStyleAttribute("overflow", "none");
                output.AddAttribute("unselectable", "on");
                output.RenderBeginTag("div");
                //отрисовка ресурса
                PreviousEventColumnNumber = -1;
                foreach (Event ep in d.events.OrderBy(x => x.Column.Number).ToList())
                {
                    if (ep.Column.Number > 0)
                    {
                        //-1 типа предыдущий
                        RenderEvent(d, ep, output, d.events.Where(x => x.Column.Number == ep.Column.Number - 1).ToList());
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                    else
                    {
                        Top = 0;
                        RenderEvent(d, ep, output, null);
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                }

                if (Bottom == 0)
                    Bottom = d.Name.CalculateHeightToPixels(Convert.ToInt32(EventFontSize.Replace("px", "")), 1.5);
                ListDividersResources.Add(Bottom);
                output.RenderEndTag();
                top += Bottom;
            }
            output.RenderEndTag();
        }

        public List<int> ListDividersResources { get; set; }

        private int TotalHeight
        {
            get
            {
                int total = HeaderHeight + GridHeight;
                return total;
            }
        }
        public int GridHeight
        {
            get
            {
                int total = 0;
                if (ListDividersResources != null && ListDividersResources.Count > 0)
                {
                    for (int i = 0; i < _days.Count; i++)
                    {
                        total += ListDividersResources[i];
                    }
                }
                else
                {
                    foreach (Day d in _days)
                    {
                        total += d.MaxColumns() * EventHeight;
                    }
                }
                return total;
            }
        }

        public int GridWidth
        {
            get { return CellCount*CellWidth; }
        }

        private int RowHeaderWidthResolved
        {
            get
            {
                if (HeaderColumns.Count > 0)
                {
                    return RowHeaderColumnWidthTotal;
                }
                return RowHeaderWidth;
            }
        }

        private void RenderEvents(Day d, HtmlTextWriter output)
        {
            if (d.events.Count == 0)
            {
                output.Write("<div style='height:" + (EventHeight - 1) + "px;position:relative;width:1px;overflow:none;' unselectable='on'><!-- --></div>");
            }
            else
            {
                output.AddStyleAttribute("position", "relative");
                output.AddStyleAttribute("height", (d.MaxColumns() * EventHeight - 1) + "px"); //
                output.AddStyleAttribute("overflow", "none");
                output.AddAttribute("unselectable", "on");
                output.RenderBeginTag("div");

                foreach (Event ep in d.events)
                {
                    RenderEvent(d, ep, output);
                }

                // div relative
                output.RenderEndTag();
            }
        }

        private void DoBeforeEventRender(BeforeEventRenderEventArgs args)
        {
            if (BeforeEventRender != null)
            {
                BeforeEventRender(this, args);
            }
        }

        private void DoBeforeResHeaderRender(BeforeResHeaderRenderEventArgs args)
        {
            if (BeforeResHeaderRender != null)
            {
                BeforeResHeaderRender(this, args);
            }
        }


        private int Top { get; set; }
        private int Bottom { get; set; }
        private int PreviousEventColumnNumber { get; set; }


        public enum TextAlignInBlock
        {
            Left,
            Center,
            Right,
            Full,
        }

        /// <summary>
        /// Формирование отступов для Events, с двумя заголовками
        /// </summary>
        /// <param name="output"></param>
        private void RenderEventsTwoHeaders(HtmlTextWriter output)
        {
            output.AddStyleAttribute("position", "absolute");
            output.AddStyleAttribute("top", "0px");
            output.AddStyleAttribute("left", "0px");
            output.AddStyleAttribute("height", "0px");
            output.AddStyleAttribute("width", "0px");
            output.RenderBeginTag("div");

            ListDividersResources = new List<int>();

            //int top = (HeaderHeight * 2) + 2;
            int top = (HeaderHeight * 2);
            for (int i = 0; i < _days.Count; i++)
            {
                var countEvent = 0;
                Bottom = 0;
                Day d = _days[i];
                //Див с ресурсом, в который входит бары
                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("border", "none");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("left", RowHeaderWidthResolved + "px");
                //рабочий код
                //output.AddStyleAttribute("height", (d.MaxColumns() * EventHeight - 1) + "px");
                output.AddStyleAttribute("height", Bottom + "px");

                output.AddStyleAttribute("overflow", "none");
                output.AddAttribute("unselectable", "on");
                output.RenderBeginTag("div");
                //отрисовка ресурса
                PreviousEventColumnNumber = -1;
                foreach (Event ep in d.events.OrderBy(x => x.Column.Number).ToList())
                {
                    if (ep.Column.Number > 0)
                    {
                        //-1 типа предыдущий
                        RenderEvent(d, ep, output, d.events.Where(x => x.Column.Number == ep.Column.Number - 1).ToList());
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                    else
                    {
                        Top = 0;
                        RenderEvent(d, ep, output, null);
                        PreviousEventColumnNumber = ep.Column.Number;
                    }
                }

                if (Bottom == 0)
                    Bottom = d.Name.CalculateHeightToPixels(Convert.ToInt32(EventFontSize.Replace("px", "")), 1.5);
                ListDividersResources.Add(Bottom);
                output.RenderEndTag();
                top += Bottom;
            }
            output.RenderEndTag();
        }

        private int CalculateEventHeight(Event ev, int width, int fontSize, double lineHeight, Day day)
        {
            int margin = 1;

            var textWidth = 0;
            while (ev.Text.Contains("<br/>") && textWidth < width)
            {
                var text = ev.Text.Remove(ev.Text.LastIndexOf("<br/>"), "<br/>".Length);
                //вычисление ширины самой длинной строки, если текст многострочный
                var lines = text.Split(new string[] { "<br/>", "\n" }, StringSplitOptions.None);
                
                foreach (string str in lines)
                {
                    if (textWidth < str.CalculateWidthToPixels(fontSize))
                    {
                        textWidth = str.CalculateWidthToPixels(fontSize);
                    }
                }

                if (textWidth < width)
                    ev.Text = text;
            }
            return ev.Text.Replace("<br/>", "\n").CalculateHeightToPixels(fontSize, lineHeight) + HeightBar + TimeSpendingHeight + margin;
        }

        private (int height, int width, int left, int top, TextAlignInBlock textAlignInBox, int indentInsideBlockLeft, int indentInsideBlockRight, BeforeEventRenderEventArgs ea)
            CalculateResources(Day day, Event ev, List<Event> previousEventsGroup)
        {

            BeforeEventRenderEventArgs ea = new BeforeEventRenderEventArgs(ev);
            ea.InnerHTML = ev.Text;
            ea.ToolTip = ev.Text;
            ea.EventClickEnabled = EventClickHandling != EventClickHandlingEnum.Disabled;

            if (!CssOnly)
            {
                ea.DurationBarColor = ColorTranslator.ToHtml(DurationBarColor);
                ea.BackgroundColor = ColorTranslator.ToHtml(EventBackColor);
            }

            DoBeforeEventRender(ea);
            
            int max = CellCount * CellWidth;
            var fontSize = Convert.ToInt32(EventFontSize.Replace("px", ""));
            double lineHeight = 1.1; //должно быть равно в css .scheduler_transparent_event_inner line-height: 1.1;
            int height = 0;
            var top = Top;
            var left = 0;
            var width = 0;
            var indentInsideBlockLeft = 0;
            var indentInsideBlockRight = 0;
            ev.Text = ev.Text.Replace("\t", "").Replace("\n", "");

            var textAlignInBox = TextAlignInBlock.Left;
            //Блок растянут на весь интервал
            if (ev.BoxStart == day.Start && ev.BoxEnd == day.End)
            {
                left = 0;
                width = max;
                indentInsideBlockLeft = 0;
                indentInsideBlockRight = 0;
            }
            //блок начинается с начала интервала и заканчивается где-то в середине
            if (ev.BoxStart == day.Start && ev.BoxEnd < day.End)
            {
                left = 0;
                width = WidthOfBar(ev.BoxStart, ev.BoxEnd);
                indentInsideBlockLeft = 0;
                indentInsideBlockRight = WidthOfBar(ev.End, ev.BoxEnd);
            }
            //Блок находится в середине интервала
            if (ev.BoxStart > day.Start && ev.BoxEnd < day.End)
            {
                left = WidthOfBar(day.Start, ev.BoxStart);
                width = WidthOfBar(ev.BoxStart, ev.BoxEnd);
                indentInsideBlockLeft = WidthOfBar(ev.BoxStart, ev.Start);
                indentInsideBlockRight = WidthOfBar(ev.End, ev.BoxEnd);
                textAlignInBox = TextAlignInBlock.Center;
            }
            ////блок находится справа
            if (ev.BoxStart > day.Start && ev.BoxEnd == day.End)
            {
                left = WidthOfBar(day.Start, ev.BoxStart);
                width = WidthOfBar(ev.BoxStart, ev.BoxEnd);
                indentInsideBlockLeft = WidthOfBar(ev.BoxStart, ev.Start);
                indentInsideBlockRight = 0;
            }

            var textTime = CreateTimeStartEnd(day, ev);
            var eventHeightForTimeText = (textTime.divStartText + textTime.divEndText).CalculateWidthToPixels(fontSize);
            if (eventHeightForTimeText > width)
                width = eventHeightForTimeText;

            height = CalculateEventHeight(ev, width, fontSize, lineHeight, day);

            //TOdo просчет высоты между блоками равного и разного размера
            if (previousEventsGroup != null && previousEventsGroup.Count > 0)
            {
                var maxHeightEvent = previousEventsGroup.First();
                var maxHeightInPreviousGroup = CalculateEventHeight(previousEventsGroup.First(), WidthOfBar(maxHeightEvent.BoxStart, maxHeightEvent.BoxEnd), fontSize, lineHeight, day);
                foreach (var previousEvent in previousEventsGroup)
                {
                    var heightPreviousEvent = CalculateEventHeight(previousEvent, WidthOfBar(previousEvent.BoxStart, previousEvent.BoxEnd), fontSize, lineHeight, day);
                    if (heightPreviousEvent >= maxHeightInPreviousGroup)
                    {
                        maxHeightInPreviousGroup = heightPreviousEvent;
                    }
                }
                if (PreviousEventColumnNumber != ev.Column.Number)
                {
                    Top += maxHeightInPreviousGroup;
                    top = Top;
                    Bottom = Top + height; //вот это нужно для подсчета окончания блоки, а потом ресурса
                }
            }
            else
            {
                top = Top;
                Bottom = Top + height;
            }

            return (height, width, left, top, textAlignInBox, indentInsideBlockLeft, indentInsideBlockRight, ea);
        }

        private (string divStartText, string divEndText) CreateTimeStartEnd(Day day, Event ev)
        {
            var arrivalDifDays = string.Empty;
            var departureDifDays = string.Empty;

            var arrival = (ev.Arrival == DateTime.MinValue || ev.Arrival == DateTime.MaxValue) ? "" :
                $" ({ev.Arrival.ToString("t")}{arrivalDifDays})";
            var departure = (ev.Departure == DateTime.MinValue || ev.Departure == DateTime.MaxValue) ? "" :
                $"({ev.Departure.ToString("t")}{departureDifDays})";
            var startTO = ev.Start <= day.Start ? "" : ev.Start.ToString("t");
            var endTO = ev.End.ToString("t");
            return (startTO + arrival, endTO + departure);
        }
        private void RenderEvent(Day day, Event ev, HtmlTextWriter output, List<Event> previousEventsGroup = null)
        {
            //ev.Text = ev.Text.Replace("\t", "").Replace("\n", "");
            
            //Это нужно?
            //BeforeEventRenderEventArgs ea = new BeforeEventRenderEventArgs(ev);
            //ea.InnerHTML = ev.Text;
            //ea.ToolTip = ev.Text;
            //ea.EventClickEnabled = EventClickHandling != EventClickHandlingEnum.Disabled;
            
            //if (!CssOnly)
            //{
            //    ea.DurationBarColor = ColorTranslator.ToHtml(DurationBarColor);
            //    ea.BackgroundColor = ColorTranslator.ToHtml(EventBackColor);
            //}

            //DoBeforeEventRender(ea);

            var calculateResources = CalculateResources(day, ev, previousEventsGroup);
            var height = calculateResources.height;
            var width = calculateResources.width;
            var left = calculateResources.left;
            var indentInsideBlockLeft = calculateResources.indentInsideBlockLeft;
            var indentInsideBlockRight = calculateResources.indentInsideBlockRight;
            var top = calculateResources.top;
            var textAlignInBox = calculateResources.textAlignInBox;
            BeforeEventRenderEventArgs ea = calculateResources.ea;


            int timeSpendingHeight = TimeSpendingHeight;

            int startDelta = (int)Math.Floor((ev.Start - ev.BoxStart).TotalMinutes * CellWidth / CellDuration - 1);
            int realWidth = (int)Math.Floor((ev.End - ev.Start).TotalMinutes * CellWidth / CellDuration);
            realWidth = realWidth == 0 ? 1 : realWidth;


            var startEndText = CreateTimeStartEnd(day, ev);
            var startText = startEndText.divStartText;
            var endText = startEndText.divEndText;

            //TODO тут не менял
            if (!CssOnly)
            {
                output.AddAttribute("unselectable", "on");

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("left", left + "px");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("width", width + "px");
                output.AddStyleAttribute("height", height + "px");
                output.AddStyleAttribute("overflow", "hidden");
                output.AddStyleAttribute("border", "1px solid " + ColorTranslator.ToHtml(EventBorderColor));
                output.AddStyleAttribute("background-color", ea.BackgroundColor);
                output.AddStyleAttribute("white-space", "nowrap");
                output.AddStyleAttribute("font-family", EventFontFamily);
                output.AddStyleAttribute("font-size", EventFontSize);

                if (ea.EventClickEnabled && EventClickHandling != EventClickHandlingEnum.Disabled)
                {
                    if (EventClickHandling == EventClickHandlingEnum.PostBack)
                    {
                        output.AddAttribute("onclick", "javascript:event.cancelBubble=true;" + Page.ClientScript.GetPostBackEventReference(this, "PK:" + ev.Id));
                    }
                    else
                    {
                        output.AddAttribute("onclick", "javascript:event.cancelBubble=true;" + String.Format(EventClickJavaScript, ev.Id));
                    }

                    output.AddStyleAttribute("cursor", "pointer");
                }
                else
                {
                    output.AddStyleAttribute("cursor", "default");
                }

                output.RenderBeginTag("div");

                if (DurationBarVisible)
                {
                    output.Write("<div unselectable='on' style='width:" + realWidth + "px; margin-left: " + startDelta + "px; height:2px; background-color:" + ea.DurationBarColor + "; font-size:1px; position:relative;' ></div>");
                    output.Write("<div unselectable='on' style='width:" + width + "px; height:1px; background-color:" + ColorTranslator.ToHtml(EventBorderColor) + "; font-size:1px; position:relative;' ></div>");
                }

                output.AddStyleAttribute("display", "block");
                output.AddStyleAttribute("padding-left", "1px");
                output.AddAttribute("unselectable", "on");
                output.RenderBeginTag("div");
                output.Write(ea.Html);
                output.RenderEndTag();
                output.RenderEndTag();    
            }
            else
            {
                output.AddAttribute("unselectable", "on");

                output.AddStyleAttribute("position", "absolute");
                output.AddStyleAttribute("left", left + "px");
                output.AddStyleAttribute("top", top + "px");
                output.AddStyleAttribute("width", width + "px");
                output.AddStyleAttribute("height", height + "px");
                output.AddStyleAttribute("overflow", "hidden");

                //Todo отображение сетки
                //output.AddStyleAttribute("border", "1px solid red");

                output.AddAttribute("data-toggle", "modal");
                output.AddAttribute("data-target", "#eventDetailsModal");
                output.AddAttribute("data-description", ev.Text.Replace("<br/>", ""));
                output.AddAttribute("data-startDate", ev.Start.ToString("dd.MM.yyyy HH:mm"));
                output.AddAttribute("data-arrival", ev.Arrival.ToString("HH:mm") == "00:00" ? "" : ev.Arrival.ToString("HH:mm"));
                output.AddAttribute("data-endDate", ev.End.ToString("dd.MM.yyyy HH:mm"));
                output.AddAttribute("data-departure", ev.Departure.ToString("HH:mm") == "00:00" ? "" : ev.Departure.ToString("HH:mm"));
                output.AddAttribute("data-mhr", ev.MHR);
                output.AddAttribute("data-bookedmhr", ev.BookedMHR);
                output.AddAttribute("data-stationname", ev.StationName);

                string cssClass = PrefixCssClass("_event");
                if (!String.IsNullOrEmpty(ea.CssClass))
                {
                    cssClass += " " + ea.CssClass;
                }

                output.AddAttribute("class", cssClass);

                if (ea.EventClickEnabled && EventClickHandling != EventClickHandlingEnum.Disabled)
                {
                    if (EventClickHandling == EventClickHandlingEnum.PostBack)
                    {
                        output.AddAttribute("onclick", "javascript:event.cancelBubble=true;" + Page.ClientScript.GetPostBackEventReference(this, "PK:" + ev.Id));
                    }
                    else
                    {
                        //Todo пока убран alert()
                        //output.AddAttribute("onclick", "javascript:event.cancelBubble=true;" + String.Format(EventClickJavaScript, p.Id));
                    }

                    output.AddStyleAttribute("cursor", "pointer");
                }
                else
                {
                    output.AddStyleAttribute("cursor", "default");
                }

                output.RenderBeginTag("div");

                output.AddStyleAttribute("display", "block");
                
                output.AddStyleAttribute("bottom", timeSpendingHeight + "px");
                output.AddAttribute("unselectable", "on");
                if (textAlignInBox == TextAlignInBlock.Center)
                    output.AddStyleAttribute("text-align", "center");
                output.AddAttribute("class", PrefixCssClass("_event_inner"));

                if (!String.IsNullOrEmpty(ea.BackgroundColor))
                {
                    output.AddStyleAttribute("background", ea.BackgroundColor);
                }

                output.RenderBeginTag("div");
                output.Write(ev.Text);
                output.RenderEndTag();

                if (DurationBarVisible)
                {
                    output.AddStyleAttribute("position", "absolute");
                    output.AddAttribute("unselectable", "on");

                    output.AddStyleAttribute("bottom", timeSpendingHeight + "px");
                    output.AddStyleAttribute("left", indentInsideBlockLeft + "px");
                    output.AddStyleAttribute("right", indentInsideBlockRight + "px");
                    output.AddStyleAttribute("height", HeightBar + "px");
                    output.AddAttribute("class", PrefixCssClass("_event_bar"));
                    if (!String.IsNullOrEmpty(ea.DurationBarColor))
                    {
                        output.AddStyleAttribute("background-color", ea.DurationBarColor);
                    }
                    output.RenderBeginTag("div");

                    //double barLeft = 100.0 * startDelta / widthOfBar;
                    //double barWidth = 100.0 * realWidth / widthOfBar;

                    double barLeft = 100.0 * startDelta / width;
                    double barWidth = 100.0 * realWidth / width;


                    output.AddAttribute("class", PrefixCssClass("_event_bar_inner"));
                    output.AddStyleAttribute("left", barLeft + "%");
                    output.AddStyleAttribute("width", "0");
                    if (!String.IsNullOrEmpty(ea.DurationBarColor))
                    {
                        output.AddStyleAttribute("background-color", "");
                    }
                    output.RenderBeginTag("div");
                    output.RenderEndTag();

                    output.RenderEndTag();
                }

                ////Див с временем ТО
                output.AddAttribute("unselectable", "on");
                output.AddStyleAttribute("height", timeSpendingHeight + "px");
                output.AddStyleAttribute("font-size", timeSpendingHeight - 4 + "px");
                output.AddAttribute("class", PrefixCssClass("_event_bar_time"));
                output.RenderBeginTag("div");

                output.AddAttribute("unselectable", "on");
                output.AddAttribute("class", PrefixCssClass("_event_bar_start_time"));
                output.RenderBeginTag("div");
                output.Write(startText);
                output.RenderEndTag();

                output.AddAttribute("unselectable", "on");
                output.AddAttribute("class", PrefixCssClass("_event_bar_end_time"));
                output.RenderBeginTag("div");
                output.Write(endText);
                output.RenderEndTag();

                output.RenderEndTag();
                //конец дива с временем ТО


                output.RenderEndTag();    
                
            }

            
        }
        public int WidthOfBar(DateTime start, DateTime end)
        {
            return (int)Math.Floor((end - start).TotalMinutes * CellWidth / CellDuration);
        }

        private void RenderRows(HtmlTextWriter output)
        {
            for (int i = 0; i < _days.Count; i++)
            {
                output.RenderBeginTag("tr");

                RenderRowHeader(output, _days[i], i);
                RenderRowCells(output, _days[i], i);

                output.RenderEndTag();
            }
            //foreach (Day d in _days)
            //{
            //    output.RenderBeginTag("tr");

            //    RenderRowHeader(output, d);
            //    RenderRowCells(output, d);

            //    output.RenderEndTag();

            //}

        }

        private void RenderRowCells(HtmlTextWriter output, Day d, int dayNumber)
        {

            if (!CssOnly)
            {
                // render all events in the first cell
                output.AddStyleAttribute("width", "1px");
                    output.AddStyleAttribute("border-bottom", "1px solid black");
                    output.AddStyleAttribute("background-color", GetCellColor(d.Start));

                output.AddAttribute("valign", "top");
                output.AddAttribute("unselectable", "on");
                output.RenderBeginTag("td");

                RenderEvents(d, output);

                // td
                output.RenderEndTag();
            }

            RenderCells(output, d, dayNumber);
        }

        private void RenderCells(HtmlTextWriter output, Day d, int dayNumber)
        {
            int cellsToRender = CellCount;

            for (int i = 0; i < cellsToRender; i++)
            {
                DateTime start = d.Start.AddMinutes(i * CellDuration);

                int thisCellWidth = CellWidth;
                string back = GetCellColor(start);
                int height = ListDividersResources[dayNumber];
                if (!CssOnly)
                {
                    if (i == 0)
                    {
                        thisCellWidth = CellWidth - 1;
                    }
                    if (i == cellsToRender - 1)
                    {
                        output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    }
                    output.AddStyleAttribute("border-bottom", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    output.AddStyleAttribute("background-color", back);
                }

                output.AddStyleAttribute("width", thisCellWidth + "px");

                if (TimeRangeSelectedHandling != TimeRangeSelectedHandling.Disabled)
                {
                    if (TimeRangeSelectedHandling == TimeRangeSelectedHandling.PostBack)
                    {
                        output.AddAttribute("onclick", "javascript:" + Page.ClientScript.GetPostBackEventReference(this, "TIME:" + start.ToString("s") + d.Value));
                    }
                    else
                    {
                        output.AddAttribute("onclick", "javascript:" + String.Format(TimeRangeSelectedJavaScript, start.ToString("s"), d.Value));
                    }

                    if (!CssOnly)
                    {
                        output.AddAttribute("onmouseover", "this.style.backgroundColor='" + ColorTranslator.ToHtml(HoverColor) + "';");
                        output.AddAttribute("onmouseout", "this.style.backgroundColor='" + back + "';");
                    }

                    output.AddStyleAttribute("cursor", "pointer");
                }
                else
                {
                    output.AddStyleAttribute("cursor", "default");
                }


                output.RenderBeginTag("td");

                if (!CssOnly)
                {
                    output.Write("<div unselectable='on' style='display:block; width:" + (thisCellWidth - 1) + "px; height:" + (d.MaxColumns() * EventHeight - 1) + "px; border-right: 1px solid " + ColorTranslator.ToHtml(HourBorderColor) + ";' ><!-- --></div>");
                }
                else
                {

                    string business = IsBusinessCell(start) ? " " + PrefixCssClass("_cell_business") : "";
                    output.Write("<div unselectable='on' style='display:block; width:" + (thisCellWidth) + "px; height:" + height + "px;' class='" + PrefixCssClass("_cell") + business + "'>");
                    output.Write("</div>");
                }
                output.RenderEndTag();

            }
        }

        private void RenderRowHeader(HtmlTextWriter output, Day d, int dayNumber)
        {
            BeforeResHeaderRenderEventArgs ea = new BeforeResHeaderRenderEventArgs();
            ea.Columns = d.Columns;
            ea.InnerHTML = d.Name;
            ea.Name = d.Name;
            ea.Value = d.Value;
            ea.DataItem = d.DataItem;

            while (ea.Columns.Count < HeaderColumns.Count)
            {
                ea.Columns.Add(new ResourceColumn(String.Empty));
            }

            DoBeforeResHeaderRender(ea);
            
            int height = ListDividersResources[dayNumber];

            if (!CssOnly)
            {
                height -= 1;
            }

            output.AddStyleAttribute("width", (RowHeaderWidthResolved) + "px");

            if (!CssOnly)
            {
                output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("border-left", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("border-bottom", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
                output.AddStyleAttribute("font-family", HeaderFontFamily);
                output.AddStyleAttribute("font-size", HeaderFontSize);
                output.AddStyleAttribute("color", ColorTranslator.ToHtml(HeaderFontColor));
                output.AddStyleAttribute("cursor", "default");
            }

            output.AddAttribute("unselectable", "on");
            output.AddAttribute("resource", d.Value);
            output.AddAttribute("id", ClientID + "row" + d.Value);
            output.RenderBeginTag("td");

            
            if (RowHeaderColumns.Count > 0)
            {
                if (!CssOnly)
                {
                    output.Write("<div unselectable='on' style='height:" + height + "px; line-height:" + height + "px; overflow:hidden; width: " + RowHeaderWidthResolved + "px'>");
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table style='width:" + (RowHeaderColumnWidthTotal) + "px; padding: 0px; border-spacing: 0px; table-layout: fixed;'>");
                    sb.Append("<tr>");
                    sb.Append("<td style='width:" + RowHeaderColumns[0] + "px; padding: 0px; white-space: nowrap; overflow: hidden;'>");
                    sb.Append("<div>");
                    sb.Append(ea.InnerHTML);
                    sb.Append("</div>");
                    sb.Append("</td>");
                    for (int i = 1; i < RowHeaderColumns.Count; i++)
                    {
                        string html = null;
                        if (i <= d.Columns.Count)
                        {
                            html = d.Columns[i - 1].InnerHTML;
                        }

                        sb.Append("<td style='width:" + RowHeaderColumns[i] + "px; padding: 0px; white-space: nowrap;overflow: hidden;'>");
                        sb.Append(html);
                        sb.Append("</td>");
                    }
                    sb.Append("</tr>");
                    sb.Append("</table>");

                    output.Write(sb.ToString());
                    
                }
                else
                {
                    output.Write("<div unselectable='on' style='height:" + height + "px; line-height:" + height + "px; overflow:hidden; width: " + RowHeaderWidthResolved + "px'>");
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<table style='width:" + (RowHeaderColumnWidthTotal) + "px; padding: 0px; border-spacing: 0px; table-layout: fixed;'>");
                    sb.Append("<tr>");
                    sb.Append("<td style='width:" + RowHeaderColumns[0] + "px; padding: 0px; overflow: hidden;'>");

                    sb.Append("<div unselectable='on' style='height:" + height + "px; overflow:hidden; position: relative;' class='" + PrefixCssClass("_rowheader") + "'>");
                    sb.Append("<div unselectable='on' class='" + PrefixCssClass("_rowheader_inner") + "'>");
                    sb.Append(ea.InnerHTML);
                    sb.Append("</div>");

                    sb.Append("</td>");
                    for (int i = 1; i < RowHeaderColumns.Count; i++)
                    {
                        string html = null;
                        if (i <= d.Columns.Count)
                        {
                            html = d.Columns[i - 1].InnerHTML;
                        }

                        sb.Append("<td style='width:" + RowHeaderColumns[i] + "px; padding: 0px; white-space: nowrap;overflow: hidden;'>");

                        sb.Append("<div unselectable='on' style='height:" + height + "px; overflow:hidden; position: relative;' class='" + PrefixCssClass("_rowheader") + "'>");
                        sb.Append("<div unselectable='on' class='" + PrefixCssClass("_rowheader_inner") + "'>");
                        sb.Append(html);
                        sb.Append("</div>");

                        sb.Append("</td>");
                    }
                    sb.Append("</tr>");
                    sb.Append("</table>");

                    output.Write(sb.ToString());
                }
            }
            else
            {
                if (!CssOnly)
                {
                    output.Write("<div unselectable='on' style='margin-left:4px; height:" + height + "px; line-height:" + height + "px; overflow:hidden;'>");
                    output.Write(ea.InnerHTML);
                }
                else
                {
                    output.Write("<div unselectable='on' style='height:" + height + "px; overflow:hidden; position: relative;' class='" + PrefixCssClass("_rowheader") + "'>");
                    output.Write("<div unselectable='on' class='" + PrefixCssClass("_rowheader_inner") + "'>");
                    output.Write(ea.InnerHTML);
                    output.Write("</div>");
                }
            }
            
            output.Write("</div>");

            output.RenderEndTag();
        }

        private void RenderCorner(HtmlTextWriter output)
        {

            output.AddStyleAttribute("width", (RowHeaderWidthResolved) + "px");
            if (!CssOnly)
            {
                output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
                output.AddStyleAttribute("line-height", (HeaderHeight - 1) + "px");
                output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("border-top", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("border-left", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("border-bottom", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
                output.AddStyleAttribute("cursor", "default");
                output.AddStyleAttribute("white-space", "nowrap");
            }
            else
            {
                output.AddStyleAttribute("height", (HeaderHeight) + "px");
                output.AddAttribute("class", PrefixCssClass("_corner"));
            }

            output.AddStyleAttribute("overflow", "hidden");
            output.AddAttribute("unselectable", "on");
            output.RenderBeginTag("div");

            output.AddAttribute("id", ClientID + "_corner");
            output.AddAttribute("unselectable", "on");
            output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
            if (CssOnly)
            {
                output.AddAttribute("class", PrefixCssClass("_corner_inner"));
            }
            output.RenderBeginTag("div");
            output.RenderEndTag();

            output.RenderEndTag(); // td
        }


        internal void RenderHeaderColsMonth(HtmlTextWriter output)
        {
            for (int i = 0; i < Days; i++)
            {
                //DateTime from = StartDate.AddMinutes(CellDuration * i);
                //DateTime from = DateTime.Today.AddDays(i);
                DateTime from = StartDate.AddDays(i);
                //DateTime to = from.AddMinutes(CellDuration);
                Enum.TryParse(from.DayOfWeek.ToString(), out DaysOfWeek d);
                var text = $"{d.GetAttributeOfType<DisplayAttribute>().Name} {from.ToShortDateString()}";
                var tooltip = from.ToShortDateString();

                //if (CellDuration < 60) // smaller than hour, use minutes
                //{
                //    if (!CssOnly)
                //    {
                //        text = String.Format("<span style='color:gray'>{0:00}</span>", from.Minute);
                //    }
                //    else
                //    {
                //        text = String.Format("{0:00}", from.Minute);
                //    }
                //    tooltip = from.ToLongTimeString();
                //}
                //else if (CellDuration < 1440)// smaller than day, use hours
                //{
                //    text = TimeFormatter.GetHour(from, TimeFormat, "{0} {1}");
                //    tooltip = from.ToShortTimeString();
                //}
                //else if (CellDuration < 1440 * 28) // smaller than smallest month, use days
                //{
                //    text = from.Day.ToString();
                //    tooltip = from.ToLongDateString();
                //}
                //else if (CellDuration < 1440 * 365) // smaller than year, use months
                //{
                //    text = from.Month.ToString();
                //    tooltip = @from.ToString("MMMM yyyy");
                //}
                //else // use years
                //{
                //    text = from.Year.ToString();
                //    tooltip = from.Year.ToString();
                //}

                if (!CssOnly)
                {
                    if (i == 0)
                    {
                        output.AddAttribute("colspan", "2");
                    }
                    if (i == CellCount - 1)
                    {
                        output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    }
                    output.AddStyleAttribute("border-top", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    output.AddStyleAttribute("border-bottom", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    output.AddStyleAttribute("text-align", "center");
                    output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
                    output.AddStyleAttribute("font-family", HourFontFamily);
                    output.AddStyleAttribute("font-size", HourFontSize);
                    output.AddStyleAttribute("cursor", "default");
                }
                output.AddStyleAttribute("width", (CellWidth * 12) + "px");
                output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
                output.AddStyleAttribute("overflow", "hidden");
                output.AddAttribute("unselectable", "on");
                output.AddStyleAttribute("-khtml-user-select", "none");
                output.AddStyleAttribute("-moz-user-select", "none");
                output.AddAttribute("colspan", "12");
                output.RenderBeginTag("td");

                if (!CssOnly)
                {
                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("title", tooltip);
                    output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
                    output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(HourNameBorderColor));
                    output.AddStyleAttribute("width", (CellWidth - 1) + "px");
                    output.AddStyleAttribute("overflow", "hidden");
                    output.RenderBeginTag("div");
                    output.Write(text);
                    output.RenderEndTag();
                }
                else
                {
                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("title", tooltip);
                    output.AddAttribute("class", PrefixCssClass("_timeheadercol"));
                    output.AddStyleAttribute("height", (HeaderHeight) + "px");
                    output.AddStyleAttribute("width", (CellWidth * 12) + "px");
                    output.AddStyleAttribute("position", "relative");
                    output.AddStyleAttribute("overflow", "hidden");
                    output.RenderBeginTag("div");

                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("class", PrefixCssClass("_timeheadercol_inner"));
                    output.RenderBeginTag("div");

                    output.Write(text);

                    output.RenderEndTag();
                    output.RenderEndTag();
                }
                output.RenderEndTag();
            }
        }

        internal void RenderHeaderCols(HtmlTextWriter output)
        {
            for (int i = 0; i < CellCount; i++)
            {
                DateTime from = StartDate.AddMinutes(CellDuration * i);
                //DateTime to = from.AddMinutes(CellDuration);

                string text;
                string tooltip;

                if (CellDuration < 60) // smaller than hour, use minutes
                {
                    if (!CssOnly)
                    {
                        text = String.Format("<span style='color:gray'>{0:00}</span>", from.Minute);
                    }
                    else
                    {
                        text = String.Format("{0:00}", from.Minute);
                    }
                    tooltip = from.ToLongTimeString();
                }
                else if (CellDuration < 1440)// smaller than day, use hours
                {
                    text = TimeFormatter.GetHour(from, TimeFormat, "{0} {1}");
                    tooltip = from.ToShortTimeString();
                }
                else if (CellDuration < 1440 * 28) // smaller than smallest month, use days
                {
                    text = from.ToShortDateString(); //from.Day.ToString();
                    tooltip = from.ToLongDateString();
                }
                else if (CellDuration < 1440 * 365) // smaller than year, use months
                {
                    text = from.Month.ToString();
                    tooltip = @from.ToString("MMMM yyyy");
                }
                else // use years
                {
                    text = from.Year.ToString();
                    tooltip = from.Year.ToString();
                }

                if (!CssOnly)
                {
                    if (i == 0)
                    {
                        output.AddAttribute("colspan", "2");
                    }
                    if (i == CellCount - 1)
                    {
                        output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    }
                    output.AddStyleAttribute("border-top", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    output.AddStyleAttribute("border-bottom", "1px solid " + ColorTranslator.ToHtml(BorderColor));
                    output.AddStyleAttribute("text-align", "center");
                    output.AddStyleAttribute("background-color", ColorTranslator.ToHtml(HourNameBackColor));
                    output.AddStyleAttribute("font-family", HourFontFamily);
                    output.AddStyleAttribute("font-size", HourFontSize);
                    output.AddStyleAttribute("cursor", "default");
                }
                output.AddStyleAttribute("width", (CellWidth) + "px");
                output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
                output.AddStyleAttribute("overflow", "hidden");
                output.AddAttribute("unselectable", "on");
                output.AddStyleAttribute("-khtml-user-select", "none");
                output.AddStyleAttribute("-moz-user-select", "none");
                output.RenderBeginTag("td");

                if (!CssOnly)
                {
                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("title", tooltip);
                    output.AddStyleAttribute("height", (HeaderHeight - 1) + "px");
                    output.AddStyleAttribute("border-right", "1px solid " + ColorTranslator.ToHtml(HourNameBorderColor));
                    output.AddStyleAttribute("width", (CellWidth - 1) + "px");
                    output.AddStyleAttribute("overflow", "hidden");
                    output.RenderBeginTag("div");
                    output.Write(text);
                    output.RenderEndTag();
                }
                else
                {
                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("title", tooltip);
                    output.AddAttribute("class", PrefixCssClass("_timeheadercol"));
                    output.AddStyleAttribute("height", (HeaderHeight) + "px");
                    output.AddStyleAttribute("width", (CellWidth) + "px");
                    output.AddStyleAttribute("position", "relative");
                    output.AddStyleAttribute("overflow", "hidden");
                    output.RenderBeginTag("div");

                    output.AddAttribute("unselectable", "on");
                    output.AddAttribute("class", PrefixCssClass("_timeheadercol_inner"));
                    output.RenderBeginTag("div");

                    output.Write(text);
                    
                    output.RenderEndTag();
                    output.RenderEndTag();
                }
                output.RenderEndTag();
            }
        }

        private void LoadEventsToDays()
        {
            _days = new List<Day>();
            List<Event> items = (List<Event>)ViewState["Items"];

            if (Resources == null)
            {
                return;
            }

            if (ViewType == ViewTypeEnum.Resources)
            {
                foreach (Resource resource in Resources)
                {
                    Day d = new Day(StartDate, EndDate.AddDays(1), resource.Name, resource.Value, CellDuration, CellWidth, Convert.ToInt32(EventFontSize.Replace("px", "")));
                    d.Columns = resource.Columns;
                    _days.Add(d);
                }
            }
            else
            {
                if (items != null)
                {
                    foreach (Event e in items)
                    {
                        Day d = new Day(StartDate, EndDate.AddDays(1), e.Text, e.Id, CellDuration, CellWidth, Convert.ToInt32(EventFontSize.Replace("px", "")));
                        d.Columns = new List<ResourceColumn>();
                        d.DataItem = new DataItemWrapper(e.Source);
                        _days.Add(d);
                    }
                }
            }

            foreach (Day d in _days)
            {
                d.Load(items);
            }

        }


        #region Properties


        /// <summary>
        /// Gets or sets the first day to be shown. Default is DateTime.Today.
        /// </summary>
        [Description("The first day to be shown. Default is DateTime.Today.")]
        [Category("Behavior")]
        public DateTime StartDate
        {
            get
            {
                if (ViewState["StartDate"] == null)
                {
                    return DateTime.Today;
                }

                return (DateTime)ViewState["StartDate"];

            }
            set
            {
                ViewState["StartDate"] = new DateTime(value.Year, value.Month, value.Day);
            }
        }


        /// <summary>
        /// Gets the last day to be shown.
        /// </summary>
        [Browsable(false)]
        public DateTime EndDate
        {
            get
            {
                return StartDate.AddDays(Days - 1).EndOfDay();
            }
        }


        /// <summary>
        /// Gets or sets the number of days to be displayed. Default is 1.
        /// </summary>
        [Description("The number of days to be displayed on the calendar. Default value is 1.")]
        [Category("Behavior")]
        [DefaultValue(1)]
        public int Days
        {
            get
            {
                if (ViewState["Days"] == null)
                    return 1;
                return (int)ViewState["Days"];
            }
            set
            {
                int daysCount = value;

                if (daysCount < 1)
                    daysCount = 1;

                ViewState["Days"] = daysCount;
            }
        }

        /// <summary>
        /// Cell size in minutes.
        /// </summary>
        [Description("Cell width in pixels.")]
        [Category("Layout")]
        [DefaultValue(20)]
        public virtual int CellWidth
        {
            get
            {
                if (ViewState["CellWidth"] == null)
                    return 20;
                return (int)ViewState["CellWidth"];
            }
            set
            {
                ViewState["CellWidth"] = value;
            }
        }

        /// <summary>
        /// Number of cells per hour horizontally.
        /// </summary>
        [Description("Cell length in minutes.")]
        [Category("Layout")]
        [DefaultValue(60)]
        public int CellDuration
        {
            get
            {
                if (ViewState["CellDuration"] == null)
                    return 60;
                return (int)ViewState["CellDuration"];
            }
            set
            {
                ViewState["CellDuration"] = value;
            }
        }

        /// <summary>
        /// Использование двух заголовков день и часы(интервал 2 часа)
        /// </summary>
        public bool TwoHeaders { get; set; } = false;

        /// <summary>
        /// Collection of rows (resources).
        /// </summary>
        [Category("Behavior")]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [Description("Collection of rows that will be displayed on the vertical axis.")]
        public ResourceCollection Resources
        {
            get
            {
                if (ViewState["Resources"] == null)
                {
                    ResourceCollection rc = new ResourceCollection();
                    rc.designMode = DesignMode;

                    ViewState["Resources"] = rc;
                }
                return (ResourceCollection)ViewState["Resources"];
            }
        }

        /// <summary>
        /// Collection of row header columns.
        /// </summary>
        [Category("Behavior")]
        [PersistenceMode(PersistenceMode.InnerProperty)]
        [Description("Collection of row header columns that will be displayed on the vertical axis.")]
        public RowHeaderColumnCollection HeaderColumns
        {
            get
            {
                if (ViewState["HeaderColumns"] == null)
                {
                    RowHeaderColumnCollection rc = new RowHeaderColumnCollection();
                    rc.designMode = DesignMode;
                    ViewState["HeaderColumns"] = rc;
                }
                return (RowHeaderColumnCollection)ViewState["HeaderColumns"];
            }
        }

        /// <summary>
        /// Specify column widths in pixels (separated by commas) to enable multiple columns in row headers.
        /// </summary>
        [Category("Behavior")]
        [Description("Specify column widths in pixels (separated by commas) to enable multiple columns in row headers.")]
        public string RowHeaderColumnWidths
        {
            get
            {
                return (string)ViewState["RowHeaderColumnWidths"];
            }
            set
            {
                if (!WidthCollectionParser.IsValid(value))
                {
                    throw new ArgumentException("Invalid RowHeaderColumnWidths format. Valid example: \"60, 20, 20\".");
                }
                var widths = WidthCollectionParser.Parse(value);

                while (HeaderColumns.Count < widths.Count)
                {
                    HeaderColumns.Add(String.Empty, 10);
                }

                //HeaderColumns.Clear();
                for (int i = 0; i < widths.Count; i++)
                {
                    HeaderColumns[i].Width = widths[i];
                }

                ViewState["RowHeaderColumnWidths"] = value;
            }
        }


        /// <summary>
        /// Gets or sets the name of the column that contains the event starting date and time (must be convertible to DateTime).
        /// </summary>
        [Description("The name of the column that contains the event starting date and time (must be convertible to DateTime).")]
        [Category("Data")]
        public string DataStartField
        {
            get
            {
                return _dataStartField;
            }
            set
            {
                _dataStartField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Description("Номер пакета работ")]
        [Category("Data")]
        public string WPNOIField
        {
            get
            {
                return _wpnoiField;
            }
            set
            {
                _wpnoiField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Description("Номер пакета работ")]
        [Category("Data")]
        public string WPNOField
        {
            get
            {
                return _wpnoField;
            }
            set
            {
                _wpnoField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Description("Плановые трудозатраты")]
        [Category("Data")]
        public string MHRField
        {
            get
            {
                return _mhrField;
            }
            set
            {
                _mhrField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Description("Плановые трудозатраты")]
        [Category("Data")]
        public string BookedMHRField
        {
            get
            {
                return _bookedMHRField;
            }
            set
            {
                _bookedMHRField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Description("Время прилета до начала ТО DateTime")]
        [Category("Data")]
        public string ArrivalField
        {
            get
            {
                return _arrivalField;
            }
            set
            {
                _arrivalField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        //[Description("Количество дней, на которое отличаются даты прибытия и начала ТО")]
        //[Category("Data")]
        //public string ArrivalDifDaysField
        //{
        //    get
        //    {
        //        return _arrivalDifDaysField;
        //    }
        //    set
        //    {
        //        _arrivalDifDaysField = value;

        //        if (Initialized)
        //        {
        //            OnDataPropertyChanged();
        //        }

        //    }
        //}

        /// <summary>
        /// Gets or sets the name of the column that contains the event ending date and time (must be convertible to DateTime).
        /// </summary>
        [Description("The name of the column that contains the event ending date and time (must be convertible to DateTime).")]
        [Category("Data")]
        public string DataEndField
        {
            get
            {
                return _dataEndField;
            }
            set
            {
                _dataEndField = value;
                if (Initialized)
                {
                    OnDataPropertyChanged();
                }
            }
        }

        [Description("время вылета после TO(Datetime)")]
        [Category("Data")]
        public string DepartureField
        {
            get
            {
                return _departureField;
            }
            set
            {
                _departureField = value;
                if (Initialized)
                {
                    OnDataPropertyChanged();
                }
            }
        }

        //[Description("Количество дней, на которое отличаются даты отправления и окончания ТО")]
        //[Category("Data")]
        //public string DepartureDifDaysField
        //{
        //    get
        //    {
        //        return _departureDifDaysField;
        //    }
        //    set
        //    {
        //        _departureDifDaysField = value;
        //        if (Initialized)
        //        {
        //            OnDataPropertyChanged();
        //        }
        //    }
        //}

        /// <summary>
        /// Gets or sets the  name of the column that contains the name of an event.
        /// </summary>
        [Category("Data")]
        [Description("The name of the column that contains the name of an event.")]
        public string DataTextField
        {
            get
            {
                return _dataTextField;
            }
            set
            {
                _dataTextField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        /// <summary>
        /// Gets or sets the name of the column that contains the primary key. The primary key will be used for rendering the custom JavaScript actions.
        /// </summary>
        [Category("Data")]
        [Description("The name of the column that contains the primary key. The primary key will be used for rendering the custom JavaScript actions.")]
        [Obsolete("Use DataIdField instead.")]
        public string DataValueField
        {
            get { return DataIdField; }
            set { DataIdField = value; }
        }


        /// <summary>
        /// Gets or sets the name of the column that contains the primary key. The primary key will be used for rendering the custom JavaScript actions.
        /// </summary>
        [Category("Data")]
        [Description("The name of the column that contains the primary key. The primary key will be used for rendering the custom JavaScript actions.")]
        public string DataIdField
        {
            get
            {
                return _dataValueField;
            }
            set
            {
                _dataValueField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        /// <summary>
        /// Gets or sets the name of the column that contains the primary key. The primary key will be used for rendering the custom JavaScript actions.
        /// </summary>
        [Category("Data")]
        [Description("The name of the column that contains the column id.")]
        public string DataResourceField
        {
            get
            {
                return _dataResourceField;
            }
            set
            {
                _dataResourceField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Category("Data")]
        [Description("Название ресурса")]
        public string DataResourceNameField
        {
            get
            {
                return _dataResourceNameField;
            }
            set
            {
                _dataResourceNameField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Category("Data")]
        [Description("Название ресурса")]
        public string ACTypeField
        {
            get
            {
                return _acTypeField;
            }
            set
            {
                _acTypeField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }


        [Category("Data")]
        [Description("Название ресурса")]
        public string WorkTypeField
        {
            get
            {
                return _workTypeField;
            }
            set
            {
                _workTypeField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        [Category("Data")]
        [Description("Станция ТО")]
        public string StationNameField
        {
            get
            {
                return _stationNameField;
            }
            set
            {
                _stationNameField = value;

                if (Initialized)
                {
                    OnDataPropertyChanged();
                }

            }
        }

        /// <summary>
        /// Color of the hour names background.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of the hour names background.")]
        public Color HourNameBackColor
        {
            get
            {
                if (ViewState["HourNameBackColor"] == null)
                    return ColorTranslator.FromHtml("#ECE9D8");
                return (Color)ViewState["HourNameBackColor"];
            }
            set
            {
                ViewState["HourNameBackColor"] = value;
            }
        }

        /// <summary>
        /// Color of the horizontal border that separates our names.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of the horizontal border that separates our names.")]
        public Color HourNameBorderColor
        {
            get
            {
                if (ViewState["HourNameBorderColor"] == null)
                    return ColorTranslator.FromHtml("#ACA899");
                return (Color)ViewState["HourNameBorderColor"];
            }
            set
            {
                ViewState["HourNameBorderColor"] = value;
            }
        }



        /// <summary>
        /// Color of an event border.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of an event border.")]
        public Color EventBorderColor
        {
            get
            {
                if (ViewState["EventBorderColor"] == null)
                    return ColorTranslator.FromHtml("#000000");
                return (Color)ViewState["EventBorderColor"];
            }
            set
            {
                ViewState["EventBorderColor"] = value;
            }
        }

        /// <summary>
        /// Color of an event background.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of an event background.")]
        public Color EventBackColor
        {
            get
            {
                if (ViewState["EventBackColor"] == null)
                    return ColorTranslator.FromHtml("#FFFFFF");
                return (Color)ViewState["EventBackColor"];
            }
            set
            {
                ViewState["EventBackColor"] = value;
            }
        }


        ///<summary>
        ///Gets or sets the background color of the Web server control.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Drawing.Color"></see> that represents the background color of the control. The default is <see cref="F:System.Drawing.Color.Empty"></see>, which indicates that this property is not set.
        ///</returns>
        ///
        public override Color BackColor
        {
            get
            {
                if (ViewState["BackColor"] == null)
                    return ColorTranslator.FromHtml("#FFFFD5");
                return (Color)ViewState["BackColor"];
            }
            set
            {
                ViewState["BackColor"] = value;
            }
        }


        /// <summary>
        /// Gets or sets the start of the business day (in hours).
        /// </summary>
        [Description("Start of the business day (hour from 0 to 23).")]
        [Category("Behavior")]
        [DefaultValue(9)]
        public int BusinessBeginsHour
        {
            get
            {
                if (ViewState["BusinessBeginsHour"] == null)
                    return 9;
                return (int)ViewState["BusinessBeginsHour"];
            }
            set
            {
                if (value < 0)
                    ViewState["BusinessBeginsHour"] = 0;
                else if (value > 23)
                    ViewState["BusinessBeginsHour"] = 23;
                else
                    ViewState["BusinessBeginsHour"] = value;
            }
        }


        /// <summary>
        /// Gets or sets the end of the business day (hours).
        /// </summary>
        [Description("End of the business day (hour from 1 to 24).")]
        [Category("Behavior")]
        [DefaultValue(18)]
        public int BusinessEndsHour
        {
            get
            {
                if (ViewState["BusinessEndsHour"] == null)
                    return 18;
                return (int)ViewState["BusinessEndsHour"];
            }
            set
            {
                if (value < BusinessBeginsHour)
                    ViewState["BusinessEndsHour"] = BusinessBeginsHour + 1;
                else if (value > 24)
                    ViewState["BusinessEndsHour"] = 24;
                else
                    ViewState["BusinessEndsHour"] = value;
            }
        }

        /// <summary>
        /// Font family of the row header, e.g. "Tahoma".
        /// </summary>
        [Description("Font family of the event text, e.g. \"Tahoma\".")]
        [Category("Appearance")]
        [DefaultValue("Tahoma")]
        public string HeaderFontFamily
        {
            get
            {
                if (ViewState["HeaderFontFamily"] == null)
                    return "Tahoma";

                return (string)ViewState["HeaderFontFamily"];
            }
            set
            {
                ViewState["HeaderFontFamily"] = value;
            }
        }

        /// <summary>
        /// Color of the column header font.
        /// </summary>
        [Description("Color of the column header font.")]
        [Category("Appearance")]
        public Color HeaderFontColor
        {
            get
            {
                if (ViewState["HeaderFontColor"] == null)
                    return ColorTranslator.FromHtml("#000000");

                return (Color)ViewState["HeaderFontColor"];
            }
            set
            {
                ViewState["HeaderFontColor"] = value;
            }
        }

        /// <summary>
        /// Font size of the row header, e.g. "10pt".
        /// </summary>
        [Description("Font size of the row header, e.g. \"10pt\".")]
        [Category("Appearance")]
        [DefaultValue("10pt")]
        public string HeaderFontSize
        {
            get
            {
                if (ViewState["HeaderFontSize"] == null)
                    return "10pt";

                return (string)ViewState["HeaderFontSize"];
            }
            set
            {
                ViewState["HeaderFontSize"] = value;
            }
        }


        /// <summary>
        /// Font family of the time axis headers, e.g. "Tahoma".
        /// </summary>
        [Category("Appearance")]
        [Description("Font family of the hour names (horizontal axis), e.g. \"Tahoma\".")]
        [DefaultValue("Tahoma")]
        public string HourFontFamily
        {
            get
            {
                if (ViewState["HourFontFamily"] == null)
                    return "Tahoma";

                return (string)ViewState["HourFontFamily"];
            }
            set
            {
                ViewState["HourFontFamily"] = value;
            }
        }

        /// <summary>
        /// Font size of the time axis header e.g. "16pt".
        /// </summary>
        [Description("Font size of the hour names (horizontal axis), e.g. \"10pt\".")]
        [Category("Appearance")]
        [DefaultValue("10pt")]
        public string HourFontSize
        {
            get
            {
                if (ViewState["HourFontSize"] == null)
                    return "10pt";

                return (string)ViewState["HourFontSize"];
            }
            set
            {
                ViewState["HourFontSize"] = value;
            }
        }

        /// <summary>
        /// Font family of the event text, e.g. "Tahoma".
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Tahoma")]
        [Description("Font family of the event text, e.g. \"Tahoma\".")]
        public string EventFontFamily
        {
            get
            {
                if (ViewState["EventFontFamily"] == null)
                    return "Tahoma";

                return (string)ViewState["EventFontFamily"];
            }
            set
            {
                ViewState["EventFontFamily"] = value;
            }
        }

        /// <summary>
        /// Font size of the event text, e.g. "8pt".
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("7pt")]
        [Description("Font size of the event text, e.g. \"7pt\".")]
        public string EventFontSize
        {
            get
            {
                if (ViewState["EventFontSize"] == null)
                    return "7pt";

                return (string)ViewState["EventFontSize"];
            }
            set
            {
                ViewState["EventFontSize"] = value;
            }
        }

        /// <summary>
        /// Height of the event cell in pixels.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("25")]
        [Description("Height of the event cell in pixels.")]
        public int EventHeight
        {
            get
            {
                if (ViewState["EventHeight"] == null)
                    return 25;

                return (int)ViewState["EventHeight"];
            }
            set
            {
                ViewState["EventHeight"] = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue("20")]
        [Description("Высота блока времени прилета")]
        public int TimeSpendingHeight
        {
            get
            {
                if (ViewState["TimeSpendingHeight"] == null)
                    return 20;

                return (int)ViewState["TimeSpendingHeight"];
            }
            set
            {
                ViewState["TimeSpendingHeight"] = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue("4")]
        [Description("Высота бара(черточки)")]
        public int HeightBar
        {
            get
            {
                if (ViewState["HeightBar"] == null)
                    return 4;

                return (int)ViewState["HeightBar"];
            }
            set
            {
                ViewState["HeightBar"] = value;
            }
        }

        /// <summary>
        /// Height of the header cells (with hour names) in pixels.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("17")]
        [Description("Height of the header cells (with hour names) in pixels.")]
        public int HeaderHeight
        {
            get
            {
                if (ViewState["HeaderHeight"] == null)
                    return 17;

                return (int)ViewState["HeaderHeight"];
            }
            set
            {
                ViewState["HeaderHeight"] = value;
            }
        }

        /// <summary>
        /// Background color of time cells outside of the busines hours.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("#FFF4BC")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Background color of time cells outside of the busines hours.")]
        public Color NonBusinessBackColor
        {
            get
            {
                if (ViewState["NonBusinessBackColor"] == null)
                    return ColorTranslator.FromHtml("#FFF4BC");

                return (Color)ViewState["NonBusinessBackColor"];
            }
            set
            {
                ViewState["NonBusinessBackColor"] = value;
            }
        }

        /// <summary>
        /// Color of the horizontal border that separates hour names.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of the vertical border that separates hour names.")]
        public Color HourBorderColor
        {
            get
            {
                if (ViewState["HourBorderColor"] == null)
                    return ColorTranslator.FromHtml("#EAD098");
                return (Color)ViewState["HourBorderColor"];

            }
            set
            {
                ViewState["HourBorderColor"] = value;
            }
        }

        ///<summary>
        ///Gets or sets the border color of the Web control.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Drawing.Color"></see> that represents the border color of the control. The default is <see cref="F:System.Drawing.Color.Empty"></see>, which indicates that this property is not set.
        ///</returns>
        public override Color BorderColor
        {
            get
            {
                if (ViewState["BorderColor"] == null)
                    return ColorTranslator.FromHtml("#000000");
                return (Color)ViewState["BorderColor"];
            }
            set
            {
                ViewState["BorderColor"] = value;
            }
        }


        /// <summary>
        /// Color of the horizontal bar on the top of an event.
        /// </summary>
        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        [Description("Color of the horizontal bar on the top of an event.")]
        public Color DurationBarColor
        {
            get
            {
                if (ViewState["DurationBarColor"] == null)
                    return ColorTranslator.FromHtml("blue");
                return (Color)ViewState["DurationBarColor"];
            }
            set
            {
                ViewState["DurationBarColor"] = value;
            }
        }


        /// <summary>
        /// Gets or sets the time-format for hour numbers (on the top).
        /// </summary>
        [Category("Appearance")]
        [Description("The time-format that will be used for the hour numbers.")]
        [DefaultValue(TimeFormat.Clock12Hours)]
        public TimeFormat TimeFormat
        {
            get
            {
                if (ViewState["TimeFormat"] == null)
                    return TimeFormat.Clock12Hours;
                return (TimeFormat)ViewState["TimeFormat"];
            }
            set
            {
                ViewState["TimeFormat"] = value;
            }
        }


        /// <summary>
        /// Width of the control (pixels and percentage values supported).
        /// </summary>
        [Description("Width of the control (pixels and percentage values supported).")]
        public new int Width
        {
            get
            {
                return RowHeaderWidthResolved + CellCount * CellWidth;
            }
        }


        /// <summary>
        /// Width of the row header (resource names) in pixels.
        /// </summary>
        [Description("Width of the row header (resource names) in pixels.")]
        [Category("Appearance")]
        [DefaultValue(80)]
        public int RowHeaderWidth
        {
            get
            {
                if (ViewState["RowHeaderWidth"] == null)
                    return 80;
                return (int)ViewState["RowHeaderWidth"];
            }
            set
            {
                ViewState["RowHeaderWidth"] = value;
            }
        }

        /// <summary>
        /// Whether the color bar on the left side of and event should be visible.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Whether the duration bar on the top of and event should be visible.")]
        public bool DurationBarVisible
        {
            get
            {
                if (ViewState["DurationBarVisible"] == null)
                    return true;
                return (bool)ViewState["DurationBarVisible"];
            }
            set
            {
                ViewState["DurationBarVisible"] = value;
            }
        }

        /// <summary>
        /// Handling of user action (clicking an event).
        /// </summary>
        [Category("User actions")]
        [Description("Whether clicking an event should do a postback or run a javascript action. By default, it calls the javascript code specified in EventClickJavaScript property.")]
        [DefaultValue(EventClickHandlingEnum.Disabled)]
        public EventClickHandlingEnum EventClickHandling
        {
            get
            {
                if (ViewState["EventClickHandling"] == null)
                    return EventClickHandlingEnum.Disabled;
                return (EventClickHandlingEnum)ViewState["EventClickHandling"];
            }
            set
            {
                ViewState["EventClickHandling"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the Javascript code that is executed when the users clicks on an event. '{0}' will be replaced by the primary key of the event.
        /// </summary>
        [Description("Javascript code that is executed when the users clicks on an event. '{0}' will be replaced by the primary key of the event.")]
        [Category("User actions")]
        [DefaultValue("alert('{0}');")]
        public string EventClickJavaScript
        {
            get
            {
                if (ViewState["EventClickJavaScript"] == null)
                    return "alert('{0}');";
                return (string)ViewState["EventClickJavaScript"];
            }
            set
            {
                ViewState["EventClickJavaScript"] = value;
            }
        }

        /// <summary>
        /// Handling of user action (clicking a free-time slot).
        /// </summary>
        [Category("User actions")]
        [Description("Whether clicking a free-time slot should do a postback or run a javascript action. By default, it calls the javascript code specified in TimeRangeSelectedJavaScript property.")]
        [DefaultValue(TimeRangeSelectedHandling.Disabled)]
        public TimeRangeSelectedHandling TimeRangeSelectedHandling
        {
            get
            {
                if (ViewState["TimeRangeSelectedHandling"] == null)
                    return TimeRangeSelectedHandling.Disabled;
                return (TimeRangeSelectedHandling)ViewState["TimeRangeSelectedHandling"];
            }
            set
            {
                ViewState["TimeRangeSelectedHandling"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the Javascript code that is executed when the users clicks on a free time slot. '{0}' will be replaced by the starting time of that slot (i.e. '9:00'.
        /// </summary>
        [Description("Javascript code that is executed when the users clicks on a free time slot. '{0}' will be replaced by the starting time of that slot (i.e. '9:00'.")]
        [Category("User actions")]
        [DefaultValue("alert('{0}, {1}');")]
        public string TimeRangeSelectedJavaScript
        {
            get
            {
                if (ViewState["TimeRangeSelectedJavaScript"] == null)
                    return "alert('{0}, {1}');";
                return (string)ViewState["TimeRangeSelectedJavaScript"];
            }
            set
            {
                ViewState["TimeRangeSelectedJavaScript"] = value;
            }
        }

        [Category("Appearance")]
        [TypeConverter(typeof(WebColorConverter))]
        public Color HoverColor
        {
            get
            {
                if (ViewState["HoverColor"] == null)
                    return ColorTranslator.FromHtml("#FFED95");
                return (Color)ViewState["HoverColor"];

            }
            set
            {
                ViewState["HoverColor"] = value;
            }
        }

        /// <summary>
        /// Sets or gets the view type (resources or days). If set to <see cref="ViewTypeEnum.Resources">ViewTypeEnum.Resources</see> and you use data binding you have to specify <see cref="DataResourceField">DataResourceField</see> property.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(ViewTypeEnum.Resources)]
        [Description("Sets the view type.")]
        public ViewTypeEnum ViewType
        {
            get
            {
                if (ViewState["ViewType"] == null)
                    return ViewTypeEnum.Resources;

                return (ViewTypeEnum)ViewState["ViewType"];
            }
            set
            {
                ViewState["ViewType"] = value;
            }
        }


        [Category("Appearance")]
        [DefaultValue(true)]
        public bool CssOnly
        {
            get
            {
                if (ViewState["CssOnly"] == null)
                    return true;

                return (bool)ViewState["CssOnly"];
            }
            set
            {
                ViewState["CssOnly"] = value;
            }
        }

        /// <summary>
        /// Specifies the prefix of the CSS classes that contain style definitions for the elements of this control.
        /// </summary>
        [Category("Appearance")]
        [Description("Specifies the prefix of the CSS classes that contain style definitions for the elements of this control. Obsolete: Use Theme property instead.")]
        [Obsolete("Use Theme property instead.")]
        public string CssClassPrefix
        {
            get
            {
                return Theme;
            }
            set
            {
                Theme = value;
            }
        }

        /// <summary>
        /// Specifies the CSS theme.
        /// </summary>
        [Category("Appearance")]
        [Description("Specifies the CSS theme.")]
        [DefaultValue("scheduler_default")]
        public string Theme
        {
            get
            {
                if (ViewState["Theme"] == null)
                {
                    return "scheduler_default";
                } 
                return (string)ViewState["Theme"];
            }
            set
            {
                ViewState["Theme"] = value;
            }
        }

        #endregion

        #region Data binding

        ///<summary>
        ///Retrieves data from the associated data source.
        ///</summary>
        ///
        protected override void PerformSelect()
        {
            if (!IsBoundUsingDataSourceID)
            {
                OnDataBinding(EventArgs.Empty);
            }

            GetData().Select(CreateDataSourceSelectArguments(), OnDataSourceViewSelectCallback);

            RequiresDataBinding = false;
            MarkAsDataBound();

            OnDataBound(EventArgs.Empty);
        }

        private void OnDataSourceViewSelectCallback(IEnumerable retrievedData)
        {
            if (IsBoundUsingDataSourceID)
            {
                OnDataBinding(EventArgs.Empty);
            }
            PerformDataBinding(retrievedData);
        }


        ///<summary>
        ///Binds data from the data source to the control. 
        ///</summary>
        ///
        ///<param name="retrievedData">The <see cref="T:System.Collections.IEnumerable"></see> list of data returned from a <see cref="M:System.Web.UI.WebControls.DataBoundControl.PerformSelect"></see> method call.</param>
        protected override void PerformDataBinding(IEnumerable retrievedData)
        {
            ViewState["Items"] = new List<Event>();

            // don't load events in design mode
            if (DesignMode)
            {
                return;
            }

            base.PerformDataBinding(retrievedData);


            // Verify data exists.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (retrievedData == null)
            {
                return;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            // ReSharper restore HeuristicUnreachableCode

            if (String.IsNullOrEmpty(DataStartField))
            {
                throw new NullReferenceException("DataStartField property must be specified.");
            }

            if (String.IsNullOrEmpty(DataEndField))
            {
                throw new NullReferenceException("DataEndField property must be specified.");
            }

            if (String.IsNullOrEmpty(DataTextField))
            {
                throw new NullReferenceException("DataTextField property must be specified.");
            }

            if (String.IsNullOrEmpty(DataIdField))
            {
                throw new NullReferenceException("DataIdField property must be specified.");
            }

            if (ViewType == ViewTypeEnum.Resources && String.IsNullOrEmpty(DataResourceField))
            {
                throw new NullReferenceException("DataResourceField property must be specified in Resources mode (ViewType).");
            }

            foreach (object dataItem in retrievedData)
            {

                DateTime start;
                DateTime end;


                var mhr = string.Empty; 
                if (!string.IsNullOrEmpty(MHRField) && MHRField != null)
                    mhr = DataBinder.GetPropertyValue(dataItem, MHRField, null);
                var bookedMHR = string.Empty;
                if(!string.IsNullOrEmpty(BookedMHRField) && BookedMHRField != null)
                    bookedMHR = DataBinder.GetPropertyValue(dataItem, BookedMHRField, null);

                var stationName = string.Empty;
                if(!string.IsNullOrEmpty(StationNameField) && StationNameField != null)
                    stationName = DataBinder.GetPropertyValue(dataItem, StationNameField, null);

                string strStart = DataBinder.GetPropertyValue(dataItem, DataStartField, null);
                if (!DateTime.TryParse(strStart, out start))
                {
                    throw new FormatException(String.Format("Unable to convert '{0}' (from DataStartField column) to DateTime.", strStart));
                }

                string strEnd = DataBinder.GetPropertyValue(dataItem, DataEndField, null);
                if (!DateTime.TryParse(strEnd, out end))
                {
                    throw new FormatException(String.Format("Unable to convert '{0}' (from DataEndField column) to DateTime.", strEnd));
                }



                var arrival = DataBinder.GetPropertyValue(dataItem, ArrivalField, null);
                if (!DateTime.TryParse(arrival, out DateTime arrivalDateTime))
                {
                    throw new FormatException(String.Format("Unable to convert '{0}' (from ArrivalField column) to DateTime.", arrival));
                }
                var departure = DataBinder.GetPropertyValue(dataItem, DepartureField, null);
                if (!DateTime.TryParse(departure, out DateTime departureDateTime))
                {
                    throw new FormatException(String.Format("Unable to convert '{0}' (from DepartureField column) to DateTime.", departureDateTime));
                }

                string name = DataBinder.GetPropertyValue(dataItem, DataTextField, null);
                string val = DataBinder.GetPropertyValue(dataItem, DataValueField, null);


                var resourceName = Convert.ToString(DataBinder.GetPropertyValue(dataItem, DataResourceNameField, null));
                var acTypeString = DataBinder.GetPropertyValue(dataItem, ACTypeField, null);
                if (!Enum.TryParse(acTypeString, out ACType acType))
                {
                    throw new FormatException(String.Format("Unable to convert '{0}' (from ACTypeField column) to ACType.", acTypeString));
                }

                var workTypeString = DataBinder.GetPropertyValue(dataItem, WorkTypeField, null);
                if (!Enum.TryParse(workTypeString, out WorkType workType))
                {
                    workType = WorkType.Optional;
                    //throw new FormatException(String.Format("Unable to convert '{0}' (from WorkTypeField column) to WorkType.", workTypeString));
                }

                string resourceId = null;
                if (ViewType == ViewTypeEnum.Resources)
                {
                    resourceId = Convert.ToString(DataBinder.GetPropertyValue(dataItem, DataResourceField, null));    
                }
                else
                {
                    resourceId = val;
                }

                ((List<Event>)ViewState["Items"]).Add(new Event(val, StartDate, EndDate, start, end, arrivalDateTime, departureDateTime,
                    name, resourceId, resourceName, acType, workType, dataItem, Convert.ToInt32(EventFontSize.Replace("px", "")), CellWidth, CellDuration,  mhr, bookedMHR, stationName));

            }
            //Убрать если хочется, чтобы компактно выглядело
            //((ArrayList)ViewState["Items"]).Sort(new EventComparer());
        }
        #endregion


        private int CellCount
        {
            get
            {
                return Days * 24 * 60 / CellDuration;
            }
        }

        private string GetCellColor(DateTime from)
        {
            bool isBusiness = IsBusinessCell(from);

            return isBusiness ? ColorTranslator.ToHtml(BackColor) : ColorTranslator.ToHtml(NonBusinessBackColor);
        }

        private bool IsBusinessCell(DateTime from)
        {
            if (from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }
            if (CellDuration < 720) // use hours
            {
                if (from.Hour < BusinessBeginsHour || from.Hour >= BusinessEndsHour)
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        ///<summary>
        ///When implemented by a class, enables a server control to process an event raised when a form is posted to the server.
        ///</summary>
        ///
        ///<param name="eventArgument">A <see cref="T:System.String"></see> that represents an optional event argument to be passed to the event handler. </param>
        public void RaisePostBackEvent(string eventArgument)
        {
            if (eventArgument.StartsWith("PK:"))
            {
                string pk = eventArgument.Substring(3, eventArgument.Length - 3);
                DoEventClick(new EventClickEventArgs(pk));
            }
            else if (eventArgument.StartsWith("TIME:"))
            {
                DateTime time = Convert.ToDateTime(eventArgument.Substring(5, 19));
                string resource = eventArgument.Substring(24);
                DoTimeRangeSelected(new TimeRangeSelectedEventArgs(time, time.AddMinutes(CellDuration), resource));
            }
            else if (eventArgument.StartsWith("COL:"))
            {
                string arg = eventArgument.Substring(4);
                RowHeaderColumnWidths = arg;
                /*
                List<int> widths = WidthCollectionParser.Parse(arg);
                for (int i = 0; i < HeaderColumns.Count; i++ )
                {
                    HeaderColumns[i].Width = widths[i];
                }
                 */
                //RowHeaderWidth = RowHeaderColumnWidthTotal;

                DoHeaderColumnWidthChanged(new HeaderColumnWidthChangedEventArgs());
            }
            else
            {
                throw new ArgumentException("Bad argument passed from postback event.");
            }

        }

        private void DoHeaderColumnWidthChanged(HeaderColumnWidthChangedEventArgs e)
        {
            if (HeaderColumnWidthChanged != null)
            {
                HeaderColumnWidthChanged(this, e);
            }
        }

        private void DoTimeRangeSelected(TimeRangeSelectedEventArgs e)
        {
            if (TimeRangeSelected != null)
            {
                TimeRangeSelected(this, e);
            }
        }

        private void DoEventClick(EventClickEventArgs e)
        {
            if (EventClick != null)
            {
                EventClick(this, e);
            }
        }

        public void SetDataSource(DataTable data)
        {
            base.DataSource = data;
        }

        public void GetDataBing()
        {
            base.DataBind();
        }
    }
}
