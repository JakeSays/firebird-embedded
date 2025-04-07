using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;


// ReSharper disable UnusedMember.Global
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global


namespace Std.CommandLine.Utility
{
	/// <summary>
	/// Various methods for runtime validation of method arguments
	/// </summary>
	[UsedImplicitly]
	public static class Guard
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[AssertionMethod]
		public static TValue NotNull<TValue>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)]
			TValue arg,
			[InvokerParameterName] string argName)
		where TValue : class
		{
			if (arg == null)
			{
				throw new ArgumentNullException(argName);
			}

			return arg;
		}

		[AssertionMethod]
		public static void NotNull([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string arg, [InvokerParameterName] string argName)
		{
			if (arg == null)
			{
				throw new ArgumentNullException(argName);
			}
		}

		[AssertionMethod]
		[ContractAnnotation("arg:null => halt")]
        public static void NotNull(object arg, [InvokerParameterName] string argName)
		{
			if (arg == null)
			{
				throw new ArgumentNullException(argName);
			}
		}

		[AssertionMethod]
		public static void NotNullOrEmpty([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string arg, [InvokerParameterName] string argName)
		{
			if (string.IsNullOrEmpty(arg))
			{
				throw new ArgumentNullException(argName);
			}
		}

		[AssertionMethod]
		public static void True([AssertionCondition(AssertionConditionType.IS_TRUE)] bool test, [InvokerParameterName] string argName)
		{
			if (!test)
			{
				throw new ArgumentException("Invalid Argument", argName);
			}
		}

		[AssertionMethod]
		public static void True([AssertionCondition(AssertionConditionType.IS_TRUE)] Func<bool> test, [InvokerParameterName] string argName)
		{
			if (!test())
			{
				throw new ArgumentException("Invalid Argument", argName);
			}
		}

		[AssertionMethod]
		public static void True([AssertionCondition(AssertionConditionType.IS_TRUE)] Func<bool> test,
			[InvokerParameterName] string argName, string message)
		{
			if (!test())
			{
				throw new ArgumentException(argName, message);
			}
		}

		public static void Equal(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg != test)
			{
				throw new ArgumentException(argName, $"{argName} != {test}");
			}
		}

		public static void Equal(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg != test)
			{
				throw new ArgumentException(argName, $"{argName} != {test}");
			}
		}

		public static void Equal(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg != test)
			{
				throw new ArgumentException(argName, $"{argName} != {test}");
			}
		}

		public static void Equal(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg != test)
			{
				throw new ArgumentException(argName, $"{argName} != {test}");
			}
		}

		public static void Equal(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg != test)
			{
				throw new ArgumentException(argName, $"{argName} != {test}");
			}
		}

		public static void NotEqual(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg == test)
			{
				throw new ArgumentException(argName, $"{argName} == {test}");
			}
		}

		public static void NotEqual(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg == test)
			{
				throw new ArgumentException(argName, $"{argName} == {test}");
			}
		}

		public static void NotEqual(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg == test)
			{
				throw new ArgumentException(argName, $"{argName} == {test}");
			}
		}

		public static void NotEqual(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg == test)
			{
				throw new ArgumentException(argName, $"{argName} == {test}");
			}
		}

		public static void NotEqual(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg == test)
			{
				throw new ArgumentException(argName, $"{argName} == {test}");
			}
		}

		public static void Greater(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg <= test)
			{
				throw new ArgumentException(argName, $"{argName} <= {test}");
			}
		}

		public static void Greater(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg <= test)
			{
				throw new ArgumentException(argName, $"{argName} <= {test}");
			}
		}

		public static void Greater(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg <= test)
			{
				throw new ArgumentException(argName, $"{argName} <= {test}");
			}
		}

		public static void Greater(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg <= test)
			{
				throw new ArgumentException(argName, $"{argName} <= {test}");
			}
		}

		public static void Greater(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg <= test)
			{
				throw new ArgumentException(argName, $"{argName} <= {test}");
			}
		}

		public static void GreaterEqual(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg < test)
			{
				throw new ArgumentException(argName, $"{argName} < {test}");
			}
		}

		public static void GreaterEqual(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg < test)
			{
				throw new ArgumentException(argName, $"{argName} < {test}");
			}
		}

		public static void GreaterEqual(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg < test)
			{
				throw new ArgumentException(argName, $"{argName} < {test}");
			}
		}

		public static void GreaterEqual(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg < test)
			{
				throw new ArgumentException(argName, $"{argName} < {test}");
			}
		}

		public static void GreaterEqual(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg < test)
			{
				throw new ArgumentException(argName, $"{argName} < {test}");
			}
		}

		public static void Less(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg >= test)
			{
				throw new ArgumentException(argName, $"{argName} >= {test}");
			}
		}

		public static void Less(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg >= test)
			{
				throw new ArgumentException(argName, $"{argName} >= {test}");
			}
		}

		public static void Less(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg >= test)
			{
				throw new ArgumentException(argName, $"{argName} >= {test}");
			}
		}

		public static void Less(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg >= test)
			{
				throw new ArgumentException(argName, $"{argName} >= {test}");
			}
		}

		public static void Less(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg >= test)
			{
				throw new ArgumentException(argName, $"{argName} >= {test}");
			}
		}

		public static void LessEqual(short arg, short test, [InvokerParameterName] string argName)
		{
			if (arg > test)
			{
				throw new ArgumentException(argName, $"{argName} > {test}");
			}
		}

		public static void LessEqual(int arg, int test, [InvokerParameterName] string argName)
		{
			if (arg > test)
			{
				throw new ArgumentException(argName, $"{argName} > {test}");
			}
		}

		public static void LessEqual(long arg, long test, [InvokerParameterName] string argName)
		{
			if (arg > test)
			{
				throw new ArgumentException(argName, $"{argName} > {test}");
			}
		}

		public static void LessEqual(decimal arg, decimal test, [InvokerParameterName] string argName)
		{
			if (arg > test)
			{
				throw new ArgumentException(argName, $"{argName} > {test}");
			}
		}

		public static void LessEqual(DateTime arg, DateTime test, [InvokerParameterName] string argName)
		{
			if (arg > test)
			{
				throw new ArgumentException(argName, $"{argName} > {test}");
			}
		}

        [AssertionMethod]
		public static void NotEmpty<TElement>([AssertionCondition(AssertionConditionType.IS_NOT_NULL)] ICollection<TElement> list, [InvokerParameterName] string argName)
		{
			if (list == null ||
				list.Count == 0)
			{
				throw new ArgumentException("Cannot be empty", argName);
			}
		}

		public static void Optional<T>(T obj)
		{
			//does nothing on purpose..
		}

		[ContractAnnotation("=> halt")]
		public static void NeverReached()
		{
			throw new InvalidOperationException("Unreachable code executed");
		}
	}
}
