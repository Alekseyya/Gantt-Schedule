using System;
using Core.Extensions;
using Core.Model;
using Xunit;
using Xunit.Abstractions;
using static DayPilot.Web.Ui.Event;

namespace Test.Event
{
    public class EventTest
    {
        private readonly ITestOutputHelper _output;
        public EventTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EventTest_CalculateWidthOfDateTime_LeftAndRight()
        {
            var eve = new DayPilot.Web.Ui.Event();
            var barStart = new DateTime(2020, 07, 27, 17, 00, 00);
            var barEnd = new DateTime(2020, 07, 28, 09, 00, 00);
            var cellDuration = 120;
            var cellWidth = 20;
            var text = "МОЙКА ГВТ<br>A32S VP-BJA A02 - 113:01 MH (готовность 28.07.2020)";
            var textWidth = text.CalculateWidthToPixels(11) / 2;
            var widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;

            var result = eve.CalculateWidthOfDateTime(widthOfBar, textWidth, barStart, barEnd, ActionBlock.LeftAndRight, cellWidth, cellDuration);
            _output.WriteLine($"BoxStart: {result.boxStart}   BoxEnd:{result.boxEnd}");
        }

        [Fact]
        public void EventTest_CalculateWidthOfDateTime_Right()
        {
            var eve = new DayPilot.Web.Ui.Event();
            var barStart = new DateTime(2020, 07, 27, 00, 00, 00);
            var barEnd = new DateTime(2020, 07, 28, 05, 00, 00);
            var cellDuration = 120;
            var cellWidth = 20;
            var text = "A20-10-1159-01(ВВОД В ХРАНЕНИЕ)<br>A32S VP-BIP ADD - 114:12 MH (готовность 28.07.2020)";
            var textWidth = text.CalculateWidthToPixels(11) / 2;
            var widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;

            var result = eve.CalculateWidthOfDateTime(widthOfBar, textWidth, barStart, barEnd, ActionBlock.Right, cellWidth, cellDuration);
            _output.WriteLine($"BoxStart: {result.boxStart}   BoxEnd:{result.boxEnd}");
            Assert.Equal(result.boxStart, barStart);
        }

        [Fact]
        public void EventTest_CalculateWidthOfDateTime_Left()
        {
            var eve = new DayPilot.Web.Ui.Event();
            var barStart = new DateTime(2020, 07, 28, 16, 00, 00);
            var barEnd = new DateTime(2020, 07, 30, 00, 00, 00);
            var cellDuration = 120;
            var cellWidth = 20;
            var text = "A20-10-1159-01(ВВОД В ХРАНЕНИЕ)<br>A32S VP-BIP ADD - 114:12 MH (готовность 28.07.2020)";
            var textWidth = text.CalculateWidthToPixels(11) / 2;
            var widthOfBar = (int)Math.Floor((barEnd - barStart).TotalMinutes * cellWidth / cellDuration) - 2;

            var result = eve.CalculateWidthOfDateTime(widthOfBar, textWidth, barStart, barEnd, ActionBlock.Left, cellWidth, cellDuration);
            _output.WriteLine($"BoxStart: {result.boxStart}   BoxEnd:{result.boxEnd}");
            Assert.Equal(result.boxEnd, barEnd);
        }

        public enum BarWidth
        {
            Full,
            CenterMore,
            Center,
            CenterCellLeft,
            CenterCellRight,
            LeftMore,
            Left,
            Right,
            RightMore,
        }

