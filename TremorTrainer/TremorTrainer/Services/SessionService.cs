using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Repositories;

namespace TremorTrainer.Services
{
    public class SessionService : ISessionService
    {
        readonly List<Session> _items;
        private ISessionRepository _sessionRepository;
        private IMessageService _messageService;

        public SessionService(ISessionRepository sessionRepository, IMessageService messageService)
        {
            _sessionRepository = sessionRepository;
            _messageService = messageService;
            _items = _sessionRepository.GetSessions();
        }

        public async Task<bool> AddItemAsync(Session newItem)
        {
            _items.Add(newItem);
            var result = _sessionRepository.AddSession(newItem);

            if (result > 0)
            {
                return await Task.FromResult(true);
            }
            else
            {
                string errorMessage = $"Error: {Constants.UnknownErrorMessage} Details: Unable to save session to database.";
                await _messageService.ShowAsync(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        public async Task<bool> UpdateItemAsync(Session item)
        {
            var oldItem = _items.FirstOrDefault((Session arg) => arg.Id == item.Id);
            _items.Remove(oldItem);
            _items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            var oldItem = _items.Where((Session arg) => arg.Id == id).FirstOrDefault();
            _items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<Session> GetItemAsync(Guid id)
        {
            return await Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<Session>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(_items);
        }

    }

    public interface ISessionService
    {
        Task<bool> AddItemAsync(Session newItem);
        Task<Session> GetItemAsync(Guid itemId);
        Task<IEnumerable<Session>> GetItemsAsync(bool forceRefresh = false);
    }
}