﻿using System;
using System.Text;

namespace Ela.CodeModel
{
	internal static class Format
	{
        private static readonly char[] ops = new char[] { '!', '%', '&', '*', '+', '-', '.', ':', '/', '\\', '<', '=', '>', '?', '@', '^', '|', '~' };
        
        public static bool IsSymbolic(string name)
        {
            return name.IndexOfAny(ops) != -1;
        }
        
		public static string OperatorAsString(ElaOperator op)
		{
			switch (op)
			{
				case ElaOperator.BooleanAnd: return " and ";
				case ElaOperator.BooleanOr: return " or ";
				case ElaOperator.Sequence: return "$";
				default:
					return String.Empty;
			}
		}

		public static bool IsSimpleExpression(ElaExpression p)
		{
			return 
				p == null ||
				p.Type == ElaNodeType.NameReference ||
				p.Type == ElaNodeType.Primitive ||
				p.Type == ElaNodeType.ListLiteral ||
				p.Type == ElaNodeType.RecordLiteral ||
				p.Type == ElaNodeType.TupleLiteral ||
				p.Type == ElaNodeType.UnitLiteral;
		}

		public static bool IsHiddenVar(ElaExpression p)
		{
			return p != null && p.Type == ElaNodeType.NameReference &&
				((ElaNameReference)p).Name[0] == '$';
		}
        
		public static void PutInBraces(ElaExpression e, StringBuilder sb)
		{
			var simple = IsSimpleExpression(e);

			if (!simple)
			{
				sb.Append('(');
				e.ToString(sb, 0);
				sb.Append(')');
			}
			else
				e.ToString(sb, 0);				
		}
	}
}