try
{
    try
    {
        int number = int.Parse("abc");
    }
    catch (FormatException ex)
    {
        throw new Exception("Gagal memproses data.", ex);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.InnerException.Message);
}