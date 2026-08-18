using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace IT8_TechStore.Database
{
    public static class DbConfig
    {
        public static string DatabaseName = "MorphicComputersDB";

        // List of common local SSMS SQL Server connection strings to attempt
        private static readonly List<string> ConnectionCandidates = new List<string>
        {
            $"Server=localhost;Database={DatabaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3;",
            $"Server=.\\SQLEXPRESS;Database={DatabaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3;",
            $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=3;"
        };

        private static string? _workingConnectionString;

        public static string? GetConnectionString()
        {
            if (_workingConnectionString != null)
                return _workingConnectionString;

            foreach (var connStr in ConnectionCandidates)
            {
                try
                {
                    using var conn = new SqlConnection(connStr);
                    conn.Open();
                    _workingConnectionString = connStr;
                    return _workingConnectionString;
                }
                catch
                {
                    // Try master connection to see if server exists but DB is not yet created
                    string masterConnStr = connStr.Replace($"Database={DatabaseName};", "Database=master;");
                    try
                    {
                        using var masterConn = new SqlConnection(masterConnStr);
                        masterConn.Open();
                        _workingConnectionString = connStr;
                        return _workingConnectionString;
                    }
                    catch
                    {
                        // Continue to next candidate
                    }
                }
            }

            return null; // Null indicates SSMS server offline; app will use in-memory fallback smoothly
        }
    }
}
