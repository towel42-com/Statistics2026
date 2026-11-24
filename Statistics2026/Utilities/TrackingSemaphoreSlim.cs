#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class TrackingSemaphoreSlim : IDisposable
    {
        private sealed class LockHolder
        {
            public bool IsHeld;
        }

        private readonly SemaphoreSlim _semaphore;
        private Action<string>? _warnLogger;
        private TimeSpan _warnInterval;
        private readonly AsyncLocal<LockHolder?> _currentFlowHolder = new AsyncLocal<LockHolder?>();

        public int? OwnerThreadId { get; private set; }
        public string? OwnerMethod { get; private set; }
        public string? OwnerStackTrace { get; private set; }
        public DateTime? AcquiredAtUtc { get; private set; }
        public int CurrentCount => _semaphore.CurrentCount;

        public TrackingSemaphoreSlim(
            int initialCount,
            Action<string>? warnLogger = null,
            TimeSpan? warnInterval = null )
        {
            _semaphore = new SemaphoreSlim( initialCount );
            _warnLogger = warnLogger;
            _warnInterval = warnInterval ?? TimeSpan.FromSeconds( 5 );
        }

        public TrackingSemaphoreSlim(
            int initialCount,
            int maxCount,
            Action<string>? warnLogger = null,
            TimeSpan? warnInterval = null )
        {
            _semaphore = new SemaphoreSlim( initialCount, maxCount );
            _warnLogger = warnLogger;
            _warnInterval = warnInterval ?? TimeSpan.FromSeconds( 5 );
        }

        public void setWarningLogger( Action<string> warnLogger, TimeSpan? warnInterval = null )
        {
            _warnLogger = warnLogger;
            _warnInterval = warnInterval ?? TimeSpan.FromSeconds( 5 );
        }

        public bool WillDeadLock()
        {
            return _currentFlowHolder.Value?.IsHeld == true;
        }

        // Non-async outer method sets the mutable holder on the CALLER'S ExecutionContext!
        public Task WaitAsync(
            CancellationToken cancellationToken = default,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
        {
            string waiterInfo = $"{callerName} ({Path.GetFileName( filePath )}:{lineNumber})";
            ThrowIfDeadlock( waiterInfo );

            LockHolder holder = EnsureCurrentFlowHolder();
            return WaitInternalAsync( holder, waiterInfo, cancellationToken );
        }

        private async Task WaitInternalAsync(
            LockHolder holder,
            string waiterInfo,
            CancellationToken cancellationToken )
        {
            while( !await _semaphore.WaitAsync( _warnInterval, cancellationToken ).ConfigureAwait( false ) )
            {
                double heldSeconds = AcquiredAtUtc.HasValue
                    ? ( DateTime.UtcNow - AcquiredAtUtc.Value ).TotalSeconds
                    : 0;

                _warnLogger?.Invoke(
                    $"[TrackingSemaphoreSlim] {waiterInfo} on Thread {Environment.CurrentManagedThreadId} " +
                    $"has been waiting {_warnInterval.TotalSeconds:F0}s+ for lock. " +
                    $"Held for {heldSeconds:F1}s by Thread {OwnerThreadId} in {OwnerMethod}.\n" +
                    $"Owner Stack:\n{OwnerStackTrace}" );
            }

            RecordOwner( holder, waiterInfo );
        }

        public Task<bool> WaitAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
        {
            string waiterInfo = $"{callerName} ({Path.GetFileName( filePath )}:{lineNumber})";
            ThrowIfDeadlock( waiterInfo );

            LockHolder holder = EnsureCurrentFlowHolder();
            return WaitTimeoutInternalAsync( holder, timeout, waiterInfo, cancellationToken );
        }

        private async Task<bool> WaitTimeoutInternalAsync(
            LockHolder holder,
            TimeSpan timeout,
            string waiterInfo,
            CancellationToken cancellationToken )
        {
            if( !await _semaphore.WaitAsync( timeout, cancellationToken ).ConfigureAwait( false ) )
            {
                return false;
            }

            RecordOwner( holder, waiterInfo );
            return true;
        }

        public void Wait(
            CancellationToken cancellationToken = default,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
        {
            string waiterInfo = $"{callerName} ({Path.GetFileName( filePath )}:{lineNumber})";
            ThrowIfDeadlock( waiterInfo );

            LockHolder holder = EnsureCurrentFlowHolder();

            while( !_semaphore.Wait( _warnInterval, cancellationToken ) )
            {
                double heldSeconds = AcquiredAtUtc.HasValue
                    ? ( DateTime.UtcNow - AcquiredAtUtc.Value ).TotalSeconds
                    : 0;

                _warnLogger?.Invoke(
                    $"[TrackingSemaphoreSlim] {waiterInfo} on Thread {Environment.CurrentManagedThreadId} " +
                    $"has been waiting {_warnInterval.TotalSeconds:F0}s+ for lock. " +
                    $"Held for {heldSeconds:F1}s by Thread {OwnerThreadId} in {OwnerMethod}.\n" +
                    $"Owner Stack:\n{OwnerStackTrace}" );
            }

            RecordOwner( holder, waiterInfo );
        }

        public bool Wait(
            TimeSpan timeout,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0 )
        {
            string waiterInfo = $"{callerName} ({Path.GetFileName( filePath )}:{lineNumber})";
            ThrowIfDeadlock( waiterInfo );

            LockHolder holder = EnsureCurrentFlowHolder();

            if( !_semaphore.Wait( timeout, cancellationToken ) )
            {
                return false;
            }

            RecordOwner( holder, waiterInfo );
            return true;
        }

        public int Release()
        {
            ClearOwner();
            return _semaphore.Release();
        }

        public int Release( int releaseCount )
        {
            ClearOwner();
            return _semaphore.Release( releaseCount );
        }

        private LockHolder EnsureCurrentFlowHolder()
        {
            LockHolder? holder = _currentFlowHolder.Value;
            if( holder == null )
            {
                holder = new LockHolder();
                _currentFlowHolder.Value = holder;
            }
            return holder;
        }

        private void ThrowIfDeadlock( string waiterInfo )
        {
            if( WillDeadLock() )
            {
                string message =
                    $"[TrackingSemaphoreSlim] DEADLOCK DETECTED: {waiterInfo} on Thread {Environment.CurrentManagedThreadId} " +
                    $"attempted to re-enter lock already held by Thread {OwnerThreadId} in {OwnerMethod}.\n" +
                    $"Original Owner Stack:\n{OwnerStackTrace}";

                _warnLogger?.Invoke( message );
                throw new InvalidOperationException( message );
            }
        }

        private void RecordOwner( LockHolder holder, string callerInfo )
        {
            holder.IsHeld = true;
            OwnerThreadId = Environment.CurrentManagedThreadId;
            OwnerMethod = callerInfo;
            OwnerStackTrace = Environment.StackTrace;
            AcquiredAtUtc = DateTime.UtcNow;
        }

        private void ClearOwner()
        {
            if( _currentFlowHolder.Value != null )
            {
                _currentFlowHolder.Value.IsHeld = false;
            }

            OwnerThreadId = null;
            OwnerMethod = null;
            OwnerStackTrace = null;
            AcquiredAtUtc = null;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
