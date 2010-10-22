using System;
using System.Collections.Generic;
using System.Text;
using Ela.Compilation;
using Ela.CodeModel;
using Ela.Debug;
using Ela.Internal;
using Ela.Linking;
using Ela.Properties;
using Ela.Runtime.ObjectModel;

namespace Ela.Runtime
{
	public sealed partial class ElaMachine
	{
		private void Execute(WorkerThread thread)
		{
			if (thread.Busy)
				throw BusyException();

			thread.Busy = true;

			var evalStack = thread.EvalStack;
var callStack = thread.CallStack;
var frame = thread.CurrentModule;
var ops = thread.CurrentModule.Ops;
var locals = callStack.Peek().Locals;
var captures = callStack.Peek().Captures;

var left = default(RuntimeValue);
var right = default(RuntimeValue);
var res = default(RuntimeValue);
var i4 = 0;
var i4_2 = 0;

do
{
	#region Body
	var op = ops[thread.Offset];
	thread.Offset++;

	switch (op.OpCode)
	{
		#region Stack Operations
		case OpCode.Pushloc:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				evalStack.Push(locals[i4 >> 8]);
			else
				evalStack.Push(captures[captures.Count - i4_2][i4 >> 8]);
			break;
		case OpCode.Pushstr:
			evalStack.Push(new RuntimeValue(frame.Strings[op.Data]));
			break;
		case OpCode.PushI4:
			evalStack.Push(op.Data);
			break;
		case OpCode.PushI1:
			evalStack.Push(new RuntimeValue(op.Data == 1));
			break;
		case OpCode.PushR4:
			evalStack.Push(new RuntimeValue(op.Data, ElaObject.Real));
			break;
		case OpCode.PushCh:
			evalStack.Push(new RuntimeValue((Char)op.Data));
			break;
		case OpCode.Pushnull:
			evalStack.Push(new RuntimeValue(ElaObject.Null));
			break;
		case OpCode.Pushvoid:
			evalStack.Push(new RuntimeValue(ElaObject.Void));
			break;

		case OpCode.Push_lab:
			evalStack.Push(new RuntimeValue(op.Data));
			break;
		case OpCode.Pushelem:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != ARR && left.Type != STR && left.Type != TUP && left.Type != VAR)
				InvalidType(left, thread, ARR, STR, TUP);
			else if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
				evalStack.Push( ((ElaArray)left.Ref).GetValue(right.I4) );
			break;
		case OpCode.Pushfld:
			{
				var fld = frame.Strings[op.Data];
				right = evalStack.Pop();
				
				if (right.Type == MOD)
				{
					var fr = asm.Modules[right.I4];
					ScopeVar sc;
					
					if (!fr.GlobalScope.Locals.TryGetValue(fld, out sc))
						ExecuteThrow(ElaErrorType.Runtime_UndefinedVariable, thread, fld);
					else if ((sc.Flags & ElaVariableFlags.Private) == ElaVariableFlags.Private)
						ExecuteThrow(ElaErrorType.Runtime_PrivateVariable, thread, fld);
					else
					{						
						right = modules[fr.Index][sc.Address];
						evalStack.Push(right);
					}
				}
				else
				{
					right = ((ElaObject)right.Ref).GetField(fld);

					if (right.Type == ERR)
						ExecuteThrow(ElaErrorType.Runtime_UnknownField, thread, fld);				
					else
						evalStack.Push(right);
				}
			}
			break;
		case OpCode.Hasfld:
			{
				var fld = frame.Strings[op.Data];
				var obj = (ElaObject)evalStack.Pop().Ref;
				evalStack.Push(new RuntimeValue(obj.HasField(fld)));
			}
			break;
		case OpCode.Pushenum:
			right = evalStack.Pop();			
			evalStack.Push(((ElaEnumerator)right.Ref).GetNext());
			break;
		case OpCode.Pop:
			evalStack.Pop();
			break;
		case OpCode.Poploc:
			right = evalStack.Pop();
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				locals[i4 >> 8] = right;
			else
				captures[captures.Count - i4_2][i4 >> 8] = right;
			break;
		case OpCode.Popelem:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != ARR)
				InvalidType(left, thread, ARR);
			else if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (!((ElaArray)left.Ref).SetValue(right.I4, evalStack.Pop()))
					ExecuteThrow(ElaErrorType.Runtime_IndexOutOfRange, thread);
			}
			break;
		case OpCode.Popfld:
			right = evalStack.Pop();

			if (right.Type != REC)
				InvalidType(right, thread, REC);
			else
			{
				left = evalStack.Pop();
				var fld = frame.Strings[op.Data];

				if (!((ElaRecord)right.Ref).SetField(fld, left))
					ExecuteThrow(ElaErrorType.Runtime_UnknownField, thread, fld);
			}
			break;
		case OpCode.Dup:
			right = evalStack.Peek();
			evalStack.Push(right);
			break;
		case OpCode.Arrlen:
			evalStack.Push(new RuntimeValue(((ElaArray)evalStack.Pop().Ref).Length));
			break;	
		case OpCode.Pushperv:
			{
				var name = frame.Strings[op.Data];
				var perv = default(Pervasive);
				
				if (!pervasives.Peek().TryGetValue(name, out perv))
					UnknownName(name, thread);
				else
				{
					if (perv.Module == 0)
						evalStack.Push(locals[perv.Name >> 8]);
					else
						evalStack.Push(modules[perv.Module][perv.Name >> 8]);
				}				
			}		
			break;
		#endregion

