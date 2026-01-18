using System;

namespace Anarchy
{
    internal class AutoConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _valueCreate;

        public AutoConcurrentDictionary(Func<TKey, TValue> valueCreate)
        {
            _valueCreate = valueCreate;
        }

        public new TValue this[TKey key]
        {
            get
            {
            	TValue value;
				return TryGetValue(key, out value) ? value : this[key] = _valueCreate(key);
            }
            set { base[key] = value; }
        }

        public TValue this[TKey key, bool doNotSave]
        {
            get
            {
                if (doNotSave)
                {
                	TValue value;
					return TryGetValue(key, out value) ? value : _valueCreate(key);
                }
                else
                    return this[key];
            }
        }
    }
}
