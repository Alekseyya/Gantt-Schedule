using System;
using Core.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Test.Core
{
    public class DateTimeTest
    {
        private readonly ITestOutputHelper _output;
        public DateTimeTest(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void DateTimeTest_DifferentBetweenTwoDates()
        {
            _output.WriteLine(DateTime.Today.DifferenceBetweenTwoDates(DateTime.Today.AddDays(5)).ToString());
        }
    }
}