		#region Math Operations
		case OpCode.Incr:
			{
				right = locals[op.Data >> 8];
				
				if (right.Type != INT)
					InvalidType(right, thread, INT);
				else
				{
					right = new RuntimeValue(right.I4 + 1);
					evalStack.Push(right);					
					locals[op.Data >> 8] = right;
				}
			}
			break;
		case OpCode.Decr:
			{
				right = locals[op.Data >> 8];
				
				if (right.Type != INT)
					InvalidType(right, thread, INT);
				else
				{
					right = new RuntimeValue(right.I4 - 1);
					evalStack.Push(right);					
					locals[op.Data >> 8] = right;
				}
			}
			break;
		case OpCode.Add:
			{
				right = evalStack.Pop();
				left = evalStack.Peek();
				var aff = opAffinity[left.Type, right.Type];

				if (aff == INT)
					res = new RuntimeValue(left.I4 + right.I4);
				else if (aff == REA)
					res = new RuntimeValue(left.GetReal() + right.GetReal());
				else if (aff == STR)
					res = new RuntimeValue(left.Ref.ToString() + right.Ref.ToString());
				else if (aff == CHR)
					res = new RuntimeValue(new String(new char[] { (Char)left.I4, (Char)right.I4 }));
				else if (aff == LNG)
					res = new RuntimeValue(left.GetLong() + right.GetLong());
				else if (aff == DBL)
					res = new RuntimeValue(left.GetDouble() + right.GetDouble());
				else if (aff == ARR)
					res = ConcatArrays(left, right);
				else if (aff == LST)
					res = ConcatLists(left, right);
				else
				{
					InvalidOperation("+", left, right, thread);
					break;
				}

				evalStack.Replace(res);
			}
			break;
		case OpCode.Sub:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue(left.I4 - right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() - right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() - right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() - right.GetDouble());
			else
			{
				InvalidOperation("-", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Div:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
			{
				if (right.I4 == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(left.I4 / right.I4);
			}
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() / right.GetReal());
			else if (i4 == LNG)
			{
				var lng = left.GetLong();

				if (lng == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(lng / right.GetLong());
			}
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() / right.GetDouble());
			else
			{
				InvalidOperation("/", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Mul:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue(left.I4 * right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() * right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() * right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() * right.GetDouble());
			else
			{
				InvalidOperation("*", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Pow:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue((Int32)Math.Pow(left.I4, right.I4));
			else if (i4 == REA)
				res = new RuntimeValue(Math.Pow(left.GetReal(), right.GetReal()));
			else
			{
				InvalidOperation("**", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Rem:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
			{
				if (right.I4 == 0)
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
				else
					res = new RuntimeValue(left.I4 % right.I4);
			}
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() % right.GetReal());
			else if (i4 == LNG)
			{
				var lng = left.GetLong();

				if (lng == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(lng % right.GetLong());
			}
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() % right.GetDouble());
			else
			{
				InvalidOperation("%", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		#endregion

		#region Comparison Operations
		case OpCode.Ccons:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (right.Type != VAR)
				InvalidType(right, thread, VAR);
			else
			{
				var cons = left.Ref.ToString();
			
				if (((ElaVariant)right.Ref).Cons == cons)
					evalStack.Push(new RuntimeValue(true));
				else 
					evalStack.Push(new RuntimeValue(false));
					//Ccons(right.Ref, cons, thread);
			}
			break;
		case OpCode.Cgt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 > right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() > right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() > right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() > right.GetDouble());
			else
			{
				InvalidOperation(">", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Clt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 < right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() < right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() < right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() < right.GetDouble());
			else
			{
				InvalidOperation("<", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Ceq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 == right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() == right.GetReal());
			else if (i4 == STR)
				res = new RuntimeValue(left.Ref.ToString() == right.Ref.ToString());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() == right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() == right.GetDouble());
			else if (i4 == ERR)
			{
				InvalidOperation("==", left, right, thread);
				break;
			}
			else
				res = new RuntimeValue(left.Ref == right.Ref);

			evalStack.Push(res);
			break;
		case OpCode.Cneq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 != right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() != right.GetReal());
			else if (i4 == STR)
				res = new RuntimeValue(left.Ref.ToString() != right.Ref.ToString());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() != right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() != right.GetDouble());
			else if (i4 == ERR)
			{
				InvalidOperation("!=", left, right, thread);
				break;
			}
			else
				res = new RuntimeValue(left.Ref != right.Ref);

			evalStack.Push(res);
			break;
		case OpCode.Cgteq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 >= right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() >= right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() >= right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() >= right.GetDouble());
			else 
			{
				InvalidOperation(">=", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Clteq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 <= right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() <= right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() <= right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() <= right.GetDouble());
			else
			{
				InvalidOperation("<=", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		#endregion

		#region Binary Operations
		case OpCode.Listadd:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (right.Type == LST)
			{
				res = new RuntimeValue(new ElaList((ElaList)right.Ref, left));
				evalStack.Push(res);
			}
			else
				InvalidType(right, thread, LST);
			break;
		case OpCode.Listadd_Bw:
			right = evalStack.Pop();
			left = evalStack.Pop();
			res = new RuntimeValue(new ElaList((ElaList)left.Ref, right));
			evalStack.Push(res);
			break;
		case OpCode.Listtail:
			right = evalStack.Pop();

			if (right.Type != LST)
				InvalidType(right, thread, LST);
			else if (right.Ref != ElaList.Nil)
				evalStack.Push(new RuntimeValue(((ElaList)right.Ref).Next));
			else
				evalStack.Push(new RuntimeValue(ElaList.Nil));
			break;
		case OpCode.Listlen:
			right = evalStack.Pop();
			evalStack.Push(new RuntimeValue(((ElaList)right.Ref).GetLength()));
			break;
		case OpCode.Listelem:
			right = evalStack.Pop();

			if (right.Type != LST)
				InvalidType(right, thread, LST);
			else
				evalStack.Push(((ElaList)right.Ref).Value);
			break;
		case OpCode.List_isnil:
			right = evalStack.Pop();
			evalStack.Push(new RuntimeValue(right.Ref == ElaList.Nil));
			break;

		case OpCode.AndBw:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 & right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.OrBw:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 | right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.Xor:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 ^ right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.Shl:
			right = evalStack.Pop();
			left = evalStack.Pop();
			
			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (left.Type == INT)
					evalStack.Push(left.I4 << right.I4);
				else if (left.Type == LNG)
					evalStack.Push(new RuntimeValue(left.GetLong() << right.I4));
				else
					InvalidType(left, thread, INT, LNG);
			}
			break;
		case OpCode.Shr:
			right = evalStack.Pop();
			left = evalStack.Pop();
			
			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (left.Type == INT)
					evalStack.Push(left.I4 >> right.I4);
				else if (left.Type == LNG)
					evalStack.Push(new RuntimeValue(left.GetLong() >> right.I4));
				else
					InvalidType(left, thread, INT, LNG);
			}
			break;
		#endregion

		#region Unary Operations
		case OpCode.Not:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else
			{
				res = new RuntimeValue(!(right.I4 == 1));
				evalStack.Push(res);
			}
			break;
		case OpCode.Neg:
			right = evalStack.Pop();

			if (right.Type == INT)
				res = new RuntimeValue(-right.I4);
			else if (right.Type == REA)
				res = new RuntimeValue(-right.GetReal());
			else
			{
				InvalidType(right, thread, INT, REA);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Pos:
			right = evalStack.Pop();

			if (right.Type == INT)
				res = new RuntimeValue(+right.I4);
			else if (right.Type == REA)
				res = new RuntimeValue(+right.GetReal());
			else
			{
				InvalidType(right, thread, INT, REA);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.NotBw:
			right = evalStack.Pop();

			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				res = new RuntimeValue(~right.I4);
				evalStack.Push(res);
			}
			break;
		#endregion

		#region Conversion Operations
		case OpCode.ConvI4:
			ConvertI4(evalStack.Peek(), thread);
			break;
		case OpCode.ConvR4:
			ConvertR4(evalStack.Peek(), thread);
			break;
		case OpCode.ConvI1:
			ConvertI1(evalStack.Peek(), thread);
			break;
		case OpCode.ConvStr:
			ConvertStr(evalStack.Peek(), thread);
			break;
		case OpCode.ConvI8:
			ConvertI8(evalStack.Peek(), thread);
			break;
		case OpCode.ConvCh:
			ConvertCh(evalStack.Peek(), thread);
			break;
		case OpCode.ConvR8:
			ConvertR8(evalStack.Peek(), thread);
			break;
		#endregion

		#region Goto Operations
		case OpCode.Brtrue:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else if (right.I4 == 1)
				thread.Offset = op.Data;
			break;
		case OpCode.Brfalse:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else if (right.I4 == 0)
				thread.Offset = op.Data;
			break;
		case OpCode.Brvoid:
			right = evalStack.Peek();
			
			if (right.Type == VOI)
				thread.Offset = op.Data;
			break;
		case OpCode.Br:
			thread.Offset = op.Data;
			break;
		case OpCode.Br_eq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 == right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() == right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == STR)
			{
				if (left.Ref.ToString() == right.Ref.ToString())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() == right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() == right.GetDouble())
					thread.Offset = op.Data;
			}
			else if (i4 == ERR)
				InvalidOperation("==", left, right, thread);
			else
			{
				if (left.Ref == right.Ref)
					thread.Offset = op.Data;
			}
			break;
		case OpCode.Br_neq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 != right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() != right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == STR)
			{
				if (left.Ref.ToString() != right.Ref.ToString())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() != right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() != right.GetDouble())
					thread.Offset = op.Data;
			}
			else if (i4 == ERR)
				InvalidOperation("!=", left, right, thread);
			else
			{
				if (left.Ref != right.Ref)
					thread.Offset = op.Data;
			}
			break;
		case OpCode.Br_lt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 < right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() < right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() < right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() < right.GetDouble())
					thread.Offset = op.Data;
			}
			else
				InvalidOperation("<", left, right, thread);
			break;
		case OpCode.Br_gt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 > right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() > right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() > right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() > right.GetDouble())
					thread.Offset = op.Data;
			}
			else
				InvalidOperation(">", left, right, thread);
			break;
		case OpCode.Brdyn:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				right = locals[i4 >> 8];
			else
				right = captures[captures.Count - i4_2][i4 >> 8];

