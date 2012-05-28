﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public sealed class ElaLiteralPattern : ElaPattern
	{
		#region Construction
		internal ElaLiteralPattern(Token tok) : base(tok, ElaNodeType.LiteralPattern)
		{

		}


		internal ElaLiteralPattern() : base(ElaNodeType.LiteralPattern)
		{

		}
		#endregion
		

		#region Methods
		internal override void ToString(StringBuilder sb, Fmt fmt)
		{
			sb.Append(Value);
		}


		internal override bool CanFollow(ElaPattern pat)
		{
			if (pat.IsIrrefutable())
				return false;

			if (pat.Type == ElaNodeType.LiteralPattern)
			{
				var lit = (ElaLiteralPattern)pat;
				return !Value.Equals(lit.Value);				
			}

			return true;
		}
		#endregion


		#region Properties
		public ElaLiteralValue Value { get; set; }
		#endregion
	}
}