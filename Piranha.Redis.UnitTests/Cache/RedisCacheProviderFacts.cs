using FluentAssertions;
using NSubstitute;
using Piranha.Cache;
using ServiceStack.Redis;
using Xunit;

namespace Piranha.Redis.UnitTests.Cache
{
    public class RedisCacheProviderFacts
    {
        private readonly RedisCacheProvider sut;
        private readonly IRedisClientsManager manager;
        private readonly IRedisClient client;

        public RedisCacheProviderFacts()
        {
            manager = Substitute.For<IRedisClientsManager>();
            client = Substitute.For<IRedisClient>();
            sut = new RedisCacheProvider(manager);

            manager.GetClient().Returns(client);
        }

        public class the_remove_method_should : RedisCacheProviderFacts
        {
            public the_remove_method_should()
            {
                sut.Remove("abcdef");
            }

            [Fact]
            public void remove_the_item_from_the_cache()
            {
                client.Received().RemoveEntryFromHash("piranha:cache", "abcdef");
            }
        }

        public class the_contains_method_should : RedisCacheProviderFacts
        {
            public the_contains_method_should()
            {
                client.HashContainsEntry("piranha:cache", "abcdef").Returns(true);
            }

            [Fact]
            public void returns_true_the_item_from_the_cache()
            {
                var result = sut.Contains("abcdef");

                result.Should().BeTrue();
            }

            [Fact]
            public void returns_false_the_item_from_the_cache()
            {
                var result = sut.Contains("a;dklf");

                result.Should().BeFalse();
            }
        }

        public class the_indexer_set_operator_should : RedisCacheProviderFacts
        {
            public the_indexer_set_operator_should()
            {
                sut["abcdef"] = new TestObject() {Index = 1};
            }

            [Fact]
            public void set_the_value()
            {
                client.Received().SetEntryInHash("piranha:cache", "abcdef", "{\"Index\":1}");
            }

            [Fact]
            public void set_the_type()
            {
                client.Received().SetEntryInHash("piranha:cache", "abcdef:type", "\"Piranha.Redis.UnitTests.Cache.TestObject, Piranha.Redis.UnitTests\"");
            }
        }

        public class the_indexer_get_operator_should : RedisCacheProviderFacts
        {
            [Fact]
            public void return_null_if_value_not_in_hash()
            {
                client.HashContainsEntry("piranha:cache", "invalid:key").Returns(false);

                object result = sut["invalid:key"];

                result.Should().BeNull();
            }

            [Fact]
            public void return_object_from_cache()
            {
                client.HashContainsEntry("piranha:cache", "abcdef").Returns(true);
                client.GetValueFromHash("piranha:cache", "abcdef:type").Returns("\"Piranha.Redis.UnitTests.Cache.TestObject, Piranha.Redis.UnitTests\"");
                client.GetValueFromHash("piranha:cache", "abcdef").Returns("{\"Index\":1}");
                
                object result = sut["abcdef"];
                
                result.As<TestObject>().Index.Should().Be(1);
            }
        }
    }

    public class TestObject
    {
        public int Index { get; set; }
    }
}
