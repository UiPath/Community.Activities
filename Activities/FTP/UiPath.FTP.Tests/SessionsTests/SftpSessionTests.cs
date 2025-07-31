using System;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class SftpSessionTests
    {
        [Fact]
        public void SFTP_TestConnect()
        {
            IFtpSession session = new SftpSession(new FtpConfiguration("ToThrow") {Username = "Some", Password = "NotUsed", Host = "Some"});
            Assert.ThrowsAny<Exception>(session.Open);
        }
    }
}
