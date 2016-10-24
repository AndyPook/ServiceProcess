using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Reflection;
using System.Diagnostics;

namespace Pook.ServiceProcess
{
	public class ServiceConfig
	{
		public enum ServiceStartMode
		{
			Automatic,
			Delayed,
			Manual
		}

		/// <summary>
		/// Initializes a config with a default name (The name of the exe)
		/// </summary>
		/// <returns></returns>
		public static ServiceConfig Create()
		{
			string serviceName = Process.GetCurrentProcess().ProcessName;
			return Create(serviceName);
		}

		/// <summary>
		/// Initializes a config with a specified name
		/// </summary>
		/// <param name="serviceName">The desired name of the service</param>
		/// <param name="formatArgs">A list of parts to insert into the serviceName if it is a template</param>
		/// <returns></returns>
		public static ServiceConfig Create(string serviceName, params object[] formatArgs)
		{
			return new ServiceConfig().WithServiceName(serviceName, formatArgs);
		}

		private ServiceConfig()
		{
			RunAsLocalSystem();
		}

		private readonly List<string> serviceArgs = new List<string>();

		internal string ServiceName { get; private set; }
		internal string DisplayName { get; private set; }
		internal string Description { get; private set; }
		internal string[] ServicesDependedOn { get; private set; }

		public IEnumerable<string> ServiceArgs => serviceArgs;
		internal ServiceStartMode StartMode { get; private set; }

		public ProcessPriorityClass Priority { get; private set; }

		private string executablePath;
		public string ExePath
		{
			get { return executablePath ?? Assembly.GetEntryAssembly().Location; }
			private set { executablePath = value; }
		}

		private Func<ServiceBase> serviceFactory;

		public ServiceBase CreateService()
		{
			var svc = serviceFactory();
			svc.ServiceName = ServiceName;
			return svc;
		}

		internal ServiceAccount Account { get; private set; }
		internal string Username { get; private set; }
		internal string Password { get; private set; }

		/// <summary>
		/// Set the name of the service
		/// </summary>
		/// <param name="serviceName">The desired name of the service</param>
		/// <param name="formatArgs">A list of parts to insert into the serviceName if it is a template</param>
		/// <returns></returns>
		public ServiceConfig WithServiceName(string serviceName, params object[] formatArgs)
		{
			if (formatArgs == null || formatArgs.Length == 0)
				ServiceName = serviceName;
			else
				ServiceName = string.Format(serviceName, formatArgs);
			return this;
		}
		/// <summary>
		/// Set the display name of the service
		/// </summary>
		/// <param name="displayName">The desired display name of the service</param>
		/// <returns></returns>
		public ServiceConfig WithDisplayName(string displayName)
		{
			DisplayName = displayName;
			return this;
		}
		/// <summary>
		/// Set the display name of the service
		/// </summary>
		/// <param name="description">The desired description of the service</param>
		/// <returns></returns>
		public ServiceConfig WithDescription(string description)
		{
			Description = description;
			return this;
		}

		/// <summary>
		/// Add a set of arguments required on service start
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns></returns>
		public ServiceConfig WithArguments(IEnumerable<string> args)
		{
			serviceArgs.AddRange(args);
			return this;
		}
		/// <summary>
		/// Add a single argument required to start the service
		/// </summary>
		/// <param name="arg">An argument</param>
		/// <returns></returns>
		public ServiceConfig WithArgument(string arg)
		{
			serviceArgs.Add(arg);
			return this;
		}

		/// <summary>
		/// Add dependencies on other services
		/// </summary>
		/// <param name="dependencies">Other service names</param>
		/// <returns></returns>
		public ServiceConfig WithDependencies(params string[] dependencies)
		{
			if (ServicesDependedOn == null || ServicesDependedOn.Length == 0)
				ServicesDependedOn = dependencies;
			else
			{
				var newDependencies = dependencies.Where(d => !ServicesDependedOn.Contains(d)).ToList();
				if (newDependencies.Any())
				{
					var current = new List<string>(ServicesDependedOn);
					current.AddRange(newDependencies);
					ServicesDependedOn = current.ToArray();
				}
			}
			return this;
		}

		public ServiceConfig WithBelowNormalPriority()
		{
			Priority = ProcessPriorityClass.BelowNormal;
			return this;
		}
		public ServiceConfig WithNormalPriority()
		{
			Priority = ProcessPriorityClass.Normal;
			return this;
		}
		public ServiceConfig WithHighPriority()
		{
			Priority = ProcessPriorityClass.High;
			return this;
		}
		public ServiceConfig WithPriority(ProcessPriorityClass priority)
		{
			Priority = priority;
			return this;
		}

		/// <summary>
		/// Remove an argument
		/// </summary>
		/// <param name="arg">An argument</param>
		/// <returns></returns>
		public ServiceConfig RemoveArg(string arg)
		{
			serviceArgs.Remove(arg);
			return this;
		}

