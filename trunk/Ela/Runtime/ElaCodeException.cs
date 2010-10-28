﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ela.Debug;

namespace Ela.Runtime
{
	public sealed class ElaCodeException : ElaException
	{
		#region Construction
		private const string EXC_FORMAT = "{0}\r\n{1}";

		internal ElaCodeException(string message, ElaRuntimeError error, FileInfo file, int line, int col,
			IEnumerable<CallFrame> callStack) : base(message, null)
		{
			Error = new ElaMessage(message, MessageType.Error, (Int32)error, line, col);
			Error.File = file;
			CallStack = callStack;
		}
		#endregion


		#region Methods
		public override string ToString()
		{
			return String.Format(EXC_FORMAT,
				Error != null ? Error.ToString() : String.Empty,
				FormatCallStack(CallStack));
		}


		private string FormatCallStack(IEnumerable<CallFrame> points)
		{
			var sb = new StringBuilder();

			foreach (var s in points)
				sb.AppendLine(s.ToString());

			return sb.ToString();
		}
		#endregion


		#region Properties
		public ElaMessage Error { get; private set; }

		public IEnumerable<CallFrame> CallStack { get; private set; }
		#endregion
	}
}