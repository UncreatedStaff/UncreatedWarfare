using MySql.Data.MySqlClient;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare;
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Data = Uncreated.Warfare.Data;

namespace Uncreated.SQL
{
    public class AsyncDatabase : IDisposable
    {
        public MySqlConnection SQL { get; protected set; }
        private DbCaller _dbCaller;
        /// <summary>
        /// Does not open the connection, you must run <see cref="OpenAsync">OpenAsync</see> or <see cref="OpenSync">OpenSync</see>
        /// </summary>
        public AsyncDatabase()
        {
            _dbCaller = new DbCaller(this);
            SQL = new MySqlConnection(UCWarfare.I.SQL.ConnectionString);
        }
        /// <summary>
        /// Opens the connection. Not thread-blocking.
        /// </summary>
        /// <param name="method">Method to invoke asynchronously. <para>Signature: <see cref="DbCaller.D_DatabaseDelegate">D_DatabaseDelegate(AsyncDatabase)</see></para></param>
        /// <param name="callback">Method to run after the connection is opened. <para>Signature: <see cref="AsyncCallback">AsyncCallback(IAsyncResult)</see></para></param>
        private IAsyncResult InvokeWithDatabase(DbCaller.D_DatabaseDelegate method, AsyncCallback callback = null)
        {
            DbCaller.D_DatabaseDelegate caller = new DbCaller.D_DatabaseDelegate(method);
            return caller.BeginInvoke(callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
        }
        /// <summary>
        /// Closes the connection and disposes of it. Not thread-blocking.
        /// </summary>
        /// <param name="callback">Method to run after the connection is closed and disposed of. <para>Signature: <see cref="AsyncCallback">AsyncCallback(IAsyncResult)</see></para></param>
        public IAsyncResult DisposeAsync(AsyncCallback callback = null) => InvokeWithDatabase(_dbCaller.DisposeOf, callback);
        /// <summary>
        /// Opens the connection. Not thread-blocking.
        /// </summary>
        /// <param name="callback">Method to run after the connection is opened. <para>Signature: <see cref="AsyncCallback">AsyncCallback(IAsyncResult)</see></para></param>
        public IAsyncResult OpenAsync(AsyncCallback callback = null)
        {
            DbCaller.D_DatabaseDelegateWithBool caller = new DbCaller.D_DatabaseDelegateWithBool(_dbCaller.Open);
            return caller.BeginInvoke(out _, callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
        }
        /// <summary>
        /// Opens the connection and waits for a response.
        /// </summary>
        /// <returns></returns>
        public bool OpenSync() 
        {
            _dbCaller.Open(out bool success);
            return success;
        }
        /// <summary>
        /// Closes the connection. Not thread-blocking.
        /// </summary>
        /// <param name="callback">Method to run after the connection is closed. <para>Signature: <see cref="AsyncCallback">AsyncCallback(IAsyncResult)</see></para></param>
        /// <returns></returns>
        public IAsyncResult CloseAsync(AsyncCallback callback = null) => InvokeWithDatabase(_dbCaller.Close, callback);
        /// <summary>
        /// Closes the connection and waits for a response.
        /// </summary>
        public void CloseSync() => _dbCaller.Close();
        /// <summary>
        /// Synchronous. Closes the connection and waits for a response.
        /// </summary>
        public void Dispose()
        {
            IAsyncResult ar = CloseAsync(null);
            try
            {
                ar.AsyncWaitHandle.WaitOne();
                ar.AsyncWaitHandle.Dispose();
            }
            catch (ObjectDisposedException) { }
            SQL.Dispose();
        }
        /// <summary>
        /// <para>Asynchronous operation to update a player's saved username in usernames table.</para>
        /// <para>Sends a UsernameChanged event to node server as well if the username is different from that which is in the SQL database.</para>
        /// </summary>
        /// <param name="Steam64">Player to update's Steam64 ID</param>
        /// <param name="player">Current Usernames</param>
        public void UpdateUsernameAsync(ulong Steam64, FPlayerName player)
        {
            DbCaller.D_UpdateUsernameAsync caller = _dbCaller.UpdateUsername;
            caller.BeginInvoke(Steam64, player, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>
        /// Add a kill (default 1) to the "playerstats" database.
        /// </summary>
        /// <param name="Steam64">Player's Steam64 ID to add a kill to.</param>
        /// <param name="Team">Team to add the kill to.</param>
        /// <param name="amount">Amount of kills to add, default 1.</param>
        public void AddKill(ulong Steam64, byte Team, int amount = 1)
        {
            DbCaller.D_ModifyPlayerStat caller = _dbCaller.AddKill;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>
        /// Add a death (default 1) to the "playerstats" database.
        /// </summary>
        /// <param name="Steam64">Player's Steam64 ID to add a death to.</param>
        /// <param name="Team">Team to add the death to.</param>
        /// <param name="amount">Amount of deaths to add, default 1.</param>
        public void AddDeath(ulong Steam64, byte Team, int amount = 1)
        {
            DbCaller.D_ModifyPlayerStat caller = _dbCaller.AddDeath;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>
        /// Add a teamkill (default 1) to the "playerstats" database.
        /// </summary>
        /// <param name="Steam64">Player's Steam64 ID to add a teamkill to.</param>
        /// <param name="Team">Team to add the teamkill to.</param>
        /// <param name="amount">Amount of teamkills to add, default 1.</param>
        public void AddTeamkill(ulong Steam64, byte Team, int amount = 1)
        {
            DbCaller.D_ModifyPlayerStat caller = _dbCaller.AddTeamkill;
            caller.BeginInvoke(Steam64, Team, amount, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public void GetUsernameAsync(ulong ID, DbCaller.D_UsernameReceived callback)
        {
            DbCaller.D_GetUsername caller = _dbCaller.GetUsername;
            caller.BeginInvoke(ID, callback, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
    }
    public enum EComparisonType : byte
    {
        NOCOMPARISON,
        EQUALS,
        NOTEQUALS,
        LIKE,
        GREATERTHAN,
        LESSTHAN,
        GREATERTHANOREQUALTO,
        LESSTHANOREQUALTO,
        NULLSAFEEQUALS,
        ISNULL,
        ISNOTNULL,
        IS,
        ISNOT
    }
    public enum EUpdateOperation : byte
    {
        SET,
        SETFROMVALUES,
        SUBTRACT,
        ADD,
        MULTIPLY,
        DIVIDE
    }
    public enum ERequestType : byte
    {
        QUERY,
        NONQUERY
    }
    public class DbCaller
    {
        public DbCaller(AsyncDatabase manager) { this._manager = manager; }
        private readonly AsyncDatabase _manager;
        public static string GetTableName(string key) =>
            Data.TableData.ContainsKey(key) ? Data.TableData[key].TableName : key;
        public static string GetColumnName(string table, string key) =>
            Data.TableData.ContainsKey(table) && Data.TableData[table].Columns.ContainsKey(key) ? Data.TableData[table].Columns[key] : key;
        internal delegate void D_DatabaseDelegate();
        internal delegate void D_NoArgsInvokerDelegate();
        internal delegate void D_DatabaseDelegateWithBool(out bool bSuccess);
        internal delegate void D_SelectAsync(SQLSelectCallStructure Data, AsyncCallback callback);
        internal delegate void D_InsertOrUpdateAsync(SQLInsertOrUpdateStructure Data, AsyncCallback callback);
        internal delegate void D_UpdateUsernameAsync(ulong Steam64, FPlayerName player);
        internal delegate void D_DatabaseQuery<T>(T Data, out MySqlResponse Output) where T : SQLCallStructure;
        internal delegate void D_GetUsername(ulong Steam64, D_UsernameReceived callback);
        internal delegate void D_ModifyPlayerStat(ulong Steam64, byte Team, int amount);
        public delegate void D_UsernameReceived(FPlayerName usernames, bool success);

        private readonly Dictionary<EComparisonType, string> OperatorTranslations = new Dictionary<EComparisonType, string>
        {
            { EComparisonType.NOCOMPARISON, string.Empty },
            { EComparisonType.EQUALS, "=" },
            { EComparisonType.NOTEQUALS, "!=" },
            { EComparisonType.LIKE, "LIKE" },
            { EComparisonType.GREATERTHAN, ">" },
            { EComparisonType.LESSTHAN, "<" },
            { EComparisonType.GREATERTHANOREQUALTO, ">=" },
            { EComparisonType.LESSTHANOREQUALTO, "<=" },
            { EComparisonType.NULLSAFEEQUALS, "<=>" },
            { EComparisonType.ISNOT, "IS NULL" },
            { EComparisonType.ISNOTNULL, "IS NOT NULL" },
            { EComparisonType.IS, "IS" },
            { EComparisonType.ISNULL, "IS NULL" },
        };
        internal void GetUsername(ulong Steam64, D_UsernameReceived callback)
        {
            SQLSelectCallStructure s = new SQLSelectCallStructure(_manager)
            {
                tableName = GetTableName("usernames"),
                selectAll = false,
                Columns = new Dictionary<string, Type>
                {
                    { GetColumnName("usernames", "PlayerName"), typeof(string) },
                    { GetColumnName("usernames", "CharacterName"), typeof(string) },
                    { GetColumnName("usernames", "NickName"), typeof(string) },
                },
                comparisons = new EComparisonType[] { EComparisonType.EQUALS },
                conditions = new object[] { Steam64 },
                ConditionVariables = new string[] { GetColumnName("usernames", "Steam64") },
                limit = 1
            };
            SelectDataAsync(s, (ar) =>
            {
                MySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if(vagueResponse is SelectResponse response)
                {
                    if (response != null && response.executionstatus == MySqlResponse.EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(new FPlayerName()
                        {
                            Steam64 = Steam64,
                            CharacterName = response.GetColumn<string>(GetColumnName("usernames", "CharacterName")).GetValue(0),
                            PlayerName = response.GetColumn<string>(GetColumnName("usernames", "PlayerName")).GetValue(0),
                            NickName = response.GetColumn<string>(GetColumnName("usernames", "NickName")).GetValue(0)
                        }, true);
                    }
                    else
                    {
                        string id = Steam64.ToString();
                        callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    }
                } else
                {
                    F.LogError("Couldn't get username from MySql Database. Cast error.\n\"" + vagueResponse.command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    return;
                }
            });
        }
        internal void UpdateUsername(ulong Steam64, FPlayerName player)
        {
            SQLSelectCallStructure s = new SQLSelectCallStructure(_manager)
            {
                tableName = GetTableName("usernames"),
                selectAll = false,
                Columns = new Dictionary<string, Type>
                {
                    { GetColumnName("usernames", "PlayerName"), typeof(string) },
                    { GetColumnName("usernames", "CharacterName"), typeof(string) },
                    { GetColumnName("usernames", "NickName"), typeof(string) },
                },
                comparisons = new EComparisonType[] { EComparisonType.EQUALS },
                conditions = new object[] { Steam64 },
                ConditionVariables = new string[] { GetColumnName("usernames", "Steam64") },
                limit = 1
            };
            SelectDataAsync(s, new AsyncCallback((ar) =>
            {
                MySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if(vagueResponse is SelectResponse response)
                {
                    SQLInsertOrUpdateStructure s2;
                    if (response != null && response.executionstatus == MySqlResponse.EExecutionStatus.SUCCESS)
                    {
                        string oldPlayerName = response.GetColumn<string>(GetColumnName("usernames", "PlayerName")).GetValue(0);
                        string oldCharacterName = response.GetColumn<string>(GetColumnName("usernames", "CharacterName")).GetValue(0);
                        string oldNickName = response.GetColumn<string>(GetColumnName("usernames", "NickName")).GetValue(0);
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
                        Data.WebInterface?.SendUpdatedUsername(Steam64, player);
                        Dictionary<string, EUpdateOperation> varsToUpdate = new Dictionary<string, EUpdateOperation>();
                        if (updatePlayerName)
                            varsToUpdate.Add(GetColumnName("usernames", "PlayerName"), EUpdateOperation.SETFROMVALUES);
                        if (updateNickName)
                            varsToUpdate.Add(GetColumnName("usernames", "CharacterName"), EUpdateOperation.SETFROMVALUES);
                        if (updateNickName)
                            varsToUpdate.Add(GetColumnName("usernames", "NickName"), EUpdateOperation.SETFROMVALUES);
                        s2 = new SQLInsertOrUpdateStructure(_manager)
                        {
                            NewValues = new Dictionary<string, object>
                            {
                                { GetColumnName("usernames", "Steam64"), Steam64 },
                                { GetColumnName("usernames", "PlayerName"), player.PlayerName },
                                { GetColumnName("usernames", "CharacterName"), player.CharacterName },
                                { GetColumnName("usernames", "NickName"), player.NickName }
                            },
                            VariablesToUpdateIfDuplicate = varsToUpdate,
                            tableName = GetTableName("usernames"),
                            UpdateValuesIfValid = null
                        };
                    }
                    else
                    {
                        s2 = new SQLInsertOrUpdateStructure(_manager)
                        {
                            NewValues = new Dictionary<string, object>
                            {
                                { GetColumnName("usernames", "Steam64"), Steam64 },
                                { GetColumnName("usernames", "PlayerName"), player.PlayerName },
                                { GetColumnName("usernames", "CharacterName"), player.CharacterName },
                                { GetColumnName("usernames", "NickName"), player.NickName }
                            },
                            VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                        {
                            { GetColumnName("usernames", "PlayerName"), EUpdateOperation.SETFROMVALUES },
                            { GetColumnName("usernames", "CharacterName"), EUpdateOperation.SETFROMVALUES },
                            { GetColumnName("usernames", "NickName"), EUpdateOperation.SETFROMVALUES },
                        },
                            tableName = GetTableName("usernames"),
                            UpdateValuesIfValid = null
                        };
                    }
                    InsertOrUpdateAsync(s2, AsyncDatabaseCallbacks.DisposeAsyncResult);
                } else
                {
                    F.LogError("Couldn't save username to MySql Database. Cast error.\n\"" + vagueResponse.command + "\"");
                    return;
                }
            }));
        }
        internal void GetOfficerPoints(ulong Steam64, byte Team, D_UsernameReceived callback)
        {
            SQLSelectCallStructure s = new SQLSelectCallStructure(_manager)
            {
                tableName = GetTableName("levels"),
                selectAll = false,
                Columns = new Dictionary<string, Type>
                {
                    { GetColumnName("levels", "OfficerPoints"), typeof(uint) }
                },
                comparisons = new EComparisonType[] { EComparisonType.EQUALS, EComparisonType.EQUALS },
                conditions = new object[] { Steam64, Team },
                ConditionVariables = new string[] { GetColumnName("levels", "Steam64"), GetColumnName("levels", "Team") },
                limit = 1
            };
            SelectDataAsync(s, (ar) =>
            {
                MySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                if (vagueResponse is SelectResponse response)
                {
                    if (response != null && response.executionstatus == MySqlResponse.EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(new FPlayerName()
                        {
                            Steam64 = Steam64,
                            CharacterName = response.GetColumn<string>(GetColumnName("usernames", "CharacterName")).GetValue(0),
                            PlayerName = response.GetColumn<string>(GetColumnName("usernames", "PlayerName")).GetValue(0),
                            NickName = response.GetColumn<string>(GetColumnName("usernames", "NickName")).GetValue(0)
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
                    F.LogError("Couldn't get username from MySql Database. Cast error.\n\"" + vagueResponse.command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    return;
                }
            });
        }
        internal void AddOfficerPoints(ulong Steam64, byte Team, int amount = 1)
        {
            SQLInsertOrUpdateStructure s = new SQLInsertOrUpdateStructure(_manager)
            {
                tableName = GetTableName("levels"),
                NewValues = new Dictionary<string, object>
                {
                    { GetColumnName("levels", "Steam64"), Steam64 },
                    { GetColumnName("levels", "Team"), Team },
                    { GetColumnName("levels", "OfficerPoints"), amount },
                    { GetColumnName("levels", "XP"), 0 }
                },
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                {
                    { GetColumnName("levels", "OfficerPoints"), EUpdateOperation.ADD }
                },
                UpdateValuesIfValid = new List<object> { amount }
            };
            InsertOrUpdateAsync(s, AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddXP(ulong Steam64, byte Team, int amount = 1)
        {
            SQLInsertOrUpdateStructure s = new SQLInsertOrUpdateStructure(_manager)
            {
                tableName = GetTableName("levels"),
                NewValues = new Dictionary<string, object>
                {
                    { GetColumnName("levels", "Steam64"), Steam64 },
                    { GetColumnName("levels", "Team"), Team },
                    { GetColumnName("levels", "OfficerPoints"), 0 },
                    { GetColumnName("levels", "XP"), amount }
                },
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                {
                    { GetColumnName("levels", "OfficerPoints"), EUpdateOperation.ADD }
                },
                UpdateValuesIfValid = new List<object> { amount }
            };
            InsertOrUpdateAsync(s, AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddKill(ulong Steam64, byte Team, int amount = 1)
        {
            SQLInsertOrUpdateStructure s = new SQLInsertOrUpdateStructure(_manager)
            {
                tableName = GetTableName("playerstats"),
                NewValues = new Dictionary<string, object>
                {
                    { GetColumnName("playerstats", "Steam64"), Steam64 },
                    { GetColumnName("playerstats", "Team"), Team },
                    { GetColumnName("playerstats", "Kills"), amount },
                    { GetColumnName("playerstats", "Deaths"), 0 },
                    { GetColumnName("playerstats", "Teamkills"), 0 }
                },
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                {
                    { GetColumnName("playerstats", "Kills"), EUpdateOperation.ADD }
                },
                UpdateValuesIfValid = new List<object> { amount }
            };
            InsertOrUpdateAsync(s, AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddDeath(ulong Steam64, byte Team, int amount = 1)
        {
            SQLInsertOrUpdateStructure s = new SQLInsertOrUpdateStructure(_manager)
            {
                tableName = GetTableName("playerstats"),
                NewValues = new Dictionary<string, object>
                {
                    { GetColumnName("playerstats", "Steam64"), Steam64 },
                    { GetColumnName("playerstats", "Team"), Team },
                    { GetColumnName("playerstats", "Kills"), 0 },
                    { GetColumnName("playerstats", "Deaths"), amount },
                    { GetColumnName("playerstats", "Teamkills"), 0 }
                },
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                {
                    { GetColumnName("playerstats", "Deaths"), EUpdateOperation.ADD }
                },
                UpdateValuesIfValid = new List<object> { amount }
            };
            InsertOrUpdateAsync(s, AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        internal void AddTeamkill(ulong Steam64, byte Team, int amount = 1)
        {
            SQLInsertOrUpdateStructure s = new SQLInsertOrUpdateStructure(_manager)
            {
                tableName = GetTableName("playerstats"),
                NewValues = new Dictionary<string, object>
                {
                    { GetColumnName("playerstats", "Steam64"), Steam64 },
                    { GetColumnName("playerstats", "Team"), Team },
                    { GetColumnName("playerstats", "Kills"), 0 },
                    { GetColumnName("playerstats", "Deaths"), 0 },
                    { GetColumnName("playerstats", "Teamkills"), amount }
                },
                VariablesToUpdateIfDuplicate = new Dictionary<string, EUpdateOperation>
                {
                    { GetColumnName("playerstats", "Teamkills"), EUpdateOperation.ADD }
                },
                UpdateValuesIfValid = new List<object> { amount }
            };
            InsertOrUpdateAsync(s, AsyncDatabaseCallbacks.DisposeAsyncResult);
        }
        /// <summary>
        /// Gets a <see cref="MySqlResponse"/> from an IAsyncResult then disposes of it, calling WaitOne() if it is not already completed.
        /// </summary>
        /// <typeparam name="AsyncStateType"><see cref="SQLCallStructure"/> child type.</typeparam>
        /// <param name="ar"><see cref="IAsyncResult"/> of a async function using the <see cref="D_DatabaseQuery{T}"/> delegate.</param>
        /// <returns><see cref="MySqlResponse"/> from the provided SQL type.</returns>
        private MySqlResponse GetResponse<AsyncStateType>(IAsyncResult ar) where AsyncStateType : SQLCallStructure
        {
            if (ar.AsyncState is D_DatabaseQuery<AsyncStateType> rtn)
            {
                rtn.EndInvoke(out MySqlResponse response, ar);
                ar.AsyncWaitHandle.WaitOne();
                ar.AsyncWaitHandle.Dispose();
                return response;
            }
            else return default;
        }
        internal void DisposeOf()
        {
            IAsyncResult ar = _manager.CloseAsync(null);
            ar.AsyncWaitHandle.WaitOne();
            Warfare.Stats.WebCallbacks.Dispose(ar);
            _manager.SQL.Dispose();
        }
        internal void Close()
        {
            try
            {
                _manager.SQL.Close();
            }
            catch (MySqlException ex)
            {
                F.LogError("ERROR Closing Connection\n" + ex.Message);
                F.LogError("\nTrace\n" + ex.StackTrace);
            }
        }
        internal void Open(out bool bSuccess)
        {
            try
            {
                _manager.SQL.Open();
                bSuccess = true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        F.LogError("ERROR: Cannot connect to server. Server not found.");
                        break;
                    case 1045:
                        F.LogError("ERROR: SQL Invalid Login");
                        break;
                    case 1042:
                        F.LogError("ERROR: Unable to connect to any of the specified MySQL hosts.");
                        break;
                    default:
                        F.LogError($"Unknown MYSQL Error: {ex.Number}\n{ex.Message}");
                        break;
                }
                bSuccess = false;
            }
        }
        private const int TimeBetweenFinishedReadingCheck = 20;
        private delegate void WaitUntilFinishedReadingDelegate(SQLCallStructure Data, AsyncCallback Function, out SQLCallStructure DataReturn, out AsyncCallback FunctionReturn, out Type TypeReturn);
        private void FinishedReading(IAsyncResult ar)
        {
            if(ar.AsyncState is WaitUntilFinishedReadingDelegate dele)
            {
                dele.EndInvoke(out SQLCallStructure Data, out AsyncCallback Function, out Type type, ar); 
                try
                {
                    if (type == typeof(SQLSelectCallStructure))
                    {
                        D_DatabaseQuery<SQLSelectCallStructure> SelectCaller = new D_DatabaseQuery<SQLSelectCallStructure>(SelectDataAsyncCall);
                        SelectCaller.BeginInvoke((SQLSelectCallStructure)Data, out _, Function, SelectCaller);
                    }
                    else if (type == typeof(SQLInsertOrUpdateStructure))
                    {
                        D_DatabaseQuery<SQLInsertOrUpdateStructure> InsertOnDuplicateKeyUpdateCaller = new D_DatabaseQuery<SQLInsertOrUpdateStructure>(InsertIfDuplicateUpdateAsyncCall);
                        InsertOnDuplicateKeyUpdateCaller.BeginInvoke((SQLInsertOrUpdateStructure)Data, out _, Function, InsertOnDuplicateKeyUpdateCaller);
                    }
                    else
                    {
                        F.LogError("Type \"" + type.ToString() + "\" - Not a valid type.");
                    }
                }
                catch (InvalidCastException)
                {
                    F.LogError("Failed to cast \"" + type.ToString() + "\" to a valid SQL Container.");
                }
            } else
            {
                F.LogError("Failed to cast \"" + ar.AsyncState.GetType().ToString() + "\" to a valid delegate containing SQL information.");
            }
            Warfare.Stats.WebCallbacks.Dispose(ar);
        }
        private void WaitUntilFinishedReading(SQLCallStructure Data, AsyncCallback Function, out SQLCallStructure DataReturn, out AsyncCallback FunctionReturn, out Type TypeReturn)
        {
            DataReturn = Data;
            FunctionReturn = Function;
            TypeReturn = Data.GetType();
            while (!CheckIsFinishedReading(Data.DatabaseManager.SQL))
            {
                Task.Delay(TimeBetweenFinishedReadingCheck);
            }
        }
        private bool CheckIsFinishedReading(MySqlConnection SQL)
        {
            try
            {
                SQL.Ping();
                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.Message == MySql.Data.Resources.DataReaderOpen)
                {
                    return false;
                }
                else throw ex;
            }
        }
        public class SQLCallStructure
        {
            public AsyncDatabase DatabaseManager;
            public SQLCallStructure(AsyncDatabase DatabaseManager)
            {
                this.DatabaseManager = DatabaseManager;
            }
        }
        public class SQLSelectCallStructure : SQLCallStructure
        {
            public Dictionary<string, Type> Columns;
            public string tableName;
            public bool selectAll;
            public string[] ConditionVariables;
            public EComparisonType[] comparisons;
            public object[] conditions;
            public int limit;
            public SQLSelectCallStructure(AsyncDatabase DatabaseManager) : base(DatabaseManager) { }
            public SQLSelectCallStructure(
                AsyncDatabase DatabaseManager,
                Dictionary<string, Type> Columns,
                string tableName,
                bool selectAll = false,
                string[] ConditionVariables = null,
                EComparisonType[] comparisons = null,
                object[] conditions = null,
                int limit = -1
                ) : base(DatabaseManager)
            {
                this.Columns = Columns;
                this.tableName = tableName;
                this.selectAll = selectAll;
                this.ConditionVariables = ConditionVariables;
                this.comparisons = comparisons;
                this.conditions = conditions;
                this.limit = limit;
            }
        }
        public class SQLInsertOrUpdateStructure : SQLCallStructure
        {
            public string tableName;
            public Dictionary<string, object> NewValues;
            public Dictionary<string, EUpdateOperation> VariablesToUpdateIfDuplicate;
            public List<object> UpdateValuesIfValid;
            public SQLInsertOrUpdateStructure(AsyncDatabase DatabaseManager) : base(DatabaseManager) { }
        }
        private void SelectDataAsyncCall(SQLSelectCallStructure Data, out MySqlResponse Output)
        {
            StringBuilder query = new StringBuilder();
            query.Append("SELECT ");
            if (Data.selectAll) query.Append("*");
            else
            {
                for (int i = 0; i < Data.Columns.Count; i++)
                {
                    if (i != 0) query.Append(", ");
                    query.Append("`" + Data.Columns.ElementAt(i).Key + "`");
                }
            }
            query.Append(" FROM `").Append(Data.tableName).Append('`');
            if(Data.comparisons != null && Data.ConditionVariables != null && Data.conditions != null && 
                Data.comparisons.Length == Data.ConditionVariables.Length && Data.comparisons.Length == Data.conditions.Length)
            {
                for (int i = 0; i < Data.comparisons.Length; i++)
                {
                    if(Data.comparisons[i] != EComparisonType.NOCOMPARISON && Data.conditions[i] != null)
                    {
                        if (i == 0) query.Append(" WHERE ");
                        else query.Append(" AND ");
                        query.Append("`" + Data.ConditionVariables[i] + "` ");
                        query.Append(OperatorTranslations[Data.comparisons[i]]);
                        if (Data.comparisons[i] != EComparisonType.ISNULL && Data.comparisons[i] != EComparisonType.ISNOTNULL)
                            query.Append(" @" + i.ToString());
                    }
                }
            }
            if (Data.limit != -1)
                query.Append(" LIMIT " + Data.limit.ToString());
            query.Append(';');
            string CommandText = query.ToString();
            SelectResponse rtn = new SelectResponse(CommandText);
            using (MySqlCommand Q = new MySqlCommand(CommandText, Data.DatabaseManager.SQL))
            {
                for(int i = 0; i < Data.comparisons.Length; i++)
                {
                    Q.Parameters.AddWithValue("@" + i.ToString(), Data.conditions[i]);
                }
                using (MySqlDataReader R = Q.ExecuteReader())
                {
                    while (R.Read())
                    {
                        foreach (KeyValuePair<string, Type> column in Data.Columns)
                        {
                            try
                            {
                                int ordinal = R.GetOrdinal(column.Key);
                                if (column.Value == typeof(ulong))
                                    rtn.AddValueToColumn(R.GetUInt64(ordinal), column.Key);
                                else if (column.Value == typeof(string))
                                    rtn.AddValueToColumn(R.GetString(ordinal), column.Key);
                                else if (column.Value == typeof(int))
                                    rtn.AddValueToColumn(R.GetInt32(ordinal), column.Key);
                                else if (column.Value == typeof(ushort))
                                    rtn.AddValueToColumn(R.GetUInt16(ordinal), column.Key);
                                else if (column.Value == typeof(uint))
                                    rtn.AddValueToColumn(R.GetUInt32(ordinal), column.Key);
                                else if (column.Value == typeof(DateTime))
                                    rtn.AddValueToColumn(R.GetDateTime(ordinal), column.Key);
                                else if (column.Value == typeof(decimal))
                                    rtn.AddValueToColumn(R.GetDecimal(ordinal), column.Key);
                                else if (column.Value == typeof(float))
                                    rtn.AddValueToColumn(R.GetFloat(ordinal), column.Key);
                                else if (column.Value == typeof(byte))
                                    rtn.AddValueToColumn(R.GetByte(ordinal), column.Key);
                                else if (column.Value == typeof(sbyte))
                                    rtn.AddValueToColumn(R.GetSByte(ordinal), column.Key);
                                else if (column.Value == typeof(long))
                                    rtn.AddValueToColumn(R.GetInt64(ordinal), column.Key);
                                else if (column.Value == typeof(short))
                                    rtn.AddValueToColumn(R.GetInt16(ordinal), column.Key);
                                else if (column.Value == typeof(sbyte))
                                    rtn.AddValueToColumn(R.GetSByte(ordinal), column.Key);
                                else if (column.Value == typeof(char))
                                    rtn.AddValueToColumn(R.GetChar(ordinal), column.Key);
                                else if (column.Value == typeof(Guid))
                                    rtn.AddValueToColumn(R.GetGuid(ordinal), column.Key);
                                else if (column.Value == typeof(TimeSpan))
                                    rtn.AddValueToColumn(R.GetTimeSpan(ordinal), column.Key);
                                else
                                    rtn.AddValueToColumn(R.GetValue(ordinal), column.Key);
                            }
                            catch (Exception ex)
                            {
                                F.LogError("Exception in MySql SELECT statement:\n\"" + rtn.command + "\"\nError:\n" + ex.ToString());
                                rtn.executionstatus = MySqlResponse.EExecutionStatus.FAILURE;
                            }
                        }
                    }
                }
            }
            if (rtn.Columns.Count > 0) rtn.executionstatus = MySqlResponse.EExecutionStatus.SUCCESS;
            else rtn.executionstatus = MySqlResponse.EExecutionStatus.NORESULTS;
            Output = rtn;
        }
        private void InsertIfDuplicateUpdateAsyncCall(SQLInsertOrUpdateStructure Data, out MySqlResponse Output)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<int, object> Parameters = new Dictionary<int, object>();
            sb.Append("INSERT INTO `").Append(Data.tableName).Append("` (");
            for (int i = 0; i < Data.NewValues.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append("`" + Data.NewValues.ElementAt(i).Key + "`");
            }
            sb.Append(") VALUES(");
            for (int i = 0; i < Data.NewValues.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append("@" + i.ToString());
                Parameters.Add(i, Data.NewValues.ElementAt(i).Value);
            }
            sb.Append(") ON DUPLICATE KEY UPDATE ");
            for (int i = 0; i < Data.VariablesToUpdateIfDuplicate.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append("`" + Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + "` = ");
                switch (Data.VariablesToUpdateIfDuplicate.ElementAt(i).Value)
                {
                    case EUpdateOperation.SETFROMVALUES:
                        sb.Append("VALUES(" + Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + ")");
                        break;
                    case EUpdateOperation.SET:
                        int setp = Parameters.ElementAt(Parameters.Count - 1).Key + 1;
                        sb.Append("@" + setp.ToString());
                        Parameters.Add(setp, Data.UpdateValuesIfValid[i]);
                        break;
                    case EUpdateOperation.ADD:
                        int addp = Parameters.ElementAt(Parameters.Count - 1).Key + 1;
                        sb.Append(Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + " + @" + addp.ToString());
                        Parameters.Add(addp, Data.UpdateValuesIfValid[i]);
                        break;
                    case EUpdateOperation.SUBTRACT:
                        int subp = Parameters.ElementAt(Parameters.Count - 1).Key + 1;
                        sb.Append(Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + " - @" + subp.ToString());
                        Parameters.Add(subp, Data.UpdateValuesIfValid[i]);
                        break;
                    case EUpdateOperation.DIVIDE:
                        int divp = Parameters.ElementAt(Parameters.Count - 1).Key + 1;
                        sb.Append(Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + " / @" + divp.ToString());
                        Parameters.Add(divp, Data.UpdateValuesIfValid[i]);
                        break;
                    case EUpdateOperation.MULTIPLY:
                        int mulp = Parameters.ElementAt(Parameters.Count - 1).Key + 1;
                        sb.Append(Data.VariablesToUpdateIfDuplicate.ElementAt(i).Key + " / @" + mulp.ToString());
                        Parameters.Add(mulp, Data.UpdateValuesIfValid[i]);
                        break;
                }
            }
            sb.Append(";");
            string query = sb.ToString();
            NonQueryResponse rtn = new NonQueryResponse(query, 0);
            using (MySqlCommand Q = new MySqlCommand(query, Data.DatabaseManager.SQL))
            {
                foreach (KeyValuePair<int, object> Parameter in Parameters)
                    Q.Parameters.AddWithValue('@' + Parameter.Key.ToString(), Parameter.Value);
                try
                {
                    rtn.RowsAffected = Q.ExecuteNonQuery();
                    if (rtn.RowsAffected == 0) rtn.executionstatus = MySqlResponse.EExecutionStatus.NORESULTS;
                    else rtn.executionstatus = MySqlResponse.EExecutionStatus.SUCCESS;
                }
                catch (Exception ex)
                {
                    F.LogError("Exception in MySql INSERT ON DUPLICATE UPDATE statement:\n\"" + rtn.command + "\"\nError:\n" + ex.ToString());
                    rtn.executionstatus = MySqlResponse.EExecutionStatus.FAILURE;
                }
            }
            Output = rtn;
        }
        /// <param name="callback">Cast AsyncState to <see cref="D_DatabaseQuery{SQLSelectCallStructure}">DatabaseQuery&lt;SQLInsertOrUpdateStructure&gt;</see></param>
        internal void InsertOrUpdateAsync(SQLInsertOrUpdateStructure Data, AsyncCallback callback)
        {
            WaitUntilFinishedReadingDelegate caller = new WaitUntilFinishedReadingDelegate(WaitUntilFinishedReading);
            caller.BeginInvoke(Data, callback, out _, out _, out _, FinishedReading, caller);
        }
        /// <param name="callback">Cast AsyncState to <see cref="D_DatabaseQuery{SQLSelectCallStructure}">DatabaseQuery&lt;SQLSelectCallStructure&gt;</see></param>
        internal void SelectDataAsync(SQLSelectCallStructure Data, AsyncCallback callback)
        {
            WaitUntilFinishedReadingDelegate caller = new WaitUntilFinishedReadingDelegate(WaitUntilFinishedReading);
            caller.BeginInvoke(Data, callback, out _, out _, out _, FinishedReading, caller);
        }
    }
    public abstract class MySqlResponse
    {
        public enum EExecutionStatus : byte
        {
            UNSET,
            SUCCESS,
            FAILURE,
            NORESULTS
        }
        public string command;
        public EExecutionStatus executionstatus = EExecutionStatus.UNSET;
        public MySqlResponse(string command)
        {
            if (UCWarfare.Config.Debug)
                F.Log(command, ConsoleColor.Green);
            this.command = command;
        }
    }
    public class NonQueryResponse : MySqlResponse
    {
        public int RowsAffected;
        public override string ToString()
        {
            return command + "\n" + RowsAffected.ToString() + " rows affected";
        }
        public NonQueryResponse(string command, int rowsChanged) : base(command)
        {
            this.RowsAffected = rowsChanged;
        }
    }
    public class SelectResponse : MySqlResponse
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(command + '\n');
            sb.Append("Columns: ");
            for(int i = 0; i < Columns.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(Columns[i].ColumnName).Append(": ");
                sb.Append(Columns[i].GetType().Name);
            }
            return sb.ToString();
        }
        public abstract class SqlColumn
        {
            public string ColumnName;
            public SqlColumn(string name)
            {
                this.ColumnName = name;
            }
        }
        public class SqlColumn<T> : SqlColumn
        {
            List<T> values;
            public SqlColumn(string name) : base(name)
            {
                this.values = new List<T>();
            }
            public void AddValue(T value) => values.Add(value);
            public T GetValue(int i = 0)
            {
                if (i < values.Count)
                {
                    return values[i];
                } else return default;
            }
        }
        public List<SqlColumn> Columns;
        public SelectResponse(string command) : base(command)
        {
            Columns = new List<SqlColumn>();
        }
        public void AddValueToColumn<T>(T value, string columnName)
        {
            SqlColumn column = Columns.FirstOrDefault(x => x.ColumnName == columnName);
            if (column == null)
            {
                column = new SqlColumn<T>(columnName);
                Columns.Add(column);
            }
            try
            {
                SqlColumn<T> NewColumn = (SqlColumn<T>)column;
                NewColumn.AddValue(value);
            } catch (InvalidCastException)
            {
                return;
            }
        }
        public SqlColumn<T> GetColumn<T>(string name)
        {
            SqlColumn column = Columns.FirstOrDefault(x => x.ColumnName == name);
            if (column == default(SqlColumn)) return null;
            try
            {
                return (SqlColumn<T>)column;
            } catch (InvalidCastException)
            {
                return null;
            }
        } 
    }
}
