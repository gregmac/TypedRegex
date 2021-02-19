using System;
using System.Text.RegularExpressions;

namespace TypedRegex.Samples
{
    public static class Program
    {
        public static void Main()
        {
            //var r= new TypedRegex1("b/c");

            //r.b ? == Test;

            //var r = TypedRegexFactory.Create(new Regex("aa(?<t1>bb)"));

            //HelloWorldGenerated.HelloWorld.SayHello();
        }
    }

    [TypedRegexAttribute(@"a-f(?<g1>\d+)")]
    public partial class TestRegex 
    {
        public void Test()
        {
            var x = this.IsMatch("af");
            //this.
        }
    }
}
