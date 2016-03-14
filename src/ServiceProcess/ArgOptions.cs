using System;
using System.Collections.Generic;
using System.Linq;

namespace Pook.ServiceProcess
{
	public class ArgOptions
	{
		public static ArgOptions With(IEnumerable<string> args)
		{
			return new ArgOptions(args);
		}

		public ArgOptions(IEnumerable<string> args)
		{
			parsedArgs = GetParsedArgs(args);
		}
		public ArgOptions(IEnumerable<KeyValuePair<string, string>> parsedArgs)
		{
			this.parsedArgs = GetParsedArgs(parsedArgs);
		}
		public ArgOptions(IEnumerable<ParsedArg> parsedArgs)
		{
			this.parsedArgs = parsedArgs.ToArray();
		}

		private readonly IEnumerable<ParsedArg> parsedArgs;
		private readonly Dictionary<string, Action<string>> optionActions = new Dictionary<string, Action<string>>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Parse a set of command line arguments into a set o KeyValuePairs
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static IEnumerable<ParsedArg> GetParsedArgs(params string[] args)
		{
			return GetParsedArgs((IEnumerable<string>)args);
		}

		/// <summary>
		/// Parse a set of command line arguments into a set o KeyValuePairs
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static IEnumerable<ParsedArg> GetParsedArgs(IEnumerable<string> args)
		{
			if (args == null)
				return Enumerable.Empty<ParsedArg>();

			return args.Select(x => new ParsedArg(x)).ToArray();
		}
		/// <summary>
		/// COnvert a set of KVP parsed args into <see cref="ParsedArg"/>s
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static IEnumerable<ParsedArg> GetParsedArgs(IEnumerable<KeyValuePair<string, string>> args)
		{
			if (args == null)
				return Enumerable.Empty<ParsedArg>();

			return args.Select(x => new ParsedArg(x)).ToArray();
		}

		/// <summary>
		/// Specify what should happen when the specified key is 
		/// </summary>
		/// <param name="key">arg name</param>
		/// <param name="action">What to do with the arg value</param>
		/// <param name="aliases">aliases that will also invoke the action</param>
		/// <returns>Fluent interface</returns>
		public ArgOptions On(string key, Action<string> action, params string[] aliases)
		{
			optionActions[key] = action;
			foreach (var alias in aliases)
				optionActions[alias] = action;
			return this;
		}

		/// <summary>
		/// Process each arg
		/// </summary>
		public void Execute()
		{
			foreach (var arg in parsedArgs)
			{
				Action<string> action;
				if (optionActions.TryGetValue(arg.Key, out action))
					action(arg.Value);
			}
		}

		public struct ParsedArg
		{
			public ParsedArg(string arg)
			{
				var parts = arg.Split('=');
				key = parts[0].Trim('\"');
				value = parts.Length == 2 ? parts[1].Trim('\"') : null;
			}
			public ParsedArg(string key, string value)
			{
				this.key = key;
				this.value = value;
			}
			public ParsedArg(KeyValuePair<string, string> arg)
			{
				key = arg.Key;
				value = arg.Value;
			}

			private readonly string key;
			private readonly string value;

			public string Key { get { return key; } }
			public string Value { get { return value; } }

			public override string ToString()
			{
				return key + (value == null ? string.Empty : "=" + value);
			}
		}
	}
}