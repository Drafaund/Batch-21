namespace VariableAndParameters
{
    class Program
    {
        static void Main(string[] args)
        {
            bool registered = RegisterUser("user@mail.com", "secret", "secret", out string userId, out string message);
            Console.WriteLine($"{registered} | {userId} | {message}");

            var config = ParseConfiguration("host=localhost; port=5432");
            foreach (var (key, value) in config)
            {
                Console.WriteLine($"{key} = {value}");
            }

            ProcessSalesData([100, 250, 75], out int total, out int highest, out int average);
            Console.WriteLine($"Total: {total}, Highest: {highest}, Average: {average}");
        }

        static bool RegisterUser(string email, string password, string confirmPassword, out string userId, out string validationMessage)
        {
            userId = "";
            validationMessage = "";
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                validationMessage = "Invalid email address.";
                return false;
            }
            if (password != confirmPassword)
            {
                validationMessage = "Passwords do not match.";
                return false;
            }

            // Generate user ID
            userId = $"USR_{DateTime.Now.Ticks % 1000000:D5}";
            validationMessage = "User registered successfully.";
            return true;
        }

        static Dictionary<string, string> ParseConfiguration(string configString)
        {
            var result = new Dictionary<string, string>();
            var pairs = configString.Split(';');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split("=");
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            return result;
        }

        static void ProcessSalesData(double[] sales, out int total, out int highest, out int average)
        {
            total = 0;
            highest = 0;

            foreach (int sale in sales)
            {
                total += sale;
                if (sale > highest)
                {
                    highest = sale;
                }
            }

            average = sales.Length > 0 ? total / sales.Length : 0;
        }
    }
}
