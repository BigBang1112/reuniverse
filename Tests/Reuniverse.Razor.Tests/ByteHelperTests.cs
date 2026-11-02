namespace Reuniverse.Razor.Tests;

public class ByteHelperTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1023)]
    public async Task ToByteSize_BelowOneKilobyte_ReturnsBytes(int size)
    {
        // Act
        var result = ByteHelper.ToByteSize(size);

        // Assert
        await Assert.That(result).IsEqualTo($"{size} B");
    }

    [Test]
    [Arguments(1024, 1.0)]
    [Arguments(1536, 1.5)]
    public async Task ToByteSize_BelowOneMegabyte_ReturnsKilobytes(int size, double kilobytes)
    {
        // Act
        var result = ByteHelper.ToByteSize(size);

        // Assert
        await Assert.That(result).IsEqualTo($"{kilobytes:F2} kB");
    }

    [Test]
    [Arguments(1048576, 1.0)]
    [Arguments(5242880, 5.0)]
    public async Task ToByteSize_FromOneMegabyte_ReturnsMegabytes(int size, double megabytes)
    {
        // Act
        var result = ByteHelper.ToByteSize(size);

        // Assert
        await Assert.That(result).IsEqualTo($"{megabytes:F2} MB");
    }
}
