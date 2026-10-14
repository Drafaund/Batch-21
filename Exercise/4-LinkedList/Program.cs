Node? head = null;
Node? tail = null;

Append(5);
Append(10);
Append(15);
Print();
PrintReverse();

void Append(int val)
{
    var node = new Node(val);
    if (head == null)
    {
        head = node;
        tail = node;
    }
    else
    {
        node.Previous = tail;
        tail!.Next = node;
        tail = node;
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

void PrintReverse()
{
    var values = new List<int>();
    for (var current = tail; current != null; current = current.Previous)
    {
        values.Add(current.Value);
    }
    Console.WriteLine($"Reversed: {string.Join(" -> ", values)}");
}

class Node
{
    public int Value;
    public Node? Next;
    public Node? Previous;

    public Node(int value)
    {
        Value = value;
    }
}
