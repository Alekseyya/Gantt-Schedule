using Core.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Test.Core
{
    public class StringTest
    {
        private readonly ITestOutputHelper _output;
        public StringTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void StringTest_ConvertStringToPixel()
        {
            _output.WriteLine("Hello World".CalculateWidthToPixels(12).ToString());
        }
    }
}
