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
using Uncreated.Warfare.Flags;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Data = Uncreated.Warfare.Data;

namespace Uncreated.SQL
{
    public class Database : IDisposable
    {
        public MySqlConnection SQL { get; protected set; }
        public Database(string connection_string)
        {
            SQL = new MySqlConnection(connection_string);
        }
        /// <summary>Synchronous. Closes the connection and waits for a response.</summary>
        public virtual void Dispose()
        {
            CloseSync();
            SQL.Dispose();
        }
        /// <summary>Closes the connection and waits for a response.</summary>
        public virtual void CloseSync()
        {
            try
            {
                SQL.Close();
            }
            catch (MySqlException ex)
            {
                LogError("ERROR Closing Connection\n" + ex.Message);
                LogError("\nTrace\n" + ex.StackTrace);
            }
        }
        /// <summary>Opens the connection and waits for a response.</summary>
        public virtual void OpenSync()
        {
            try
            {
                SQL.Open();
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        LogError("ERROR: Cannot connect to server. Server not found.");
                        break;
                    case 1045:
                        LogError("ERROR: SQL Invalid Login");
                        break;
                    case 1042:
                        LogError("ERROR: Unable to connect to any of the specified MySQL hosts.");
                        break;
                    default:
                        LogError($"Unknown MYSQL Error: {ex.Number}\n{ex.Message}");
                        break;
                }
            }
        }
        public virtual string GetTableName(string key)
        {
            return key;
        }
        public virtual string GetColumnName(string table_key, string column_key, out string table_name)
        {
            table_name = table_key;
            return column_key;
        }
        public virtual void Log(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogError(string message, ConsoleColor color = ConsoleColor.Red)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = temp;
        }
        public virtual void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(ex);
            Console.ForegroundColor = temp;
        }
    }
    public class AsyncDatabase<TBackend> : Database, IDisposable where TBackend : DbCaller, new()
    {
        protected readonly TBackend _dbCaller;
        /// <summary>Does not open the connection, you must run <see cref="OpenAsync"/> or <see cref="Database.OpenSync"/>.</summary>
        public AsyncDatabase(string connection_string) : base(connection_string)
        {
            _dbCaller = new TBackend();
            _dbCaller.SetManager(this);
        }
        public IAsyncResult DisposeAsync(AsyncCallback callback = null)
        {
            D_DatabaseDelegate caller = new D_DatabaseDelegate(_dbCaller.DisposeOf);
            return caller.BeginInvoke(callback ?? AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>Opens the connection. Not thread-blocking.</summary>
        public IAsyncResult OpenAsync(AsyncCallback callback = null)
        {
            D_DatabaseDelegateWithBool caller = new D_DatabaseDelegateWithBool(_dbCaller.Open);
            return caller.BeginInvoke(out _, callback ?? AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        /// <summary>Closes the connection. Not thread-blocking.</summary>
        public IAsyncResult CloseAsync(AsyncCallback callback = null)
        {
            D_DatabaseDelegate caller = new D_DatabaseDelegate(_dbCaller.Close);
            return caller.BeginInvoke(callback ?? AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public void CustomSqlQuery(string command, D_ReaderAction readerLoopAction, AsyncCallback callback, params object[] parameters)
        {
            D_CustomReader caller = _dbCaller.CustomReaderCall;
            caller.BeginInvoke(command, readerLoopAction, parameters, callback ?? AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public void CustomSqlNonQuery(string command, AsyncCallback callback, params object[] parameters)
        {
            D_CustomWriter caller = _dbCaller.CustomNonQueryCall;
            caller.BeginInvoke(command, parameters, callback ?? AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
        }
        public void CustomSqlQuerySync(string command, D_ReaderAction readerLoopAction, params object[] parameters)
        {
            D_CustomReader caller = _dbCaller.CustomReaderCall;
            IAsyncResult ar = caller.BeginInvoke(command, readerLoopAction, parameters, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            ar.AsyncWaitHandle.WaitOne();
        }
        public void CustomSqlNonQuerySync(string command, params object[] parameters)
        {
            D_CustomWriter caller = _dbCaller.CustomNonQueryCall;
            IAsyncResult ar = caller.BeginInvoke(command, parameters, AsyncDatabaseCallbacks.DisposeAsyncResult, caller);
            ar.AsyncWaitHandle.WaitOne();
        }
    }
    // internal use
    public delegate void D_DatabaseDelegate();
    public delegate void D_DatabaseDelegateWithBool(out bool bSuccess);
    public delegate void D_SelectAsync(SQLSelectCallStructure Data, AsyncCallback callback);
    public delegate void D_InsertOrUpdateAsync(SQLInsertOrUpdateStructure Data, AsyncCallback callback);
    public delegate void D_UpdateUsernameAsync(ulong Steam64, FPlayerName player);
    public delegate void D_DatabaseQuery<T>(T Data, out IMySqlResponse Output) where T : ISQLCallStructure;
    public delegate void D_GetUsername(ulong Steam64, D_UsernameReceived callback);
    public delegate void D_GetUInt32Balance(ulong Steam64, byte Team, D_Uint32BalanceReceived callback);
    public delegate void D_AddPlayerStat(ulong Steam64, byte Team, int amount);
    public delegate void D_SubtractPlayerStat(ulong Steam64, byte Team, int amount, D_Difference onUnderZero, bool clampOnSubtract);
    public delegate void D_CustomReader(string command, D_ReaderAction readerLoop, object[] parameters);
    public delegate void D_CustomWriter(string command, object[] parameters);
    // public use
    public delegate void D_UsernameReceived(FPlayerName usernames, bool success);
    public delegate void D_Uint32BalanceReceived(uint balance, bool success);
    public delegate void D_Difference(long difference);
    public delegate void D_ReaderAction(MySqlDataReader reader);
    public class DbCaller
    {
        public DbCaller() { }
        protected Database _manager;
        public void SetManager(Database manager) => this._manager = manager;
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
        /// <summary>
        /// Gets a <see cref="MySqlResponse"/> from an IAsyncResult then disposes of it, calling WaitOne() if it is not already completed.
        /// </summary>
        /// <typeparam name="AsyncStateType"><see cref="ISQLCallStructure"/> child type.</typeparam>
        /// <param name="ar"><see cref="IAsyncResult"/> of a async function using the <see cref="D_DatabaseQuery{T}"/> delegate.</param>
        /// <returns><see cref="MySqlResponse"/> from the provided SQL type.</returns>
        protected IMySqlResponse GetResponse<AsyncStateType>(IAsyncResult ar) where AsyncStateType : ISQLCallStructure
        {
            if (ar.AsyncState is D_DatabaseQuery<AsyncStateType> rtn)
            {
                rtn.EndInvoke(out IMySqlResponse response, ar);
                ar.AsyncWaitHandle.WaitOne();
                ar.AsyncWaitHandle.Dispose();
                return response;
            }
            else return default;
        }
        protected bool GetSelectResponse(IAsyncResult ar, out SelectResponse response)
        {
            IMySqlResponse vagueResponse = GetResponse<SQLSelectCallStructure>(ar);
            if (vagueResponse is SelectResponse r)
            {
                response = r;
                return true;
            }
            else
            {
                response = null;
                return false;
            }
        }
        public void DisposeOf()
        {
            _manager.CloseSync();
            _manager.SQL.Dispose();
        }
        public void Close()
        {
            try
            {
                _manager.SQL.Close();
            }
            catch (MySqlException ex)
            {
                _manager.LogError("ERROR Closing Connection\n" + ex.Message);
                _manager.LogError("\nTrace\n" + ex.StackTrace);
            }
        }
        public void Open(out bool bSuccess)
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
                        _manager.LogError("ERROR: Cannot connect to server. Server not found.");
                        break;
                    case 1045:
                        _manager.LogError("ERROR: SQL Invalid Login");
                        break;
                    case 1042:
                        _manager.LogError("ERROR: Unable to connect to any of the specified MySQL hosts.");
                        break;
                    default:
                        _manager.LogError($"Unknown MYSQL Error: {ex.Number}\n{ex.Message}");
                        break;
                }
                bSuccess = false;
            }
        }
        protected const int TimeBetweenFinishedReadingCheck = 20;
        protected delegate void WaitUntilFinishedReadingDelegate(ISQLCallStructure Data, AsyncCallback Function, out ISQLCallStructure DataReturn, out AsyncCallback FunctionReturn, out Type TypeReturn);
        protected void FinishedReading(ISQLCallStructure Data, AsyncCallback Function, Type type)
        {
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
                    _manager.LogError("Type \"" + type.ToString() + "\" - Not a valid type.");
                }
            }
            catch (InvalidCastException)
            {
                _manager.LogError("Failed to cast \"" + type.ToString() + "\" to a valid SQL Container.");
            }
        }
        protected void WaitUntilFinishedReading(ISQLCallStructure Data, AsyncCallback Function, out ISQLCallStructure DataReturn, out AsyncCallback FunctionReturn, out Type TypeReturn)
        {
            DataReturn = Data;
            FunctionReturn = Function;
            TypeReturn = Data.GetType();
            while (!CheckIsFinishedReading(Data.DatabaseManager.SQL))
            {
                Task.Delay(TimeBetweenFinishedReadingCheck);
            }
        }
        protected bool CheckIsFinishedReading(MySqlConnection SQL)
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
        protected void SelectDataAsyncCall(SQLSelectCallStructure Data, out IMySqlResponse Output)
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
                                _manager.LogError("Exception in MySql SELECT statement:\n\"" + rtn.Command + "\"\nError:\n" + ex.ToString());
                                rtn.ExecutionStatus = EExecutionStatus.FAILURE;
                            }
                        }
                    }
                }
            }
            if (rtn.Columns.Count > 0) rtn.ExecutionStatus = EExecutionStatus.SUCCESS;
            else rtn.ExecutionStatus = EExecutionStatus.NORESULTS;
            Output = rtn;
        }
        protected void InsertIfDuplicateUpdateAsyncCall(SQLInsertOrUpdateStructure Data, out IMySqlResponse Output)
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
                    if (rtn.RowsAffected == 0) rtn.ExecutionStatus = EExecutionStatus.NORESULTS;
                    else rtn.ExecutionStatus = EExecutionStatus.SUCCESS;
                }
                catch (Exception ex)
                {
                    _manager.LogError("Exception in MySql INSERT ON DUPLICATE UPDATE statement:\n\"" + rtn.Command + "\"\nError:\n" + ex.ToString());
                    rtn.ExecutionStatus = EExecutionStatus.FAILURE;
                }
            }
            Output = rtn;
        }
        /// <param name="callback">Cast AsyncState to <see cref="D_DatabaseQuery{SQLSelectCallStructure}">DatabaseQuery&lt;SQLInsertOrUpdateStructure&gt;</see></param>
        internal void InsertOrUpdateAsync(SQLInsertOrUpdateStructure Data, AsyncCallback callback)
        {
            WaitUntilFinishedReading(Data, callback, out _, out _, out _);
            FinishedReading(Data, callback, typeof(SQLInsertOrUpdateStructure));
        }
        /// <param name="callback">Cast AsyncState to <see cref="D_DatabaseQuery{SQLSelectCallStructure}">DatabaseQuery&lt;SQLSelectCallStructure&gt;</see></param>
        public void SelectData(SQLSelectCallStructure Data, AsyncCallback callback) 
        {
            WaitUntilFinishedReading(Data, callback, out _, out _, out _);
            FinishedReading(Data, callback, typeof(SQLSelectCallStructure));
        }
        public void CustomReaderCall(string command, D_ReaderAction readerLoop, params object[] parameters)
        {
            using (MySqlCommand Q = new MySqlCommand(command, _manager.SQL))
            {
                if(parameters != default)
                    for (int i = 0; i < parameters.Length; i++)
                        Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                using (MySqlDataReader R = Q.ExecuteReader())
                {
                    while (R.Read())
                    {
                        readerLoop.Invoke(R);
                    }
                    R.Close();
                    R.Dispose();
                    Q.Dispose();
                }
            }
        }
        public void CustomNonQueryCall(string command, params object[] parameters)
        {
            using (MySqlCommand Q = new MySqlCommand(command, _manager.SQL))
            {
                if (parameters != default)
                    for (int i = 0; i < parameters.Length; i++)
                        Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                try
                {
                    Q.ExecuteNonQuery();
                }
                finally
                {
                    Q.Dispose();
                }
            }
        }
    }
    public interface IMySqlResponse
    {
        string Command { get; set; }
        EExecutionStatus ExecutionStatus { get; set; }
    }
    public class NonQueryResponse : IMySqlResponse
    {
        public int RowsAffected;
        public string Command { get; set; }
        public EExecutionStatus ExecutionStatus { get; set; }
        public override string ToString()
        {
            return this.Command + "\n" + RowsAffected.ToString() + " rows affected";
        }
        public NonQueryResponse(string command, int rowsChanged)
        {
            this.Command = command;
            this.RowsAffected = rowsChanged;
        }
    }
    public class SelectResponse : IMySqlResponse
    {
        public string Command { get; set; }
        public EExecutionStatus ExecutionStatus { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Command + '\n');
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
            public T[] GetAllValues() => values.ToArray();
            public T[] GetAllValues(Func<T, bool> predicate) => values.Where(predicate).ToArray();
        }
        public List<SqlColumn> Columns;
        public SelectResponse(string command)
        {
            this.Command = command;
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
    public interface ISQLCallStructure
    {
        Database DatabaseManager { get; set; }
    }
    public class SQLSelectCallStructure : ISQLCallStructure
    {
        public Dictionary<string, Type> Columns;
        public string tableName;
        public bool selectAll;
        public string[] ConditionVariables;
        public EComparisonType[] comparisons;
        public object[] conditions;
        public int limit;
        public Database DatabaseManager { get; set; }
        public SQLSelectCallStructure(Database DatabaseManager)
        { 
            this.DatabaseManager = DatabaseManager;
        }
    }
    public class SQLInsertOrUpdateStructure : ISQLCallStructure
    {
        public string tableName;
        public Dictionary<string, object> NewValues;
        public Dictionary<string, EUpdateOperation> VariablesToUpdateIfDuplicate;
        public List<object> UpdateValuesIfValid;
        public Database DatabaseManager { get; set; }
        public SQLInsertOrUpdateStructure(Database DatabaseManager)
        {
            this.DatabaseManager = DatabaseManager;
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
    public enum EExecutionStatus : byte
    {
        UNSET,
        SUCCESS,
        FAILURE,
        NORESULTS
    }
    public struct MySqlData
    {
        public string Host;
        public string Database;
        public string Password;
        public string Username;
        public ushort Port;
        public string CharSet;
        [Newtonsoft.Json.JsonIgnore]
        public string ConnectionString { get => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};"; }
    }
}
