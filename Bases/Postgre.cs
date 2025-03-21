﻿using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AnyBaseLib.Bases
{
    internal class PostgreDriver: IAnyBase
    {
        private NpgsqlConnection dbConn;
        private CommitMode commit_mode;
        private bool trans_started;
        private DbTransaction transaction;
        private string builder;

        public void Set(CommitMode commit_mode, string db_name, string db_host, string db_user = "", string db_pass = "", string ssl_mode = "")
        {
            this.commit_mode = commit_mode;

            var db_host_arr = db_host.Split(":");
            var db_server = db_host_arr[0];
            int db_port = 3306;
            if (db_host_arr.Length > 1)
                db_port = int.Parse(db_host_arr[1]);

            SslMode npgSslMode = SslMode.Prefer;
            if (!string.IsNullOrEmpty(ssl_mode))
            {
                // SSLmode
                switch (ssl_mode.ToLower())
                {
                    case "disable":
                        npgSslMode = SslMode.Disable;
                        break;
                    case "prefer":
                        npgSslMode = SslMode.Prefer;
                        break;
                    case "require":
                        npgSslMode = SslMode.Require;
                        break;
                    case "verifyfull":
                    case "verify-full":
                        npgSslMode = SslMode.VerifyFull;
                        break;
                    case "verifyca":
                    case "verify-ca":
                        npgSslMode = SslMode.VerifyCA;
                        break;
                }
            }

            builder = new NpgsqlConnectionStringBuilder
            {
                Host = db_server,
                Database = db_name,
                Username = db_user,
                Password = db_pass,
                SslMode = npgSslMode,
                Port = db_port
                
            }.ConnectionString;

            dbConn = GetNewConn();

            if (commit_mode != CommitMode.AutoCommit)
                new Task(TimerCommit).Start();
        }

        private void TimerCommit()
        {
            while (true)
            {
                if (trans_started)
                    SetTransState(false);
                Thread.Sleep(5000);
            }
        }

        public List<List<string>> Query(string q, List<string> args, bool non_query = false)
        {

            if (commit_mode != CommitMode.AutoCommit)
            {
                if (!trans_started && non_query) SetTransState(true);
                else
                {
                    if (trans_started && !non_query) SetTransState(false);
                }
            }

            return Common.Query(dbConn, Common._PrepareClear(q, args), non_query);
        }
        public void QueryAsync(string q, List<string> args, Action<List<List<string>>> action = null, bool non_query = false)
        {
            /*
            if (commit_mode != CommitMode.AutoCommit)
            {
                if (!trans_started && non_query) SetTransState(true);
                else
                {
                    if (trans_started && !non_query) SetTransState(false);
                }
            }*/

            Common.QueryAsync(GetNewConn(), Common._PrepareClear(q, args), action, non_query);
        }

        private NpgsqlConnection GetNewConn()
        {
            var conn = new NpgsqlConnection(builder);
            conn.Open();
            return conn;
        }

        private void SetTransState(bool state)
        {
            if (state)
            {
                transaction = dbConn.BeginTransaction();
                trans_started = true;
            }
            else
            {
                if (commit_mode == CommitMode.NoCommit)
                    transaction.Rollback();
                else
                    transaction.Commit();
                //transaction.Dispose();
                trans_started = false;
            }
        }

        public DbConnection GetConn()
        { return dbConn; }

        public void Close()
        {
            dbConn.Close();
        }

        public bool Init()
        {
            return Common.Init(dbConn, "PostgreSQL");
        }
    }
}
