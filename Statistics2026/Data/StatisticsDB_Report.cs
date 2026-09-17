using MediaBrowser.Controller.Entities;
using Statistics2026.Api;
using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private string GetSingleValueFromSQL( string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null )
        {
            CheckIsValid( ECheckType.eReport );

            var cmd = new SQLCmdDef( sql, parameters );

            var retVal = string.Empty;
            _dbHelper.ExecuteCommand( cmd, statement =>
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
            CheckIsValid( ECheckType.eReport );

            var retVal = new TextBasedStatCard( title, help, EStatCardSize.eSmall );
            var value = GetSingleValueFromSQL( sql, parameters, formatter );
            retVal.AddLine( value );
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleValue( string title, string? help, object value )
        {
            CheckIsValid( ECheckType.eReport );

            var retVal = new TextBasedStatCard( title, help, EStatCardSize.eSmall );
            retVal.AddLine( value.ToString() );
            return retVal;
        }

        public StatCard StatisticFor( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckType.eReport );

            var statGen = new StatGen( whichStatistic, videoType, _dbHelper );
            return statGen.GetStatCard();
        }

        public StatGen.StatCardValues StatCardValuesFor( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckType.eReport );

            var statGen = new StatGen( whichStatistic, videoType, _dbHelper );
            return statGen.GetStatCardValues();
        }
    }
}
