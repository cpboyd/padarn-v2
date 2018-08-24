//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.Collections.Specialized;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Provides a way to index and retrieve a collection of <see cref="IHttpModule"/> objects.
    /// </summary>
    public sealed class HttpModuleCollection : NameObjectCollectionBase
    {
        private IHttpModule[] _all;
        private string[] _allKeys;

        /// <summary>
        /// Initializes a new instance of the HttpModuleCollection class.
        /// </summary>
        internal HttpModuleCollection()
            : base(StringComparer.InvariantCultureIgnoreCase)
        { }

        /// <summary>
        /// Gets a string array containing all the keys (module names) in the HttpModuleCollection.
        /// </summary>
        public string[] AllKeys
        {
            get
            {
                if (_allKeys == null)
                {
                    _allKeys = BaseGetAllKeys();
                }

                return _allKeys;
            }
        }

        /// <summary>
        /// Gets the <see cref="IHttpModule"/> object with the specified name from the HttpModuleCollection.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IHttpModule this[string name]
        {
            get { return Get(name); }
        }

        /// <summary>
        /// Gets the <see cref="IHttpModule"/> object with the specified numerical index from the HttpModuleCollection.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IHttpModule this[int index]
        {
            get { return Get(index); }
        }

        /// <summary>
        /// Copies members of the module collection to an Array, beginning at the specified index of the array.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="index"></param>
        public void CopyTo(Array dest, int index)
        {
            if (_all == null)
            {
                int count = Count;
                _all = new IHttpModule[count];
                for (int i = 0; i < count; i++)
                {
                    _all[i] = Get(i);
                }
            }
            _all.CopyTo(dest, index);
        }

        /// <summary>
        /// Returns the <see cref="IHttpModule"/> object with the specified index from the HttpModuleCollection.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IHttpModule Get(int index)
        {
            return (IHttpModule)BaseGet(index);
        }

        /// <summary>
        /// Returns the <see cref="IHttpModule"/> object with the specified name from the HttpModuleCollection.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IHttpModule Get(string name)
        {
            return (IHttpModule)BaseGet(name);
        }

        /// <summary>
        /// Returns the key (name) of the <see cref="IHttpModule"/> object at the specified numerical index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetKey(int index)
        {
            return BaseGetKey(index);
        }

        internal void Add(string name, IHttpModule module)
        {
            _all = null;
            _allKeys = null;

            BaseAdd(name, module);
        }

        internal void AddRange(HttpModuleCollection collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                Add(collection.GetKey(i), collection.Get(i));
            }
        }

        internal void Init(HttpApplication application)
        {
            for (int i = 0; i < Count; i++)
            {
                Get(i).Init(application);
            }
        }
    }
}
