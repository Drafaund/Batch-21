#region Week 1

Node? head = null;

Append(5);
Append(10);
Print();

void Append(int val)
{
    var node = new Node(val);
    if (head == null)
    {
        head = node;
    }
    else
    {
        var current = head;
        while (current.Next != null)
        {
            current = current.Next;
        }
        current.Next = node;
    }
    Console.WriteLine($"Appended {val}");
}

void Print()
{
    var values = new List<int>();
    for (var current = head; current != null; current = current.Next)
    {
        values.Add(current.Value);
    }
    Console.WriteLine($"Sequence: {string.Join(" -> ", values)}");
}

#endregion

#region Week 2

DoublyNode? headW2 = null;
DoublyNode? tailW2 = null;

AppendW2(5);
AppendW2(10);
AppendW2(15);
PrintW2();
PrintReverseW2();

void AppendW2(int val)
{
    var node = new DoublyNode(val);
    if (headW2 == null)
    {
        headW2 = node;
        tailW2 = node;
    }
    else
    {
        node.Previous = tailW2;
        tailW2!.Next = node;
        tailW2 = node;
    }
    Console.WriteLine($"Appended {val}");
}

void PrintW2()
{
    var values = new List<int>();
    for (var current = headW2; current != null; current = current.Next)
    {
        values.Add(current.Value);
    }
    Console.WriteLine($"Sequence: {string.Join(" -> ", values)}");
}

void PrintReverseW2()
{
    var values = new List<int>();
    for (var current = tailW2; current != null; current = current.Previous)
    {
        values.Add(current.Value);
    }
    Console.WriteLine($"Reversed: {string.Join(" -> ", values)}");
}

#endregion

#region Week 3

DoublyNode? headW3 = null;
DoublyNode? tailW3 = null;

InsertW3(5);
InsertW3(2);
InsertW3(8);
PrintW3();

void InsertW3(int val)
{
    var node = new DoublyNode(val);

    // Find the first node that is greater than val; the new node goes before it.
    var next = headW3;
    while (next != null && next.Value <= val)
    {
        next = next.Next;
    }

    if (next == null)
    {
        // Empty list, or val is the largest: insert at the tail.
        node.Previous = tailW3;
        if (tailW3 == null)
        {
            headW3 = node;
        }
        else
        {
            tailW3.Next = node;
        }
        tailW3 = node;
    }
    else
    {
        node.Next = next;
        node.Previous = next.Previous;
        if (next.Previous == null)
        {
            headW3 = node;
        }
        else
        {
            next.Previous.Next = node;
        }
        next.Previous = node;
    }
    Console.WriteLine($"Inserted {val}");
}

void PrintW3()
{
    var values = new List<int>();
    for (var current = headW3; current != null; current = current.Next)
    {
        values.Add(current.Value);
    }
    Console.WriteLine($"Sequence: {string.Join(" -> ", values)}");
}

#endregion

#region Week 4

var myList = new ConfigurableLinkedList();
myList.SetSorting((a, b) => b.CompareTo(a));
myList.AddFilter(val => val > 2);
myList.AddFilter(val => val != 5);

myList.Append(5);
myList.Append(2);
myList.Append(8);
myList.Append(3);
myList.Print();

class ConfigurableLinkedList
{
    private Node? _head;
    private Func<int, int, int>? _comparer;
    private readonly List<Func<int, bool>> _filters = new();

    public void SetSorting(Func<int, int, int> comparer)
    {
        _comparer = comparer;
    }

    public void AddFilter(Func<int, bool> filterRule)
    {
        _filters.Add(filterRule);
    }

    public void Append(int val)
    {
        var node = new Node(val);
        if (_head == null)
        {
            _head = node;
        }
        else
        {
            var current = _head;
            while (current.Next != null)
            {
                current = current.Next;
            }
            current.Next = node;
        }
        Console.WriteLine($"Appended {val}");
    }

    public void Print()
    {
        var values = new List<int>();
        for (var current = _head; current != null; current = current.Next)
        {
            if (_filters.All(filter => filter(current.Value)))
            {
                values.Add(current.Value);
            }
        }

        if (_comparer != null)
        {
            values = values.OrderBy(val => val, Comparer<int>.Create(new Comparison<int>(_comparer))).ToList();
        }

        Console.WriteLine($"Sequence: {string.Join(" -> ", values)}");
    }
}

#endregion

#region Node

class Node
{
    public int Value;
    public Node? Next;

    public Node(int value)
    {
        Value = value;
    }
}

class DoublyNode
{
    public int Value;
    public DoublyNode? Next;
    public DoublyNode? Previous;

    public DoublyNode(int value)
    {
        Value = value;
    }
}

#endregion
