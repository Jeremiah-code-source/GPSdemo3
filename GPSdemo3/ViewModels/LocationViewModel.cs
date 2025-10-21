using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using GPSdemo3.Configuration;
using GPSdemo3.Services;
using GPSdemo3.Models;

namespace GPSdemo3.ViewModels
{
    public class LocationViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private double? _latitude;
        private double? _longitude;
        private string _address;
        private string _statusMessage;

        // Fields for manual adding
        private string _newLocationName;
        private string _newLatitude;
        private string _newLongitude;

        public event PropertyChangedEventHandler PropertyChanged;

        // Commands
        public ICommand GetLocationCommand { get; }
        public ICommand RouteToTableMountainCommand { get; }
        public ICommand RouteToVAWaterfrontCommand { get; }
        public ICommand RouteToCapePointCommand { get; }
        public ICommand LoadLocationsCommand { get; }
        public ICommand AddLocationCommand { get; }
        public ICommand DeleteLocationCommand { get; }

        // Collection of saved locations from DB
        public ObservableCollection<LocationModel> SavedLocations { get; set; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                (GetLocationCommand as Command)?.ChangeCanExecute();
            }
        }

        public double? Latitude
        {
            get => _latitude;
            private set
            {
                if (_latitude == value) return;
                _latitude = value;
                OnPropertyChanged(nameof(Latitude));
                OnPropertyChanged(nameof(DisplayLocation));
            }
        }

        public double? Longitude
        {
            get => _longitude;
            private set
            {
                if (_longitude == value) return;
                _longitude = value;
                OnPropertyChanged(nameof(Longitude));
                OnPropertyChanged(nameof(DisplayLocation));
            }
        }

        public string Address
        {
            get => _address;
            private set
            {
                if (_address == value) return;
                _address = value;
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(DisplayLocation));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(DisplayLocation));
            }
        }

        public string DisplayLocation
        {
            get
            {
                if (IsBusy) return "Retrieving location...";
                if (!string.IsNullOrWhiteSpace(StatusMessage)) return StatusMessage;
                if (Latitude is null || Longitude is null) return "Tap 'My Location' to fetch.";

                if (!string.IsNullOrWhiteSpace(Address))
                    return $"{Address}\n({Latitude:0.0000}, {Longitude:0.0000})";

                return $"({Latitude:0.0000}, {Longitude:0.0000})";
            }
        }

        // Bound fields for Add Location form
        public string NewLocationName
        {
            get => _newLocationName;
            set { _newLocationName = value; OnPropertyChanged(nameof(NewLocationName)); }
        }

        public string NewLatitude
        {
            get => _newLatitude;
            set { _newLatitude = value; OnPropertyChanged(nameof(NewLatitude)); }
        }

        public string NewLongitude
        {
            get => _newLongitude;
            set { _newLongitude = value; OnPropertyChanged(nameof(NewLongitude)); }
        }

        // Constructor
        public LocationViewModel()
        {
            GetLocationCommand = new Command(async () => await GetLocationAsync(), () => !IsBusy);
            RouteToTableMountainCommand = new Command(
                async () => await OpenAzureMapsRouteAsync("Table Mountain Aerial Cableway", -33.9648, 18.4031),
                () => !IsBusy);
            RouteToVAWaterfrontCommand = new Command(
                async () => await OpenAzureMapsRouteAsync("V&A Waterfront", -33.9036, 18.4204),
                () => !IsBusy);
            RouteToCapePointCommand = new Command(
                async () => await OpenAzureMapsRouteAsync("Cape Point", -34.3568, 18.4975),
                () => !IsBusy);

            // FIXED: Proper async command initialization
            LoadLocationsCommand = new Command(async () => await LoadLocationsAsync());
            AddLocationCommand = new Command(async () => await AddNewLocationAsync());
            DeleteLocationCommand = new Command<LocationModel>(async (loc) => await DeleteLocationAsync(loc));

            // Load locations on startup
            Task.Run(async () => await LoadLocationsAsync());
        }

        // Get user's current device location
        private async Task GetLocationAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = string.Empty;
                Address = string.Empty;

                if (!await EnsureLocationPermissionAsync())
                    return;

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                Location location = null;

                try
                {
                    location = await Geolocation.GetLocationAsync(request);
                }
                catch (FeatureNotEnabledException)
                {
                    StatusMessage = "Location services disabled.";
                }
                catch
                {
                    StatusMessage = "Failed to get active location.";
                }

                if (location == null)
                    location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    StatusMessage = "Location not available.";
                    return;
                }

                Latitude = location.Latitude;
                Longitude = location.Longitude;

                await ReverseGeocodeAsync(location.Latitude, location.Longitude);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Add new location manually - FIXED VERSION
        private async Task AddNewLocationAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== AddNewLocationAsync STARTED ===");

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(NewLocationName))
                {
                    StatusMessage = "Please enter a location name.";
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter a location name.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewLatitude))
                {
                    StatusMessage = "Please enter latitude.";
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter latitude.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewLongitude))
                {
                    StatusMessage = "Please enter longitude.";
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Please enter longitude.", "OK");
                    return;
                }

                // Parse coordinates
                if (!double.TryParse(NewLatitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                {
                    StatusMessage = "Invalid latitude format.";
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Invalid latitude format. Use decimal numbers (e.g., -33.9248)", "OK");
                    return;
                }

                if (!double.TryParse(NewLongitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    StatusMessage = "Invalid longitude format.";
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Invalid longitude format. Use decimal numbers (e.g., 18.4241)", "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Parsed values - Name: {NewLocationName}, Lat: {lat}, Lon: {lon}");

                // Create new location
                var newLocation = new LocationModel
                {
                    Name = NewLocationName,
                    Latitude = lat,
                    Longitude = lon,
                    Timestamp = DateTime.Now
                };

                System.Diagnostics.Debug.WriteLine("Creating DatabaseService instance...");
                var db = new DatabaseService();

                System.Diagnostics.Debug.WriteLine("Calling SaveLocationAsync...");
                await db.SaveLocationAsync(newLocation);

                System.Diagnostics.Debug.WriteLine("Location saved successfully!");

                // Show success message
                StatusMessage = $"? Added '{NewLocationName}' successfully!";
                await Application.Current.MainPage.DisplayAlert("Success", $"Location '{NewLocationName}' has been saved!", "OK");

                // Reload locations
                await LoadLocationsAsync();

                // Clear input fields
                NewLocationName = string.Empty;
                NewLatitude = string.Empty;
                NewLongitude = string.Empty;

                System.Diagnostics.Debug.WriteLine("=== AddNewLocationAsync COMPLETED ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? ERROR in AddNewLocationAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                StatusMessage = $"Error: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add location:\n{ex.Message}", "OK");
            }
        }

        // Delete location from DB
        private async Task DeleteLocationAsync(LocationModel location)
        {
            if (location == null) return;

            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Delete",
                    $"Are you sure you want to delete '{location.Name}'?",
                    "Yes",
                    "No");

                if (!confirm) return;

                var db = new DatabaseService();
                await db.DeleteLocationAsync(location.Id);

                SavedLocations.Remove(location);
                StatusMessage = $"Deleted '{location.Name}'.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
                StatusMessage = $"Error deleting: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete location:\n{ex.Message}", "OK");
            }
        }

        // Load all locations from DB
        private async Task LoadLocationsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading locations from database...");

                var db = new DatabaseService();
                var locations = await db.GetAllLocationsAsync();

                SavedLocations.Clear();
                foreach (var loc in locations)
                {
                    SavedLocations.Add(loc);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {locations.Count} location(s) from database.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load locations error: {ex.Message}");
                StatusMessage = "Error loading locations: " + ex.Message;
            }
        }

        // Open route in Google Maps
        private async Task OpenAzureMapsRouteAsync(string destinationName, double destLat, double destLon)
        {
            try
            {
                if (Latitude is null || Longitude is null)
                    await GetLocationAsync();

                if (Latitude is null || Longitude is null)
                {
                    StatusMessage = "Unable to determine current location.";
                    return;
                }

                var uri = $"https://www.google.com/maps/dir/?api=1&origin={Latitude},{Longitude}&destination={destLat},{destLon}&travelmode=driving";
                await Launcher.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Route error: " + ex);
            }
        }

        // Location permission helper
        private async Task<bool> EnsureLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }

        // Reverse geocoding via Azure Maps
        private async Task ReverseGeocodeAsync(double lat, double lon)
        {
            try
            {
                using var http = new HttpClient();
                var url = $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&query={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}&subscription-key={AzureMapsConfig.SubscriptionKey}";
                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("addresses", out var addresses) &&
                    addresses.GetArrayLength() > 0)
                {
                    var addressElem = addresses[0].GetProperty("address");
                    if (addressElem.TryGetProperty("freeformAddress", out var ff))
                        Address = ff.GetString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Reverse geocode failed: " + ex);
            }
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}