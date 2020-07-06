using System.Threading.Tasks;
using Xunit;

namespace DocAcquire.Tests
{
    public class VerificationServiceTests
    {
        [Fact]
        public async void TestGetVerifiedDataAsync()
        {
            //arrange
            var activity = new VerificationService();

            //act
            var result = await activity.GetVerifiedDataAsync(
                new int[] { /*Document Ids will go here*/ },
               "<AUTH TOKEN HERE>"
               , "http://localhost/" //DocAcquire Service Base Url
               );


            //assert
            Assert.True(result.Count > 0);
        }
    }
}
