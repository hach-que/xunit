using System;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Xunit.Sdk
{
#if PLATFORM_LINUX || PLATFORM_MACOS
    /// <summary>
    /// An implementation of <see cref="ISourceInformationProvider"/> that always returns no
    /// source information. Useful for test runners which don't need or cannot provide source
    /// information during discovery.
    /// </summary>
    public class NullMonoSourceInformationProvider : LongLivedMarshalByRefObject, ISourceInformationProvider
    {
        /// <inheritdoc/>
        public ISourceInformation GetSourceInformation(ITestCase testCase)
        {
            return new SourceInformation();
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
#endif

    /// <summary>
    /// This class proxies for the real implementation of <see cref="ITestFramework"/>, based on
    /// whether the user has overridden the choice via <see cref="TestFrameworkAttribute"/>. If
    /// no attribute is found, defaults to <see cref="XunitTestFramework"/>.
    /// </summary>
    public class TestFrameworkProxy : LongLivedMarshalByRefObject, ITestFramework
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestFrameworkProxy"/> class.
        /// </summary>
        /// <param name="testAssemblyObject">The test assembly (expected to implement <see cref="IAssemblyInfo"/>).</param>
        /// <param name="sourceInformationProviderObject">The source information provider (expected to implement <see cref="ISourceInformationProvider"/>).</param>
        public TestFrameworkProxy(object testAssemblyObject, object sourceInformationProviderObject)
        {
#if PLATFORM_LINUX || PLATFORM_MACOS
            InnerTestFramework = new XunitTestFramework();
            SourceInformationProvider =  new NullMonoSourceInformationProvider();
#else
            var testAssembly = (IAssemblyInfo)testAssemblyObject;
            var sourceInformationProvider = (ISourceInformationProvider)sourceInformationProviderObject;

            var testFrameworkType = typeof(XunitTestFramework);

            try
            {
                var testFrameworkAttr = testAssembly.GetCustomAttributes(typeof(ITestFrameworkAttribute)).FirstOrDefault();
                if (testFrameworkAttr != null)
                {
                    var discovererAttr = testFrameworkAttr.GetCustomAttributes(typeof(TestFrameworkDiscovererAttribute)).FirstOrDefault();
                    if (discovererAttr != null)
                    {
                        var discoverer = ExtensibilityPointFactory.GetTestFrameworkTypeDiscoverer(discovererAttr);
                        if (discoverer != null)
                            testFrameworkType = discoverer.GetTestFrameworkType(testFrameworkAttr);
                        // else                     // TODO: Log environmental error
                    }
                    // else                     // TODO: Log environmental error
                }
            }
            catch
            {
                // TODO: Log environmental error
            }

            try
            {
                InnerTestFramework = (ITestFramework)Activator.CreateInstance(testFrameworkType);
            }
            catch
            {
                // TODO: Log environmental error
                InnerTestFramework = new XunitTestFramework();
            }

            SourceInformationProvider = sourceInformationProvider;
#endif
        }

        /// <summary>
        /// Gets the test framework that's being wrapped by the proxy.
        /// </summary>
        public ITestFramework InnerTestFramework { get; private set; }

        /// <inheritdoc/>
        public ISourceInformationProvider SourceInformationProvider
        {
            set { InnerTestFramework.SourceInformationProvider = value; }
        }

        /// <inheritdoc/>
        public ITestFrameworkDiscoverer GetDiscoverer(IAssemblyInfo assembly)
        {
            return InnerTestFramework.GetDiscoverer(assembly);
        }

        /// <inheritdoc/>
        public ITestFrameworkExecutor GetExecutor(AssemblyName assemblyName)
        {
            return InnerTestFramework.GetExecutor(assemblyName);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            InnerTestFramework.Dispose();
        }
    }
}