        public static readonly object[][] InitData =
        {
            new object[] {
                "1", 
                new DateTime(2020, 07, 27, 00, 00, 00),
                new DateTime(2020, 07, 30, 00, 00, 00),

                new DateTime(2020, 07, 13, 00, 00, 00),
                new DateTime(2020, 07, 31, 00, 00, 00),
                "A32S VP-BFF 10DY+3DY - 84:19 MH (готовность 31.07.2020)",
                11,
                120,
                20,
                BarWidth.Full
            },
            new object[]{
                "2",
                new DateTime(2020, 07, 27, 00, 00, 00),
                new DateTime(2020, 07, 30, 00, 00, 00),

                new DateTime(2020, 07, 26, 17, 00, 00),
                new DateTime(2020, 07, 28, 05, 00, 00),
                "A20-10-1159-01(ВВОД В ХРАНЕНИЕ)<br>A32S VP-BIP ADD - 114:12 MH (готовность 28.07.2020)",
                11,
                120,
                20,
                BarWidth.LeftMore
            },
            new object[]{
            "3",
            new DateTime(2020, 07, 27, 00, 00, 00),
            new DateTime(2020, 07, 30, 00, 00, 00),

            new DateTime(2020, 07, 26, 19, 59, 00),
            new DateTime(2020, 07, 28, 20, 00, 00),
            "APU CHNG.ENG.CHNG.<br>A32S VP-BME DY - 326:10 MH (готовность 28.07.2020)",
            11,
            120,
            20,
            BarWidth.Left
            },
            new object[]{
            "4",
            new DateTime(2020, 07, 27, 00, 00, 00),
            new DateTime(2020, 07, 30, 00, 00, 00),

            new DateTime(2020, 07, 27, 17, 00, 00),
            new DateTime(2020, 07, 28, 09, 00, 00),
            "A32S VP-BJA 10DY+3DY - 30:23 MH (готовность 28.07.2020)",
            11,
            120,
            20,
            BarWidth.CenterMore
            },
            new object[]{
                "5",
                new DateTime(2020, 07, 27, 00, 00, 00),
                new DateTime(2020, 07, 30, 00, 00, 00),

                new DateTime(2020, 07, 28, 17, 00, 00),
                new DateTime(2020, 12, 31, 20, 00, 00),
                "A32S VQ-BTW STORAGE IN SVO -  MH (готовность 31.12.2020)werwerwerwerwerwerwwwwwwwww",
                11,
                120,
                20,
                BarWidth.RightMore
            },
            new object[]{
                "6",
                new DateTime(2020, 07, 27, 00, 00, 00),
                new DateTime(2020, 07, 30, 00, 00, 00),

                new DateTime(2020, 07, 28, 17, 00, 00),
                new DateTime(2020, 12, 31, 20, 00, 00),
                "A32S VQ-BTW STORAGE IN SVO -  MH (готовность 31.12.2020)",
                11,
                120,
                20,
                BarWidth.Right
            },
            new object[]{
                "7",
                new DateTime(2020, 07, 27, 00, 00, 00),
                new DateTime(2020, 07, 30, 00, 00, 00),

                new DateTime(2020, 06, 28, 17, 00, 00),
                new DateTime(2020, 07, 28, 18, 00, 00),
                "A32S VQ-BTW STORAGE IN SVO -  MH (готовность 31.12.2020)",
                11,
                120,
                20,
                BarWidth.Left
            },
            new object[]{
            "8",
            new DateTime(2020, 07, 27, 00, 00, 00),
            new DateTime(2020, 07, 30, 00, 00, 00),

            new DateTime(2020, 07, 27, 06, 00, 00),
            new DateTime(2020, 07, 27, 12, 00, 00),
            "A32S VP-BCA 10DY+3DY - 35:14 MH (готовность 28.07.2020)",
            11,
            120,
            20,
            BarWidth.CenterCellLeft
            },
            new object[]{
            "9",
            new DateTime(2020, 07, 27, 00, 00, 00),
            new DateTime(2020, 07, 30, 00, 00, 00),

            new DateTime(2020, 07, 29, 20, 00, 00),
            new DateTime(2020, 07, 29, 22, 00, 00),
            "A32S VP-BCA 10DY+3DY - 35:14 MH (готовность 28.07.2020)",
            11,
            120,
            20,
            BarWidth.CenterCellRight
            }

        };

        [Fact]
        public void EventTest_CalculateWidthOfDateTime_WidthOfBar()
        {
            var ev = new DayPilot.Web.Ui.Event();
            var barStart = new DateTime(2020, 07, 27, 00, 0, 00, 00);
            var barEnd = new DateTime(2020, 07, 28, 05, 00, 00, 00);
            var cellWidth = 20;
            var cellDuration = 120;
            var result = ev.WidthOfBar(barStart, barEnd, cellWidth, cellDuration);
            Assert.True(result < 1000);
        }

        [Theory]
        [MemberData(nameof(InitData))]
        public void EventTest_CalculateWidthOfDateTime_SetBox_BoxStart(
            string id, DateTime dayStart, DateTime dayEnd,
            DateTime start, DateTime end, string text, int fontSize, int cellDuration, int cellWidth, BarWidth barWidth)
        {
            var ev = new DayPilot.Web.Ui.Event(id, dayStart, dayEnd, start, end,
                default, default, text, "","",ACType.RRJ, null, fontSize, cellWidth, cellDuration, 1, "", "", "", 0,0, "");

            var result = ev.SetBoxes(dayStart, dayEnd, fontSize, cellWidth, cellDuration);

            switch (barWidth)
            {
                case BarWidth.Full:
                    Assert.Equal(result.BoxStart, dayStart);
                    Assert.Equal(result.BoxEnd, dayEnd);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.LeftMore:
                    Assert.Equal(result.BoxStart, dayStart);
                    Assert.True(result.BoxEnd > end);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.Left:
                    Assert.Equal(result.BoxStart, dayStart);
                    Assert.True(result.BoxEnd <= end);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.CenterCellLeft:
                    Assert.Equal(result.BoxStart, dayStart);
                    Assert.True(result.BoxEnd > end);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.CenterCellRight:
                    Assert.True(result.BoxStart < start);
                    Assert.Equal(result.BoxEnd, dayEnd);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.Center:
                    Assert.Equal(result.BoxStart, start);
                    Assert.Equal(result.BoxEnd, end);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.CenterMore:
                    Assert.True(result.BoxStart < start);
                    Assert.True(result.BoxEnd > end);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.Right:
                    Assert.Equal(result.BoxStart, start);
                    Assert.Equal(result.BoxEnd, dayEnd);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
                case BarWidth.RightMore:
                    Assert.True(result.BoxStart < start);
                    Assert.Equal(result.BoxEnd, dayEnd);
                    _output.WriteLine($"BoxStart: {result.BoxStart}  BoxEnd: {result.BoxEnd}");
                    break;
            }
        }

    }
}
