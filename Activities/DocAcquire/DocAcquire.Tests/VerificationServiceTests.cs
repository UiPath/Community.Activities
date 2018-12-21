using Xunit;

namespace DocAcquire.Tests
{
    public class VerificationServiceTests
    {
        [Fact]
        public void TestGetVerifiedDataAsync()
        {
            //arrange
            var activity = new VerificationService();

            //act
            var result = activity.GetVerifiedDataAsync(
                new int[] { /*Document Ids will go here*/ },
               "<AUTH TOKEN HERE>"
               , "http://localhost/" //DocAcquire Service Base Url
               ).Result;


            //assert
            //Assert.True(result.Count > 0);
        }
    }
}
