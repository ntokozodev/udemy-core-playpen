using System.Net;
using System.Text.Json;
using AuthPlaypen.OpenIddict.Redis.Models;
using AuthPlaypen.OpenIddict.Redis.Stores;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace AuthPlaypen.Api.Tests;

public class RedisOpenIddictScopeStoreTests
{
    [Fact]
    public async Task ListAsync_ShouldReturnScopes_WithOffsetAndCountApplied()
    {
        // Arrange
        var store = CreateStore(
            new RedisOpenIddictScope { Id = "s1", Name = "orders.read" },
            new RedisOpenIddictScope { Id = "s2", Name = "orders.write" },
            new RedisOpenIddictScope { Id = "s3", Name = "orders.admin" });

        // Act
        var scopes = new List<RedisOpenIddictScope>();
        await foreach (var item in store.ListAsync(count: 1, offset: 1, CancellationToken.None))
        {
            scopes.Add(item);
        }

        // Assert
        var scope = Assert.Single(scopes);
        Assert.Equal("orders.write", scope.Name);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnNumberOfScopes()
    {
        // Arrange
        var store = CreateStore(
            new RedisOpenIddictScope { Id = "s1", Name = "orders.read" },
            new RedisOpenIddictScope { Id = "s2", Name = "orders.write" });

        // Act
        var count = await store.CountAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
    }

    private static RedisOpenIddictScopeStore CreateStore(params RedisOpenIddictScope[] scopes)
    {
        var payloadByKey = scopes.ToDictionary(
            scope => $"oidc:scopes:id:{scope.Id}",
            scope => (RedisValue)JsonSerializer.Serialize(scope, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var keys = scopes
            .Select(scope => (RedisKey)$"oidc:scopes:id:{scope.Id}")
            .ToArray();

        var database = new Mock<IDatabase>();
        database
            .Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                var keyValue = (string)key!;
                return payloadByKey.TryGetValue(keyValue, out var payload)
                    ? payload
                    : RedisValue.Null;
            });

        var server = new Mock<IServer>();
        server
            .Setup(s => s.Keys(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(keys);

        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(database.Object);
        multiplexer.Setup(m => m.GetEndPoints(It.IsAny<bool>())).Returns([new DnsEndPoint("localhost", 6379)]);
        multiplexer.Setup(m => m.GetServer(It.IsAny<EndPoint>(), It.IsAny<object?>())).Returns(server.Object);

        return new RedisOpenIddictScopeStore(multiplexer.Object);
    }
}
