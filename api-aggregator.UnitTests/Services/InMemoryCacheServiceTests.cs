using api_aggregator.Services;
using api_aggregator.Services.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace api_aggregator.UnitTests.Services;

public class InMemoryCacheServiceTests
{
    [Fact]
    public void Set_AndGet_ShouldReturn_CachedValue()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var testData = new List<string> { "test1", "test2" };
        
        // Act
        var setResult = service.Set("testKey", testData, 5);
        var getResult = service.Get<List<string>>("testKey");
        
        // Assert
        setResult.Success.Should().BeTrue();
        getResult.Success.Should().BeTrue();
        getResult.Value.Should().NotBeNull();
        getResult.Value.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void Get_NonExistentKey_ShouldReturn_Default()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        
        // Act
        var result = service.Get<string>("nonExistent");
        
        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldDelete_CachedValue()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        service.Set("testKey", "testValue", 5);
        
        // Act
        var removeResult = service.Remove("testKey");
        var getResult = service.Get<string>("testKey");
        
        // Assert
        removeResult.Success.Should().BeTrue();
        getResult.Success.Should().BeTrue();
        getResult.Value.Should().BeNull();
    }

    [Fact]
    public void Set_WithDifferentTypes_ShouldMaintain_TypeSafety()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        
        // Act
        var setResult1 = service.Set("stringKey", "test", 5);
        var setResult2 = service.Set("intKey", 42, 5);
        var setResult3 = service.Set("listKey", new List<int> { 1, 2, 3 }, 5);
        
        // Assert
        setResult1.Success.Should().BeTrue();
        setResult2.Success.Should().BeTrue();
        setResult3.Success.Should().BeTrue();
        
        service.Get<string>("stringKey").Value.Should().Be("test");
        service.Get<int>("intKey").Value.Should().Be(42);
        service.Get<List<int>>("listKey").Value.Should().BeEquivalentTo(new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void Clear_ShouldReturn_Success()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        
        // Act
        var result = service.Clear();
        
        // Assert
        result.Success.Should().BeTrue();
    }
}
