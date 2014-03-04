using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Piranha.Cache;
using Piranha.IO;
using ServiceStack;
using ServiceStack.Redis;
using Xunit;

namespace Piranha.Redis.UnitTests.Cache
{
    public class RedisMediaCacheProviderFacts
    {
        private readonly RedisMediaCacheProvider sut;
        private readonly IRedisClientsManager manager;
        private readonly IRedisClient client;

        public RedisMediaCacheProviderFacts()
        {
            manager = Substitute.For<IRedisClientsManager>();
            client = Substitute.For<IRedisClient>();
            sut = new RedisMediaCacheProvider(manager);

            manager.GetClient().Returns(client);
        }

        public class TheGetMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;
            private byte[] data;

            public TheGetMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
                data = Encoding.UTF8.GetBytes("Something testable");

                client.HashContainsEntry("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "published:100:100").Returns(true);
                client.GetValueFromHash("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "published:100:100").Returns(data.ToJson());
            }

            [Fact]
            public void MakeTheCorrectKeys()
            {
                //Act
                sut.Get(id, 100, 100);

                //Assert
                client.Received(1).HashContainsEntry("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "published:100:100");
            }

            [Fact]
            public void ReturnTheByteArray()
            {
                //Act
                byte[] result = sut.Get(id, 100, 100);

                //Assert
                result.Should().BeEquivalentTo(data);
            }

            [Fact]
            public void ReturnNull()
            {
                client.HashContainsEntry("", "").ReturnsForAnyArgs(false);

                //Act
                var result = sut.Get(id, 100, 100);

                //Assert
                result.Should().BeNull();
            }
        }

        public class TheGetDraftMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;
            private byte[] data;

            public TheGetDraftMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
                data = Encoding.UTF8.GetBytes("Something testable");

                client.HashContainsEntry("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "draft:100:100").Returns(true);
                client.GetValueFromHash("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "draft:100:100").Returns(data.ToJson());
            }

            [Fact]
            public void MakeTheCorrectKeys()
            {
                //Act
                sut.GetDraft(id, 100, 100);

                //Assert
                client.Received(1).HashContainsEntry("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "draft:100:100");
            }

            [Fact]
            public void ReturnTheByteArray()
            {
                //Act
                var result = sut.GetDraft(id, 100, 100);

                //Assert
                result.Should().BeEquivalentTo(data);
            }

            [Fact]
            public void ReturnNull()
            {
                client.HashContainsEntry("", "").ReturnsForAnyArgs(false);

                //Act
                var result = sut.GetDraft(id, 100, 100);

                //Assert
                result.Should().BeNull();
            }
        }

        public class ThePutMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;
            private byte[] data;

            public ThePutMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
                data = Encoding.UTF8.GetBytes("Something testable");
            }

            [Fact]
            public void SetEntryInHash()
            {
                sut.Put(id, data, 100, 100);

                client.Received(1).SetEntryInHash("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "published:100:100", data.ToJson());
            }
        }

        public class ThePutDraftMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;
            private byte[] data;

            public ThePutDraftMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
                data = Encoding.UTF8.GetBytes("Something testable");
            }

            [Fact]
            public void SetEntryInHash()
            {
                sut.PutDraft(id, data, 100, 100);

                client.Received(1).SetEntryInHash("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301", "draft:100:100", data.ToJson());
            }
        }

        public class TheDeleteMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;

            public TheDeleteMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
            }

            [Fact]
            public void RemoveTheKey()
            {
                sut.Delete(id);

                client.Received(1).Remove("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301");
            }

            [Fact]
            public void RemoveTheKeyForUpload()
            {
                sut.Delete(id, MediaType.Upload);

                client.Received(1).Remove("piranha:upload:3f2504e0-4f89-41d3-9a0c-0305e82c3301");
            }
        }

        public class TheGetTotalSizeMethodShould : RedisMediaCacheProviderFacts
        {
            private Guid id;

            public TheGetTotalSizeMethodShould()
            {
                id = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
            }

            [Fact]
            public void RemoveTheKey()
            {
                var data = Encoding.UTF8.GetBytes("Something testable");
                var data2 = Encoding.UTF8.GetBytes("Something testable 2");
                var data3 = Encoding.UTF8.GetBytes("Something testable 23");

                int expectedResult = data.Length + data2.Length + data3.Length;

                client.GetAllEntriesFromHash("piranha:media:3f2504e0-4f89-41d3-9a0c-0305e82c3301")
                    .Returns(new Dictionary<string, string>
                    {
                        { "1", data.ToJson() },
                        { "2", data2.ToJson() },
                        { "3", data3.ToJson() }
                    }
                    );

                var result = sut.GetTotalSize(id);

                result.Should().Be(expectedResult);
            }
        }
    }
}
