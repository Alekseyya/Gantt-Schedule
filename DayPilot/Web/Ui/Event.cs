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
using Core.Extensions;
using Core.Model;

namespace DayPilot.Web.Ui
{
	/// <summary>
	/// Summary description for Event.
	/// </summary>
	[Serializable]
	public class Event
	{

        [NonSerialized]
        internal object Source;

	    /// <summary>
	    /// Event start.
	    /// </summary>
	    public DateTime Start;


        public Int64 WPNOI;
        public string WPNO;
        public string MHR;
        public string BookedMHR;

        /// <summary>
        /// ¬рем€ прилета до начала “ќ
        /// </summary>
        public DateTime Arrival;

		/// <summary>
		/// Event end;
		/// </summary>
		public DateTime End;

        /// <summary>
        /// врем€ вылета после TO
        /// </summary>
        public DateTime Departure;

        /// <summary>
		/// Event name;
		/// </summary>
		public string Text;

		/// <summary>
		/// Event primary key.
		/// </summary>
		public string Id;

	    public string Resource;
	    public string ResourceName;
	    public ACType ACType;
	    public WorkType WorkType;

		/// <summary>
		/// Column to which this event belongs.
		/// </summary>
		[NonSerialized]
		public Column Column;

        public string StationName;

        /// <summary>
		/// Constructor.
		/// </summary>
		public Event()
		{
		}

		/// <summary>
		/// Constructor that prefills the fields.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="text"></param>
		public Event(string id, DateTime start, DateTime end, string text) : this(id, start, end, text, null, null)
		{

		}

        public Event(string id, DateTime start, DateTime end, string text, string resource, object source)
        {
            this.Id = id;
            this.Start = start;
            this.End = end;
            this.Text = text;
            this.Resource = resource;
            Source = source;
        }

        public Event(string id, DateTime start, DateTime end, DateTime arrival, DateTime departure, string text, string resource, object source)
        {
            this.Id = id;
            this.Start = start;
            this.Arrival = arrival;
            this.End = end;
            this.Departure = departure;
            this.Text = text;
            this.Resource = resource;
            Source = source;
        }

        public Event(string id, DateTime dayStart, DateTime dayEnd,
            DateTime start, DateTime end, DateTime arrival, DateTime departure,
            string text, string resource, string resourceName, ACType acType, object source, 
            int fontSize, int cellWidth, int cellDuration, Int64 wpnoi,
            string wpno, string mhr, string bookedMHR, string stationName)
        {
            this.Id = id;
            this.Start = start;
            this.Arrival = arrival;
            this.End = end;
            this.Departure = departure;
            this.Text = text.Replace("\t", "").Replace("\n", "");
            this.Resource = resource;
            this.ResourceName = resourceName;
            this.ACType = acType;
            this.WPNOI = wpnoi;
            this.WPNO = wpno;
            this.MHR = mhr;
            this.BookedMHR = bookedMHR;
            this.StationName = stationName;
            Source = source;
            var boxes = SetBoxes(dayStart, dayEnd, fontSize, cellWidth, cellDuration);
            BoxStart = boxes.BoxStart;
            BoxEnd = boxes.BoxEnd;
        }

        public Event(string id, DateTime dayStart, DateTime dayEnd,
            DateTime start, DateTime end, DateTime arrival, DateTime departure,
            string text, string resource, string resourceName, ACType acType, WorkType workType, object source,
            int fontSize, int cellWidth, int cellDuration, string mhr, string bookedMHR, string stationName)
        {
            this.Id = id;
            this.Start = start;
            this.Arrival = arrival;
            this.End = end;
            this.Departure = departure;
            this.Text = text.Replace("\t", "").Replace("\n", "");
            this.Resource = resource;
            this.ResourceName = resourceName;
            this.WorkType = workType;
            this.ACType = acType;
            //this.WPNOI = wpnoi;
            //this.WPNO = wpno;
            this.MHR = mhr;
            this.BookedMHR = bookedMHR;
            this.StationName = stationName;
            Source = source;
            var boxes = SetBoxes(dayStart, dayEnd, fontSize, cellWidth, cellDuration);
            BoxStart = boxes.BoxStart;
            BoxEnd = boxes.BoxEnd;
        }


