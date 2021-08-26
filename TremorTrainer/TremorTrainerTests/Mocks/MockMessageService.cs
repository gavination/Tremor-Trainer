using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Services;

namespace TremorTrainerTests.Mocks
{
    public class MockMessageService : Mock<IMessageService>
    {
        public MockMessageService ShowAsync()
        {
            Setup(x => x.ShowAsync(It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
            return this;
        }
    }
}
