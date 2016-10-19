using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ComponentModel;

namespace Pook.ServiceProcess
{
	public class WindowsServiceInstaller : Installer
	{
		private WindowsServiceInstaller(string serviceName)
			: this(ServiceConfig.Create(serviceName)) { }
		private WindowsServiceInstaller(ServiceConfig config)
		{
			InitializeComponent();
			InitFromConfig(config);
		}

		public static void UninstallService(string serviceName)
		{
			UninstallService(serviceName, Assembly.GetEntryAssembly().Location);
		}
		public static void UninstallService(string serviceName, string exePath)
		{
			try
			{
				using (var ti = new TransactedInstaller())
				{
					using (var pi = new WindowsServiceInstaller(serviceName))
					{
						ti.Installers.Add(pi);
						var ctx = new InstallContext();
						ctx.Parameters["assemblypath"] = $"\"{exePath}\"";
						ti.Context = ctx;
						ti.Uninstall(null);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine("*** Failed ***");
				Console.WriteLine(ex.Message);
			}
		}
		public static void InstallService(string serviceName)
		{
			InstallService(ServiceConfig.Create(serviceName));
		}
		public static void InstallService(ServiceConfig config)
		{
			try
			{
				using (var ti = new TransactedInstaller())
				{
					using (var wsi = new WindowsServiceInstaller(config))
					{
						ti.Installers.Add(wsi);
						var ctx = new InstallContext(null, config.ServiceArgs.ToArray());

						// Value may be changed by FixImagePath
						ctx.Parameters["assemblypath"] = $"{config.ExePath}";

						ti.Context = ctx;
						ti.Install(new Hashtable());
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine("*** Failed ***");
				Console.WriteLine(ex.Message);
			}
		}

		private ServiceProcessInstaller serviceProcessInstaller;
		private ServiceInstaller serviceInstaller;

		private void InitFromConfig(ServiceConfig config)
		{
			serviceProcessInstaller = new ServiceProcessInstaller
			{
				Account = config.Account,
				Password = config.Password,
				Username = config.Username
			};

			serviceInstaller = new ServiceInstaller
			{
				ServiceName = config.ServiceName,
				DisplayName = config.DisplayName ?? config.ServiceName,
				Description = config.Description ?? ("Artesian " + config.ServiceName),
				StartType = config.IsAutomatic ? ServiceStartMode.Automatic : ServiceStartMode.Manual,
				DelayedAutoStart = config.IsDelayedAutoStart,
				ServicesDependedOn = config.ServicesDependedOn
			};

			Installers.AddRange(
				new Installer[]
				{
			serviceProcessInstaller,
			serviceInstaller
				});
		}

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private readonly IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() { }

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
				components?.Dispose();

			serviceInstaller?.Dispose();
			serviceProcessInstaller?.Dispose();

			base.Dispose(disposing);
		}
	}
}