﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public sealed class ElaVariantPattern : ElaPattern
	{
		#region Construction
		internal ElaVariantPattern(Token tok) : base(tok, ElaNodeType.VariantPattern)
		{

		}


		public ElaVariantPattern() : base(ElaNodeType.VariantPattern)
		{

		}
		#endregion


		#region Methods
		internal override void ToString(StringBuilder sb, Fmt fmt)
		{
			sb.Append('`');
			sb.Append(Tag);

			if (Pattern != null)
			{
				sb.Append(' ');
				Format.PutInBraces(Pattern, sb, fmt);
			}
		}


		internal override bool CanFollow(ElaPattern pat)
		{
			if (pat.IsIrrefutable())
				return false;

			if (pat.Type == ElaNodeType.VariantPattern)
			{
				var var = (ElaVariantPattern)pat;

				if (var.Tag != Tag)
					return true;

				if (Pattern == null && var.Pattern == null)
					return false;

				if (Pattern == null)
					return true;

				return Pattern.CanFollow(var.Pattern);
			}

			if (Pattern != null)
				return Pattern.CanFollow(pat);

			return true;
		}
		#endregion


		#region Properties
		public string Tag { get; set; }

		public ElaPattern Pattern { get; set; }
		#endregion
	}
}
