#region Enumerator 
var numbers = new List<int> { 10, 20, 30 };

var enumerator = numbers.GetEnumerator();

Console.WriteLine(enumerator.Current);
Console.WriteLine(enumerator.MoveNext());
// Console.WriteLine(enumerator.Current);

Console.WriteLine(enumerator.MoveNext());
Console.WriteLine(enumerator.Current);

Console.WriteLine(enumerator.MoveNext());
Console.WriteLine(enumerator.Current);

Console.WriteLine(enumerator.MoveNext());

#endregion

#region Iterator
IEnumerable<int> GetNumbers()
{
    Console.WriteLine("Menghasilkan 1");
    yield return 1;

    Console.WriteLine("Menghasilkan 2");
    yield return 2;

    Console.WriteLine("Menghasilkan 3");
    yield return 3;
}

string test()
{
    Console.WriteLine("Test");
    return "Test";
}

foreach (int number in GetNumbers()) // Ini pemanggilan ke GetNumbers() dan akan mengembalikan IEnumerable<int>
{
    Console.WriteLine($"Diterima: {number}");
}

foreach (char text in test()) // Ini pemangilan ke test() dan akan mengembalikan IEnumerable<char>
{
    Console.WriteLine($"Diterima: {text}");
}
#endregion

#region iterator part 2
IEnumerable<int> Numbers()
{
    Console.WriteLine("Generate 1");
    yield return 1;

    Console.WriteLine("Generate 2");
    yield return 2;

    Console.WriteLine("Generate 3");
    yield return 3;
}

Numbers(); //Lazy evaluation, iterator belum jalan

foreach (int number in Numbers()) //Baru jalan
{
    Console.WriteLine($"Diterima: {number}");
}
#endregion

#region Iterator Combination

IEnumerable<int> Fibs(int number)
{
	for(int i = 1, prev = 1, curr = 1; i <= number ; i++)
	{
		yield return prev;
		int newNumber = prev + curr;
		prev = curr;
		curr = newNumber;
		
	}
}

IEnumerable<int> EvenNumbersOnly(IEnumerable<int> sequence)
{
    foreach (int x in sequence)
    {
        if (x % 2 == 0)
        {
            yield return x;
        }
    }
}
foreach (int number in EvenNumbersOnly(Fibs(10)))
{
    Console.Write(number + " ");
}
#endregion