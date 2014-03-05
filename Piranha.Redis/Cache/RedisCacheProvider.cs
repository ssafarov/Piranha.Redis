using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Piranha.Cache;
using Piranha.Models;
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace Piranha.Cache
{
    public class RedisCacheProvider : ICacheProvider
    {
        private const string PiranhaHash = "piranha:cache";
        private readonly IRedisClientsManager redisClientsManager;

        public RedisCacheProvider()
        {
            string redisUrl = ConfigurationSettings.AppSettings["RedisUrl"];

            if(string.IsNullOrEmpty(redisUrl))
                throw new Exception("Must provide a valid redis url");

            redisClientsManager = new PooledRedisClientManager(
                poolSize: 1000,
                poolTimeOutSeconds: 1,
                readWriteHosts: new[] { redisUrl }
            );
        }

        public RedisCacheProvider(IRedisClientsManager redisClientsManager)
        {
            this.redisClientsManager = redisClientsManager;
        }

        public void Remove(string key)
        {
            using (var redisClient = redisClientsManager.GetClient())
            {
                redisClient.RemoveEntryFromHash(PiranhaHash, key);
            }
        }

        public bool Contains(string key)
        {
            using (var redisClient = redisClientsManager.GetClient())
            {
                return redisClient.HashContainsEntry(PiranhaHash, key);
            }
        }

        public object this[string key]
        {
            get
            {
                using (var redisClient = redisClientsManager.GetClient())
                {
                    if (redisClient.HashContainsEntry(PiranhaHash, key))
                    {
                        string resultJson = redisClient.GetValueFromHash(PiranhaHash, key);
                        string objType = redisClient.GetValueFromHash(PiranhaHash, String.Format("{0}:type", key));

                        Type t = objType.To<Type>();
                        object result = JsonSerializer.DeserializeFromString(resultJson, t);

                        return result;
                    }
                }
                return null;
            }
            set
            {
                using (var redisClient = redisClientsManager.GetClient())
                {
                    redisClient.SetEntryInHash(PiranhaHash, key, value.ToJson());
                    redisClient.SetEntryInHash(PiranhaHash, String.Format("{0}:type", key), value.GetType().ToJson());
                }
            }
        }
    }
}
