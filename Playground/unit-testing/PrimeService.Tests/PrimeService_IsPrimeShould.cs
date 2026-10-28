namespace PrimeService.Tests;


[TestFixture] // Marks the class as containing unit tests
public class PrimeService_IsPrimeShould
{
    private PrimeService _primeService;

    [SetUp] // This method is executed before each test method in the fixture
    public void SetUp()
    {
        _primeService = new PrimeService(); // Initialize the service instance for each test
    }

    [Test] // Marks this method as a unit test
    public void IsPrime_InputIs1_ReturnFalse()
    {
        // Arrange (setup the test conditions)
        var result = _primeService.IsPrime(1);

        // Assert (verify the outcome)
        Assert.That(result, Is.False, "1 should not be prime");
    }

    [Test]

    public void IsPrime_InputIs2_ReturnTrue()
    {
        // Arrange
        var result = _primeService.IsPrime(2);

        // Assert
        Assert.That(result, Is.True, "2 should be prime");
    }

    [Test]
    public void IsPrime_InputIsEvenNumberGreaterThan2_ReturnFalse()
    {
        // Arrange
        var result = _primeService.IsPrime(4);

        // Assert
        Assert.That(result, Is.False, "Even numbers greater than 2 should not be prime");
    }

    #region Parameterized tests with [TestCase]

    // One method, run once per [TestCase]; each one is reported as a separate test.
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(100)]
    public void IsPrime_InputIsNotPrime_ReturnFalse(int value)
    {
        var result = _primeService.IsPrime(value);

        Assert.That(result, Is.False, $"{value} should not be prime");
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(97)]
    public void IsPrime_InputIsPrime_ReturnTrue(int value)
    {
        var result = _primeService.IsPrime(value);

        Assert.That(result, Is.True, $"{value} should be prime");
    }

    #endregion
}
