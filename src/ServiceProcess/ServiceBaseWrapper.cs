using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Reflection;
using System.Threading.Tasks;

namespace Pook.ServiceProcess
{
	public class ServiceBaseWrapper<T> : ServiceBase where T : class
	{
		public ServiceBaseWrapper(Func<T> instanceFactory, Action<T> start = null, Action<T> stop = null)
		{
			if (instanceFactory == null)
				throw new ArgumentNullException(nameof(instanceFactory));
			this.instanceFactory = instanceFactory;

            StartAction = start ?? GetMethod("Start");
            StopAction = stop ?? GetMethod("Stop");
            WithArgsAction = GetWithArgsMethod();
        }

        private readonly Func<T> instanceFactory;
        private T instance;

        private static Action<T> GetMethod(string name)
        {
            // Start/Stop methods must not have parameters
            // if method is incompatible, return Action that Trace's a message rather than throw. This provides nicer reporting.

            Type type = typeof(T);
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                Trace.WriteLine("Service method NOT found: " + name);
                return i => Trace.Fail("Method not found " + typeof(T).Name + "." + name);
            }

            var parameters = method.GetParameters();

            if (parameters.Length > 0)
                return i => Trace.Fail("Cannot invoke " + typeof(T).Name + "." + name + " because it has parameters");

            return i => method.Invoke(i, null);
        }

        private static Action<T, IEnumerable<string>> GetWithArgsMethod()
        {
            Type type = typeof(T);
            var withArgs = type.GetMethod("WithArgs", new[] { typeof(IEnumerable<string>) });
            if (withArgs == null)
                return null;

            return (i, args) => withArgs.Invoke(i, new object[] { args });
        }

        public Action<T> StartAction { get; }
        public Action<T> StopAction { get; }
        public Action<T, IEnumerable<string>> WithArgsAction { get; }

        public void Start(string[] args)
        {
            OnStart(args);
        }

        private string GetVersion()
        {
            var assy = Assembly.GetEntryAssembly();
            if (assy == null)
                return string.Empty;

            // try get InformationalVersion as this supports full SemVer 
            object[] infoVerAttributes = assy.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (infoVerAttributes.Length > 0)
                return ((AssemblyInformationalVersionAttribute)infoVerAttributes[0]).InformationalVersion;

            // fall back to FileVersion 
            object[] fileVerAttributes = assy.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            if (fileVerAttributes.Length > 0)
                return ((AssemblyFileVersionAttribute)fileVerAttributes[0]).Version;

            return string.Empty;
        }

        protected override void OnStart(string[] args)
        {
            Trace.WriteLine("Service: " + ServiceName + " (" + typeof(T).Name + ") " + GetVersion());
            Trace.WriteLine("Starting...");

            args = IncludeCommandLineArgs(args).ToArray();
            instance = instanceFactory();

            if (args.Length > 0)
            {
                Trace.WriteLine("Service: arguments: " + string.Join(" ", args));

                if (WithArgsAction == null)
                    Trace.TraceError("No WithArgs method to process arguments");
                else
                    WithArgsAction(instance, args);
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    StartAction(instance);
                    Trace.WriteLine("Started");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("ServiceStart FAILED: " + ex.MessageAggregator());
                    ExitCode = 1;
                    Stop();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private bool stopped;
        protected override void OnStop()
        {
            if (stopped)
                return;
            stopped = true;

            StopAction(instance);
        }

        /// <summary>
        /// Combine service args with args provided on command line
        /// </summary>
        /// <param name="first"></param>
        /// <returns></returns>
        private IEnumerable<string> IncludeCommandLineArgs(IEnumerable<string> first)
        {
            var args = new HashSet<string>(Environment.GetCommandLineArgs().Skip(1));
            args.UnionWith(first);
            return args;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (instance == null)
                return;

            if (!stopped)
                StopAction(instance);
            stopped = true;

			var disposableService = instance as IDisposable;
			disposableService?.Dispose();
		}
	}
}