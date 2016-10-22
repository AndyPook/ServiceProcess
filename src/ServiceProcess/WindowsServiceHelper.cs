using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Pook.ServiceProcess
{
	internal class WindowsServiceHelper
	{
		private enum ServiceAction
		{
			Unknown = 0,
			RunAsService,
			RunAsConsole,
			Install,
			Uninstall
		}

		static WindowsServiceHelper()
		{
			Type serviceBaseType = typeof(ServiceBase);
			onStart = serviceBaseType.GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
			if (onStart == null)
				throw new MissingMethodException("OnStart");
		}

		private static readonly MethodInfo onStart;

		/// <summary>
		/// Start a ServiceBase with args
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="args"></param>
		public static void Start(ServiceBase svc, params string[] args)
		{
			Start(svc, (IEnumerable<string>)args);
		}

		/// <summary>
		/// Start a ServiceBase with args
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="args"></param>
		public static void Start(ServiceBase svc, IEnumerable<string> args)
		{
			try
			{
				onStart.Invoke(svc, new object[] { args.ToArray() });
			}
			catch (Exception ex)
			{
				Trace.TraceError("ServiceBase: ServiceStart FAILED: " + ex.MessageAggregator());
			}
		}

		public static void Stop(ServiceBase svc)
		{
			svc.Stop();
		}
		public static void Start(ServiceConfig config)
		{
			var action = ServiceAction.Unknown;

			ArgOptions
				.With(config.ServiceArgs)
				.On("-i", v => action = ServiceAction.Install)
				.On("-u", v => action = ServiceAction.Uninstall)
				.On("-c", v => action = ServiceAction.RunAsConsole)
				.On("-s", v => action = ServiceAction.RunAsService)
				.On("-start", v =>
				{
					switch (v)
					{
						case "a":
						case "auto":
							config.StartAutomatically();
							break;
						case "m":
						case "manual":
							config.StartManually();
							break;
						case "d":
						case "delayed":
							config.StartDelayed();
							break;
					}
				})
				.On("-priority", v =>
				{
					switch (v)
					{
						case "b":
						case "belownormal":
							config.WithBelowNormalPriority();
							break;
						case "n":
						case "normal":
							config.WithNormalPriority();
							break;
						case "h":
						case "high":
							config.WithHighPriority();
							break;
						default:
							ProcessPriorityClass p;
							if (Enum.TryParse(v, true, out p))
								config.WithPriority(p);
							break;
					}
				})
				.On("-runas", v => config.RunAs(v))
				.On("-w", v =>
				{
					if (string.IsNullOrEmpty(v))
					{
						Console.WriteLine("Press any key to continue...");
						Console.ReadKey();
					}
					else
					{
						int pause;
						if (int.TryParse(v, out pause))
						{
							Trace.WriteLine("Pausing " + pause + " seconds");
							Task.Delay(pause * 1000).Wait();
						}
						else
							Trace.WriteLine("Unrecognised value for 'wait': " + v);
					}
				})
				.On("-name", v =>
				{
					if (!string.IsNullOrEmpty(v))
						config.WithServiceName(v);
				})
				.On("-?", v =>
				{
					Console.WriteLine("Available options:");
					Console.WriteLine("  -u                             Uninstall the windows service");
					Console.WriteLine("  -i                             Install as a windows service");
					Console.WriteLine("  -c                             Run as a console app");
					Console.WriteLine("  -w[=seconds]                   Wait on newline (or seconds) before starting the service (allows for attaching debugger)");
					Console.WriteLine("  -priority=[high|belownormal]   Set the service priority");
					Console.WriteLine("  -start=[delayed|auto|manual]   Set the service start mode");
					Console.WriteLine("  -name=<someName>               Override the name of the service");
					Console.WriteLine("  [service args]                 Other arguments are passed to the service");
				})
				.Execute();

			if (action == ServiceAction.Unknown)
				action =
					Environment.UserInteractive ?
					ServiceAction.RunAsConsole :
					ServiceAction.RunAsService;

			switch (action)
			{
				case ServiceAction.RunAsConsole:
					RunAsConsole(config);
					break;

				case ServiceAction.Install:
					config.RemoveArg("-i");
					WindowsServiceInstaller.InstallService(config);
					break;
				case ServiceAction.Uninstall:
					WindowsServiceInstaller.UninstallService(config.ServiceName);
					break;

				case ServiceAction.RunAsService:
					var svc = config.CreateService();
					// ReSharper disable once AssignNullToNotNullAttribute
					Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					if (config.Priority != ProcessPriorityClass.Normal)
						Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
					ServiceBase.Run(svc);
					break;
			}
		}

		private static void RunAsConsole(ServiceConfig config)
		{
			var svc = config.CreateService();
			try
			{
				// We've already dealt with the args
				Start(svc, config.ServiceArgs);
				Console.WriteLine("Started...");
				Console.WriteLine("Press enter to exit:");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error starting service: {0}", ex.MessageAggregator());
				Console.WriteLine("Press enter to exit:");
				Console.ReadLine();
				return;
			}

			try
			{
				Stop(svc);
				var disposableService = svc as IDisposable;
				if (disposableService != null)
					disposableService.Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error stopping service: {0}", ex.MessageAggregator());
				Console.WriteLine("Press enter to exit:");
				Console.ReadLine();
			}
		}
	}
}