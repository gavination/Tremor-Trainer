using Moq;
using System.Threading.Tasks;
using TremorTrainer.Services;

namespace TremorTrainerTests.Mocks
{
    public class MockTimerService : Mock<ITimerService>
    {
        public MockTimerService StartTimerAsync()
        {
            Setup(x => x.StartTimerAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            return this;
        }

        public MockTimerService StopTimerAsync()
        {
            Setup(x => x.StopTimerAsync())
                .Returns(Task.CompletedTask);
            return this;
        }
    }
}
