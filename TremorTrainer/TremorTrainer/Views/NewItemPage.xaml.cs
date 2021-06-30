using System;
using System.Collections.Generic;
using System.ComponentModel;
using TremorTrainer.Models;
using TremorTrainer.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TremorTrainer.Views
{
    public partial class NewItemPage : ContentPage
    {
        public Item Item { get; set; }

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new NewItemViewModel();
        }
    }
}