        public (DateTime BoxStart, DateTime BoxEnd) SetBoxes(DateTime dayStart, DateTime dayEnd, int fontSize,
            int cellWidth, int cellDuration)
        {
            var boxStart = DateTime.MinValue;
            var boxEnd = DateTime.MaxValue;

            //вычисление ширины самой длинной строки, если текст многострочный
            var lines = Text.Split(new string[] { "<br/>", "\n" }, StringSplitOptions.None);
            var textWidth = 0;
            foreach(string str in lines)
            {
                if(textWidth < str.CalculateWidthToPixels(fontSize))
                {
                    textWidth = str.CalculateWidthToPixels(fontSize) + 2;
                }
            }

            //Ѕлок раст€нут на весь интервал
            if (Start <= dayStart && End >= dayEnd)
            {
                boxStart = dayStart;
                boxEnd = dayEnd;
                return (boxStart, boxEnd);
            }
            //блок начинаетс€ с начала интервала и заканчиваетс€ где-то в середине
            var widthOfBarWithDayStart = WidthOfBar(dayStart, End, cellWidth, cellDuration);
            if (Start < dayStart && (textWidth > widthOfBarWithDayStart || textWidth <= widthOfBarWithDayStart))
            {
                boxStart = dayStart;
                boxEnd = CalculateWidthOfDateTime(widthOfBarWithDayStart, textWidth, dayStart, End, ActionBlock.Right, cellWidth, cellDuration).boxEnd;
                return (boxStart, boxEnd);
            }
            //Ѕлок находитс€ в середине интервала
            var widthOfBarInMiddleDay = WidthOfBar(Start, End, cellWidth, cellDuration);
            if (textWidth <= widthOfBarInMiddleDay && (dayStart < Start && dayEnd > End))
            {
                boxStart = Start;
                boxEnd = End;
                return (boxStart, boxEnd);
            }
            //≈сли блок больше бара и находитс€ в середине
            if (textWidth > widthOfBarInMiddleDay)
            {
                var widthBoxMiddle = CalculateWidthOfDateTime(widthOfBarInMiddleDay, textWidth, Start, End, ActionBlock.LeftAndRight, cellWidth, cellDuration);
                //≈сли блок находитс€ в середине, но текст расширил блок так, что он выходит за dayStart( начало сетки)
                if (widthBoxMiddle.boxStart < dayStart)
                {
                    boxStart = dayStart;
                    boxEnd = widthBoxMiddle.boxEnd + (dayStart - widthBoxMiddle.boxStart);
                    return (boxStart, boxEnd);
                }//≈сли блок находитс€ по середине, но текст расширил блок так, что он выходит за dayEnd(конец сетки)
                else if (widthBoxMiddle.boxEnd > dayEnd)
                {
                    boxStart = widthBoxMiddle.boxStart - (widthBoxMiddle.boxEnd - dayEnd);
                    boxEnd = dayEnd;
                    return (boxStart, boxEnd);
                }
                //≈сли блок находитс€ посередине
                else
                {
                    boxStart = widthBoxMiddle.boxStart;
                    boxEnd = widthBoxMiddle.boxEnd;
                    return (boxStart, boxEnd);
                }
            }
            //блок находитс€ справа, начало в середине
            var widthBarRight = WidthOfBar(Start, dayEnd, cellWidth, cellDuration);
            if ((textWidth <= widthBarRight || textWidth > widthBarRight) && End >= dayEnd)
            {
                var widthBoxRight = CalculateWidthOfDateTime(widthBarRight, textWidth, Start, dayEnd, ActionBlock.Left, cellWidth, cellDuration);
                boxStart = widthBoxRight.boxStart;
                boxEnd = widthBoxRight.boxEnd;
                return (boxStart, boxEnd);
            }

            return (boxStart, boxEnd);
        }

        public int WidthOfBar(DateTime start, DateTime end, int cellWidth, int cellDuration)
        {
           return (int)Math.Floor((end - start).TotalMinutes * cellWidth / cellDuration) - 2;
        }

        /// <summary>
        /// ¬ какую сторону наращивать блок
        /// </summary>
        public enum ActionBlock
        {
            Left,
            Right,
            LeftAndRight
        }


        public (DateTime boxStart, DateTime boxEnd) CalculateWidthOfDateTime(int widthOfBar, int textWidth, DateTime barStart, DateTime barEnd, ActionBlock action,
                    int cellWidth, int cellDuration)
        {
            while (true)
            {
                if (widthOfBar >= textWidth)
                {
                    return (barStart, barEnd);
                }

                switch (action)
                {
                    case ActionBlock.Left:
                        barStart = barStart.AddMinutes(-1);
                        widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;
                        break;
                    case ActionBlock.Right:
                        barEnd = barEnd.AddMinutes(1);
                        widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;
                        break;
                    case ActionBlock.LeftAndRight:
                        barStart = barStart.AddMinutes(-1);
                        barEnd = barEnd.AddMinutes(1);
                        widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;
                        break;
                }
            }
        }

        
        public DateTime BoxStart { get; set; }
        public DateTime BoxEnd { get; set; }
        
        /// <summary>
        /// ѕерекрываютс€ ли блоки
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool OverlapsWith(Event e)
		{
            //Todo старый код, который раскизывает ементы между собой
            //return (this.BoxStart < e.BoxEnd && this.BoxEnd > e.Start);

            //≈сли в прерыдущем column одни типы ¬— - A32S - а в следущем другой тип, новый блок не создавать
            //≈брать если надо все компактно выводить
            if (this.ACType != e.ACType || this.WorkType != e.WorkType)
                return true;
            return (this.BoxStart < e.BoxEnd && this.BoxEnd > e.BoxStart);
        }

	}
}
