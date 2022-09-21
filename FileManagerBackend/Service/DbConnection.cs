using MySql.Data.MySqlClient;

namespace FileManagerBackend.Service
{
    public class DbConnection
    {
        public static IConfiguration Configuration;

        public static MySqlConnection GetDatabaseConnection(string DatabaseName)
        {
            return new MySqlConnection(Configuration["MySQLConnectionStrings:" + DatabaseName]);
        }
    }
}
