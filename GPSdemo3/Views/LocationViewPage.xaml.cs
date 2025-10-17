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
            BindingContext ??= new LocationViewModel(); // Ensure binding
        }

        private void OnLocationButtonClicked(object sender, EventArgs e)
        {
            if (BindingContext is LocationViewModel vm && vm.GetLocationCommand.CanExecute(null))
                vm.GetLocationCommand.Execute(null);
        }
    }
}