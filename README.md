# TypedRegex

C# Source Generator to make classes from Regular Expressions. 

```
using TypedRegex;
[Regex(@"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<min>\d{2}):(?<sec>\d{2}\.\d{3})Z$")]
public partial class IsoDate { }
```

```
if (IsoDate.TryMatch("2021-02-03T01:23:45.678Z", out var isoDate))
{
    Console.WriteLine($"IsoDate: year={isoDate.Year} month={isoDate.Month} day={isoDate.Day}");
}
```

There's also full IEnumerable support, so you can do:

```
foraech (var isoDate in IsoDate.Matches(input))
{
    Console.WriteLine($"Found date: {input.Year}");
}
```

## API

The source generator fills out your `CustomType` class with the following static methods:

```
public static bool IsMatch(string input);
public static CustomType Match(string input);
public static bool TryMatch(string input, out CustomType match)
public static IEnumerable<CustomType> Matches(string input)
```

There is a property added for each match group in the regular expression, named the same as 
the group name, or named `Group0`, `Group1`, etc for non-named groups. Each of these is a `TypedRegex.MatchGroup`
which is kind of a wrapper around System.Text.RegularExpressions.Capture with some additional features:

* Implicit cast to `string`
* `string Value` property is the first match (same as `Capture.Value`)
* `int CaptureCount` provides number of captures (eg: if a group is repeated)
* `IEnumerable<string> Values` property is the string value of each capture

