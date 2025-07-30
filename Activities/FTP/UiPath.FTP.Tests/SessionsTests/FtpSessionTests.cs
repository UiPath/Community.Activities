using System;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class FtpSessionTests
    {
        [Fact]
        public void FTP_TestConnect()
        {
            //Maybe to initialize a in-memory FTP Server using FubarDev.FtpServer??
            IFtpSession session = new FtpSession(new FtpConfiguration("ToThrow"), default);
            Assert.ThrowsAny<Exception>(session.Open);
        }
    }
}
