using System;

namespace TypedRegex.Samples
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine(Test1.IsMatch("42"));
            Console.WriteLine(Test1.IsMatch("a42b"));

            if (Test1.TryMatch("aaaa11111", out var match1))
            {
                Console.WriteLine($"Found it with {match1.FirstGroup}, in {match1.Value}");
            }

            foreach (var match in Test1.Matches("ab42df55555"))
            {
                Console.WriteLine($"Got {match.FirstGroup}");
            }
        }
    }

    // language=regex
    [Regex(@"(?<digits>(?<firstGroup>\d)\d+)")]
    public partial class Test1 { }
}
