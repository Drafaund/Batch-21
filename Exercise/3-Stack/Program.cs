var stack = new Stack<string>();

Type("foo");
Type("bar");
Undo();
Undo();

void Type(string word)
{
    stack.Push(word);
    Console.WriteLine($"Typed {word}");
}

void Undo()
{
    if(stack.Count == 0)
    {
        Console.WriteLine("There's nothing to undo");
        return;
    }
    Console.WriteLine($"Undid {stack.Peek()}");
    stack.Pop();
    
}

