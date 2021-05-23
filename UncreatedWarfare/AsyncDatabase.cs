﻿using MySql.Data.MySqlClient;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UnityEngine;

namespace UncreatedWarfare
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
            _dbCaller = new DbCaller();
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
            return caller.BeginInvoke(this, callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
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
            return caller.BeginInvoke(this, out _, callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
        }
        /// <summary>
        /// Opens the connection and waits for a response.
        /// </summary>
        /// <returns></returns>
        public bool OpenSync() 
        {
            _dbCaller.Open(this, out bool success);
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
        public void CloseSync() => _dbCaller.Close(this);
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
            caller.BeginInvoke(this, Steam64, player, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>
        /// Add a kill (default 1) to the "playerstats" database.
        /// </summary>
        /// <param name="Steam64">Player's Steam64 ID to add a kill to.</param>
        /// <param name="amount">Amount of kills to add, default 1.</param>
        public void AddKill(ulong Steam64, int amount = 1)
        {

        }
        private int overlayStep = 0;
        public void CreateFlagTestAreaOverlay(Player player, List<Zone> zones)
        {
            if(overlayStep == 0)
            {
                List<Zone> newZones = zones;
                newZones.Sort(delegate (Zone a, Zone b)
                {
                    return b.BoundsArea.CompareTo(a.BoundsArea);
                });
                Texture2D img = new Texture2D(Level.size, Level.size);
                List<Vector2> PointsToTest = new List<Vector2>();
                for (int i = -1 * img.width / 2; i < img.width / 2; i += 1)
                {
                    for (int j = -1 * img.height / 2; j < img.height / 2; j += 1)
                    {
                        PointsToTest.Add(new Vector2(i, j));
                    }
                }
                UCWarfare.I.StartCoroutine(enumerator());
                IEnumerator<WaitForSeconds> enumerator()
                {
                    _dbCaller.SendPlayerZoneOverlay(img, player, newZones, PointsToTest, overlayStep, out bool done);
                    overlayStep++;
                    yield return new WaitForSeconds(0.5f);
                    if (!done)
                        UCWarfare.I.StartCoroutine(enumerator());
                    else
                    {
                        _dbCaller.SendPlayerZoneOverlay(img, player, newZones, PointsToTest, -1, out _);
                        AsyncDatabaseCallbacks.PlayerReceivedZonesCallback(player);
                        overlayStep = 0;
                    }
                }
            } else
            {
                player.SendChat("A player is already running this procedure, try again in a few minutes.", UCWarfare.GetColor("default"));
            }
        }
        public void GetUsernameAsync(ulong ID, DbCaller.D_UsernameReceived callback)
        {
            DbCaller.D_GetUsername caller = _dbCaller.GetUsername;
            caller.BeginInvoke(this, ID, callback, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
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
    public struct FPlayerName
    {
        public ulong Steam64;
        public string PlayerName;
        public string CharacterName;
        public string NickName;
        public FPlayerName(SteamPlayerID player)
        {
            this.PlayerName = player.playerName;
            this.CharacterName = player.characterName;
            this.NickName = player.nickName;
            this.Steam64 = player.steamID.m_SteamID;
        }
        public FPlayerName(SteamPlayer player)
        {
            this.PlayerName = player.playerID.playerName;
            this.CharacterName = player.playerID.characterName;
            this.NickName = player.playerID.nickName;
            this.Steam64 = player.playerID.steamID.m_SteamID;
        }
        public FPlayerName(UnturnedPlayer player)
        {
            this.PlayerName = player.Player.channel.owner.playerID.playerName;
            this.CharacterName = player.Player.channel.owner.playerID.characterName;
            this.NickName = player.Player.channel.owner.playerID.nickName;
            this.Steam64 = player.Player.channel.owner.playerID.steamID.m_SteamID;
        }
        public FPlayerName(Player player)
        {
            this.PlayerName = player.channel.owner.playerID.playerName;
            this.CharacterName = player.channel.owner.playerID.characterName;
            this.NickName = player.channel.owner.playerID.nickName;
            this.Steam64 = player.channel.owner.playerID.steamID.m_SteamID;
        }
    }
    public class DbCaller
    {
        public static string GetTableName(string key) => 
            Data.TableData.ContainsKey(key) ? Data.TableData[key].TableName : key;
        public static string GetColumnName(string table, string key) => 
            Data.TableData.ContainsKey(table) && Data.TableData[table].Columns.ContainsKey(key) ? Data.TableData[table].Columns[key] : key;
        internal delegate void D_DatabaseDelegate(AsyncDatabase DatabaseManager);
        internal delegate void D_NoArgsInvokerDelegate();
        internal delegate void D_DatabaseDelegateWithBool(AsyncDatabase DatabaseManager, out bool bSuccess);
        internal delegate void D_SelectAsync(SQLSelectCallStructure Data, AsyncCallback callback);
        internal delegate void D_InsertOrUpdateAsync(SQLInsertOrUpdateStructure Data, AsyncCallback callback);
        internal delegate void D_UpdateUsernameAsync(AsyncDatabase DatabaseManager, ulong Steam64, FPlayerName player);
        internal delegate void D_DatabaseQuery<T>(T Data, out MySqlResponse Output) where T : SQLCallStructure;
        internal delegate void D_GetUsername(AsyncDatabase DatabaseManager, ulong Steam64, D_UsernameReceived callback);
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
        internal void GetUsername(AsyncDatabase DatabaseManager, ulong Steam64, D_UsernameReceived callback)
        {
            SQLSelectCallStructure s = new SQLSelectCallStructure(DatabaseManager)
            {
                tableName = GetTableName("usernames"),
                selectAll = false,
                Columns = new Dictionary<string, Type>
                {
                    { GetColumnName("usernames", "PlayerName"), typeof(string) },
                    { GetColumnName("usernames", "CharacterName"), typeof(string) },
                    { GetColumnName("usernames", "NickName"), typeof(string) },
                },
                comparison = EComparisonType.EQUALS,
                condition = Steam64,
                ConditionVariable = GetColumnName("usernames", "Steam64"),
                limit = 1
            };
            SelectDataAsync(s, (ar) =>
            {
                MySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                try
                {
                    SelectResponse response = (SelectResponse)vagueResponse;
                    if (response != null && response.executionstatus == MySqlResponse.EExecutionStatus.SUCCESS)
                    {
                        callback.Invoke(new FPlayerName()
                        {
                            Steam64 = Steam64,
                            CharacterName = response.GetColumn<string>(GetColumnName("usernames", "CharacterName")).GetValue(0),
                            PlayerName = response.GetColumn<string>(GetColumnName("usernames", "PlayerName")).GetValue(0),
                            NickName = response.GetColumn<string>(GetColumnName("usernames", "NickName")).GetValue(0)
                        }, true);
                    } else
                    {
                        string id = Steam64.ToString();
                        callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    }
                }
                catch (InvalidCastException)
                {
                    F.LogError("Couldn't get username from MySql Database. Cast error.\n\"" + vagueResponse.command + "\"");
                    string id = Steam64.ToString();
                    callback.Invoke(new FPlayerName() { Steam64 = Steam64, CharacterName = id, NickName = id, PlayerName = id }, false);
                    return;
                }
            });
        }
        internal void UpdateUsername(AsyncDatabase DatabaseManager, ulong Steam64, FPlayerName player)
        {
            SQLSelectCallStructure s = new SQLSelectCallStructure(DatabaseManager)
            {
                tableName = GetTableName("usernames"),
                selectAll = false,
                Columns = new Dictionary<string, Type> 
                { 
                    { GetColumnName("usernames", "PlayerName"), typeof(string) }, 
                    { GetColumnName("usernames", "CharacterName"), typeof(string) }, 
                    { GetColumnName("usernames", "NickName"), typeof(string) }, 
                },
                comparison = EComparisonType.EQUALS,
                condition = Steam64,
                ConditionVariable = GetColumnName("usernames", "Steam64"),
                limit = 1
            };
            SelectDataAsync(s, new AsyncCallback((ar) =>
            {
                MySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
                try
                {
                    SelectResponse response = (SelectResponse)vagueResponse;
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
                        s2 = new SQLInsertOrUpdateStructure(DatabaseManager)
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
                    } else
                    {
                        s2 = new SQLInsertOrUpdateStructure(DatabaseManager)
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
                } catch (InvalidCastException)
                {
                    F.LogError("Couldn't save username to MySql Database. Cast error.\n\"" + vagueResponse.command + "\"");
                    return;
                }
            }));
        }
        /// <summary>
        /// Gets a <see cref="MySqlResponse"/> from an IAsyncResult then disposes of it, calling WaitOne() if it is not already completed.
        /// </summary>
        /// <typeparam name="AsyncStateType"><see cref="SQLCallStructure"/> child type.</typeparam>
        /// <param name="ar"><see cref="IAsyncResult"/> of a async function using the <see cref="D_DatabaseQuery{T}"/> delegate.</param>
        /// <returns><see cref="MySqlResponse"/> from the provided SQL type.</returns>
        private MySqlResponse GetResponse<AsyncStateType>(IAsyncResult ar) where AsyncStateType : SQLCallStructure
        {
            D_DatabaseQuery<AsyncStateType> rtn;
            try
            {
                rtn = (D_DatabaseQuery<AsyncStateType>)ar.AsyncState;
            }
            catch (InvalidCastException)
            {
                rtn = default(D_DatabaseQuery<AsyncStateType>);
            }
            rtn.EndInvoke(out MySqlResponse response, ar);
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Dispose();
            return response;
        }
        internal void DisposeOf(AsyncDatabase DatabaseManager)
        {
            IAsyncResult ar = DatabaseManager.CloseAsync(null);
            ar.AsyncWaitHandle.WaitOne();
            Stats.WebCallbacks.Dispose(ar);
            DatabaseManager.SQL.Dispose();
        }
        internal void Close(AsyncDatabase DatabaseManager)
        {
            try
            {
                DatabaseManager.SQL.Close();
            }
            catch (MySqlException ex)
            {
                F.LogError("ERROR Closing Connection\n" + ex.Message);
                F.LogError("\nTrace\n" + ex.StackTrace);
            }
        }
        internal void Open(AsyncDatabase DatabaseManager, out bool bSuccess)
        {
            try
            {
                DatabaseManager.SQL.Open();
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
            try
            {
                ((WaitUntilFinishedReadingDelegate)ar.AsyncState).EndInvoke(out SQLCallStructure Data, out AsyncCallback Function, out Type type, ar);
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
                    } else
                    {
                        F.LogError("Type \"" + type.ToString() + "\" - Not a valid type.");
                    }
                } catch (InvalidCastException)
                {
                    F.LogError("Failed to cast \"" + type.ToString() + "\" to a valid SQL Container.");
                }
            } catch (InvalidCastException)
            {
                F.LogError("Failed to cast \"" + ar.AsyncState.GetType().ToString() + "\" to a valid delegate containing SQL information.");
            }
            Stats.WebCallbacks.Dispose(ar);
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
            public string ConditionVariable;
            public EComparisonType comparison;
            public object condition;
            public int limit;
            public SQLSelectCallStructure(AsyncDatabase DatabaseManager) : base(DatabaseManager) { }
            public SQLSelectCallStructure(
                AsyncDatabase DatabaseManager,
                Dictionary<string, Type> Columns,
                string tableName,
                bool selectAll = false,
                string ConditionVariable = "none",
                EComparisonType comparison = EComparisonType.NOCOMPARISON,
                object condition = null,
                int limit = -1
                ) : base(DatabaseManager)
            {
                this.Columns = Columns;
                this.tableName = tableName;
                this.selectAll = selectAll;
                this.ConditionVariable = ConditionVariable;
                this.comparison = comparison;
                this.condition = condition;
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
            if (Data.comparison != EComparisonType.NOCOMPARISON && Data.condition != null)
            {
                query.Append(" WHERE ");
                query.Append("`" + Data.ConditionVariable + "` ");
                query.Append(OperatorTranslations[Data.comparison]);
                if (Data.comparison != EComparisonType.ISNULL && Data.comparison != EComparisonType.ISNOTNULL)
                    query.Append(" @0");
            }
            if (Data.limit != -1)
                query.Append(" LIMIT " + Data.limit.ToString());
            query.Append(';');
            string CommandText = query.ToString();
            SelectResponse rtn = new SelectResponse(CommandText);
            using (MySqlCommand Q = new MySqlCommand(CommandText, Data.DatabaseManager.SQL))
            {
                Q.Parameters.AddWithValue("@0", Data.condition);
                using (MySqlDataReader R = Q.ExecuteReader())
                {
                    while(R.Read())
                    {
                        foreach(KeyValuePair<string, Type> column in Data.Columns)
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
                            } catch (Exception ex)
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
            for(int i = 0; i < Data.NewValues.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append("`" + Data.NewValues.ElementAt(i).Key + "`");
            }
            sb.Append(") VALUES(");
            for(int i = 0; i < Data.NewValues.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append("@" + i.ToString());
                Parameters.Add(i, Data.NewValues.ElementAt(i).Value);
            }
            sb.Append(") ON DUPLICATE KEY UPDATE ");
            for(int i = 0; i < Data.VariablesToUpdateIfDuplicate.Count; i++)
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
                foreach(KeyValuePair<int, object> Parameter in Parameters)
                    Q.Parameters.AddWithValue('@' + Parameter.Key.ToString(), Parameter.Value);
                try
                {
                    rtn.RowsAffected = Q.ExecuteNonQuery();
                    if (rtn.RowsAffected == 0) rtn.executionstatus = MySqlResponse.EExecutionStatus.NORESULTS;
                    else rtn.executionstatus = MySqlResponse.EExecutionStatus.SUCCESS;
                } catch (Exception ex)
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
        internal void SendPlayerZoneOverlay(Texture2D img, Player player, List<Zone> zones, List<Vector2> PointsToTest, int step, out bool complete)
        {
            complete = false;
            F.Log("STEP " + step.ToString());
            if (step == 0)
            {
                if (File.Exists(Level.info.path + @"\Map.png"))
                {
                    byte[] fileData = File.ReadAllBytes(Level.info.path + @"\Map.png");
                    img.LoadImage(fileData, false);
                }
                img.Apply();
            }
            else if (step == 1)
            {
                foreach (Zone zone in zones)
                {
                    if (zone.GetType() == typeof(PolygonZone))
                    {
                        PolygonZone pzone = (PolygonZone)zone;
                        for (int i = 0; i < pzone.PolygonInverseZone.Lines.Length; i++)
                        {
                            F.DrawLine(img, pzone.PolygonInverseZone.Lines[i], Color.black, false);
                        }
                    }
                    else if (zone.GetType() == typeof(CircleZone))
                    {
                        CircleZone czone = (CircleZone)zone;
                        F.DrawCircle(img, czone.InverseZone.Center.x + img.width / 2, czone.InverseZone.Center.y + img.height / 2, czone.CircleInverseZone.Radius, Color.black, false);
                    }
                    else if (zone.GetType() == typeof(RectZone))
                    {
                        RectZone rzone = (RectZone)zone;
                        for (int i = 0; i < rzone.RectInverseZone.lines.Length; i++)
                        {
                            F.DrawLine(img, rzone.RectInverseZone.lines[i], Color.black, false);
                        }
                    }
                }
                //player.SendChat("Completed step 2", UCWarfare.GetColor("default"));
                img.Apply();
            }
            else if (step > 1)
            {
                int z = (step - 2) * 3;    //0
                int next = (step - 1) * 3; //3
                if (zones.Count <= next) complete = true;
                System.Random r = new System.Random();
                for (int e = z; e < (zones.Count > next ? next : zones.Count); e++)
                {
                    Color zonecolor = $"{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}{r.Next(0, 10)}".Hex();
                    for (int i = 0; i < PointsToTest.Count; i++)
                    {
                        if (zones[e].InverseZone.IsInside(new Vector2(PointsToTest[i].x, PointsToTest[i].y)))
                        {
                            img.SetPixel((int)Math.Round(PointsToTest[i].x + img.width / 2), (int)Math.Round(PointsToTest[i].y + img.height / 2), zonecolor);
                        }
                    }
                }
                //player.SendChat("Completed step " + (step + 1).ToString(), UCWarfare.GetColor("default"));
                img.Apply();
            }
            else if (step == -1)
            {
                img.Apply();
                Texture2D flipped = F.FlipVertical(img);
                F.SavePhotoToDisk(Data.FlagStorage + "zonearea.png", flipped);
                UnityEngine.Object.Destroy(flipped);
                UnityEngine.Object.Destroy(img);
                complete = true;
            }
        }
    }
    public class MySqlResponse
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