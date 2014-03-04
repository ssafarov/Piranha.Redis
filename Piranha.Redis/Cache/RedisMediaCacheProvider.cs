using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Piranha.IO;
using ServiceStack;
using ServiceStack.Redis;

namespace Piranha.Cache
{
    public class RedisMediaCacheProvider : IMediaCacheProvider
    {
        private readonly IRedisClientsManager redisClientsManager;

        public RedisMediaCacheProvider()
        {
            string redisUrl = ConfigurationSettings.AppSettings["RedisUrl"];

            if (string.IsNullOrEmpty(redisUrl))
                throw new Exception("Must provide a valid redis url");

            redisClientsManager = new PooledRedisClientManager(
                poolSize: 1000,
                poolTimeOutSeconds: 1,
                readWriteHosts: new[] { redisUrl }
            );
        }

        public RedisMediaCacheProvider(IRedisClientsManager redisClientsManager)
        {
            this.redisClientsManager = redisClientsManager;
        }

        private string BuildItemKey(int width, int? height, bool draft = false)
        {
            return draft
                ? String.Format("draft:{0}:{1}", width, height)
                : String.Format("published:{0}:{1}", width, height);
        }

        private string BuildHashKey(Guid id, MediaType type)
        {
            switch (type)
            {
                case MediaType.Media:
                    return String.Format("piranha:media:{0}", id);
                case MediaType.Upload:
                    return String.Format("piranha:upload:{0}", id);
            }

            throw new ArgumentException(String.Format("Unknown type {0}", type));
        }


        /// <summary>
        /// Gets the data for the cached image with the given dimensions. In case of
        /// a cache miss null is returned.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The optional height of the image</param>
        /// <param name="type">The media type</param>
        /// <returns>
        /// The binary data, null in case of a cache miss
        /// </returns>
        public byte[] Get(Guid id, int width, int? height, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);
            var itemKey = BuildItemKey(width, height);

            using (var redisClient = redisClientsManager.GetClient())
            {
                if (redisClient.HashContainsEntry(hashKey, itemKey))
                {
                    return redisClient.GetValueFromHash(hashKey, itemKey).To<byte[]>();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the draft data for the cached image with the given dimensions. In case of
        /// a cache miss null is returned.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The optional height of the image</param>
        /// <param name="type">The media type</param>
        /// <returns>
        /// The binary data, null in case of a cache miss
        /// </returns>
        public byte[] GetDraft(Guid id, int width, int? height, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);
            var itemKey = BuildItemKey(width, height, true);

            using (var redisClient = redisClientsManager.GetClient())
            {
                if (redisClient.HashContainsEntry(hashKey, itemKey))
                {
                    return redisClient.GetValueFromHash(hashKey, itemKey).To<byte[]>();
                }
            }
            return null;
        }

        /// <summary>
        /// Stores the given cache data for the image with the given id.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="data">The media data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The optional height of the image</param>
        /// <param name="type">The media type</param>
        public void Put(Guid id, byte[] data, int width, int? height, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);
            var itemKey = BuildItemKey(width, height);

            using (var redisClient = redisClientsManager.GetClient())
            {
                redisClient.SetEntryInHash(hashKey, itemKey, data.ToJson());
            }
        }

        /// <summary>
        /// Stores the given cache data for the image draft with the given id.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="data">The media data</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The optional height of the image</param>
        /// <param name="type">The media type</param>
        public void PutDraft(Guid id, byte[] data, int width, int? height, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);
            var itemKey = BuildItemKey(width, height, true);

            using (var redisClient = redisClientsManager.GetClient())
            {
                redisClient.SetEntryInHash(hashKey, itemKey, data.ToJson());
            }
        }

        /// <summary>
        /// Deletes all cached images related to the given id, both draft and published.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="type">The media type</param>
        public void Delete(Guid id, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);

            using (var redisClient = redisClientsManager.GetClient())
            {
                redisClient.Remove(hashKey);
            }
        }

        /// <summary>
        /// Gets the total size of all items in the cache.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="type">The media type</param>
        /// <returns>The size of the cache in bytes.</returns>
        public long GetTotalSize(Guid id, MediaType type = MediaType.Media)
        {
            var hashKey = BuildHashKey(id, type);

            long size = 0;

            using (var redisClient = redisClientsManager.GetClient())
            {
                foreach (var hashItem in redisClient.GetAllEntriesFromHash(hashKey))
                {
                    var item = hashItem.Value.To<byte[]>();
                    if (item != null)
                    {
                        size += item.Length;
                    }
                }
            }

            return size;
        }
    }
}
