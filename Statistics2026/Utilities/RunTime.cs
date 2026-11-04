using System;
using System.Text;

namespace Statistics2026.Utilities
{
    public class RunTime : IComparable<RunTime>
    {
        private TimeSpan _timeSpan;

        public int Days => _timeSpan.Days;
        public int Hours => _timeSpan.Hours;
        public int Minutes => _timeSpan.Minutes;
        public int Seconds => _timeSpan.Seconds;
        public int Milliseconds => _timeSpan.Milliseconds;
        public long Ticks => _timeSpan.Ticks;
        public bool IsNegative => _timeSpan < TimeSpan.Zero;

        public RunTime( TimeSpan timeSpan = new TimeSpan() )
        {
            _timeSpan = timeSpan;
        }

        static public RunTime CreateFromMS( long ms )
        {
            return new RunTime( new TimeSpan( ms * TimeSpan.TicksPerMillisecond ) );
        }


        public RunTime( object ticks )
        {
            _timeSpan = new TimeSpan( Convert.ToInt64( ticks ) );
        }

        public void Add( TimeSpan timespan )
        {
            _timeSpan = _timeSpan.Add( timespan );
        }

        public void Add( long? ticks )
        {
            _timeSpan = _timeSpan.Add( new TimeSpan( ticks ?? 0 ) );
        }

        public override string ToString()
        {
            if ( IsNegative )
            {
                return $"-{new RunTime( _timeSpan.Negate() ).ToString()}";
            }
            return $"<td>{Days}</td><td>{Hours}</td><td>{Minutes}</td>";
        }

        public string ToLongString()
        {
            if( IsNegative )
            {
                return $"-{new RunTime( _timeSpan.Negate() ).ToLongString()}";
            }

            var days = Days != 1
                   ? $"{Days} days"
                   : $"{Days} day";
            var hours = Hours != 1
                ? $"{Hours} hours"
                : $"{Hours} hour";
            var minutes = Minutes != 1
                ? $"{Minutes} minutes"
                : $"{Minutes} minute";
            return $"{days}, {hours} and {minutes}";

        }

        public string ToShortString( bool verbose = true )
        {
            if( IsNegative )
            {
                return $"-{new RunTime( _timeSpan.Negate() ).ToShortString( verbose )}";
            }
            if ( verbose )
                return $"{Days:D2}:{Hours:D2}:{Minutes:D2}:{Seconds:D2}.{Milliseconds:D3}";
            else
            {
                StringBuilder retVal = new StringBuilder();

                if( Days > 0 )
                    retVal.Append( $"{Days:D2}:" );
                if( ( retVal.Length > 0 ) || ( Hours > 0 ) )
                    retVal.Append( $"{Hours:D2}:" );

                if( ( retVal.Length > 0 ) || ( Minutes > 0 ) )
                    retVal.Append( $"{Minutes:D2}:" );

                retVal.Append( $"{Seconds:D2}.{Milliseconds:D3}" );

                return retVal.ToString();
            }
        }

        public int CompareTo( RunTime other )
        {
            if( ReferenceEquals( this, other ) )
                return 0;
            if( ReferenceEquals( null, other ) )
                return 1;
            return _timeSpan.CompareTo( other._timeSpan );
        }
    }
}
