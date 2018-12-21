using Xunit;

namespace DocAcquire.Tests
{
    public class DocumentUploadServiceTests
    {
        [Fact]
        public void TestUploadAsync()
        {
            //arrange
            var activity = new DocumentUploadService();

            //act
            var result = activity.UploadAsync(
                new DataContracts.AttachmentItem {
                    Content = new byte[1] /*Replace with real file you want to extract data from in byte array*/,
                    Name ="Document Name"},
               "<AUTH TOKEN HERE>"
               , "http://localhost/" //DocAcquire Service Base Url
               ).Result;


            //assert
            Assert.True(result.DocumentId > 0);
            Assert.True(result.Success);

        }
    }
}
