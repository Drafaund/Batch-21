Node? head = null;
Node? tail = null;

Append(5);
Append(10);
Print();

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

class Node
{
    public int Value;
    public Node? Next;

    public Node(int value)
    {
        Value = value;
    }
}
