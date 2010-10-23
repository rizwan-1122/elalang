﻿using System;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public sealed class ElaAsyncLiteral : ElaFunctionLiteral
	{
		#region Construction
		internal ElaAsyncLiteral(Token tok) : base(tok, ElaNodeType.AsyncLiteral)
		{

		}


		public ElaAsyncLiteral() : base(ElaNodeType.AsyncLiteral)
		{

		}
		#endregion


		#region Properties
		public override int ParameterCount { get { return 0; } }
		#endregion
	}
}