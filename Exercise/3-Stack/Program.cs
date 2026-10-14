const int maxHistory = 3;
var stack = new Stack<string>();

Type("A");
Type("B");
Type("C");
Type("D");
Undo();

void Type(string word)
{
    if (stack.Count == maxHistory)
    {
        var items = stack.ToArray();
        stack.Clear();

        for (var index = items.Length - 2; index >= 0; index--)
        {
            stack.Push(items[index]);
        }

        Console.WriteLine($"Dropped bottom, Typed {word}");
        stack.Push(word);
        return;
    }

    stack.Push(word);
    Console.WriteLine($"Typed {word}");
}

void Undo()
{
    if (stack.Count == 0)
    {
        Console.WriteLine("There's nothing to undo");
        return;
    }

    Console.WriteLine($"Undid {stack.Peek()}");
    stack.Pop();
}
