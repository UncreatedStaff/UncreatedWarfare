using MySql.Data.MySqlClient;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    public class AsyncDatabase : IDisposable
    {
        public MySqlConnection SQL { get; protected set; }
        public AsyncDatabase()
        {
            SQL = new MySqlConnection(UCWarfare.Config.SQL.ConnectionString);
        }
        private IAsyncResult InvokeWithDatabase(DbCaller.DatabaseDelegate method, AsyncCallback callback = null)
        {
            DbCaller.DatabaseDelegate caller = new DbCaller.DatabaseDelegate(method);
            return caller.BeginInvoke(this, callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
        }
        public IAsyncResult DisposeAsync(AsyncCallback callback = null) => InvokeWithDatabase(DbCaller.DisposeOf, callback);
        public IAsyncResult OpenAsync(AsyncCallback callback = null)
        {
            DbCaller.DatabaseDelegateWithBool caller = new DbCaller.DatabaseDelegateWithBool(DbCaller.Open);
            return caller.BeginInvoke(this, out _, callback == null ? AsyncDatabaseCallbacks.DisposeAsyncResult : callback, caller);
        }
        public bool OpenSync() 
        {
            DbCaller.Open(this, out bool success);
            return success;
        }
        public IAsyncResult CloseAsync(AsyncCallback callback = null) => InvokeWithDatabase(DbCaller.Close, callback);
        public void CloseSync() => DbCaller.Close(this);
        /// <summary>
        /// Synchronous
        /// </summary>
        public void Dispose()
        {
            IAsyncResult ar = CloseAsync(null);
            ar.AsyncWaitHandle.WaitOne();
            ar.AsyncWaitHandle.Dispose();
            SQL.Dispose();
        }
        protected void BeginSqlRequest(ERequestType Type)
        {
            if(Type == ERequestType.QUERY)
            {
                using (MySqlCommand Q = new MySqlCommand())
                {

                }
            }
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
    public enum ERequestType : byte
    {
        QUERY,
        NONQUERY
    }
    public class MySqlResult
    {

    }
    public static class DbCaller
    {
        internal delegate void DatabaseDelegate(AsyncDatabase DatabaseManager);
        internal delegate void NoArgsInvokerDelegate();
        internal delegate void DatabaseDelegateWithBool(AsyncDatabase DatabaseManager, out bool bSuccess);

        private static readonly Dictionary<EComparisonType, string> OperatorTranslations = new Dictionary<EComparisonType, string>
        {
            { EComparisonType.NOCOMPARISON, string.Empty },
            { EComparisonType.EQUALS, "=" },
            { EComparisonType.NOTEQUALS, "!=" },
            { EComparisonType.LIKE, "LIKE" },
            { EComparisonType.GREATERTHAN, ">" },
            { EComparisonType.LESSTHAN, "<" },
            { EComparisonType.GREATERTHANOREQUALTO, ">=" },
            { EComparisonType.LESSTHANOREQUALTO, "<=" },
            { EComparisonType.LESSTHANOREQUALTO, "<=>" },
            { EComparisonType.LESSTHANOREQUALTO, "IS NULL" },
            { EComparisonType.LESSTHANOREQUALTO, "IS NOT NULL" },
            { EComparisonType.LESSTHANOREQUALTO, "IS" },
            { EComparisonType.LESSTHANOREQUALTO, "IS NOT" },
        };

        internal static void DisposeOf(this AsyncDatabase DatabaseManager)
        {
            IAsyncResult ar = DatabaseManager.CloseAsync(null);
            ar.AsyncWaitHandle.WaitOne();
            Stats.WebCallbacks.Dispose(ar);
            DatabaseManager.SQL.Dispose();
        }
        internal static void Close(AsyncDatabase DatabaseManager)
        {
            try
            {
                DatabaseManager.SQL.Close();
            }
            catch (MySqlException ex)
            {
                CommandWindow.LogError("ERROR Closing Connection\n" + ex.Message);
                CommandWindow.LogError("\nTrace\n" + ex.StackTrace);
            }
        }

        internal static void Open(this AsyncDatabase DatabaseManager, out bool bSuccess)
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
                        CommandWindow.LogError("ERROR: Cannot connect to server. Server not found.");
                        break;
                    case 1045:
                        CommandWindow.LogError("ERROR: SQL Invalid Login");
                        break;
                    case 1042:
                        CommandWindow.LogError("ERROR: Unable to connect to any of the specified MySQL hosts.");
                        break;
                    default:
                        CommandWindow.LogError($"Unknown MYSQL Error: {ex.Number}\n{ex.Message}");
                        break;
                }
                bSuccess = false;
            }
        }
        private const int TimeBetweenFinishedReadingCheck = 20;
        private delegate void WaitUntilFinishedReadingDelegate<T>(T Data, AsyncCallback Function, out T DataReturn, out AsyncCallback FunctionReturn, out string TypeReturn) where T : SQLCallStructure;
        private static void FinishedReading(IAsyncResult ar)
        {
            try
            {
                ((WaitUntilFinishedReadingDelegate<SQLCallStructure>)ar.AsyncState).EndInvoke(out SQLCallStructure Data, out AsyncCallback Function, out string type, ar);
                try
                {
                    switch (type)
                    {
                        case nameof(SQLSelectCallStructure):
                            DatabaseQuery<SQLSelectCallStructure> SelectCaller = new DatabaseQuery<SQLSelectCallStructure>(SelectDataAsyncCall);
                            SelectCaller.BeginInvoke((SQLSelectCallStructure)Data, out _, Function, SelectCaller);
                            break;
                        case nameof(SQLInsertOnDuplicateKeyUpdateStructure):
                            DatabaseQuery<SQLInsertOnDuplicateKeyUpdateStructure> InsertOnDuplicateKeyUpdateCaller = new DatabaseQuery<SQLInsertOnDuplicateKeyUpdateStructure>(InsertIfDuplicateUpdateAsyncCall);
                            InsertOnDuplicateKeyUpdateCaller.BeginInvoke((SQLInsertOnDuplicateKeyUpdateStructure)Data, out _, Function, InsertOnDuplicateKeyUpdateCaller);
                            break;
                        default:
                            CommandWindow.LogError(type + " - is not a valid type.");
                            break;
                    }
                } catch (InvalidCastException)
                {
                    CommandWindow.LogError("Failed to cast " + type + " to a valid SQL Container.");
                }
            } catch (InvalidCastException)
            {
                CommandWindow.LogError("Failed to cast " + ar.AsyncState.GetType().ToString() + " to a valid SQL Container.");
            }
            Stats.WebCallbacks.Dispose(ar);
        }
        private static void WaitUntilFinishedReading<T>(T Data, AsyncCallback Function, out T DataReturn, out AsyncCallback FunctionReturn, out string TypeReturn) where T : SQLCallStructure
        {
            DataReturn = Data;
            FunctionReturn = Function;
            TypeReturn = nameof(T);
            while (!CheckIsFinishedReading(Data.DatabaseManager.SQL))
            {
                Task.Delay(TimeBetweenFinishedReadingCheck);
            }
        }
        private static bool CheckIsFinishedReading(MySqlConnection SQL)
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
        public delegate void DatabaseQuery<T>(T Data, out MySqlResponse Output) where T : SQLCallStructure;
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
            public string condition;
            public int limit;
            public SQLSelectCallStructure(
                AsyncDatabase DatabaseManager,
                Dictionary<string, Type> Columns,
                string tableName,
                bool selectAll = false,
                string ConditionVariable = "none",
                EComparisonType comparison = EComparisonType.NOCOMPARISON,
                string condition = "none",
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
        public class SQLInsertOnDuplicateKeyUpdateStructure : SQLCallStructure
        {
            public SQLInsertOnDuplicateKeyUpdateStructure(AsyncDatabase DatabaseManager) : base(DatabaseManager)
            {
            }
        }
        private static void SelectDataAsyncCall(SQLSelectCallStructure Data, out MySqlResponse Output)
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
            if (Data.comparison != EComparisonType.NOCOMPARISON)
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
            Output = new SelectResponse(CommandText);
            using (MySqlCommand Q = new MySqlCommand(CommandText, Data.DatabaseManager.SQL))
            {
                Q.Parameters.AddWithValue("@0", Data.condition);
                using (MySqlDataReader R = Q.ExecuteReader())
                {
                    while(R.Read())
                    {
                        foreach(KeyValuePair<string, Type> column in Data.Columns)
                        {

                        }
                    }
                }
            }
        }
        private static void InsertIfDuplicateUpdateAsyncCall(SQLInsertOnDuplicateKeyUpdateStructure Data, out MySqlResponse Output)
        {
            Output = new NonQueryResponse("", 0);
        }
        internal static void SelectDataAsync(SQLSelectCallStructure Data, AsyncCallback callback)
        {
            WaitUntilFinishedReadingDelegate<SQLSelectCallStructure> caller = new WaitUntilFinishedReadingDelegate<SQLSelectCallStructure>(WaitUntilFinishedReading);
            caller.BeginInvoke(Data, callback, out _, out _, out _, FinishedReading, caller);
        }
        internal static void SelectDataAsync(
            this AsyncDatabase DatabaseManager,
            AsyncCallback callback,
            Dictionary<string, Type> Columns,
            string tableName,
            bool selectAll = false,
            string ConditionVariable = "none",
            EComparisonType comparison = EComparisonType.NOCOMPARISON,
            string condition = "none",
            int limit = -1
            ) => SelectDataAsync(
                new SQLSelectCallStructure(DatabaseManager, Columns, tableName, selectAll, ConditionVariable, comparison, condition, limit), callback
                );
    }
    public class MySqlResponse
    {
        public string command;
        public MySqlResponse(string command)
        {
            this.command = command;
        }
    }
    public class NonQueryResponse : MySqlResponse
    {
        public int RowsUpdated;
        public NonQueryResponse(string command, int rowsChanged) : base(command)
        {
            this.RowsUpdated = rowsChanged;
        }
    }
    public class SelectResponse : MySqlResponse
    {
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
    }
}
