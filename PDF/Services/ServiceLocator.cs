using Docnet.Core;
using PDF.Interfaces;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace PDF.Services
{
    public sealed class ServiceLocator
    {
        private static readonly Lazy<ServiceLocator> _lazy =
            new(() => new ServiceLocator(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ServiceLocator Instance => _lazy.Value;

        private readonly ConcurrentDictionary<Type, object> _services = new();

        static ServiceLocator()
        {
            NativeLibrary.SetDllImportResolver(
                typeof(DocLib).Assembly,
                static (libraryName, assembly, searchPath) =>
                {
                    var directory = Path.GetDirectoryName(typeof(ServiceLocator).Assembly.Location)!;
                    NativeLibrary.TryLoad(Path.Combine(directory, libraryName + ".dll"), out var handle);
                    return handle;
                });
        }

        private ServiceLocator()
        {
            Register<IPdfRenderService>(new PdfRenderService());
            Register<IPdfFileRepository>(new PdfFileRepository());
        }

        public void Register<TService>(TService implementation) where TService : class
            => _services[typeof(TService)] = implementation;

        public TService Resolve<TService>() where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var service))
                return (TService)service;
            throw new InvalidOperationException(
                $"Service '{typeof(TService).FullName}' is not registered in {nameof(ServiceLocator)}.");
        }

        public bool TryResolve<TService>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TService? service)
            where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var obj))
            {
                service = (TService)obj;
                return true;
            }
            service = null;
            return false;
        }
    }
}
