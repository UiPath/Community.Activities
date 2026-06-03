using System;
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

        // Used to capture a bool OutArgument value out of a WorkflowInvoker run.
        // The result array must have at least one element; result[0] is set to value.
        public static void CopyBool(bool value, bool[] result)
        {
            result[0] = value;
        }

        // Walks InnerException / AggregateException.InnerExceptions so callers can assert
        // on the original exception type even after WorkflowInvoker / async wrapping.
        public static IEnumerable<Exception> Flatten(Exception ex)
        {
            while (ex != null)
            {
                yield return ex;
                if (ex is AggregateException agg)
                {
                    foreach (var inner in agg.InnerExceptions)
                        foreach (var e in Flatten(inner))
                            yield return e;
                    yield break;
                }
                ex = ex.InnerException;
            }
        }
    }
}
