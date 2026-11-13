
namespace Statistics2026.Utilities
{
    public class Debouncer
    {
        public CancellationTokenSource? CancellationToken = null;
        private readonly object _lock = new object();

        public void RunLaterAsync( int ms, Func<CancellationToken, Task<bool>> func )
        {
            lock( _lock )
            {
                // Cancel the previous scheduled execution
                CancellationToken?.Cancel();
                CancellationToken?.Dispose();
                CancellationToken = null;

                // Create a new token for the current execution
                CancellationToken = new CancellationTokenSource();
                var token = CancellationToken.Token;

                // Start the delay and execution task
                Task.Run( async () =>
                {
                    try
                    {
                        while( true )
                        {
                            // Wait for 5 seconds
                            await Task.Delay( ms, token ).ConfigureAwait( false );

                            // Execute the action if not canceled
                            var runAgain = await func( token ).ConfigureAwait( false );
                            if( !runAgain )
                                break;
                        }
                    }
                    catch( OperationCanceledException )
                    {
                        // Intentionally left blank: The task was canceled because a new call came in
                    }
                }, token );
            }
        }
    }
}
