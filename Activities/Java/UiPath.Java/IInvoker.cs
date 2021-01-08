using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Java
{
    public interface IInvoker
    {
        Task StartJavaService(int timeout);

        Task LoadJar(string jarPath, CancellationToken ct);

        Task<JavaObject> InvokeMethod(string methodName, string className, JavaObject javaObject, List<object> parameters, List<Type> parametersTypes, CancellationToken ct);

        Task<JavaObject> InvokeConstructor(string className, List<object> parameters, List<Type> parametersTypes, CancellationToken ct);

        Task<JavaObject> InvokeGetField(JavaObject javaObject, string fieldName, string className, CancellationToken ct);

        Task StopJavaService();

        Task ReleaseAsync();
    }
}
