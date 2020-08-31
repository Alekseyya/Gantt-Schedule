using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DayPilot.Web.Ui
{
	/// <summary>
	/// Block is a set of concurrent events.
	/// </summary>
	public class Block
	{
		public List<Column> Columns;
		private List<Event> events = new List<Event>();


		internal Block()
		{
		}

		internal void Add(Event ev)
		{
            events.Add(ev);
			arrangeColumns();
		}

		private Column createColumn()
		{
			Column col = new Column();
			this.Columns.Add(col);
			col.Block = this;

			return col;
		}


		private void arrangeColumns()
		{
			// cleanup
			this.Columns = new List<Column>();

			foreach(Event e in events)
				e.Column = null;

			// there always will be at least one column because arrangeColumns is called only from Add()
			createColumn();

			foreach (Event e in events)
			{
				foreach (Column col in Columns)
				{
					if (col.CanAdd(e))
					{
						col.Add(e);
						break;
					}
				}
				// Невозможно вставить в column(row), тогда создается новая Column(row) и добавляется бар(Event)
				if (e.Column == null)
				{
					Column col = createColumn();
					col.Add(e);
				}
			}
		}
		//Перекрывает или нет
		internal bool OverlapsWith(Event e)
		{
			if (events.Count == 0)
				return false;
            //this. box - это предыдущий блок, e - текущий блок
            //Если в прерыдущем column одни типы ВС - A32S - а в следущем другой тип, новый блок не создавать
			//Если надо будет пододвинуть блоки в один ряд, убрать и немного переделать
            //if (events.Cast<Event>().All(x => x.ACType != e.ACType))
			if(events.Any(x=>x.ACType != e.ACType) || events.Any(evt => evt.WorkType != e.WorkType))
                return true;

            return (this.BoxStart < e.BoxEnd && this.BoxEnd > e.BoxStart);
		}

		internal DateTime BoxStart
		{
			get
			{
				DateTime min = DateTime.MaxValue;

				foreach(Event ev in events)
				{
					if (ev.BoxStart < min)
						min = ev.BoxStart;
				}

				return min;
			}
		}
		
		internal DateTime BoxEnd
		{
			get
			{
				DateTime max = DateTime.MinValue;

				foreach(Event ev in events)
				{
					if (ev.BoxEnd > max)
						max = ev.BoxEnd;
				}

				return max;
			}
		}

	}
}
