namespace GPSdemo3.Models
{
    public class LocationModel
    {
        public int Id { get; set; }              // Optional: primary key
        public string Name { get; set; }         // Location name
        public double Latitude { get; set; }     // Coordinates
        public double Longitude { get; set; }    // Coordinates
        public DateTime Timestamp { get; set; }  // When the record was saved
    }
}
