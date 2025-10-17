using System;
using System.Globalization;

namespace GPSdemo3.Models
{
    /// <summary>
    /// Plain data container (Model) for location-related information.
    /// Keep UI logic in ViewModels; use this model to store/transfer location data.
    /// </summary>
    public class LocationInfo
    {
        /// <summary>
        /// Latitude in decimal degrees. Nullable until known.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude in decimal degrees. Nullable until known.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Human-readable address from reverse geocoding (if resolved).
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Optional status or error message associated with this sample.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// UTC timestamp for when this location snapshot was captured.
        /// </summary>
        public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Convenience: true if both latitude and longitude are available.
        /// </summary>
        public bool HasCoordinates => Latitude is not null && Longitude is not null;

        /// <summary>
        /// Convenience: coordinates formatted as "(lat, lon)" with 4 decimals, or empty if not available.
        /// </summary>
        public string FormattedCoordinates =>
            HasCoordinates
                ? $"({Latitude:0.0000}, {Longitude:0.0000})"
                : string.Empty;

        /// <summary>
        /// Returns a compact display string combining address (if present) and coordinates.
        /// </summary>
        public string DisplayText
        {
            get
            {
                // Show address + coords if available, otherwise just coords, otherwise any status.
                if (!string.IsNullOrWhiteSpace(Address) && HasCoordinates)
                    return $"{Address}\n{FormattedCoordinates}";

                if (HasCoordinates)
                    return FormattedCoordinates;

                if (!string.IsNullOrWhiteSpace(Address))
                    return Address;

                return StatusMessage ?? string.Empty;
            }
        }
        }
}