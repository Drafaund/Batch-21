#region Week 2

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
#endregion

#region Week 3

const int maxHistoryW3 = 3;
var stackW3 = new Stack<string>();
var redoStackW3 = new Stack<string>();

TypeW3("A");
TypeW3("B");
UndoW3();
RedoW3();

void TypeW3(string word)
{
    redoStackW3.Clear();

    if (stackW3.Count == maxHistoryW3)
    {
        var items = stackW3.ToArray();
        stackW3.Clear();

        for (var index = items.Length - 2; index >= 0; index--)
        {
            stackW3.Push(items[index]);
        }

        Console.WriteLine($"Dropped bottom, Typed {word}");
        stackW3.Push(word);
        return;
    }

    stackW3.Push(word);
    Console.WriteLine($"Typed {word}");
}

void UndoW3()
{
    if (stackW3.Count == 0)
    {
        Console.WriteLine("There's nothing to undo");
        return;
    }

    var word = stackW3.Pop();
    redoStackW3.Push(word);
    Console.WriteLine($"Undid {word}");
}

void RedoW3()
{
    if (redoStackW3.Count == 0)
    {
        Console.WriteLine("There's nothing to redo");
        return;
    }

    var word = redoStackW3.Pop();
    stackW3.Push(word);
    Console.WriteLine($"Redid {word}");
}

#endregion