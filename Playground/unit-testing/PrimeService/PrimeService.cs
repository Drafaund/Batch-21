namespace PrimeService;

public class PrimeService
{
    public bool IsPrime(int candidate)
    {
        if (candidate < 2)
        {
            return false;
        }

        for (int i = 2; i * i <= candidate; i++)
        {
            if (candidate % i == 0)
            {
                return false;
            }
        }

        return true;
    }
}
