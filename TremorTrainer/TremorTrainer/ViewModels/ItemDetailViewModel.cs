using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Services;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class ItemDetailViewModel : BaseViewModel
    {
        private Guid _itemId;
        private string _text;
        private string _description;

        private readonly ISessionService _dataStore;
        public Guid Id { get; set; }

        public ItemDetailViewModel(ISessionService dataStore)
        {
            _dataStore = dataStore;
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public Guid ItemId
        {
            get
            {
                return _itemId;
            }
            set
            {
                _itemId = value;
                LoadItemId(value);
            }
        }

        public async void LoadItemId(Guid itemId)
        {
            try
            {
                var item = await _dataStore.GetItemAsync(itemId);
                Id = item.Id;
                Text = item.Text;
                Description = item.Description;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }
    }
}
