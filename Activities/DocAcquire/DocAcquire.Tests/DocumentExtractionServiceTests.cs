﻿using Xunit;

namespace DocAcquire.Tests
{
    public class DocumentExtractionServiceTests
    {
        [Fact]
        public async void TestExtractAsync()
        {
            //arrange
            var activity = new DocumentExtractionService();

            //act
            var result = await activity.ExtractAsync(
                new DataContracts.AttachmentItem {
                    Content = new byte[1] /*Replace with real file you want to extract data from in byte array*/,
                    Name ="Document Name"},
               "<AUTH TOKEN HERE>"
               , "http://localhost/" //DocAcquire Service Base Url
               );


            //assert
            Assert.True(result.Result == DataContracts.ResultStatus.Success);
            Assert.True(result.Fields.Count > 0);

        }
    }
}
