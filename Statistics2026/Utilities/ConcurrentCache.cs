using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Statistics2026.Utilities
{
    [CollectionBuilder( typeof( ConcurrentCache ), nameof( ConcurrentCache.Create ) )]
    public class ConcurrentCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
    {
        private readonly object _syncRoot = new();

        // Store the node directly in the dictionary value for O(1) list operations
        private readonly Dictionary<TKey, (TValue Value, LinkedListNode<TKey> Node)> _dictionary;
        private readonly LinkedList<TKey> _keyList = new();
        private int _maxCapacity;

        public int MaxCapacity
        {
            get { lock( _syncRoot ) return _maxCapacity; }
            set
            {
                lock( _syncRoot )
                {
                    _maxCapacity = value;
                    TrimToCapacity();
                }
            }
        }

        public ConcurrentCache( int maxCapacity = 50 )
        {
            if( maxCapacity <= 0 )
                throw new ArgumentOutOfRangeException( nameof( maxCapacity ) );

            _maxCapacity = maxCapacity;
            _dictionary = new Dictionary<TKey, (TValue, LinkedListNode<TKey>)>( maxCapacity );
        }

        public int Count
        {
            get { lock( _syncRoot ) return _dictionary.Count; }
        }

        public TValue this[ TKey key ]
        {
            get
            {
                lock( _syncRoot )
                {
                    if( _dictionary.TryGetValue( key, out var entry ) )
                    {
                        // Refresh LRU order on Read
                        _keyList.Remove( entry.Node );
                        _keyList.AddFirst( entry.Node );
                        return entry.Value;
                    }
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                lock( _syncRoot )
                {
                    if( _dictionary.TryGetValue( key, out var entry ) )
                    {
                        // Update existing entry
                        _keyList.Remove( entry.Node );
                        _keyList.AddFirst( entry.Node );
                        _dictionary[ key ] = (value, entry.Node);
                    }
                    else
                    {
                        // Insert new entry
                        var node = new LinkedListNode<TKey>( key );
                        _keyList.AddFirst( node );
                        _dictionary[ key ] = (value, node);

                        TrimToCapacity();
                    }
                }
            }
        }

        public bool TryGetValue( TKey key, out TValue value )
        {
            lock( _syncRoot )
            {
                if( _dictionary.TryGetValue( key, out var entry ) )
                {
                    _keyList.Remove( entry.Node );
                    _keyList.AddFirst( entry.Node );
                    value = entry.Value;
                    return true;
                }
            }
            value = default!;
            return false;
        }

        public void Add( TKey key, TValue value ) => this[ key ] = value;

        private void TrimToCapacity()
        {
            // Internal helper assuming caller holds the lock
            while( _dictionary.Count > _maxCapacity && _keyList.Count > 0 )
            {
                var oldestNode = _keyList.Last;
                _keyList.RemoveLast();
                _dictionary.Remove( oldestNode.Value );
            }
        }

        // Thread-safe enumeration via cloning/snapshotting
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey, TValue>> snapshot;
            lock( _syncRoot )
            {
                snapshot = new List<KeyValuePair<TKey, TValue>>( _dictionary.Count );
                // Maintain LRU chronological order in output if preferred, 
                // or just iterate the dictionary. Let's iterate chronologically:
                foreach( var key in _keyList )
                {
                    snapshot.Add( new KeyValuePair<TKey, TValue>( key, _dictionary[ key ].Value ) );
                }
            }

            foreach( var item in snapshot )
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class ConcurrentCache
    {
        public static ConcurrentCache<TKey, TValue> Create<TKey, TValue>( ReadOnlySpan<KeyValuePair<TKey, TValue>> items ) where TKey : notnull
        {
            var cache = new ConcurrentCache<TKey, TValue>( items.Length == 0 ? 50 : items.Length );
            foreach( var item in items )
            {
                cache.Add( item.Key, item.Value );
            }
            return cache;
        }
    }
}