		/// <summary>
		/// Sets the path the service should start.
		/// Only for use when installing the service from another process
		/// </summary>
		/// <param name="exePath">The path to the exe the service should start</param>
		/// <returns></returns>
		public ServiceConfig WithExe(string exePath)
		{
			ExePath = exePath;
			return this;
		}

		/// <summary>
		/// A function that will create the service instance
		/// </summary>
		/// <typeparam name="T">The type of the instance. Required to be IService or ServiceBase</typeparam>
		/// <param name="ctor">The factory function</param>
		/// <returns></returns>
		public ServiceConfig From<T>(Func<T> ctor) where T : class
		{
			if (typeof(ServiceBase).IsAssignableFrom(typeof(T)))
				serviceFactory = () => ctor() as ServiceBase;
			else
				serviceFactory = () => new ServiceBaseWrapper<T>(ctor);

			return this;
		}
		/// <summary>
		/// Defines the type of the required service instance.
		/// The class must have a default constructor.
		/// </summary>
		/// <typeparam name="T">The type of the instance. Required to be IService or ServiceBase</typeparam>
		/// <returns></returns>
		public ServiceConfig From<T>() where T : class, new()
		{
			return From(() => new T());
		}


		/// <summary>
		/// Configures the service to run as LocalSystem
		/// </summary>
		/// <returns></returns>
		public ServiceConfig RunAsLocalSystem()
		{
			Account = ServiceAccount.LocalSystem;
			Username = null;
			Password = null;
			return this;
		}
		/// <summary>
		/// Configures the service to run as LocalService
		/// </summary>
		/// <returns></returns>
		public ServiceConfig RunAsLocalService()
		{
			Account = ServiceAccount.LocalService;
			Username = null;
			Password = null;
			return this;
		}
		/// <summary>
		/// Configures the service to run as NetworkService
		/// </summary>
		/// <returns></returns>
		public ServiceConfig RunAsNetworkService()
		{
			Account = ServiceAccount.NetworkService;
			Username = null;
			Password = null;
			return this;
		}
		/// <summary>
		/// Configures the service to run as a named builtin account.
		/// <para>l, local, localsystem</para>
		/// <para>ls, localservice</para>
		/// <para>ns, network, networkservice</para>
		/// </summary>
		/// <param name="account"></param>
		/// <returns></returns>
		public ServiceConfig RunAs(string account)
		{
			if (string.IsNullOrWhiteSpace(account))
				return this;

			switch (account.ToLowerInvariant())
			{
				case "l":
				case "local":
				case "localsystem":
					RunAsLocalSystem();
					break;
				case "ls":
				case "localservice":
					RunAsLocalService();
					break;
				case "ns":
				case "network":
				case "networkservice":
					RunAsNetworkService();
					break;
				default:
					var parts = account.Split(';');
					if (parts.Length != 2)
						throw new ArgumentException("RunAsUser needs 'user;Password'");
					RunAsUser(parts[0], parts[1]);
					break;
			}

			return this;
		}
		/// <summary>
		/// Configures the service to run as a specified user
		/// </summary>
		/// <param name="username">The user name</param>
		/// <param name="password">The users password</param>
		/// <returns></returns>
		public ServiceConfig RunAsUser(string username, string password)
		{
			Account = ServiceAccount.User;
			Username = username;
			Password = password;
			return this;
		}

		/// <summary>
		/// Configures the service to be delayed-start
		/// </summary>
		/// <returns></returns>
		public ServiceConfig Start(ServiceStartMode startMode)
		{
			StartMode = startMode;
			return this;
		}
		/// <summary>
		/// Configures the service to be delayed-start
		/// </summary>
		/// <returns></returns>
		public ServiceConfig StartDelayed()
		{
			StartMode = ServiceStartMode.Delayed;
			return this;
		}
		/// <summary>
		/// Configures the service to start automatically
		/// </summary>
		/// <returns></returns>
		public ServiceConfig StartAutomatically()
		{
			StartMode = ServiceStartMode.Automatic;
			return this;
		}
		/// <summary>
		/// Configures the service to start manually
		/// </summary>
		/// <returns></returns>
		public ServiceConfig StartManually()
		{
			StartMode = ServiceStartMode.Manual;
			return this;
		}

		public bool IsAutomatic => StartMode == ServiceStartMode.Automatic || StartMode == ServiceStartMode.Delayed;
		public bool IsDelayedAutoStart => StartMode == ServiceStartMode.Delayed;

		/// <summary>
		/// Start the service
		/// </summary>
		/// <param name="args">Any required command line arguments</param>
		public void Start(params string[] args)
		{
			WithArguments(args);
			WindowsServiceHelper.Start(this);
		}

		/// <summary>
		/// Install the service
		/// </summary>
		public void Install()
		{
			WindowsServiceInstaller.InstallService(this);
		}
	}
}