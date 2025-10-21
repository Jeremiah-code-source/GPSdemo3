using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GPSdemo3.Models;


namespace GPSdemo3.Services
{
    public class DatabaseService
    {
        // <-- REPLACE this with your actual connection string
        private readonly string _connectionString =
        "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Kenan Prins\\MatricLearningDB.mdf;Integrated Security=True;Connect Timeout=30;";


        public async Task SaveLocationAsync(LocationModel location)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = "INSERT INTO Locations (Name, Latitude, Longitude) VALUES (@Name, @Latitude, @Longitude)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", location.Name);
                        cmd.Parameters.AddWithValue("@Latitude", location.Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", location.Longitude);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ {rows} row(s) inserted successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving location: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }



        public async Task<List<LocationModel>> GetAllLocationsAsync()
        {
            var list = new List<LocationModel>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string query = "SELECT Id, Name, Latitude, Longitude, Timestamp FROM Locations ORDER BY Id DESC";
                using var cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var info = new LocationModel
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
}
