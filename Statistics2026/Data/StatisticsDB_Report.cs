using MediaBrowser.Controller.Entities;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        private Task RunSQLOnRowAsync( string sql, Func<IResultSet, Task<bool>> onRow, List<(string name, object? value)>? parameters = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            var cmd = new SQLCmdDef( sql, parameters );

            string retVal = string.Empty;
            return ExecuteCommandAsync( cmd, async (statement) =>
            {
                IResultSet row = statement.Current;
                return await onRow( row ).ConfigureAwait( false );
            } );
        }

        private async Task<string> GetSingleValueFromSQLAsync( string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            var cmd = new SQLCmdDef( sql, parameters );

            string retVal = string.Empty;
            await ExecuteCommandAsync( cmd, statement =>
                {
                    IResultSet row = statement.Current;
                    long count = row.GetInt64( 0 );
                    retVal = formatter?.Invoke( count ) ?? count.ToString();
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );
            return retVal;
        }

        private async Task<TextBasedStatCard> ValueGroupForSingleItemAsync( string title, string? help, string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );
            string value = await GetSingleValueFromSQLAsync( sql, parameters, formatter ).ConfigureAwait( false );
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

        private Task<StatGen.StatCardValues> StatCardValuesForAsync( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckLevel.eReport );

            var statGen = new StatGen( whichStatistic, videoType, this );
            return statGen.GetStatCardValuesAsync();
        }

        public Task<StatCard> StatisticForAsync( User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType )
        {
            CheckIsValid( ECheckLevel.eReport );

            var statGen = new StatGen( whichStatistic, videoType, this );
            return statGen.GetStatCardAsync();
        }
    }
}
