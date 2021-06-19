using System.ComponentModel;
using Xamarin.Forms;
using TremorTrainer.ViewModels;

namespace TremorTrainer.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}