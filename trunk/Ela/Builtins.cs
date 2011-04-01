﻿using System;
using Ela.CodeModel;
using Ela.Compilation;

namespace Ela
{
	internal static class Builtins
	{
		internal static int Params(ElaBuiltinKind kind)
		{
			return kind >= ElaBuiltinKind.Showf ? 2 : 1;
		}


		internal static ElaBuiltinKind Kind(string func)
		{
			switch (func)
			{
				case "typeid": return ElaBuiltinKind.Typeid;
				case "not": return ElaBuiltinKind.Not;
				case "flip": return ElaBuiltinKind.Flip;
				case "force": return ElaBuiltinKind.Force;
				case "length": return ElaBuiltinKind.Length;
				case "type": return ElaBuiltinKind.Type;
				case "succ": return ElaBuiltinKind.Succ;
				case "pred": return ElaBuiltinKind.Pred;
				case "max": return ElaBuiltinKind.Max;
				case "min": return ElaBuiltinKind.Min;
				case "show": return ElaBuiltinKind.Show;
				case "isref": return ElaBuiltinKind.IsRef;
				case "nil": return ElaBuiltinKind.Nil;
                case "head": return ElaBuiltinKind.Head;
                case "tail": return ElaBuiltinKind.Tail;
                case "isnil": return ElaBuiltinKind.IsNil;
                case "fst": return ElaBuiltinKind.Fst;
                case "fst3": return ElaBuiltinKind.Fst3;
                case "snd": return ElaBuiltinKind.Snd;
                case "snd3": return ElaBuiltinKind.Snd3;
				case "showf": return ElaBuiltinKind.Showf;
				
				case "equal": return ElaBuiltinKind.Equal;
				case "notequal": return ElaBuiltinKind.NotEqual;
				case "greaterequal": return ElaBuiltinKind.GreaterEqual;
				case "lesserequal": return ElaBuiltinKind.LesserEqual;
				case "greater": return ElaBuiltinKind.Greater;
				case "lesser": return ElaBuiltinKind.Lesser;
				case "add": return ElaBuiltinKind.Add;
				case "subtract": return ElaBuiltinKind.Subtract;
				case "multiply": return ElaBuiltinKind.Multiply;
				case "divide": return ElaBuiltinKind.Divide;
				case "remainder": return ElaBuiltinKind.Remainder;
				case "power": return ElaBuiltinKind.Power;
				case "negate": return ElaBuiltinKind.Negate;
				case "bitwiseand": return ElaBuiltinKind.BitwiseAnd;
				case "bitwiseor": return ElaBuiltinKind.BitwiseOr;
				case "bitwisexor": return ElaBuiltinKind.BitwiseXor;
				case "shiftright": return ElaBuiltinKind.ShiftRight;
				case "shiftleft": return ElaBuiltinKind.ShiftLeft;
				case "bitwisenot": return ElaBuiltinKind.BitwiseNot;
				case "concat": return ElaBuiltinKind.Concat;
				case "cons": return ElaBuiltinKind.Cons;
				case "compforward": return ElaBuiltinKind.CompForward;
				case "compbackward": return ElaBuiltinKind.CompBackward;

				default: return ElaBuiltinKind.None;
			}
		}
	}
}
