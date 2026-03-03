using CorporateMemo.Domain.Services;

namespace CorporateMemo.Domain.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="MemoNumberGenerator"/> static class.
/// Tests verify the memo number format, sanitisation logic, and validation guards.
///
/// Convention: method names follow the pattern MethodName_Scenario_ExpectedResult.
/// </summary>
public class MemoNumberGeneratorTests
{
    // ============================================================
    // Generate — happy path tests
    // ============================================================

    /// <summary>
    /// Generate with normal inputs should produce the correct format:
    /// [username]-[YYYYMMDD]-[seq] e.g. "jsmith-20260302-001"
    /// </summary>
    [Fact]
    public void Generate_ValidInputs_ReturnsCorrectFormat()
    {
        // Arrange
        var username = "jsmith";
        var date = new DateTime(2026, 3, 2);
        var sequence = 1;

        // Act
        var result = MemoNumberGenerator.Generate(username, date, sequence);

        // Assert
        Assert.Equal("jsmith-20260302-001", result);
    }

    /// <summary>
    /// Generate should zero-pad the sequence number to 3 digits.
    /// Sequence 12 should become "012", sequence 100 should become "100".
    /// </summary>
    [Theory]
    [InlineData(1, "001")]
    [InlineData(9, "009")]
    [InlineData(12, "012")]
    [InlineData(99, "099")]
    [InlineData(100, "100")]
    [InlineData(999, "999")]
    public void Generate_VariousSequenceNumbers_ZeroPadsToThreeDigits(int sequence, string expectedSeqPart)
    {
        // Arrange
        var date = new DateTime(2026, 1, 15);

        // Act
        var result = MemoNumberGenerator.Generate("user", date, sequence);

        // Assert — only check the sequence portion at the end
        Assert.EndsWith($"-{expectedSeqPart}", result);
    }

    /// <summary>
    /// Generate should embed the date as YYYYMMDD in the middle portion of the memo number.
    /// </summary>
    [Theory]
    [InlineData(2026, 3, 2, "20260302")]
    [InlineData(2026, 12, 31, "20261231")]
    [InlineData(2025, 1, 1, "20250101")]
    public void Generate_VariousDates_FormatsDateCorrectly(int year, int month, int day, string expectedDatePart)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var result = MemoNumberGenerator.Generate("user", date, 1);