			if (right.I4 > 0)
				thread.Offset = right.I4;
			break;
		case OpCode.Yield:
			{
				i4 = op.Data;
				i4_2 = i4 & Byte.MaxValue;

				if (i4_2 == 0)
					locals[i4 >> 8] = new RuntimeValue(thread.Offset);
				else
					captures[captures.Count - i4_2][i4 >> 8] = new RuntimeValue(thread.Offset);
					
				if (evalStack.Peek().Type == VOI)
					ExecuteThrow(ElaErrorType.Runtime_UnableYieldVoid, thread);
				else
				{
					var om = callStack.Pop();				
					var cp = callStack.Peek();
					thread.SwitchModule(cp.ModuleHandle);
					thread.Offset = om.ReturnAddress;
					return;
				}
			}
			break;
		case OpCode.Ret:
			{
				var om = callStack.Pop();
					
				if (om.ReturnValue != null)
					om.ReturnValue.Value = evalStack.Peek();
			
				var cp = callStack.Peek();
				thread.SwitchModule(cp.ModuleHandle);
				locals = cp.Locals;
				captures = cp.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
				
				if (om.ReturnAddress == 0)
					return;
				else
					thread.Offset = om.ReturnAddress;
			}
			break;
		#endregion

		#region CreateNew Operations
		case OpCode.Newcomp:
			ComposeFunctions(thread);
			break;
		case OpCode.Newlazy:
			{
				right = evalStack.Pop();
				var lazy = new ElaLazy();
				lazy.Function = (ElaFunction)right.Ref;
				evalStack.Push(new RuntimeValue(lazy));
			}
			break;
		case OpCode.Newfun:
			{
				i4 = frame.Layouts[op.Data].Size;
				var lst = new FastList<RuntimeValue[]>(captures);
				lst.Add(locals);
				evalStack.Push(new RuntimeValue(new ElaFunction(op.Data,
					thread.CurrentModuleIndex, evalStack.Pop().I4, lst, this)));
			}
			break;
		case OpCode.Newcor:
			{
				i4 = frame.Layouts[op.Data].Size;
				var layout = thread.CurrentModule.Layouts[op.Data];
				var lst = new FastList<RuntimeValue[]>(captures);
				lst.Add(locals);
				var newMem = new RuntimeValue[layout.Size];					
				evalStack.Push(new RuntimeValue(new ElaCoroutine(op.Data,
					thread.CurrentModuleIndex, lst, this, newMem)));
			}
			break;
		case OpCode.Newlist:
			evalStack.Push(new RuntimeValue(ElaList.Nil));
			break;
		case OpCode.Newtab:
			{
				var obj = new ElaRecord(op.Data);
				ConstructTable(thread, obj, op.Data);
			}
			break;
		case OpCode.Newadt:
			{
				var tup = new ElaVariant(op.Data, evalStack.Pop().ToString());

				for (var j = op.Data - 1; j > -1; j--)
					tup.InternalSetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(tup));
			}
			break;
		case OpCode.Newarr:
			{
				var lst = new ElaArray(op.Data);

				for (var j = op.Data - 1; j > -1; j--)
					lst.SetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(lst));
			}
			break;
		case OpCode.Newtup:
			{
				var lst = new ElaTuple(op.Data);

				for (var j = op.Data - 1; j > -1; j--)
					lst.InternalSetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(lst));
			}
			break;
		case OpCode.Newenum:
			{
				right = evalStack.Pop();
				
				if (right.Type != ARR && right.Type != LST && right.Type != TUP && 
					right.Type != STR && right.Type != COR)
					InvalidType(right, thread, ARR, LST, TUP, STR, COR);
				else
					evalStack.Push(new RuntimeValue(new ElaEnumerator(right.Ref)));
			}
			break;
		case OpCode.NewI8:
			{
				right = evalStack.Pop();
				left = evalStack.Pop();
				var conv = new Conv();
				conv.I4_1 = left.I4;
				conv.I4_2 = right.I4;
				evalStack.Push(new RuntimeValue(conv.I8));
			}
			break;
		case OpCode.NewR8:
			{
				right = evalStack.Pop();
				left = evalStack.Pop();
				var conv = new Conv();
				conv.I4_1 = left.I4;
				conv.I4_2 = right.I4;
				evalStack.Push(new RuntimeValue(conv.R8));
			}
			break;	
		case OpCode.Prot:
			{
				right = evalStack.Pop();
				left = evalStack.Peek();
				right.Ref.Prototype = left.Ref;
				evalStack.Replace(right);
			}
			break;	
		case OpCode.Newmod:
			{
				var str = frame.Strings[op.Data];
				var mod = asm.ModuleMap[str];
				evalStack.Push(new RuntimeValue(mod.Index, ElaObject.Module));
			}
			break;
		#endregion

		#region Function Operations
		case OpCode.Call:
			if (Call(thread, op.Data))
			{
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Calla:
			CallAsync(thread);
			break;
		case OpCode.Epilog:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				locals[i4 >> 8] = new RuntimeValue(0);
			else
				captures[captures.Count - i4_2][i4 >> 8] = new RuntimeValue(0);
			
			evalStack.Pop();			
			evalStack.Push(new RuntimeValue(ElaObject.Void));
			break;
		#endregion

		#region Complex and Debug
		case OpCode.Nop:
			break;
		case OpCode.Cout:
			Console.WriteLine(evalStack.Pop().ToString());
			break;
		case OpCode.Cin:
			evalStack.Push(new RuntimeValue(Console.ReadLine()));
			break;
		case OpCode.Sleep:
			System.Threading.Thread.Sleep(op.Data);
			break;
		case OpCode.Typeof:
			right = evalStack.Pop();
			left = evalStack.Pop();
			evalStack.Push(new RuntimeValue(left.Type == right.I4));
			break;
		case OpCode.Start:
			break;
		case OpCode.Finally:
			break;
		case OpCode.Leave:
			break;
		case OpCode.Throw:
			{
				ExecuteThrow(ElaErrorType.Code_User, thread);
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;					
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Throw_S:
			{
				ExecuteThrow((ElaErrorType)op.Data, thread);
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;					
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Term:
			{
				if (callStack.Count > 1)
				{
					var modMem = callStack.Pop();
					thread.Offset = modMem.ReturnAddress;
					pervasives.Pop();
					ReadPervasives(thread.CurrentModule);					
					
					var olp = callStack.Peek();					
					thread.SwitchModule(olp.ModuleHandle);					
					locals = olp.Locals;
					captures = olp.Captures;					
					ops = thread.CurrentModule.Ops;
					frame = thread.CurrentModule;
					evalStack.Pop();
				}
				else
				{
					thread.Busy = false;
					
					lock (syncRoot)
						Threads.Remove(thread);
						
					if (thread.ReturnValue != null)
					{
						thread.ReturnValue.Value = thread.EvalStack.Pop();
						thread.ReturnValue.Thread = null;
					}
						
					return;
				}
			}
			break;
		case OpCode.Runmod:
			{
				var frm = thread.GetModule(frame.Strings[op.Data]);
				
				if (modules[frm.Index] == null)
				{
					pervasives.Push(new Dictionary<String,Pervasive>());

					if (frm is IntrinsicFrame)
						modules[frm.Index] = ((IntrinsicFrame)frm).Memory;
					else
					{
						i4 = frm.Layouts[0].Size;
						locals = new RuntimeValue[i4];
						modules[frm.Index] = locals;
						captures = FastList<RuntimeValue[]>.Empty;
						var modMem = new CallPoint(thread.Offset, frm.Index, evalStack.Count, 
							null, locals, captures);
						callStack.Push(modMem);
						thread.SwitchModule(frm.Index);
						ops = thread.CurrentModule.Ops;
						frame = thread.CurrentModule;					
						thread.Offset = 1;
					}
				}
			}
			break;
		case OpCode.Valueof:
			if (ValueOf(thread))
			{
				var cp = callStack.Peek();
				locals = cp.Locals;
				captures = cp.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		#endregion
	}
	#endregion
}
while (true);
		}
		
		
		internal void ExecuteStep(WorkerThread thread)
		{
			var evalStack = thread.EvalStack;
var callStack = thread.CallStack;
var frame = thread.CurrentModule;
var ops = thread.CurrentModule.Ops;
var locals = callStack.Peek().Locals;
var captures = callStack.Peek().Captures;

var left = default(RuntimeValue);
var right = default(RuntimeValue);
var res = default(RuntimeValue);
var i4 = 0;
var i4_2 = 0;

do
{
	#region Body
	var op = ops[thread.Offset];
	thread.Offset++;

	switch (op.OpCode)
	{
		#region Stack Operations
		case OpCode.Pushloc:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				evalStack.Push(locals[i4 >> 8]);
			else
				evalStack.Push(captures[captures.Count - i4_2][i4 >> 8]);
			break;
		case OpCode.Pushstr:
			evalStack.Push(new RuntimeValue(frame.Strings[op.Data]));
			break;
		case OpCode.PushI4:
			evalStack.Push(op.Data);
			break;
		case OpCode.PushI1:
			evalStack.Push(new RuntimeValue(op.Data == 1));
			break;
		case OpCode.PushR4:
			evalStack.Push(new RuntimeValue(op.Data, ElaObject.Real));
			break;
		case OpCode.PushCh:
			evalStack.Push(new RuntimeValue((Char)op.Data));
			break;
		case OpCode.Pushnull:
			evalStack.Push(new RuntimeValue(ElaObject.Null));
			break;
		case OpCode.Pushvoid:
			evalStack.Push(new RuntimeValue(ElaObject.Void));
			break;

		case OpCode.Push_lab:
			evalStack.Push(new RuntimeValue(op.Data));
			break;
		case OpCode.Pushelem:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != ARR && left.Type != STR && left.Type != TUP && left.Type != VAR)
				InvalidType(left, thread, ARR, STR, TUP);
			else if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
				evalStack.Push( ((ElaArray)left.Ref).GetValue(right.I4) );
			break;
		case OpCode.Pushfld:
			{
				var fld = frame.Strings[op.Data];
				right = evalStack.Pop();
				
				if (right.Type == MOD)
				{
					var fr = asm.Modules[right.I4];
					ScopeVar sc;
					
					if (!fr.GlobalScope.Locals.TryGetValue(fld, out sc))
						ExecuteThrow(ElaErrorType.Runtime_UndefinedVariable, thread, fld);
					else if ((sc.Flags & ElaVariableFlags.Private) == ElaVariableFlags.Private)
						ExecuteThrow(ElaErrorType.Runtime_PrivateVariable, thread, fld);
					else
					{						
						right = modules[fr.Index][sc.Address];
						evalStack.Push(right);
					}
				}
				else
				{
					right = ((ElaObject)right.Ref).GetField(fld);

					if (right.Type == ERR)
						ExecuteThrow(ElaErrorType.Runtime_UnknownField, thread, fld);				
					else
						evalStack.Push(right);
				}
			}
			break;
		case OpCode.Hasfld:
			{
				var fld = frame.Strings[op.Data];
				var obj = (ElaObject)evalStack.Pop().Ref;
				evalStack.Push(new RuntimeValue(obj.HasField(fld)));
			}
			break;
		case OpCode.Pushenum:
			right = evalStack.Pop();			
			evalStack.Push(((ElaEnumerator)right.Ref).GetNext());
			break;
		case OpCode.Pop:
			evalStack.Pop();
			break;
		case OpCode.Poploc:
			right = evalStack.Pop();
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				locals[i4 >> 8] = right;
			else
				captures[captures.Count - i4_2][i4 >> 8] = right;
			break;
		case OpCode.Popelem:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != ARR)
				InvalidType(left, thread, ARR);
			else if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (!((ElaArray)left.Ref).SetValue(right.I4, evalStack.Pop()))
					ExecuteThrow(ElaErrorType.Runtime_IndexOutOfRange, thread);
			}
			break;
		case OpCode.Popfld:
			right = evalStack.Pop();

			if (right.Type != REC)
				InvalidType(right, thread, REC);
			else
			{
				left = evalStack.Pop();
				var fld = frame.Strings[op.Data];

				if (!((ElaRecord)right.Ref).SetField(fld, left))
					ExecuteThrow(ElaErrorType.Runtime_UnknownField, thread, fld);
			}
			break;
		case OpCode.Dup:
			right = evalStack.Peek();
			evalStack.Push(right);
			break;
		case OpCode.Arrlen:
			evalStack.Push(new RuntimeValue(((ElaArray)evalStack.Pop().Ref).Length));
			break;	
		case OpCode.Pushperv:
			{
				var name = frame.Strings[op.Data];
				var perv = default(Pervasive);
				
				if (!pervasives.Peek().TryGetValue(name, out perv))
					UnknownName(name, thread);
				else
				{
					if (perv.Module == 0)
						evalStack.Push(locals[perv.Name >> 8]);
					else
						evalStack.Push(modules[perv.Module][perv.Name >> 8]);
				}				
			}		
			break;
		#endregion

		#region Math Operations
		case OpCode.Incr:
			{
				right = locals[op.Data >> 8];
				
				if (right.Type != INT)
					InvalidType(right, thread, INT);
				else
				{
					right = new RuntimeValue(right.I4 + 1);
					evalStack.Push(right);					
					locals[op.Data >> 8] = right;
				}
			}
			break;
		case OpCode.Decr:
			{
				right = locals[op.Data >> 8];
				
				if (right.Type != INT)
					InvalidType(right, thread, INT);
				else
				{
					right = new RuntimeValue(right.I4 - 1);
					evalStack.Push(right);					
					locals[op.Data >> 8] = right;
				}
			}
			break;
		case OpCode.Add:
			{
				right = evalStack.Pop();
				left = evalStack.Peek();
				var aff = opAffinity[left.Type, right.Type];

				if (aff == INT)
					res = new RuntimeValue(left.I4 + right.I4);
				else if (aff == REA)
					res = new RuntimeValue(left.GetReal() + right.GetReal());
				else if (aff == STR)
					res = new RuntimeValue(left.Ref.ToString() + right.Ref.ToString());
				else if (aff == CHR)
					res = new RuntimeValue(new String(new char[] { (Char)left.I4, (Char)right.I4 }));
				else if (aff == LNG)
					res = new RuntimeValue(left.GetLong() + right.GetLong());
				else if (aff == DBL)
					res = new RuntimeValue(left.GetDouble() + right.GetDouble());
				else if (aff == ARR)
					res = ConcatArrays(left, right);
				else if (aff == LST)
					res = ConcatLists(left, right);
				else
				{
					InvalidOperation("+", left, right, thread);
					break;
				}

				evalStack.Replace(res);
			}
			break;
		case OpCode.Sub:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue(left.I4 - right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() - right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() - right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() - right.GetDouble());
			else
			{
				InvalidOperation("-", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Div:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
			{
				if (right.I4 == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(left.I4 / right.I4);
			}
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() / right.GetReal());
			else if (i4 == LNG)
			{
				var lng = left.GetLong();

				if (lng == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(lng / right.GetLong());
			}
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() / right.GetDouble());
			else
			{
				InvalidOperation("/", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Mul:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue(left.I4 * right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() * right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() * right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() * right.GetDouble());
			else
			{
				InvalidOperation("*", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Pow:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
				res = new RuntimeValue((Int32)Math.Pow(left.I4, right.I4));
			else if (i4 == REA)
				res = new RuntimeValue(Math.Pow(left.GetReal(), right.GetReal()));
			else
			{
				InvalidOperation("**", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		case OpCode.Rem:
			right = evalStack.Pop();
			left = evalStack.Peek();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT)
			{
				if (right.I4 == 0)
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
				else
					res = new RuntimeValue(left.I4 % right.I4);
			}
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() % right.GetReal());
			else if (i4 == LNG)
			{
				var lng = left.GetLong();

				if (lng == 0)
				{
					ExecuteThrow(ElaErrorType.Runtime_DivideByZero, thread);
					break;
				}
				else
					res = new RuntimeValue(lng % right.GetLong());
			}
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() % right.GetDouble());
			else
			{
				InvalidOperation("%", left, right, thread);
				break;
			}

			evalStack.Replace(res);
			break;
		#endregion

		#region Comparison Operations
		case OpCode.Ccons:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (right.Type != VAR)
				InvalidType(right, thread, VAR);
			else
			{
				var cons = left.Ref.ToString();
			
				if (((ElaVariant)right.Ref).Cons == cons)
					evalStack.Push(new RuntimeValue(true));
				else 
					evalStack.Push(new RuntimeValue(false));
					//Ccons(right.Ref, cons, thread);
			}
			break;
		case OpCode.Cgt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 > right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() > right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() > right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() > right.GetDouble());
			else
			{
				InvalidOperation(">", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Clt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 < right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() < right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() < right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() < right.GetDouble());
			else
			{
				InvalidOperation("<", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Ceq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 == right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() == right.GetReal());
			else if (i4 == STR)
				res = new RuntimeValue(left.Ref.ToString() == right.Ref.ToString());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() == right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() == right.GetDouble());
			else if (i4 == ERR)
			{
				InvalidOperation("==", left, right, thread);
				break;
			}
			else
				res = new RuntimeValue(left.Ref == right.Ref);

			evalStack.Push(res);
			break;
		case OpCode.Cneq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 != right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() != right.GetReal());
			else if (i4 == STR)
				res = new RuntimeValue(left.Ref.ToString() != right.Ref.ToString());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() != right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() != right.GetDouble());
			else if (i4 == ERR)
			{
				InvalidOperation("!=", left, right, thread);
				break;
			}
			else
				res = new RuntimeValue(left.Ref != right.Ref);

			evalStack.Push(res);
			break;
		case OpCode.Cgteq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 >= right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() >= right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() >= right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() >= right.GetDouble());
			else 
			{
				InvalidOperation(">=", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Clteq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
				res = new RuntimeValue(left.I4 <= right.I4);
			else if (i4 == REA)
				res = new RuntimeValue(left.GetReal() <= right.GetReal());
			else if (i4 == LNG)
				res = new RuntimeValue(left.GetLong() <= right.GetLong());
			else if (i4 == DBL)
				res = new RuntimeValue(left.GetDouble() <= right.GetDouble());
			else
			{
				InvalidOperation("<=", left, right, thread);
				break;
			}

			evalStack.Push(res);
			break;
		#endregion

		#region Binary Operations
		case OpCode.Listadd:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (right.Type == LST)
			{
				res = new RuntimeValue(new ElaList((ElaList)right.Ref, left));
				evalStack.Push(res);
			}
			else
				InvalidType(right, thread, LST);
			break;
		case OpCode.Listadd_Bw:
			right = evalStack.Pop();
			left = evalStack.Pop();
			res = new RuntimeValue(new ElaList((ElaList)left.Ref, right));
			evalStack.Push(res);
			break;
		case OpCode.Listtail:
			right = evalStack.Pop();

			if (right.Type != LST)
				InvalidType(right, thread, LST);
			else if (right.Ref != ElaList.Nil)
				evalStack.Push(new RuntimeValue(((ElaList)right.Ref).Next));
			else
				evalStack.Push(new RuntimeValue(ElaList.Nil));
			break;
		case OpCode.Listlen:
			right = evalStack.Pop();
			evalStack.Push(new RuntimeValue(((ElaList)right.Ref).GetLength()));
			break;
		case OpCode.Listelem:
			right = evalStack.Pop();

			if (right.Type != LST)
				InvalidType(right, thread, LST);
			else
				evalStack.Push(((ElaList)right.Ref).Value);
			break;
		case OpCode.List_isnil:
			right = evalStack.Pop();
			evalStack.Push(new RuntimeValue(right.Ref == ElaList.Nil));
			break;

		case OpCode.AndBw:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 & right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.OrBw:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 | right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.Xor:
			right = evalStack.Pop();
			left = evalStack.Pop();

			if (left.Type != INT || right.Type != INT)
				InvalidType(left, thread, INT);
			else
			{
				res = new RuntimeValue(left.I4 ^ right.I4);
				evalStack.Push(res);
			}
			break;
		case OpCode.Shl:
			right = evalStack.Pop();
			left = evalStack.Pop();
			
			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (left.Type == INT)
					evalStack.Push(left.I4 << right.I4);
				else if (left.Type == LNG)
					evalStack.Push(new RuntimeValue(left.GetLong() << right.I4));
				else
					InvalidType(left, thread, INT, LNG);
			}
			break;
		case OpCode.Shr:
			right = evalStack.Pop();
			left = evalStack.Pop();
			
			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				if (left.Type == INT)
					evalStack.Push(left.I4 >> right.I4);
				else if (left.Type == LNG)
					evalStack.Push(new RuntimeValue(left.GetLong() >> right.I4));
				else
					InvalidType(left, thread, INT, LNG);
			}
			break;
		#endregion

		#region Unary Operations
		case OpCode.Not:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else
			{
				res = new RuntimeValue(!(right.I4 == 1));
				evalStack.Push(res);
			}
			break;
		case OpCode.Neg:
			right = evalStack.Pop();

			if (right.Type == INT)
				res = new RuntimeValue(-right.I4);
			else if (right.Type == REA)
				res = new RuntimeValue(-right.GetReal());
			else
			{
				InvalidType(right, thread, INT, REA);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.Pos:
			right = evalStack.Pop();

			if (right.Type == INT)
				res = new RuntimeValue(+right.I4);
			else if (right.Type == REA)
				res = new RuntimeValue(+right.GetReal());
			else
			{
				InvalidType(right, thread, INT, REA);
				break;
			}

			evalStack.Push(res);
			break;
		case OpCode.NotBw:
			right = evalStack.Pop();

			if (right.Type != INT)
				InvalidType(right, thread, INT);
			else
			{
				res = new RuntimeValue(~right.I4);
				evalStack.Push(res);
			}
			break;
		#endregion

		#region Conversion Operations
		case OpCode.ConvI4:
			ConvertI4(evalStack.Peek(), thread);
			break;
		case OpCode.ConvR4:
			ConvertR4(evalStack.Peek(), thread);
			break;
		case OpCode.ConvI1:
			ConvertI1(evalStack.Peek(), thread);
			break;
		case OpCode.ConvStr:
			ConvertStr(evalStack.Peek(), thread);
			break;
		case OpCode.ConvI8:
			ConvertI8(evalStack.Peek(), thread);
			break;
		case OpCode.ConvCh:
			ConvertCh(evalStack.Peek(), thread);
			break;
		case OpCode.ConvR8:
			ConvertR8(evalStack.Peek(), thread);
			break;
		#endregion

		#region Goto Operations
		case OpCode.Brtrue:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else if (right.I4 == 1)
				thread.Offset = op.Data;
			break;
		case OpCode.Brfalse:
			right = evalStack.Pop();

			if (right.Type != BYT)
				InvalidType(right, thread, BYT);
			else if (right.I4 == 0)
				thread.Offset = op.Data;
			break;
		case OpCode.Brvoid:
			right = evalStack.Peek();
			
			if (right.Type == VOI)
				thread.Offset = op.Data;
			break;
		case OpCode.Br:
			thread.Offset = op.Data;
			break;
		case OpCode.Br_eq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 == right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() == right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == STR)
			{
				if (left.Ref.ToString() == right.Ref.ToString())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() == right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() == right.GetDouble())
					thread.Offset = op.Data;
			}
			else if (i4 == ERR)
				InvalidOperation("==", left, right, thread);
			else
			{
				if (left.Ref == right.Ref)
					thread.Offset = op.Data;
			}
			break;
		case OpCode.Br_neq:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 != right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() != right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == STR)
			{
				if (left.Ref.ToString() != right.Ref.ToString())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() != right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() != right.GetDouble())
					thread.Offset = op.Data;
			}
			else if (i4 == ERR)
				InvalidOperation("!=", left, right, thread);
			else
			{
				if (left.Ref != right.Ref)
					thread.Offset = op.Data;
			}
			break;
		case OpCode.Br_lt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 < right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() < right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() < right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() < right.GetDouble())
					thread.Offset = op.Data;
			}
			else
				InvalidOperation("<", left, right, thread);
			break;
		case OpCode.Br_gt:
			right = evalStack.Pop();
			left = evalStack.Pop();
			i4 = opAffinity[left.Type, right.Type];

			if (i4 == INT || i4 == CHR)
			{
				if (left.I4 > right.I4)
					thread.Offset = op.Data;
			}
			else if (i4 == REA)
			{
				if (left.GetReal() > right.GetReal())
					thread.Offset = op.Data;
			}
			else if (i4 == LNG)
			{
				if (left.GetLong() > right.GetLong())
					thread.Offset = op.Data;
			}
			else if (i4 == DBL)
			{
				if (left.GetDouble() > right.GetDouble())
					thread.Offset = op.Data;
			}
			else
				InvalidOperation(">", left, right, thread);
			break;
		case OpCode.Brdyn:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				right = locals[i4 >> 8];
			else
				right = captures[captures.Count - i4_2][i4 >> 8];

			if (right.I4 > 0)
				thread.Offset = right.I4;
			break;
		case OpCode.Yield:
			{
				i4 = op.Data;
				i4_2 = i4 & Byte.MaxValue;

				if (i4_2 == 0)
					locals[i4 >> 8] = new RuntimeValue(thread.Offset);
				else
					captures[captures.Count - i4_2][i4 >> 8] = new RuntimeValue(thread.Offset);
					
				if (evalStack.Peek().Type == VOI)
					ExecuteThrow(ElaErrorType.Runtime_UnableYieldVoid, thread);
				else
				{
					var om = callStack.Pop();				
					var cp = callStack.Peek();
					thread.SwitchModule(cp.ModuleHandle);
					thread.Offset = om.ReturnAddress;
					return;
				}
			}
			break;
		case OpCode.Ret:
			{
				var om = callStack.Pop();
					
				if (om.ReturnValue != null)
					om.ReturnValue.Value = evalStack.Peek();
			
				var cp = callStack.Peek();
				thread.SwitchModule(cp.ModuleHandle);
				locals = cp.Locals;
				captures = cp.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
				
				if (om.ReturnAddress == 0)
					return;
				else
					thread.Offset = om.ReturnAddress;
			}
			break;
		#endregion

		#region CreateNew Operations
		case OpCode.Newcomp:
			ComposeFunctions(thread);
			break;
		case OpCode.Newlazy:
			{
				right = evalStack.Pop();
				var lazy = new ElaLazy();
				lazy.Function = (ElaFunction)right.Ref;
				evalStack.Push(new RuntimeValue(lazy));
			}
			break;
		case OpCode.Newfun:
			{
				i4 = frame.Layouts[op.Data].Size;
				var lst = new FastList<RuntimeValue[]>(captures);
				lst.Add(locals);
				evalStack.Push(new RuntimeValue(new ElaFunction(op.Data,
					thread.CurrentModuleIndex, evalStack.Pop().I4, lst, this)));
			}
			break;
		case OpCode.Newcor:
			{
				i4 = frame.Layouts[op.Data].Size;
				var layout = thread.CurrentModule.Layouts[op.Data];
				var lst = new FastList<RuntimeValue[]>(captures);
				lst.Add(locals);
				var newMem = new RuntimeValue[layout.Size];					
				evalStack.Push(new RuntimeValue(new ElaCoroutine(op.Data,
					thread.CurrentModuleIndex, lst, this, newMem)));
			}
			break;
		case OpCode.Newlist:
			evalStack.Push(new RuntimeValue(ElaList.Nil));
			break;
		case OpCode.Newtab:
			{
				var obj = new ElaRecord(op.Data);
				ConstructTable(thread, obj, op.Data);
			}
			break;
		case OpCode.Newadt:
			{
				var tup = new ElaVariant(op.Data, evalStack.Pop().ToString());

				for (var j = op.Data - 1; j > -1; j--)
					tup.InternalSetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(tup));
			}
			break;
		case OpCode.Newarr:
			{
				var lst = new ElaArray(op.Data);

				for (var j = op.Data - 1; j > -1; j--)
					lst.SetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(lst));
			}
			break;
		case OpCode.Newtup:
			{
				var lst = new ElaTuple(op.Data);

				for (var j = op.Data - 1; j > -1; j--)
					lst.InternalSetValue(j, evalStack.Pop());

				evalStack.Push(new RuntimeValue(lst));
			}
			break;
		case OpCode.Newenum:
			{
				right = evalStack.Pop();
				
				if (right.Type != ARR && right.Type != LST && right.Type != TUP && 
					right.Type != STR && right.Type != COR)
					InvalidType(right, thread, ARR, LST, TUP, STR, COR);
				else
					evalStack.Push(new RuntimeValue(new ElaEnumerator(right.Ref)));
			}
			break;
		case OpCode.NewI8:
			{
				right = evalStack.Pop();
				left = evalStack.Pop();
				var conv = new Conv();
				conv.I4_1 = left.I4;
				conv.I4_2 = right.I4;
				evalStack.Push(new RuntimeValue(conv.I8));
			}
			break;
		case OpCode.NewR8:
			{
				right = evalStack.Pop();
				left = evalStack.Pop();
				var conv = new Conv();
				conv.I4_1 = left.I4;
				conv.I4_2 = right.I4;
				evalStack.Push(new RuntimeValue(conv.R8));
			}
			break;	
		case OpCode.Prot:
			{
				right = evalStack.Pop();
				left = evalStack.Peek();
				right.Ref.Prototype = left.Ref;
				evalStack.Replace(right);
			}
			break;	
		case OpCode.Newmod:
			{
				var str = frame.Strings[op.Data];
				var mod = asm.ModuleMap[str];
				evalStack.Push(new RuntimeValue(mod.Index, ElaObject.Module));
			}
			break;
		#endregion

		#region Function Operations
		case OpCode.Call:
			if (Call(thread, op.Data))
			{
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Calla:
			CallAsync(thread);
			break;
		case OpCode.Epilog:
			i4 = op.Data;
			i4_2 = i4 & Byte.MaxValue;

			if (i4_2 == 0)
				locals[i4 >> 8] = new RuntimeValue(0);
			else
				captures[captures.Count - i4_2][i4 >> 8] = new RuntimeValue(0);
			
			evalStack.Pop();			
			evalStack.Push(new RuntimeValue(ElaObject.Void));
			break;
		#endregion

		#region Complex and Debug
		case OpCode.Nop:
			break;
		case OpCode.Cout:
			Console.WriteLine(evalStack.Pop().ToString());
			break;
		case OpCode.Cin:
			evalStack.Push(new RuntimeValue(Console.ReadLine()));
			break;
		case OpCode.Sleep:
			System.Threading.Thread.Sleep(op.Data);
			break;
		case OpCode.Typeof:
			right = evalStack.Pop();
			left = evalStack.Pop();
			evalStack.Push(new RuntimeValue(left.Type == right.I4));
			break;
		case OpCode.Start:
			break;
		case OpCode.Finally:
			break;
		case OpCode.Leave:
			break;
		case OpCode.Throw:
			{
				ExecuteThrow(ElaErrorType.Code_User, thread);
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;					
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Throw_S:
			{
				ExecuteThrow((ElaErrorType)op.Data, thread);
				var mem = callStack.Peek();
				locals = mem.Locals;
				captures = mem.Captures;					
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		case OpCode.Term:
			{
				if (callStack.Count > 1)
				{
					var modMem = callStack.Pop();
					thread.Offset = modMem.ReturnAddress;
					pervasives.Pop();
					ReadPervasives(thread.CurrentModule);					
					
					var olp = callStack.Peek();					
					thread.SwitchModule(olp.ModuleHandle);					
					locals = olp.Locals;
					captures = olp.Captures;					
					ops = thread.CurrentModule.Ops;
					frame = thread.CurrentModule;
					evalStack.Pop();
				}
				else
				{
					thread.Busy = false;
					
					lock (syncRoot)
						Threads.Remove(thread);
						
					if (thread.ReturnValue != null)
					{
						thread.ReturnValue.Value = thread.EvalStack.Pop();
						thread.ReturnValue.Thread = null;
					}
						
					return;
				}
			}
			break;
		case OpCode.Runmod:
			{
				var frm = thread.GetModule(frame.Strings[op.Data]);
				
				if (modules[frm.Index] == null)
				{
					pervasives.Push(new Dictionary<String,Pervasive>());

					if (frm is IntrinsicFrame)
						modules[frm.Index] = ((IntrinsicFrame)frm).Memory;
					else
					{
						i4 = frm.Layouts[0].Size;
						locals = new RuntimeValue[i4];
						modules[frm.Index] = locals;
						captures = FastList<RuntimeValue[]>.Empty;
						var modMem = new CallPoint(thread.Offset, frm.Index, evalStack.Count, 
							null, locals, captures);
						callStack.Push(modMem);
						thread.SwitchModule(frm.Index);
						ops = thread.CurrentModule.Ops;
						frame = thread.CurrentModule;					
						thread.Offset = 1;
					}
				}
			}
			break;
		case OpCode.Valueof:
			if (ValueOf(thread))
			{
				var cp = callStack.Peek();
				locals = cp.Locals;
				captures = cp.Captures;
				ops = thread.CurrentModule.Ops;
				frame = thread.CurrentModule;
			}
			break;
		#endregion
	}
	#endregion
}
while (false);
		}
	}
}