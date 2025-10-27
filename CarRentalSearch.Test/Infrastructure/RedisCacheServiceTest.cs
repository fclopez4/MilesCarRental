using System.Text.Json;
using CarRentalSearch.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarRentalSearch.Test.Infrastructure;

public class RedisCacheServiceTest
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly RedisCacheService _sut;

    public RedisCacheServiceTest()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _sut = new RedisCacheService(_cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedObject()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 1, Name = "Test" };
        var serializedData = JsonSerializer.SerializeToUtf8Bytes(testObject);

        _cacheMock
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _sut.GetAsync<TestClass>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "non_existent_key";

        _cacheMock
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.GetAsync<TestClass>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenDeserializationFails_ReturnsNull()
    {
        // Arrange
        var key = "invalid_data_key";
        var invalidData = "invalid json data"u8.ToArray();

        _cacheMock
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidData);

        // Act
        var result = await _sut.GetAsync<TestClass>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenDeserializationFails_LogsError()
    {
        // Arrange
        var key = "invalid_data_key";
        var invalidData = "invalid json data"u8.ToArray();

        _cacheMock
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidData);

        // Act
        await _sut.GetAsync<TestClass>(key);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(key)),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithDefaultDuration_StoresDataWithDefaultExpiration()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 1, Name = "Test" };

        // Act
        await _sut.SetAsync(key, testObject);

        // Assert
        _cacheMock.Verify(
            x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => 
                    o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(15)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithCustomDuration_StoresDataWithSpecifiedExpiration()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 1, Name = "Test" };
        var customDuration = TimeSpan.FromMinutes(30);

        // Act
        await _sut.SetAsync(key, testObject, customDuration);

        // Assert
        _cacheMock.Verify(
            x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(o => 
                    o.AbsoluteExpirationRelativeToNow == customDuration),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_SerializesObjectCorrectly()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 42, Name = "SerializedTest" };
        byte[]? capturedData = null;

        _cacheMock
            .Setup(x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        await _sut.SetAsync(key, testObject);

        // Assert
        capturedData.Should().NotBeNull();
        var deserializedObject = JsonSerializer.Deserialize<TestClass>(capturedData!);
        deserializedObject.Should().NotBeNull();
        deserializedObject!.Id.Should().Be(42);
        deserializedObject.Name.Should().Be("SerializedTest");
    }

    [Fact]
    public async Task SetAsync_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 1, Name = "Test" };
        var exception = new Exception("Cache error");

        _cacheMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _sut.SetAsync(key, testObject);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(key)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenExceptionOccurs_DoesNotThrow()
    {
        // Arrange
        var key = "test_key";
        var testObject = new TestClass { Id = 1, Name = "Test" };

        _cacheMock
            .Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        var act = async () => await _sut.SetAsync(key, testObject);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WithComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var key = "complex_key";
        var complexObject = new ComplexTestClass
        {
            Id = 1,
            Name = "Complex",
            Items = new List<string> { "Item1", "Item2", "Item3" },
            Nested = new TestClass { Id = 2, Name = "Nested" }
        };
        var serializedData = JsonSerializer.SerializeToUtf8Bytes(complexObject);

        _cacheMock
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedData);

        // Act
        var result = await _sut.GetAsync<ComplexTestClass>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Complex");
        result.Items.Should().HaveCount(3);
        result.Nested.Should().NotBeNull();
        result.Nested!.Name.Should().Be("Nested");
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var key = "complex_key";
        var complexObject = new ComplexTestClass
        {
            Id = 1,
            Name = "Complex",
            Items = new List<string> { "Item1", "Item2" },
            Nested = new TestClass { Id = 2, Name = "Nested" }
        };
        byte[]? capturedData = null;

        _cacheMock
            .Setup(x => x.SetAsync(
                key,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (k, data, opts, ct) => capturedData = data);

        // Act
        await _sut.SetAsync(key, complexObject);

        // Assert
        capturedData.Should().NotBeNull();
        var deserializedObject = JsonSerializer.Deserialize<ComplexTestClass>(capturedData!);
        deserializedObject.Should().BeEquivalentTo(complexObject);
    }

    // Test helper classes
    private class TestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class ComplexTestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Items { get; set; }
        public TestClass? Nested { get; set; }
    }
}