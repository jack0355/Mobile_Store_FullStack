using Microsoft.Data.SqlClient;

namespace Mobile_Store_Progressed.Data
{
    public class DataBaseConnection
    {
        private readonly string _connectionString;

        public DataBaseConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MobileStore")!;

        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
