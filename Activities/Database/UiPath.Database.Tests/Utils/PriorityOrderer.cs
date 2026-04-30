using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace UiPath.Database.Tests.Utils
{
    public class PriorityOrderer : ITestCaseOrderer
    {
        // CA1822 suppressed: method is an ITestCaseOrderer interface implementation and cannot be static
#pragma warning disable CA1822
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
            => OrderTestCasesCore(testCases);
#pragma warning restore CA1822

        private static IEnumerable<TTestCase> OrderTestCasesCore<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            var sorted = new SortedDictionary<int, List<TTestCase>>();

            foreach (var testCase in testCases)
            {
                var attr = testCase.TestMethod.Method
                    .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName)
                    .FirstOrDefault();

                int priority = attr?.GetNamedArgument<int>("Priority") ?? 0;

                if (!sorted.TryGetValue(priority, out var list))
                    sorted[priority] = list = new List<TTestCase>();

                list.Add(testCase);
            }

            return sorted.Values.SelectMany(x => x);
        }
    }
}
