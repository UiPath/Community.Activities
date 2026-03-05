using System.Collections.Generic;

namespace UiPath.FTP.Tests
{
    public class TestsHelper
    {
        //Used in order to retrieve the value of an out argument from a child (it needs to be public)
        public static void CopyObjects(IEnumerable<FtpObjectInfo> lstObjects, IList<FtpObjectInfo> newObjects)
        {
            newObjects.Clear();
            foreach (var obj in lstObjects)
                newObjects.Add(obj);
        }
    }
}
