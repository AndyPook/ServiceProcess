# ServiceProcess
Simple framework for Windows Services

# Build
Open solution in VisualStudio, build.

No dependencies or nuget packages required

# Details
To create a service
 * Create a "Console Application"
 * Add the following code
```csharp
    ServiceConfig
			.Create()
			.From<SampleServiceHost>()
			.Start(args);
```
See ```ServiceConfig``` for more options (such as; Delayed start; Naming; specifying user; priority; ...)

The ```SampleServiceHost``` class is a simple POCO with a couple of well-known methods
 * "Start()" run at service startup
 * "Stop()"  run at service shutdown
 * If the class implements IDisposable "Dispose()" will also be called
 * "WithArgs(IEnumerable<string>)" will be called before Start if any commandline args are provided
 
Serveral command line args are understood by this framework
 * "-c" run as a console app instead of service. 
 Very useful for dev and debugging. 
 Typically add "-c" as an arg in the "debug" tab of the project properties
 * "-i" install as a service
 * "-u" uninstall the service
 
See ```WindowsServiceHelper``` for more options.
