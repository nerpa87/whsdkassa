using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Configuration;

using System.Collections;
using System.Collections.Specialized;

using System.Diagnostics;

namespace whsdkassa
{
    class DbInserter
    {
        private DbConnection _conn;
        private NameValueCollection _settings;

        public DbInserter(NameValueCollection settings) {
            this._settings = settings;

            ConnectionStringSettings conSettings = ConfigurationManager.ConnectionStrings["dbConnect"];
            String conString = conSettings.ConnectionString;
            String provName = conSettings.ProviderName;

            DbProviderFactory dbpf = DbProviderFactories.GetFactory(provName);
            this._conn = dbpf.CreateConnection();
            this._conn.ConnectionString = conString;

            
            //this._conn.Close();
        }

        public void insertPrevedData(Hashtable prevedData) {
            this.connect();

            DbCommand c = this._conn.CreateCommand();
            Hashtable mainData = (Hashtable)prevedData["mainData"];
            c.CommandText = String.Format(
                "INSERT INTO encashments (operatorid, envelope, dt, amount, concession, network, plaza) VALUES({0}, {1}, to_date('{2}','yyyymmddhh24miss'), {3}, {4}, {5}, {6})",
                mainData["operatorId"], mainData["envelope"], mainData["dateTime"], Math.Round(100 * (double)mainData["amount"]), this._settings["concession"], this._settings["network"], this._settings["plaza"]
                );
            c.ExecuteNonQuery();
            //getting last insert id
            int id = 0;
            if (true) {//if oracle db!!!
                c.CommandText = "SELECT seq_encashments_id.currval FROM dual";
                using (DbDataReader reader = c.ExecuteReader()) {//OracleDataReader
                    while (reader.Read()) { 
                        id = reader.GetInt32(0);
                        break;
                    }
                }
            }
            //inserting coins data
            IEnumerable coinsData = (IEnumerable)prevedData["coinsData"];
            foreach (Hashtable coinsLine in coinsData) {
                DbCommand c1 = this._conn.CreateCommand();
                c1.CommandText = String.Format(
                    "INSERT INTO encashmentinfo (encashmentid, cash_type, cash_value, cash_number) VALUES({0},{1},{2},{3})",
                    id, coinsLine["coinFlag"], Math.Round(100 * (double)coinsLine["fNominal"]), coinsLine["itemsCount"]);
                c1.ExecuteNonQuery();
            }
            Trace.TraceInformation("Data from '" + prevedData["filePath"] + "' inserted");
        }

        public void connect() {
            if (this._conn.State != ConnectionState.Open) {
                this._conn.Open();
                if (this._conn.State == ConnectionState.Open)
                {
                    Trace.TraceInformation("DB Server connected");
                }
                else {
                    Trace.TraceInformation("Failed to open connection to DB server");
                }
            }
        }

        public void disconnect() {
            if (this._conn.State == System.Data.ConnectionState.Open) {
                this._conn.Close();
                if (this._conn.State == ConnectionState.Closed)
                {
                    Trace.TraceInformation("DB Server disconnected");
                }
                else {
                    Trace.TraceInformation("Failed to disconnect DB server");
                }
            }
        }

    }
}
