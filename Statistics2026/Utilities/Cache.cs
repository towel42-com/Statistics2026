using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Statistics2026.Utilities
{
    [CollectionBuilder( typeof( Cache ), nameof( Cache.Create ) )]
    public class Cache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> // <-- Add Interface
    {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly Queue<TKey> _keyQueue;

        public int MaxCapacity
        {
            get;
            set
            {
                field = value;
                while( _dictionary.Count > field && _keyQueue.Count > 0 )
                {
                    var oldestKey = _keyQueue.Dequeue();
                    _dictionary.Remove( oldestKey );
                }
            }
        } = 50;

        public Cache( int maxCapacity = 50 )
        {
            _dictionary = new Dictionary<TKey, TValue>( maxCapacity );
            _keyQueue = new Queue<TKey>( maxCapacity );
            MaxCapacity = maxCapacity;
        }

        public int Count => _dictionary.Count;

        public TValue this[ TKey key ]
        {
            get => _dictionary[ key ];
            set
            {
                if( !_dictionary.ContainsKey( key ) )
                {
                    if( _dictionary.Count >= MaxCapacity )
                    {
                        var oldestKey = _keyQueue.Dequeue();
                        _dictionary.Remove( oldestKey );
                    }
                    _keyQueue.Enqueue( key );
                }
                _dictionary[ key ] = value;
            }
        }

        public bool TryGetValue( TKey key, out TValue value )
        {
            return _dictionary.TryGetValue( key, out value );
        }

        public void Add( TKey key, TValue value )
        {
            this[ key ] = value;
        }

        // --- Required to satisfy CollectionBuilder Element Type requirements ---
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class Cache
    {
        public static Cache<TKey, TValue> Create<TKey, TValue>( ReadOnlySpan<KeyValuePair<TKey, TValue>> items )
        {
            var dict = new Cache<TKey, TValue>( items.Length == 0 ? 50 : items.Length );
            foreach( var item in items )
            {
                dict.Add( item.Key, item.Value );
            }
            return dict;
        }
    }
}

namespace System.Runtime.CompilerServices
{
#if !NET8_0_OR_GREATER
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false )]
    internal sealed class CollectionBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }
        public string MethodName { get; }

        public CollectionBuilderAttribute( Type builderType, string methodName )
        {
            BuilderType = builderType;
            MethodName = methodName;
        }
    }
#endif
}
