#region Week 1 - Foo Bar

Console.WriteLine("--- Week 1 ---");
Console.WriteLine(GenerateWeek1(15));

string GenerateWeek1(int n)
{
    var items = new List<string>();

    for (var x = 1; x <= n; x++)
    {
        if (x % 3 == 0 && x % 5 == 0)
        {
            items.Add("foobar");
        }
        else if (x % 3 == 0)
        {
            items.Add("foo");
        }
        else if (x % 5 == 0)
        {
            items.Add("bar");
        }
        else
        {
            items.Add(x.ToString());
        }
    }

    return string.Join(", ", items);
}

#endregion

#region Week 2 - Jazz

Console.WriteLine();
Console.WriteLine("--- Week 2 ---");
Console.WriteLine(GenerateWeek2(105));

string GenerateWeek2(int n)
{
    var items = new List<string>();

    for (var x = 1; x <= n; x++)
    {
        var result = string.Empty;

        if (x % 3 == 0)
        {
            result += "foo";
        }

        if (x % 5 == 0)
        {
            result += "bar";
        }

        if (x % 7 == 0)
        {
            result += "jazz";
        }

        items.Add(result.Length == 0 ? x.ToString() : result);
    }

    return string.Join(", ", items);
}

#endregion

#region Week 3 - Divisor Rule Table

// Ascending divisor order, so the output is deterministic.
var ruleTable = new (int Divisor, string Output)[]
{
    (3, "foo"),
    (4, "baz"),
    (5, "bar"),
    (7, "jazz"),
    (9, "huzz"),
};

Console.WriteLine();
Console.WriteLine("--- Week 3 ---");
Console.WriteLine(GenerateWeek3(105));
Console.WriteLine($"36 => {EvaluateWeek3(36)}");

string GenerateWeek3(int n)
{
    var items = new List<string>();

    for (var x = 1; x <= n; x++)
    {
        items.Add(EvaluateWeek3(x));
    }

    return string.Join(", ", items);
}

string EvaluateWeek3(int x)
{
    var result = string.Empty;

    foreach (var (divisor, output) in ruleTable)
    {
        if (x % divisor == 0)
        {
            result += output;
        }
    }

    return result.Length == 0 ? x.ToString() : result;
}

#endregion

#region Week 4 - FooBarGenerator Class

Console.WriteLine();
Console.WriteLine("--- Week 4 ---");

var generator = new FooBarGenerator();
generator.AddRule(9, "huzz");
generator.AddRule(3, "foo");
generator.AddRule(7, "jazz");
generator.AddRule(5, "bar");
generator.AddRule(4, "baz");

Console.WriteLine(generator.GenerateSequence(1, 15));
Console.WriteLine($"36 => {generator.Evaluate(36)}");
Console.WriteLine($"105 => {generator.Evaluate(105)}");
Console.WriteLine($"11 => {generator.Evaluate(11)}");

// A rule added later is picked up on the next call.
generator.AddRule(11, "quux");
Console.WriteLine($"11 => {generator.Evaluate(11)}");

class FooBarGenerator
{
    // SortedDictionary keeps keys in ascending order, so rules are always
    // evaluated by ascending divisor no matter the order they were added in.
    private readonly SortedDictionary<int, string> _rules = new();

    // Registering an existing divisor replaces its output.
    public void AddRule(int divisor, string output)
    {
        if (divisor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(output);

        _rules[divisor] = output;
    }

    public string Evaluate(int number)
    {
        var result = string.Empty;

        foreach (var (divisor, output) in _rules)
        {
            if (number % divisor == 0)
            {
                result += output;
            }
        }

        return result.Length == 0 ? number.ToString() : result;
    }

    public string GenerateSequence(int start, int end)
    {
        var items = new List<string>();

        for (var x = start; x <= end; x++)
        {
            items.Add(Evaluate(x));
        }

        return string.Join(", ", items);
    }
}

#endregion
