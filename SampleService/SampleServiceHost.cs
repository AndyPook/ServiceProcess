using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SampleService
{
	public class SampleServiceHost : IDisposable
	{
		IEnumerable<string> args;

		public void WithArgs(IEnumerable<string> args)
		{
			this.args = args;
		}

		public void Start()
		{
			Trace.WriteLine(string.Empty);
			Trace.WriteLine("Sample service started");
			if (args != null)
				Trace.WriteLine("args: " + string.Join(", ", args));
		}

		public void Stop()
		{
			Trace.WriteLine("Sample service stopped");
		}

		public void Dispose()
		{
			Trace.WriteLine("Sample service disposed");
		}
	}
}