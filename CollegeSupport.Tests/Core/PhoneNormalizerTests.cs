using CollegeSupport.Core.Exceptions;
using CollegeSupport.Core.Services;

namespace CollegeSupport.Tests.Core;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("9001234567", "+79001234567")]
    [InlineData("8 (912) 345-67-89", "+79123456789")]
    [InlineData("+7 900 111-22-33", "+79001112233")]
    public void Normalize_AcceptsRussianNumbers(string input, string expected) =>
        Assert.Equal(expected, PhoneNormalizer.Normalize(input));

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("+1 234 567 8901")]
    public void Normalize_ThrowsForInvalidNumber(string input) =>
        Assert.Throws<BusinessRuleException>(() => PhoneNormalizer.Normalize(input));
}
