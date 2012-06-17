﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Ela.CodeModel;
using System.Globalization;

namespace Ela.Parsing
{
	internal sealed partial class Parser
	{
		private static readonly ElaVariableReference hiddenVar = new ElaVariableReference { VariableName = "$0" };
		private static readonly ElaVariablePattern hiddenPattern = new ElaVariablePattern { Name = "$0" };
        private int oldKind = 0;

		#region Methods
		private bool RequireEndBlock()
		{
			if (la.kind == _EBLOCK)
				return true;

			if (la.kind == _PIPE)
				return false;

			if (la.kind == _ET || la.kind == _IN)
			{
				scanner.PopIndent();
				return false;
			}

			return true;
		}

        private void BuildMask(ref int count, ref int mask, string val, string targ)
        {
            var flag = 0;

            if (val == targ)
                flag = 1;
            else if (val != "_")
                AddError(ElaParserError.InvalidFunctionSignature, val);

            mask |= flag << count;
            count++;
        }

		private void SetObjectMetadata(ElaBinding varExp)
		{
			if (varExp.InitExpression != null)
			{
                if (varExp.InitExpression.Type == ElaNodeType.FunctionLiteral && varExp.VariableName != null)
				{
                    ((ElaFunctionLiteral)varExp.InitExpression).Name = varExp.VariableName;
					varExp.VariableFlags |= ElaVariableFlags.Function;
				}
                else if ((varExp.InitExpression.Type < ElaNodeType.FunctionLiteral || varExp.InitExpression.Type == ElaNodeType.Primitive) && varExp.VariableName != null)
					varExp.VariableFlags |= ElaVariableFlags.ObjectLiteral;
			}
		}


        private bool CheckFend(ElaBinding bin)
        {
            var and = la.kind != _EBLOCK && la.kind != _IN && (la.kind != _ident || la.val != bin.VariableName);

            if (!and)
            {
                oldKind = la.kind;
                la.kind = _EBLOCK;
            }

            return and;
        }


        private void ProcessFunctionParameters(ElaFunctionLiteral mi, Token ot, ElaBinding varExp)
        {
            if (mi.Body.Entries.Count > 1)
            {
                var patterns = default(List<ElaPattern>);
                var pars = 0;

                if (mi.Body.Entries[0].Pattern.Type != ElaNodeType.PatternGroup)
                    pars = 1;
                else
                {
                    patterns = ((ElaPatternGroup)mi.Body.Entries[0].Pattern).Patterns;
                    pars = patterns.Count;
                }

                var tp = new ElaTupleLiteral(ot);

                for (var i = 0; i < pars; i++)
                	tp.Parameters.Add(new ElaVariableReference(ot) { VariableName = "$" + i });
                
                mi.Body.Expression = tp;
            }
        }


		private ElaExpression GetOperatorFun(string op, ElaExpression left, ElaExpression right)
		{
            var fc = new ElaFunctionCall(t) {
                Target = new ElaVariableReference(t) { VariableName = op }
            };

            if (left != null)
                fc.Parameters.Add(left);
            else
                fc.FlipParameters = true;

            if (right != null)
                fc.Parameters.Add(right);

            return fc;
		}


		private ElaExpression GetPrefixFun(ElaExpression funexp, ElaExpression par, bool flip)
		{
			var fc = new ElaFunctionCall(t) { Target = funexp };
			fc.Parameters.Add(par);
			fc.FlipParameters = flip;
			return fc;
		}


		private ElaExpression GetPartialFun(ElaExpression exp)
		{
			var m = new ElaMatch { Expression = hiddenVar };
			
            if (exp != null)
                m.SetLinePragma(exp.Line, exp.Column);
			
            m.Entries.Add(new ElaMatchEntry { Expression = exp, Pattern = hiddenPattern });			
			var ret = new ElaFunctionLiteral() { Body = m };
			
            if (exp != null)
                ret.SetLinePragma(exp.Line, exp.Column);

			return ret;
		}

		private ElaLiteralValue ParseInt(string val)
		{
			return ParseInt(val, false);
		}


		private ElaLiteralValue ParseInt(string val, bool negate)
		{
			if (TrimLast(ref val, 'l', 'L'))
			{
				var res = default(Int64);
				
				if (!Int64.TryParse(val, out res))
				{
					try
					{
						res = Convert.ToInt64(val, 16);
					}
					catch 
					{
						AddError(ElaParserError.InvalidIntegerSyntax);
					}
				}
				
				return new ElaLiteralValue(negate ? -res : res);
			}
			else
			{
				var res = default(Int32);
				
				if (!Int32.TryParse(val, out res))
				{
					try
					{
						res = Convert.ToInt32(val, 16);
					}
					catch 
					{
						AddError(ElaParserError.InvalidIntegerSyntax);
					}
				}
				
				return new ElaLiteralValue(negate ? -res : res);
			}
		}


		private ElaLiteralValue ParseString(string val)
		{
			return new ElaLiteralValue(ReadString(val));
		}
		
		
		private ElaLiteralValue ParseChar(string val)
		{
			var str = ReadString(val);
			return new ElaLiteralValue(str[0]);
		}


		private ElaLiteralValue ParseReal(string val)
		{
			return ParseReal(val, false);
		}


		private ElaLiteralValue ParseReal(string val, bool negate)
		{
            if (TrimLast(ref val, 'd', 'D'))
			{
				var res = default(Double);
				
				if (!Double.TryParse(val, NumberStyles.Float, Culture.NumberFormat, out res))
					AddError(ElaParserError.InvalidRealSyntax);
				
				return new ElaLiteralValue(negate ? -res : res);
			}
            else
            {
                var res = default(Single);

                if (!Single.TryParse(val.Trim('f', 'F'), NumberStyles.Float, Culture.NumberFormat, out res))
                    AddError(ElaParserError.InvalidRealSyntax);

                return new ElaLiteralValue(negate ? -res : res);
            }
		}


		private string ReadString(string val)
		{
			if (val.Length > 0) 
			{
				if (val[0] != '<')
				{
					var res = EscapeCodeParser.Parse(ref val);

					if (res > 0)
						AddError(ElaParserError.InvalidEscapeCode, res);
				}
				else
					val = val.Substring(2, val.Length - 4);
			}
			
			return val;
		}
		
		
		private bool TrimLast(ref string val, char cl, char cu)
		{			
			var lc = val[val.Length - 1];			
			
			if (lc == cl || lc == cu)
			{
				val = val.Remove(val.Length - 1, 1);
				return true;
			}
			else
				return false;
		}
		#endregion


		#region Properties
		public ElaExpression Expression { get; private set; }
		#endregion
	}
}