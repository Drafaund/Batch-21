#region Week 1

var stackW1 = new Stack<string>();

TypeW1("foo");
TypeW1("bar");
UndoW1();
UndoW1();

void TypeW1(string word)
{
    stackW1.Push(word);
    Console.WriteLine($"Typed {word}");
}

void UndoW1()
{
    if (stackW1.Count == 0)
    {
        Console.WriteLine("There's nothing to undo");
        return;
    }

    Console.WriteLine($"Undid {stackW1.Pop()}");
}

#endregion

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

#region Week 4 - History Class with Validation Rules

var history = new History();
history.AddValidationRule(word => !string.IsNullOrWhiteSpace(word));
history.AddValidationRule(word => word.Length <= 10);

history.Type("hello");
history.Type("");
class History
{
    private readonly int _maxHistory;
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private readonly List<Func<string, bool>> _rules = new();

    public History(int maxHistory = 3)
    {
        _maxHistory = maxHistory;
    }

    public void AddValidationRule(Func<string, bool> rule)
    {
        _rules.Add(rule);
    }

    public void Type(string word)
    {
        if (!_rules.All(rule => rule(word)))
        {
            Console.WriteLine($"Rejected {word}");
            return;
        }

        _redoStack.Clear();

        if (_undoStack.Count == _maxHistory)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();

            for (var index = items.Length - 2; index >= 0; index--)
            {
                _undoStack.Push(items[index]);
            }

            Console.WriteLine($"Dropped bottom, Typed {word}");
            _undoStack.Push(word);
            return;
        }

        _undoStack.Push(word);
        Console.WriteLine($"Typed {word}");
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
        {
            Console.WriteLine("There's nothing to undo");
            return;
        }

        var word = _undoStack.Pop();
        _redoStack.Push(word);
        Console.WriteLine($"Undid {word}");
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
        {
            Console.WriteLine("There's nothing to redo");
            return;
        }

        var word = _redoStack.Pop();
        _undoStack.Push(word);
        Console.WriteLine($"Redid {word}");
    }
}

#endregion