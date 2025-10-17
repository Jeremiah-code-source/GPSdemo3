using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace GPSdemo3.Services
{
    public class DatabaseService
    {
        // <-- REPLACE this with your actual connection string
        private readonly string _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=C:\\USERS\\KENAN PRINS\\MATRICLEARNINGDB.MDF;Trusted_Connection=True;TrustServerCertificate=True;";

        public async Task SaveLocationAsync(string name, double latitude, double longitude)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string query = "INSERT INTO Locations (Name, Latitude, Longitude) VALUES (@Name, @Latitude, @Longitude)";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Latitude", latitude);
                cmd.Parameters.AddWithValue("@Longitude", longitude);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveLocationAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LocationInfo>> GetAllLocationsAsync()
        {
            var list = new List<LocationInfo>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string query = "SELECT Id, Name, Latitude, Longitude, Timestamp FROM Locations ORDER BY Id DESC";
                using var cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var info = new LocationInfo
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Latitude = reader.IsDBNull(2) ? 0.0 : reader.GetDouble(2),
                        Longitude = reader.IsDBNull(3) ? 0.0 : reader.GetDouble(3),
                        Timestamp = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                    };
                    list.Add(info);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllLocationsAsync error: {ex.Message}");
                throw;
            }

            return list;
        }

        public async Task DeleteLocationAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string query = "DELETE FROM Locations WHERE Id = @Id";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteLocationAsync error: {ex.Message}");
                throw;
            }
        }
    }

    public class LocationInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}



//"Server=(localdb)\\MSSQLLocalDB;Database=C:\\USERS\\KENAN PRINS\\MATRICLEARNINGDB.MDF;Trusted_Connection=True;TrustServerCertificate=True;"