Console.WriteLine("--- Week 1 ---");
var week1 = new Week1Queue();
week1.Enqueue("A");
week1.Enqueue("B");
week1.Process();
week1.Process();
week1.Process();
week1.Process();

Console.WriteLine();
Console.WriteLine("--- Week 2 ---");
var week2 = new Week2VipQueue();
week2.Enqueue("A");
week2.Enqueue("B");
week2.EnqueueVip("C");
week2.Process();
week2.Process();
week2.Process();

Console.WriteLine();
Console.WriteLine("--- Week 3 ---");
var week3 = new Week3PriorityQueue();
week3.Enqueue("A", 1);
week3.Enqueue("B", 5);
week3.Enqueue("C", 5);
week3.Process();
week3.Process();

Console.WriteLine();
Console.WriteLine("--- Week 4 ---");
var week4 = new KeywordPriorityQueue();
week4.AddRule("urgent", 10);
week4.AddRule("normal", 5);
week4.Enqueue("normal");
week4.Enqueue("urgent");
week4.Enqueue("other");
week4.Process();
week4.Process();
week4.Process();
week4.Process();

#region Week 1 - Basic Queue

class Week1Queue
{
    private readonly Queue<string> _items = new();

    public void Enqueue(string value)
    {
        _items.Enqueue(value);
        Console.WriteLine($"Queued {value}");
    }

    public void Process()
    {
        if (_items.Count == 0)
        {
            Console.WriteLine("Queue is empty");
            return;
        }

        Console.WriteLine($"Processed {_items.Dequeue()}");
    }
}

#endregion

#region Week 2 - VIP Queue
class Week2VipQueue
{
    private readonly LinkedList<string> _items = new();

    public void Enqueue(string value)
    {
        _items.AddLast(value);
        Console.WriteLine($"Queued {value}");
    }

    public void EnqueueVip(string value)
    {
        _items.AddFirst(value);
        Console.WriteLine($"VIP Queued {value}");
    }

    public void Process()
    {
        if (_items.Count == 0)
        {
            Console.WriteLine("Queue is empty");
            return;
        }

        var value = _items.First!.Value;
        _items.RemoveFirst();
        Console.WriteLine($"Processed {value}");
    }
}

#endregion

#region Week 3 - Priority Queue

class Week3PriorityQueue
{
    private readonly List<(string Value, int Priority)> _items = new();

    public void Enqueue(string value, int priority)
    {
        _items.Add((value, priority));
        Console.WriteLine($"Queued {value} with priority {priority}");
    }

    public void Process()
    {
        if (_items.Count == 0)
        {
            Console.WriteLine("Queue is empty");
            return;
        }

        var highestIndex = 0;
        for (var i = 1; i < _items.Count; i++)
        {
            if (_items[i].Priority > _items[highestIndex].Priority)
            {
                highestIndex = i;
            }
        }

        var item = _items[highestIndex];
        _items.RemoveAt(highestIndex);
        Console.WriteLine($"Processed {item.Value}");
    }
}

#endregion

#region Week 4 - Keyword-Based Priority Queue

class KeywordPriorityQueue
{
    private readonly Dictionary<string, int> _rules = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Value, int Priority)> _items = new();

    public void AddRule(string keyword, int priority)
    {
        _rules[keyword] = priority;
    }

    public void Enqueue(string value)
    {
        var priority = 0;
        var matched = false;

        foreach (var (keyword, rulePriority) in _rules)
        {
            if (!value.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!matched || rulePriority > priority)
            {
                priority = rulePriority;
                matched = true;
            }
        }

        _items.Add((value, priority));
        Console.WriteLine($"Queued {value} with priority {priority}");
    }

    public void Process()
    {
        if (_items.Count == 0)
        {
            Console.WriteLine("Queue is empty");
            return;
        }

        var highestIndex = 0;
        for (var i = 1; i < _items.Count; i++)
        {
            if (_items[i].Priority > _items[highestIndex].Priority)
            {
                highestIndex = i;
            }
        }

        var item = _items[highestIndex];
        _items.RemoveAt(highestIndex);
        Console.WriteLine($"Processed {item.Value}");
    }
}

#endregion
