using Pook.ServiceProcess;
using System.Diagnostics;

namespace SampleService
{
	class Program
	{
		static void Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());

			ServiceConfig
				.Create("SampleService")
				.From<SampleServiceHost>()
				.Start(args);
		}
	}
}