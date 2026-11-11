using MediaBrowser.Controller.Entities;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        private string GetSingleValueFromSQL( string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            var cmd = new SQLCmdDef( sql, parameters );

            var retVal = string.Empty;
            ExecuteCommand( cmd, statement =>
                {
                    var row = statement.Current;
                    var count = row.GetInt64( 0 );
                    retVal = formatter?.Invoke( count ) ?? count.ToString();
                    return false;
                } );
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleItem( string title, string? help, string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );
            var value = GetSingleValueFromSQL( sql, parameters, formatter );
            retVal.AddLine( value, false );
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleValue( string title, string? help, object value )
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );
            retVal.AddLine( value.ToString(), false );
            return retVal;
        }

        public StatCard StatisticFor( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckLevel.eReport );

            var statGen = new StatGen( whichStatistic, videoType, this );
            return statGen.GetStatCard();
        }

        public StatGen.StatCardValues StatCardValuesFor( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckLevel.eReport );

            var statGen = new StatGen( whichStatistic, videoType, this );
            return statGen.GetStatCardValues();
        }
    }
}
