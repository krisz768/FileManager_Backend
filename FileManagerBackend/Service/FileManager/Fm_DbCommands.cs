using FileManagerBackend.Model.FileManager;
using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection;

namespace FileManagerBackend.Service.FileManager
{
    public class Fm_DbCommands
    {
        private readonly MySqlConnection MySqlConn;

        public Fm_DbCommands ()
        {
            MySqlConn = DbConnection.GetDatabaseConnection("FileManager");
            MySqlConn.Open ();
        }

        private async void OpenConnection()
        {
            if (!MySqlConn.State.Equals(ConnectionState.Open))
            {
                await MySqlConn.OpenAsync();
            }
        }

        public async Task<Fm_User> GetUserByUsername(string Username)
        {  
            try
            {
                OpenConnection();

                Fm_User User = null;

                string CommandString = "SELECT * FROM `Users` WHERE `Username` = BINARY ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("username", Username);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        User = new Fm_User(Convert.ToInt32(reader["Id"]), reader["Username"].ToString(), reader["Password"].ToString(), reader["RootPath"].ToString());
                    }
                }

                return User;
            } catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<Fm_User> CreateShare()
        {
            try
            {
                OpenConnection();

                Fm_User User = null;

                string CommandString = "SELECT * FROM `Users` WHERE `Username` = BINARY ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("username", Username);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        User = new Fm_User(Convert.ToInt32(reader["Id"]), reader["Username"].ToString(), reader["Password"].ToString(), reader["RootPath"].ToString());
                    }
                }

                return User;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }
    }
}
