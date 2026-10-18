Generate(105);

void Generate(int number)
{
    for (var i = 1; i <= number; i++)
    {
        var result = string.Empty;

        if (i % 3 == 0)
        {
            result += "foo";
        }

        if (i % 4 == 0)
        {
            result += "baz";
        }

        if (i % 5 == 0)
        {
            result += "bar";
        }

        if (i % 7 == 0)
        {
            result += "jazz";
        }

        if (i % 9 == 0)
        {
            result += "huzz";
        }

        if (result.Length == 0)
        {
            result = i.ToString();
        }

        Console.WriteLine(result);
    }
}