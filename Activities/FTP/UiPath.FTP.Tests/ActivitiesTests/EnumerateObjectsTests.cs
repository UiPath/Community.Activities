using Moq;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.FTP.Activities;
using Xunit;

namespace UiPath.FTP.Tests.ActivitiesTests
{
    public class EnumerateObjectsTests
    {
        [Theory]
        [InlineData(FtpFilterObjectType.None)]
        [InlineData(FtpFilterObjectType.Directory)]
        [InlineData(FtpFilterObjectType.File)]
        [InlineData(FtpFilterObjectType.Other)]
        [InlineData(FtpFilterObjectType.Directory | FtpFilterObjectType.File)]
        [InlineData(FtpFilterObjectType.Directory | FtpFilterObjectType.File | FtpFilterObjectType.Link | FtpFilterObjectType.Other)]
        public void EnumerateObjects_FilterTests(FtpFilterObjectType filter)
        {
            var session = new Mock<IFtpSession>();
            SetupFtpMock();

            //the actual files returned
            var files = new List<FtpObjectInfo>();

            var filesVariable = new Variable<IEnumerable<FtpObjectInfo>>();
            var activity = new WithFtpSession(session.Object)
            {
                Host = new InArgument<string>("SomeHost"),
                Username = new InArgument<string>("SomeUsername"),
                Password = new InArgument<string>("SomePassword"),
            };

            if (activity.Body.Handler is Sequence mainSeq)
            {
                mainSeq.Variables.Add(filesVariable);
                var enumerateObjects = new EnumerateObjects()
                {
                    RemotePath = new InArgument<string>("."),
                    Filter = filter,
                    Files = new OutArgument<IEnumerable<FtpObjectInfo>>(filesVariable)
                };
                mainSeq.Activities.Add(enumerateObjects);
                //Do some trick here to retrieve the list of items returned
                mainSeq.Activities.Add(new InvokeMethod()
                {
                    TargetType = typeof(TestsHelper),
                    MethodName = nameof(TestsHelper.CopyObjects),
                    Parameters =
                    {
                        new InArgument<IEnumerable<FtpObjectInfo>>(ctx => filesVariable.Get(ctx)),
                        new InArgument<IList<FtpObjectInfo>>(ctx => files),
                    }
                });
            }

            var res = WorkflowInvoker.Invoke(activity, TimeSpan.FromSeconds(5));

            Verify();

            void Verify()
            {
                if ((filter & FtpFilterObjectType.Directory) != 0)
                    Assert.Equal(4, files.Where(x => x.Type == FtpObjectType.Directory).ToList().Count());
                else
                    Assert.Empty(files.Where(x => x.Type == FtpObjectType.Directory).ToList());
                
                if ((filter & FtpFilterObjectType.File) != 0)
                    Assert.Equal(3, files.Where(x => x.Type == FtpObjectType.File).Count());
                else
                    Assert.Empty(files.Where(x => x.Type == FtpObjectType.File).ToList());
                
                if ((filter & FtpFilterObjectType.Link) != 0)
                    Assert.Equal(2, files.Where(x => x.Type == FtpObjectType.Link).Count());
                else
                    Assert.Empty(files.Where(x => x.Type == FtpObjectType.Link).ToList());

                if ((filter & FtpFilterObjectType.Other) != 0)
                    Assert.Single(files, x => x.Type == FtpObjectType.Other);
                else
                    Assert.Empty(files.Where(x => x.Type == FtpObjectType.Other).ToList());
            }

            void SetupFtpMock()
            {
                IEnumerable<FtpObjectInfo> lstObjects = new FtpObjectInfo[]
                {
                    new FtpObjectInfo() {Name = "Directory1", Type = FtpObjectType.Directory},
                    new FtpObjectInfo() {Name = "Directory2", Type = FtpObjectType.Directory},
                    new FtpObjectInfo() {Name = "Directory3", Type = FtpObjectType.Directory},
                    new FtpObjectInfo() {Name = "Directory4", Type = FtpObjectType.Directory},
                    new FtpObjectInfo() {Name = "File1", Type = FtpObjectType.File},
                    new FtpObjectInfo() {Name = "File2", Type = FtpObjectType.File},
                    new FtpObjectInfo() {Name = "File3", Type = FtpObjectType.File},
                    new FtpObjectInfo() {Name = "Link1", Type = FtpObjectType.Link},
                    new FtpObjectInfo() {Name = "Link2", Type = FtpObjectType.Link},
                    new FtpObjectInfo() {Name = "Other1", Type = FtpObjectType.Other},
                };

                var resultTask = Task.FromResult(lstObjects);
                session.Setup(a => a.EnumerateObjectsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(resultTask);
            }
        }

        //If the filters will change, make sure to adjust the activity also
        [Fact]
        public void TestFilterOptions_EnumValues()
        {
            Assert.Equal(5, Enum.GetValues(typeof(FtpFilterObjectType)).Length);
        }
    }
}