        // Assert — the date portion is in the middle
        Assert.Contains($"-{expectedDatePart}-", result);
    }

    /// <summary>
    /// Generate should sanitise the username: convert uppercase to lowercase and
    /// remove any characters that are not alphanumeric or a hyphen.
    /// </summary>
    [Theory]
    [InlineData("JSmith", "jsmith")]           // uppercase → lowercase
    [InlineData("john.doe", "johndoe")]        // dot removed
    [InlineData("ALICE@CORP", "alicecorp")]    // @ removed, uppercase lowered
    [InlineData("Bob Smith", "bobsmith")]      // space removed
    [InlineData("test-user", "test-user")]     // hyphens preserved
    [InlineData("user123", "user123")]         // digits preserved
    public void Generate_UsernameWithSpecialChars_SanitisesCorrectly(string rawUsername, string expectedSanitised)
    {
        // Arrange
        var date = new DateTime(2026, 3, 2);

        // Act
        var result = MemoNumberGenerator.Generate(rawUsername, date, 1);

        // Assert — the sanitised username is the prefix before the first hyphen
        Assert.StartsWith(expectedSanitised + "-", result);
    }

    /// <summary>
    /// Generate with a username that becomes entirely empty after sanitisation
    /// should fall back to "user".
    /// </summary>
    [Fact]
    public void Generate_UsernameBecomesEmptyAfterSanitisation_UsesUserFallback()
    {
        // Arrange — "@@@" has no letters, digits, or hyphens
        var date = new DateTime(2026, 3, 2);

        // Act
        var result = MemoNumberGenerator.Generate("@@@", date, 1);

        // Assert
        Assert.StartsWith("user-", result);
    }

    /// <summary>
    /// Generate with a time portion in the DateTime should only use the date part.
    /// Two calls with the same date but different times should produce identical memo numbers.
    /// </summary>
    [Fact]
    public void Generate_DateWithTimePortion_IgnoresTimePart()
    {
        // Arrange
        var dateWithMidnight = new DateTime(2026, 3, 2, 0, 0, 0);
        var dateWithNoon = new DateTime(2026, 3, 2, 12, 30, 45);

        // Act
        var result1 = MemoNumberGenerator.Generate("user", dateWithMidnight, 1);
        var result2 = MemoNumberGenerator.Generate("user", dateWithNoon, 1);

        // Assert — both should produce the same memo number because the date is the same
        Assert.Equal(result1, result2);
    }

    // ============================================================
    // Generate — validation guard tests
    // ============================================================

    /// <summary>
    /// Generate should throw ArgumentNullException when the username is null.
    /// </summary>
    [Fact]
    public void Generate_NullUsername_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MemoNumberGenerator.Generate(null!, DateTime.UtcNow, 1));
    }

    /// <summary>
    /// Generate should throw ArgumentNullException when the username is whitespace-only.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Generate_EmptyOrWhitespaceUsername_ThrowsArgumentNullException(string username)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MemoNumberGenerator.Generate(username, DateTime.UtcNow, 1));
    }

    /// <summary>
    /// Generate should throw ArgumentOutOfRangeException when the sequence number is zero or negative.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Generate_InvalidSequenceNumber_ThrowsArgumentOutOfRangeException(int sequence)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MemoNumberGenerator.Generate("user", DateTime.UtcNow, sequence));
    }

    // ============================================================
    // SanitiseUsername — direct tests
    // ============================================================

    /// <summary>
    /// SanitiseUsername should convert the input to lowercase.
    /// </summary>
    [Fact]
    public void SanitiseUsername_MixedCase_ReturnsLowercase()
    {
        // Act
        var result = MemoNumberGenerator.SanitiseUsername("JohnDoe");

        // Assert
        Assert.Equal("johndoe", result);
    }

    /// <summary>
    /// SanitiseUsername should keep letters, digits, and hyphens.
    /// </summary>
    [Fact]
    public void SanitiseUsername_AllowedCharacters_PreservesCharacters()
    {
        // Act
        var result = MemoNumberGenerator.SanitiseUsername("abc-123");

        // Assert
        Assert.Equal("abc-123", result);
    }

    /// <summary>
    /// SanitiseUsername should remove periods, underscores, spaces, and other special characters.
    /// </summary>
    [Fact]
    public void SanitiseUsername_DisallowedCharacters_RemovesCharacters()
    {
        // Act
        var result = MemoNumberGenerator.SanitiseUsername("john.doe_smith@corp.com");

        // Assert — only letters/digits/hyphens remain
        Assert.Equal("johndoesmithcorpcom", result);
    }

    /// <summary>
    /// SanitiseUsername with only special characters should return the "user" fallback.
    /// </summary>
    [Fact]
    public void SanitiseUsername_OnlySpecialChars_ReturnsFallback()
    {
        // Act
        var result = MemoNumberGenerator.SanitiseUsername("!@#$%");

        // Assert
        Assert.Equal("user", result);
    }

    /// <summary>
    /// SanitiseUsername should handle an empty string and return the "user" fallback.
    /// </summary>
    [Fact]
    public void SanitiseUsername_EmptyString_ReturnsFallback()
    {
        // Act
        var result = MemoNumberGenerator.SanitiseUsername("");

        // Assert
        Assert.Equal("user", result);
    }

    /// <summary>
    /// The full generated memo number should match the expected pattern regex.
    /// Pattern: [a-z0-9-]+-[0-9]{8}-[0-9]{3}
    /// </summary>
    [Fact]
    public void Generate_Output_MatchesExpectedPattern()
    {
        // Arrange
        var date = new DateTime(2026, 6, 15);

        // Act
        var result = MemoNumberGenerator.Generate("testUser99", date, 3);

        // Assert — check individual components by splitting on "-"
        // Expected: "testuser99-20260615-003"
        var parts = result.Split('-');
        Assert.Equal(3, parts.Length);
        Assert.Equal("testuser99", parts[0]);
        Assert.Equal("20260615", parts[1]);
        Assert.Equal("003", parts[2]);
    }
}
