﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public sealed class ElaDefaultPattern : ElaPattern
	{
		#region Construction
		internal ElaDefaultPattern(Token tok) : base(tok, ElaNodeType.DefaultPattern)
		{

		}


		public ElaDefaultPattern() : base(ElaNodeType.DefaultPattern)
		{

		}
		#endregion


		#region Methods
		internal override void ToString(StringBuilder sb, Fmt fmt)
		{
			sb.Append('_');
		}


		internal override bool IsIrrefutable()
		{
			return true;
		}


		internal override bool CanFollow(ElaPattern pat)
		{
			return !IsIrrefutable();
		}
		#endregion
	}
}
