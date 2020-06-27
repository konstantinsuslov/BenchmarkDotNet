using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Running
{
    // VS generates bad assembly binding redirects for ValueTuple for Full .NET Framework
    // we need to keep the logic that uses it in a separate method and create DirtyAssemblyResolveHelper first
    // so it can ignore the version mismatch ;)
    public static class BenchmarkRunner
    {
        [PublicAPI]
        public static Summary Run<T>(IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(typeof(T), config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, config, args));
        }

        [PublicAPI]
        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(type, methods, config, args));
        }

        [PublicAPI]
        public static Summary[] Run(Assembly assembly, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(assembly, config, args));
        }

        [PublicAPI]
        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(new[] { benchmarkRunInfo }).Single());
        }

        [PublicAPI]
        public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunWithDirtyAssemblyResolveHelper(benchmarkRunInfos));
        }

        [PublicAPI]
        public static Summary RunUrl(string url, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunUrlWithDirtyAssemblyResolveHelper(url, config, args));
        }

        [PublicAPI]
        public static Summary RunSource(string source, IConfig config = null, string[] args = null)
        {
            using (DirtyAssemblyResolveHelper.Create())
                return RunWithExceptionHandling(() => RunSourceWithDirtyAssemblyResolveHelper(source, config, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, IConfig config, string[] args = null)
        {
            var runInfo = BenchmarkConverter.TypeToBenchmarks(type, config, args);
            return runInfo == null ? null : BenchmarkRunnerClean.Run(new[] { runInfo }).Single();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunWithDirtyAssemblyResolveHelper(Type type, MethodInfo[] methods, IConfig config = null, string[] args = null)
        {
            var runInfo = BenchmarkConverter.MethodsToBenchmarks(type, methods, config, args);
            return runInfo == null ? null : BenchmarkRunnerClean.Run(new[] { runInfo }).Single();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(Assembly assembly, IConfig config = null, string[] args = null)
        {
            var runInfos = assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config, args));
            return BenchmarkRunnerClean.Run(runInfos.Where(i => i != null).ToArray());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary[] RunWithDirtyAssemblyResolveHelper(BenchmarkRunInfo[] benchmarkRunInfos)
            => BenchmarkRunnerClean.Run(benchmarkRunInfos);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunUrlWithDirtyAssemblyResolveHelper(string url, IConfig config = null, string[] args = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.UrlToBenchmarks(url, config, args)).Single()
                : throw new NotSupportedException("Supported only on Full .NET Framework");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Summary RunSourceWithDirtyAssemblyResolveHelper(string source, IConfig config = null, string[] args = null)
            => RuntimeInformation.IsFullFramework
                ? BenchmarkRunnerClean.Run(BenchmarkConverter.SourceToBenchmarks(source, config, args)).Single()
                : throw new NotSupportedException("Supported only on Full .NET Framework");

        private static Summary RunWithExceptionHandling(Func<Summary> run)
        {
            try
            {
                return run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return Summary.NothingToRun(e.Message, string.Empty, string.Empty);
            }
        }

        private static Summary[] RunWithExceptionHandling(Func<Summary[]> run)
        {
            try
            {
                return run();
            }
            catch (InvalidBenchmarkDeclarationException e)
            {
                ConsoleLogger.Default.WriteLineError(e.Message);
                return new[] { Summary.NothingToRun(e.Message, string.Empty, string.Empty) };
            }
        }
    }
}