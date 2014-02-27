using System;

namespace Piranha.Cache
{
	public class RedisCacheProvider : ICacheProvider
	{
		public object this[string key] {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public void Remove(string key) {
			throw new NotImplementedException();
		}

		public bool Contains(string key) {
			throw new NotImplementedException();
		}
	}
}
