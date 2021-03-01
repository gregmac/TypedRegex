using System;
using System.Text.RegularExpressions;

namespace TypedRegex.Samples
{
    public static class Program
    {
        public static void Main()
        {
            
            // TryMatch() can get a single match
            if (IsoDate.TryMatch("2021-02-03T01:23:45.678Z", out var isoDate))
            {
                Console.WriteLine($"IsoDate: year={isoDate.Year} month={isoDate.Month} day={isoDate.Day}");
            }

            // IsMatch() allows simple validity checks
            var testUsernames = new[] { "foo-bar", "foobar", "3foobar", "foo-bar-", "-foo-bar", "foo--bar" };
            foreach (var username in testUsernames)
            {
                Console.WriteLine($"GithubUsername: {username}: {GithubUsername.IsMatch(username)}");
            }

            // Matches() allows iterating through several matches
            var sourceText =
                "This is some example text showing URL matching, for https://example.org. " +
                "It will match with IPs http://192.0.2.1 or ftp://some.site.example.org and" +
                "also long ones https://user:secret@example.org:4433/some/path?query=abc&param=234.";

            foreach (var url in UrlRegex.Matches(sourceText))
            {
                Console.WriteLine($"Found URL, scheme = {url.Scheme}, fullhost = {url.Fullhost}");
                foreach (var path in url.Path) { Console.WriteLine("  " + path); }
            }
        }
    }

    /// <summary>
    /// Parse a ISO-8601 date
    /// </summary>
    // language=regex
    [Regex(@"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<min>\d{2}):(?<sec>\d{2}\.\d{3})Z$")]
    public partial class IsoDate { }

    /// <summary>
    /// Test a username according to Github rules:
    /// Username may only contain alphanumeric characters
    /// and only single hyphens, and cannot begin or end with a hyphen.
    /// </summary>
    // language=regex
    [Regex(@"^([a-z\d]+-)*[a-z\d]+$", RegexOptions.IgnoreCase)]
    public partial class GithubUsername { }

    // language=regex
    [Regex(@"

        (?:(?<scheme>[a-z]+:)?//)
        (?:(?<username>\S+)(?::(?<password>\S*))?@)?
        (?<fullhost>
          localhost
          |(?<ipv4>(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)){3})
          |
             (?<host>(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)
             (?<domain>\.(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)*
             (?<tld>\.[a-z\u00a1-\uffff]{2,})
        )
        (?<port>:\d{2,5})?
        (?:
          (?<path>/[^/?]*)*
          (?:
            \?
            (?<queryString>\S*)
          )?
        )?", RegexOptions.IgnorePatternWhitespace)]
    public partial class UrlRegex { }
}
