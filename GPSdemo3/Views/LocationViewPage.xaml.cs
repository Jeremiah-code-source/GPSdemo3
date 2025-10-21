using GPSdemo3.ViewModels;
using Microsoft.Maui.Controls;
using System;

namespace GPSdemo3.Views
{
    public partial class LocationViewPage : ContentPage
    {
        public LocationViewPage()
        {
            InitializeComponent();
            // Do NOT reset BindingContext here — it’s already set in XAML
        }

        private void OnLocationButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel vm && vm.GetLocationCommand.CanExecute(null))
                vm.GetLocationCommand.Execute(null);
        }
    }
}
