using System;
using Microsoft.Data.SqlClient;

namespace ERP.winforms.Database
{
    public static class DbConfig
    {
        public static string DatabaseName => "MorphicComputersDB";

        private static readonly string[] PossibleServers = new[]
        {
            "localhost",
            @".\SQLEXPRESS",
            @"(localdb)\MSSQLLocalDB"
        };

        private static string? _cachedConnectionString;

        public static string? GetConnectionString()
        {
            if (_cachedConnectionString != null)
                return _cachedConnectionString;

            foreach (var server in PossibleServers)
            {
                string testConnStr = $"Server={server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2;";
                try
                {
                    using (var conn = new SqlConnection(testConnStr))
                    {
                        conn.Open();
                        _cachedConnectionString = $"Server={server};Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=5;";
                        return _cachedConnectionString;
                    }
                }
                catch
                {
                    // Continue to next server candidate
                }
            }

            return null;
        }
    }
}
