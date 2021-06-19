using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Players;

namespace Uncreated.Warfare
{
    public class WarfareSQL : AsyncDatabase<WarfareCaller>
    {
        public WarfareSQL(string connection_string) : base(connection_string) { }
        public override void Log(string message, ConsoleColor color = ConsoleColor.Gray) => F.Log(message, color);
        public override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow) => F.LogWarning(message, color);
        public override void LogError(string message, ConsoleColor color = ConsoleColor.Red) => F.LogError(message, color);
        public override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red) => F.LogError(ex, color);
        /// <summary>
        /// <para>Asynchronous operation to update a player's saved username in usernames table.</para>
        /// <para>Sends a UsernameChanged event to node server as well if the username is different from that which is in the SQL database.</para>
        /// </summary>
        public void UpdateUsernameAsync(ulong Steam64, FPlayerName player)
        {
            D_UpdateUsernameAsync caller = _dbCaller.UpdateUsername;
            caller.BeginInvoke(Steam64, player, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>
        /// Add a kill (default 1) to the "playerstats" table.
        /// </summary>
        /// <param name="Steam64">Player's Steam64 ID to add a kill to.</param>
        /// <param name="Team">Team to add the kill to.</param>
        /// <param name="amount">Amount of kills to add, default 1.</param>
        public void AddKill(ulong Steam64, byte Team, int amount = 1)
        {
            D_AddPlayerStat caller = _dbCaller.AddKill;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>Add a death (default 1) to the "playerstats" table.</summary>
        public void AddDeath(ulong Steam64, byte Team, int amount = 1)
        {
            D_AddPlayerStat caller = _dbCaller.AddDeath;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>Add a teamkill (default 1) to the "playerstats" table.</summary>
        public void AddTeamkill(ulong Steam64, byte Team, int amount = 1)
        {
            D_AddPlayerStat caller = _dbCaller.AddTeamkill;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>Add (or subtract with a negative <paramref name="amount"/>) xp to the "levels" table.</summary>
        public void AddXP(ulong Steam64, byte Team, int amount, bool clampOnSubtract = false, D_Difference amtTooHigh = null)
        {
            if (amount < 0)
            {
                D_SubtractPlayerStat caller = _dbCaller.SubtractXP;
                caller.BeginInvoke(Steam64, Team, -amount, amtTooHigh, clampOnSubtract, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            }
            else
            {
                D_AddPlayerStat caller = _dbCaller.AddXP;
                caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            }
        }
        /// <summary>Add (or subtract with a negative <paramref name="amount"/>) xp to the "levels" table.</summary>
        /// <returns>The difference between the amount and the original value if the amount was higher than the value. (or 0 if it was successful)</returns>
        public long AddXPSync(ulong Steam64, byte Team, int amount, bool clampOnSubtract = false)
        {
            if (amount < 0)
            {
                D_SubtractPlayerStat caller = _dbCaller.SubtractXP;
                long difference = 0;
                IAsyncResult ar = caller.BeginInvoke(Steam64, Team, -amount, (dif) => { difference = dif; }, clampOnSubtract, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
                ar.AsyncWaitHandle.WaitOne();
                return difference;
            }
            else
            {
                D_AddPlayerStat caller = _dbCaller.AddXP;
                IAsyncResult ar = caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
                ar.AsyncWaitHandle.WaitOne();
                return 0;
            }
        }
        /// <summary>Add (or subtract with a negative <paramref name="amount"/>) officer points to the "levels" table.</summary>
        public void AddOfficerPoints(ulong Steam64, byte Team, int amount, D_Difference amtTooHigh = null, bool clampOnSubtract = false)
        {
            if (amount < 0)
            {
                D_SubtractPlayerStat caller = _dbCaller.SubtractOfficerPoints;
                caller.BeginInvoke(Steam64, Team, -amount, amtTooHigh, clampOnSubtract, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            }
            else
            {
                D_AddPlayerStat caller = _dbCaller.AddOfficerPoints;
                caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            }
        }
        /// <summary>Add officer points to the "levels" table.</summary>
        public long AddOfficerPointsSync(ulong Steam64, byte Team, int amount = 1, bool clampOnSubtract = false)
        {
            if (amount < 0)
            {
                D_SubtractPlayerStat caller = _dbCaller.SubtractOfficerPoints;
                long difference = 0;
                IAsyncResult ar = caller.BeginInvoke(Steam64, Team, -amount, (dif) => { difference = dif; }, clampOnSubtract, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
                ar.AsyncWaitHandle.WaitOne();
                return difference;
            }
            else
            {
                D_AddPlayerStat caller = _dbCaller.AddOfficerPoints;
                IAsyncResult ar = caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
                ar.AsyncWaitHandle.WaitOne();
                return 0;
            }
        }
        /// <summary>Retreive a player's xp from the "levels" table.</summary>
        public void GetXP(ulong Steam64, byte Team, D_Uint32BalanceReceived callback)
        {
            D_GetUInt32Balance caller = _dbCaller.GetXP;
            caller.BeginInvoke(Steam64, Team, callback, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public void GetOfficerPoints(ulong Steam64, byte Team, D_Uint32BalanceReceived callback)
        {
            D_GetUInt32Balance caller = _dbCaller.GetOfficerPoints;
            caller.BeginInvoke(Steam64, Team, callback, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public uint GetXPSync(ulong Steam64, byte Team)
        {
            D_GetUInt32Balance caller = _dbCaller.GetXP;
            uint xp = 0;
            IAsyncResult ar = caller.BeginInvoke(Steam64, Team,
                (balance, success) => { if (success) xp = balance; },
                AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            ar.AsyncWaitHandle.WaitOne();
            return xp;
        }
        public uint GetOfficerPointsSync(ulong Steam64, byte Team)
        {
            D_GetUInt32Balance caller = _dbCaller.GetOfficerPoints;
            uint op = 0;
            IAsyncResult ar = caller.BeginInvoke(Steam64, Team,
                (balance, success) => { if (success) op = balance; },
                AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            ar.AsyncWaitHandle.WaitOne();
            return op;
        }
        public void GetUsernameAsync(ulong ID, D_UsernameReceived callback)
        {
            D_GetUsername caller = _dbCaller.GetUsername;
            caller.BeginInvoke(ID, callback, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public override string GetTableName(string key)
        {
            if (Data.TableData.TryGetValue(key, out MySqlTableLang lang))
                return lang.TableName;
            else return key;
        }
        public override string GetColumnName(string table_key, string column_key, out string table_name)
        {
            if (Data.TableData.TryGetValue(table_key, out MySqlTableLang lang))
            {
                table_name = lang.TableName;
                if (lang.Columns.TryGetValue(column_key, out string column))
                    return column;
                else return column_key;
            }
            else
            {
                table_name = table_key;
                return column_key;
            }
        }
    }
    public class WarfareCaller : DbCaller
    {
        internal void GetUsername(ulong Steam64, D_UsernameReceived callback)
        {
            SelectData(StructGetUsername(_manager, Steam64), (ar) =>
            {
                IMySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if (vagueResponse is SelectResponse response)
                {
                    if (response != null && response.ExecutionStatus == EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(new FPlayerName()
                        {
                            Steam64 = Steam64,
                            CharacterName = response.GetColumn<string>(_manager.GetColumnName("usernames", "CharacterName", out _)).GetValue(0),
                            PlayerName = response.GetColumn<string>(_manager.GetColumnName("usernames", "PlayerName", out _)).GetValue(0),
                            NickName = response.GetColumn<string>(_manager.GetColumnName("usernames", "NickName", out _)).GetValue(0)
                        }, true);
                    }
                    else
                    {
                        string id = Steam64.ToString();
                        callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    }
                }
                else
                {
                    _manager.LogError("Couldn't get username from MySql Database. Cast error.\n\"" + vagueResponse.Command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    return;
                }
            });
        }
        internal void UpdateUsername(ulong Steam64, FPlayerName player)
        {
            SelectData(StructGetUsername(_manager, Steam64), new AsyncCallback((ar) =>
            {
                IMySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if (vagueResponse is SelectResponse response)
                {
                    SQLInsertOrUpdateStructure s2;
                    if (response != null && response.ExecutionStatus == EExecutionStatus.SUCCESS)
                    {
                        string oldPlayerName = response.GetColumn<string>(_manager.GetColumnName("usernames", "PlayerName", out _)).GetValue(0);
                        string oldCharacterName = response.GetColumn<string>(_manager.GetColumnName("usernames", "CharacterName", out _)).GetValue(0);
                        string oldNickName = response.GetColumn<string>(_manager.GetColumnName("usernames", "NickName", out _)).GetValue(0);
                        bool updatePlayerName = false;
                        bool updateCharacterName = false;
                        bool updateNickName = false;
                        if (oldPlayerName == null || oldPlayerName != player.PlayerName)
                            updatePlayerName = true;
                        if (oldCharacterName == null || oldCharacterName != player.CharacterName)
                            updateCharacterName = true;
                        if (oldNickName == null || oldNickName != player.NickName)
                            updateNickName = true;
                        if (!updatePlayerName && !updateNickName && !updateCharacterName) return;
                        Networking.Client.SendPlayerUpdatedUsername(player);
                        Dictionary<string, EUpdateOperation> varsToUpdate = new Dictionary<string, EUpdateOperation>();
                        if (updatePlayerName)
                            varsToUpdate.Add(_manager.GetColumnName("usernames", "PlayerName", out _), EUpdateOperation.SETFROMVALUES);
                        if (updateNickName)
                            varsToUpdate.Add(_manager.GetColumnName("usernames", "CharacterName", out _), EUpdateOperation.SETFROMVALUES);
                        if (updateNickName)
                            varsToUpdate.Add(_manager.GetColumnName("usernames", "NickName", out _), EUpdateOperation.SETFROMVALUES);
                        s2 = new SQLInsertOrUpdateStructure(_manager)
                        {
                            NewValues = new Dictionary<string, object>
                            {
                                { _manager.GetColumnName("usernames", "Steam64", out _), Steam64 },
                                { _manager.GetColumnName("usernames", "PlayerName", out _), player.PlayerName },
                                { _manager.GetColumnName("usernames", "CharacterName", out _), player.CharacterName },
                                { _manager.GetColumnName("usernames", "NickName", out string tablename), player.NickName }
                            },
                            VariablesToUpdateIfDuplicate = varsToUpdate,
                            tableName = tablename,
                            UpdateValuesIfValid = null
                        };
                    }
                    else
                    {
                        s2 = new SQLInsertOrUpdateStructure(_manager)
                        {
                            NewValues = new Dictionary<string, object>
                            {
                                { _manager.GetColumnName("usernames", "Steam64", out _), Steam64 },
                                { _manager.GetColumnName("usernames", "PlayerName", out _), player.PlayerName },
                                { _manager.GetColumnName("usernames", "CharacterName", out _), player.CharacterName },
                                { _manager.GetColumnName("usernames", "NickName", out _), player.NickName }
                            },
                            VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                        {
                            { _manager.GetColumnName("usernames", "PlayerName", out _), EUpdateOperation.SETFROMVALUES },
                            { _manager.GetColumnName("usernames", "CharacterName", out _), EUpdateOperation.SETFROMVALUES },
                            { _manager.GetColumnName("usernames", "NickName", out string tablename), EUpdateOperation.SETFROMVALUES },
                        },
                            tableName = tablename,
                            UpdateValuesIfValid = null
                        };
                    }
                    InsertOrUpdateAsync(s2, AsyncDatabaseCallbacks.DisposeAsyncResult);
                }
                else
                {
                    _manager.LogError("Couldn't save username to MySql Database. Cast error.\n\"" + vagueResponse.Command + "\"");
                    return;
                }
            }));
        }
        internal void GetOfficerPoints(ulong Steam64, byte Team, D_Uint32BalanceReceived callback)
        {
            SelectData(StructS64TeamCompare(_manager, "levels", "OfficerPoints", Steam64, Team, typeof(uint)), (ar) =>
            {
                IMySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if (vagueResponse is SelectResponse response)
                {
                    if (response != null && response.ExecutionStatus == EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(response.GetColumn<uint>(_manager.GetColumnName("levels", "OfficerPoints", out _)).GetValue(0), true);
                    }
                    else
                    {
                        callback.Invoke(0, false);
                    }
                }
                else
                {
                    _manager.LogError("Couldn't get Officer Points balance from MySql Database. Cast error.\n\"" + vagueResponse.Command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(0, false);
                    return;
                }
            });
        }
        internal void GetXP(ulong Steam64, byte Team, D_Uint32BalanceReceived callback)
        {
            SelectData(StructS64TeamCompare(_manager, "levels", "XP", Steam64, Team, typeof(uint)), (ar) =>
            {
                _manager.Log("Received");
                IMySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if (vagueResponse is SelectResponse response)
                {
                    if (response != null && response.ExecutionStatus == EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(response.GetColumn<uint>(_manager.GetColumnName("levels", "XP", out _)).GetValue(0), true);
                    }
                    else
                    {
                        callback.Invoke(0, false);
                    }
                }
                else
                {
                    _manager.LogError("Couldn't get XP balance from MySql Database. Cast error.\n\"" + vagueResponse.Command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(0, false);
                }
            });
        }
        internal void SubtractOfficerPoints(ulong Steam64, byte Team, int amount = 1, D_Difference onFailureToClamp = null, bool clampOnSubtract = false)
        {
            GetOfficerPoints(Steam64, Team, (balance, success) =>
            {
                if (success && balance >= amount)
                {
                    InsertOrUpdateAsync(
                        StructSubtractFromUintS64AndTeam(_manager, "levels", Steam64, Team, "OfficerPoints", amount, new Dictionary<string, object>
                        { { _manager.GetColumnName("levels", "XP", out _), 0 } }),
                        AsyncDatabaseCallbacks.DisposeAsyncResult);
                }
                else if (onFailureToClamp != null)
                {
                    if (clampOnSubtract)
                    {
                        InsertOrUpdateAsync(
                           StructSetUintS64AndTeam(_manager, "levels", Steam64, Team, "OfficerPoints", 0, new Dictionary<string, object>
                           { { _manager.GetColumnName("levels", "XP", out _), 0 } }),
                           AsyncDatabaseCallbacks.DisposeAsyncResult);
                    }
                    else
                        onFailureToClamp.Invoke(amount - balance);
                }
            });
        }
        internal void SubtractXP(ulong Steam64, byte Team, int amount = 1, D_Difference onFailureToClamp = null, bool clampOnSubtract = false)
        {
            GetXP(Steam64, Team, (balance, success) =>
            {
                if (success && balance >= amount)
                {
                    InsertOrUpdateAsync(
                       StructSubtractFromUintS64AndTeam(_manager, "levels", Steam64, Team, "XP", amount, new Dictionary<string, object>
                       { { _manager.GetColumnName("levels", "OfficerPoints", out _), 0 } }),
                       AsyncDatabaseCallbacks.DisposeAsyncResult);
                }
                else if (onFailureToClamp != null)
                {
                    if (clampOnSubtract)
                    {
                        InsertOrUpdateAsync(
                           StructSetUintS64AndTeam(_manager, "levels", Steam64, Team, "XP", 0, new Dictionary<string, object>
                           { { _manager.GetColumnName("levels", "OfficerPoints", out _), 0 } }),
                           AsyncDatabaseCallbacks.DisposeAsyncResult);
                    }
                    else
                        onFailureToClamp.Invoke(amount - balance);
                }
            });
        }
        internal void AddOfficerPoints(ulong Steam64, byte Team, int amount = 1)
        {
            InsertOrUpdateAsync(
                StructAddToUintS64AndTeam(_manager, "levels", Steam64, Team, "OfficerPoints", amount, new Dictionary<string, object>
                { { _manager.GetColumnName("levels", "XP", out _), 0 } }),
                AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddXP(ulong Steam64, byte Team, int amount = 1)
        {
            InsertOrUpdateAsync(
                StructAddToUintS64AndTeam(_manager, "levels", Steam64, Team, "XP", amount, new Dictionary<string, object>
                { { _manager.GetColumnName("levels", "OfficerPoints", out _), 0 } }),
                AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddKill(ulong Steam64, byte Team, int amount = 1)
        {
            InsertOrUpdateAsync(
                StructAddToUintS64AndTeam(_manager, "playerstats", Steam64, Team, "Kills", amount, new Dictionary<string, object>
                { { _manager.GetColumnName("playerstats", "Deaths", out _), 0 }, { _manager.GetColumnName("playerstats", "Teamkills", out _), 0 } }),
                AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddDeath(ulong Steam64, byte Team, int amount = 1)
        {
            InsertOrUpdateAsync(
                StructAddToUintS64AndTeam(_manager, "playerstats", Steam64, Team, "Deaths", amount, new Dictionary<string, object>
                { { _manager.GetColumnName("playerstats", "Kills", out _), 0 }, { _manager.GetColumnName("playerstats", "Teamkills", out _), 0 } }),
                AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddTeamkill(ulong Steam64, byte Team, int amount = 1)
        {
            InsertOrUpdateAsync(
                StructAddToUintS64AndTeam(_manager, "playerstats", Steam64, Team, "Teamkills", amount, new Dictionary<string, object>
                { { _manager.GetColumnName("playerstats", "Kills", out _), 0 }, { _manager.GetColumnName("playerstats", "Deaths", out _), 0 } }),
                AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        /// <summary>Compares a column called "Steam64" and "Team" with equals to a ulong id and byte team, limit 1.</summary>
        public static SQLSelectCallStructure StructS64TeamCompare(Database manager, string table, string column, ulong Steam64, byte Team, Type expectedReturn) =>
            new SQLSelectCallStructure(manager)
            {
                selectAll = false,
                Columns = new Dictionary<string, Type>
                    {
                        { manager.GetColumnName(table, column, out string tablename), expectedReturn }
                    },
                tableName = tablename,
                comparisons = new EComparisonType[] { EComparisonType.EQUALS, EComparisonType.EQUALS },
                conditions = new object[] { Steam64, Team },
                ConditionVariables = new string[] { manager.GetColumnName(table, "Steam64", out _), manager.GetColumnName(table, "Team", out _) },
                limit = 1
            };
        /// <summary>Selects usernames with "PlayerName", "CharacterName", and "NickName", comparing only Steam64.</summary>
        public static SQLSelectCallStructure StructGetUsername(Database manager, ulong Steam64, string table = "usernames") =>
            new SQLSelectCallStructure(manager)
            {
                selectAll = false,
                Columns = new Dictionary<string, Type>
                {
                    { manager.GetColumnName(table, "PlayerName", out _), typeof(string) },
                    { manager.GetColumnName(table, "CharacterName", out _), typeof(string) },
                    { manager.GetColumnName(table, "NickName", out string tablename), typeof(string) },
                },
                tableName = tablename,
                comparisons = new EComparisonType[] { EComparisonType.EQUALS },
                conditions = new object[] { Steam64 },
                ConditionVariables = new string[] { manager.GetColumnName(table, "Steam64", out _) },
                limit = 1
            };
        public static SQLInsertOrUpdateStructure StructAddToUintS64AndTeam(Database manager, string table, ulong Steam64, ulong Team,
            string variable, int amount, Dictionary<string, object> otherdefaults) =>
            new SQLInsertOrUpdateStructure(manager)
            {
                NewValues = (Dictionary<string, object>)new Dictionary<string, object>
                {
                    { manager.GetColumnName(table, "Steam64", out _), Steam64 },
                    { manager.GetColumnName(table, "Team", out _), Team },
                    { manager.GetColumnName(table, variable, out string tablename), amount }
                }.Union(otherdefaults),
                tableName = tablename,
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                    {
                        { manager.GetColumnName(table, variable, out _), EUpdateOperation.ADD }
                    },
                UpdateValuesIfValid = new List<object> { amount }
            };
        public static SQLInsertOrUpdateStructure StructSubtractFromUintS64AndTeam(Database manager, string table, ulong Steam64, ulong Team,
            string variable, int amount, Dictionary<string, object> otherdefaults) => 
            new SQLInsertOrUpdateStructure(manager)
            {
                NewValues = (Dictionary<string, object>)new Dictionary<string, object>
                    {
                        { manager.GetColumnName(table, "Steam64", out _), Steam64 },
                        { manager.GetColumnName(table, "Team", out _), Team },
                        { manager.GetColumnName(table, variable, out string tablename), amount }
                    }.Union(otherdefaults),
                tableName = tablename,
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                    {
                        { manager.GetColumnName(table, variable, out _), EUpdateOperation.SUBTRACT }
                    },
                UpdateValuesIfValid = new List<object> { amount }
            };
        public static SQLInsertOrUpdateStructure StructSetUintS64AndTeam(Database manager, string table, ulong Steam64, ulong Team,
            string variable, int amount, Dictionary<string, object> otherdefaults) => 
            new SQLInsertOrUpdateStructure(manager)
            {
                NewValues = (Dictionary<string, object>)new Dictionary<string, object>
                    {
                        { manager.GetColumnName(table, "Steam64", out _), Steam64 },
                        { manager.GetColumnName(table, "Team", out _), Team },
                        { manager.GetColumnName(table, variable, out string tablename), amount }
                    }.Union(otherdefaults),
                tableName = tablename,
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                    {
                        { manager.GetColumnName(table, variable, out _), EUpdateOperation.SET }
                    },
                UpdateValuesIfValid = new List<object> { amount }
            };
    }
}
