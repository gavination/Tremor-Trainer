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
        readonly List<Session> items;
        private ISessionRepository _sessionRepository;

        public SessionService(ISessionRepository sessionRepository)
        {
            items = new List<Session>()
            {
                new Session { Id = Guid.NewGuid(), Text = "First item", Description="This is an item description." },
                new Session { Id = Guid.NewGuid(), Text = "Second item", Description="This is an item description." },
                new Session { Id = Guid.NewGuid(), Text = "Third item", Description="This is an item description." },
                new Session { Id = Guid.NewGuid(), Text = "Fourth item", Description="This is an item description." },
                new Session { Id = Guid.NewGuid(), Text = "Fifth item", Description="This is an item description." },
                new Session { Id = Guid.NewGuid(), Text = "Sixth item", Description="This is an item description." }
            };

            _sessionRepository = sessionRepository;
        }

        public async Task<bool> AddItemAsync(Session newItem)
        {
            items.Add(newItem);
            var result = await _sessionRepository.AddSession(newItem);

            if (result > 0)
            {
                return await Task.FromResult(true);
            }
            else
            {
                throw new Exception();
            }
        }

        public async Task<bool> UpdateItemAsync(Session item)
        {
            var oldItem = items.FirstOrDefault((Session arg) => arg.Id == item.Id);
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            var oldItem = items.Where((Session arg) => arg.Id == id).FirstOrDefault();
            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<Session> GetItemAsync(Guid id)
        {
            return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<Session>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }

    }

    public interface ISessionService
    {
        Task<bool> AddItemAsync(Session newItem);
        Task<Session> GetItemAsync(Guid itemId);
        Task<IEnumerable<Session>> GetItemsAsync(bool forceRefresh = false);
    }
}