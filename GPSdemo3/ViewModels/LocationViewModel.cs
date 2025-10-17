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

namespace GPSdemo3.ViewModels
{
    public class LocationViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private double? _latitude;
        private double? _longitude;
        private string _address;
        private string _statusMessage;

        // New fields for manual adding
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

        // Database-linked collection
        public ObservableCollection<LocationInfo> SavedLocations { get; set; } = new ObservableCollection<LocationInfo>();

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
            private set { if (_latitude == value) return; _latitude = value; OnPropertyChanged(nameof(Latitude)); OnPropertyChanged(nameof(DisplayLocation)); }
        }

        public double? Longitude
        {
            get => _longitude;
            private set { if (_longitude == value) return; _longitude = value; OnPropertyChanged(nameof(Longitude)); OnPropertyChanged(nameof(DisplayLocation)); }
        }

        public string Address
        {
            get => _address;
            private set { if (_address == value) return; _address = value; OnPropertyChanged(nameof(Address)); OnPropertyChanged(nameof(DisplayLocation)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); OnPropertyChanged(nameof(DisplayLocation)); }
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

        // New binding properties for Add Location
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

            LoadLocationsCommand = new Command(async () => await LoadLocationsAsync());
            AddLocationCommand = new Command(async () => await AddNewLocationAsync());
            DeleteLocationCommand = new Command<LocationInfo>(async (loc) => await DeleteLocationAsync(loc));
        }

        // Get user device location
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

        // Add new location manually
        private async Task AddNewLocationAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewLocationName) ||
                    string.IsNullOrWhiteSpace(NewLatitude) ||
                    string.IsNullOrWhiteSpace(NewLongitude))
                {
                    StatusMessage = "Please fill all fields.";
                    return;
                }

                if (!double.TryParse(NewLatitude, out double lat) || !double.TryParse(NewLongitude, out double lon))
                {
                    StatusMessage = "Invalid latitude or longitude.";
                    return;
                }

                var db = new DatabaseService();
                await db.SaveLocationAsync(NewLocationName, lat, lon);

                StatusMessage = $"Added '{NewLocationName}' successfully.";
                await LoadLocationsAsync();

                // Clear input fields
                NewLocationName = string.Empty;
                NewLatitude = string.Empty;
                NewLongitude = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error adding location: " + ex.Message;
            }
        }

        // Delete location from DB
        private async Task DeleteLocationAsync(LocationInfo location)
        {
            if (location == null) return;

            var db = new DatabaseService();
            await db.DeleteLocationAsync(location.Id);

            SavedLocations.Remove(location);
            StatusMessage = $"Deleted '{location.Name}'.";
        }

        // Load all locations from DB
        private async Task LoadLocationsAsync()
        {
            try
            {
                var db = new DatabaseService();
                var locations = await db.GetAllLocationsAsync();

                SavedLocations.Clear();
                foreach (var loc in locations)
                    SavedLocations.Add(loc);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading locations: " + ex.Message;
            }
        }

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

        private async Task<bool> EnsureLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }

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
