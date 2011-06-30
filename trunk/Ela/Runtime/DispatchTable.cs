﻿using System;
using Ela.Runtime.ObjectModel;

namespace Ela.Runtime
{
    #region Generic
    internal sealed class NoneBinary : DispatchBinaryFun
    {
        private string op;

        internal NoneBinary(string op) : base(null)
        {
            this.op = op;
        }

        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            ctx.NoOperation(left, right, op);
            return ElaObject.GetDefault();
        }
    }

    internal sealed class TupleBinary : DispatchBinaryFun
    {
        internal TupleBinary(DispatchBinaryFun[][] funs) : base(funs) { }
        
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (left.TypeId == right.TypeId)
                return TupleWithTuple(left, right, ctx);
            if (left.TypeId == ElaMachine.TUP)
                return TupleWithAny(left, right, ctx);
            else
                return AnyWithTuple(left, right, ctx);
        }

        private ElaValue TupleWithTuple(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var tupleLeft = (ElaTuple)left.Ref;
            var tupleRight = (ElaTuple)right.Ref;

            if (tupleLeft.Length != tupleRight.Length)
            {
                ctx.Fail(ElaRuntimeError.TuplesLength, tupleLeft, tupleRight);
                return ElaObject.GetDefault();
            }
				

            var newArr = new ElaValue[tupleLeft.Length];

            for (var i = 0; i < tupleLeft.Length; i++)
                newArr[i] = PerformOp(tupleLeft.FastGet(i), tupleRight.FastGet(i), ctx);

            return new ElaValue(new ElaTuple(newArr));
        }

        private ElaValue AnyWithTuple(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var tuple = (ElaTuple)right.Ref;
            var newArr = new ElaValue[tuple.Length];

            for (var i = 0; i < tuple.Length; i++)
                newArr[i] = PerformOp(left, tuple.FastGet(i), ctx);

            return new ElaValue(new ElaTuple(newArr));
        }

        private ElaValue TupleWithAny(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var tuple = (ElaTuple)left.Ref;
            var newArr = new ElaValue[tuple.Length];

            for (var i = 0; i < tuple.Length; i++)
                newArr[i] = PerformOp(tuple.FastGet(i), right, ctx);

            return new ElaValue(new ElaTuple(newArr));
        }
    }

    //internal sealed class TupleUnary : DispatchBinaryFun
    //{
    //    private DispatchBinaryFun[] funs;

    //    internal TupleUnary(DispatchBinaryFun[] funs)
    //    {
    //        this.funs = funs;
    //    }

    //    protected internal override bool Call(EvalStack stack, ExecutionContext ctx)
    //    {
    //        var tuple = (ElaTuple)stack.Pop().Ref;
    //        var newArr = new ElaValue[tuple.Length];

    //        for (var i = 0; i < tuple.Length; i++)
    //            newArr[i] = ElaMachine.UnaryOp(funs, tuple.FastGet(i), ctx);

    //        stack.Push(new ElaValue(new ElaTuple(newArr)));
    //        return false;
    //    }
    //}
    
    internal sealed class ThunkBinary : DispatchBinaryFun
    {
        internal ThunkBinary(DispatchBinaryFun[][] funs) : base(funs) { }
        
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (!left.Ref.IsEvaluated())
            {
                var t = (ElaLazy)left.Ref;
                ctx.Failed = true;
                ctx.Thunk = t;
                return ElaObject.GetDefault();
            }

            if (!right.Ref.IsEvaluated())
            {
                var t = (ElaLazy)right.Ref;
                ctx.Failed = true;
                ctx.Thunk = t;
                return ElaObject.GetDefault();
            }

            return PerformOp(left.Force(ctx), right.Force(ctx), ctx);
        }
    }

    //internal sealed class ThunkUnary : DispatchBinaryFun
    //{
    //    private DispatchBinaryFun[] funs;

    //    internal ThunkUnary(DispatchBinaryFun[] funs)
    //    {
    //        this.funs = funs;
    //    }

    //    protected internal override bool Call(EvalStack stack, ExecutionContext ctx)
    //    {
    //        var left = stack.Pop();

    //        if (!left.Ref.IsEvaluated())
    //        {
    //            var t = (ElaLazy)left.Ref;
    //            ctx.Failed = true;
    //            ctx.Thunk = t;
    //            return true;
    //        }

    //        stack.Push(ElaMachine.UnaryOp(funs, left.Force(ctx), ctx));
    //        return false;
    //    }
    //}
    #endregion


    #region Add
    internal sealed class AddIntInt : DispatchBinaryFun
    {
        internal AddIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 + right.I4);
        }
    }

    internal sealed class AddIntLong : DispatchBinaryFun
    {
        internal AddIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 + right.GetLong()));            
        }
    }

    internal sealed class AddIntSingle : DispatchBinaryFun
    {
        internal AddIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 + right.DirectGetReal());            
        }
    }

    internal sealed class AddIntDouble : DispatchBinaryFun
    {
        internal AddIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.I4 + right.GetDouble()));            
        }
    }

    internal sealed class AddLongInt : DispatchBinaryFun
    {
        internal AddLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() + right.I4));            
        }
    }

    internal sealed class AddLongLong : DispatchBinaryFun
    {
        internal AddLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() + right.GetLong()));            
        }
    }

    internal sealed class AddLongSingle : DispatchBinaryFun
    {
        internal AddLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() + right.DirectGetReal());            
        }
    }

    internal sealed class AddLongDouble : DispatchBinaryFun
    {
        internal AddLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetLong() + right.GetDouble()));            
        }
    }

    internal sealed class AddSingleSingle : DispatchBinaryFun
    {
        internal AddSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() + right.DirectGetReal());            
        }
    }

    internal sealed class AddSingleInt : DispatchBinaryFun
    {
        internal AddSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() + right.I4);            
        }
    }

    internal sealed class AddSingleLong : DispatchBinaryFun
    {
        internal AddSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() + right.GetLong());            
        }
    }

    internal sealed class AddSingleDouble : DispatchBinaryFun
    {
        internal AddSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.DirectGetReal() + right.GetDouble()));            
        }
    }

    internal sealed class AddDoubleDouble : DispatchBinaryFun
    {
        internal AddDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() + right.GetDouble()));            
        }
    }

    internal sealed class AddDoubleInt : DispatchBinaryFun
    {
        internal AddDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() + right.I4));            
        }
    }

    internal sealed class AddDoubleLong : DispatchBinaryFun
    {
        internal AddDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() + right.GetLong()));            
        }
    }

    internal sealed class AddDoubleSingle : DispatchBinaryFun
    {
        internal AddDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() + right.DirectGetReal()));            
        }
    }
    #endregion


    #region Sub
    internal sealed class SubIntInt : DispatchBinaryFun
    {
        internal SubIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 - right.I4);
        }
    }

    internal sealed class SubIntLong : DispatchBinaryFun
    {
        internal SubIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 - right.GetLong()));
        }
    }

    internal sealed class SubIntSingle : DispatchBinaryFun
    {
        internal SubIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 - right.DirectGetReal());
        }
    }

    internal sealed class SubIntDouble : DispatchBinaryFun
    {
        internal SubIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.I4 - right.GetDouble()));
        }
    }

    internal sealed class SubLongInt : DispatchBinaryFun
    {
        internal SubLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() - right.I4));
        }
    }

    internal sealed class SubLongLong : DispatchBinaryFun
    {
        internal SubLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() - right.GetLong()));
        }
    }

    internal sealed class SubLongSingle : DispatchBinaryFun
    {
        internal SubLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() - right.DirectGetReal());
        }
    }

    internal sealed class SubLongDouble : DispatchBinaryFun
    {
        internal SubLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetLong() - right.GetDouble()));
        }
    }

    internal sealed class SubSingleSingle : DispatchBinaryFun
    {
        internal SubSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() - right.DirectGetReal());
        }
    }

    internal sealed class SubSingleInt : DispatchBinaryFun
    {
        internal SubSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() - right.I4);
        }
    }

    internal sealed class SubSingleLong : DispatchBinaryFun
    {
        internal SubSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() - right.GetLong());
        }
    }

    internal sealed class SubSingleDouble : DispatchBinaryFun
    {
        internal SubSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.DirectGetReal() - right.GetDouble()));
        }
    }

    internal sealed class SubDoubleDouble : DispatchBinaryFun
    {
        internal SubDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() - right.GetDouble()));
        }
    }

    internal sealed class SubDoubleInt : DispatchBinaryFun
    {
        internal SubDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() - right.I4));
        }
    }

    internal sealed class SubDoubleLong : DispatchBinaryFun
    {
        internal SubDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() - right.GetLong()));
        }
    }

    internal sealed class SubDoubleSingle : DispatchBinaryFun
    {
        internal SubDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() - right.DirectGetReal()));
        }
    }
    #endregion


    #region Mul
    internal sealed class MulIntInt : DispatchBinaryFun
    {
        internal MulIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 * right.I4);
        }
    }

    internal sealed class MulIntLong : DispatchBinaryFun
    {
        internal MulIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 * right.GetLong()));
        }
    }

    internal sealed class MulIntSingle : DispatchBinaryFun
    {
        internal MulIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 * right.DirectGetReal());
        }
    }

    internal sealed class MulIntDouble : DispatchBinaryFun
    {
        internal MulIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.I4 * right.GetDouble()));
        }
    }

    internal sealed class MulLongInt : DispatchBinaryFun
    {
        internal MulLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() * right.I4));
        }
    }

    internal sealed class MulLongLong : DispatchBinaryFun
    {
        internal MulLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() * right.GetLong()));
        }
    }

    internal sealed class MulLongSingle : DispatchBinaryFun
    {
        internal MulLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() * right.DirectGetReal());
        }
    }

    internal sealed class MulLongDouble : DispatchBinaryFun
    {
        internal MulLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetLong() * right.GetDouble()));
        }
    }

    internal sealed class MulSingleSingle : DispatchBinaryFun
    {
        internal MulSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() * right.DirectGetReal());
        }
    }

    internal sealed class MulSingleInt : DispatchBinaryFun
    {
        internal MulSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() * right.I4);
        }
    }

    internal sealed class MulSingleLong : DispatchBinaryFun
    {
        internal MulSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() * right.GetLong());
        }
    }

    internal sealed class MulSingleDouble : DispatchBinaryFun
    {
        internal MulSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.DirectGetReal() * right.GetDouble()));
        }
    }

    internal sealed class MulDoubleDouble : DispatchBinaryFun
    {
        internal MulDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() * right.GetDouble()));
        }
    }

    internal sealed class MulDoubleInt : DispatchBinaryFun
    {
        internal MulDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() * right.I4));
        }
    }

    internal sealed class MulDoubleLong : DispatchBinaryFun
    {
        internal MulDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() * right.GetLong()));
        }
    }

    internal sealed class MulDoubleSingle : DispatchBinaryFun
    {
        internal MulDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() * right.DirectGetReal()));
        }
    }
    #endregion


    #region Div
    internal sealed class DivIntInt : DispatchBinaryFun
    {
        internal DivIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.I4 / right.I4);
        }
    }

    internal sealed class DivIntLong : DispatchBinaryFun
    {
        internal DivIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.I4 / right.GetLong()));
        }
    }

    internal sealed class DivIntSingle : DispatchBinaryFun
    {
        internal DivIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 / right.DirectGetReal());
        }
    }

    internal sealed class DivIntDouble : DispatchBinaryFun
    {
        internal DivIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.I4 / right.GetDouble()));
        }
    }

    internal sealed class DivLongInt : DispatchBinaryFun
    {
        internal DivLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.GetLong() / right.I4));
        }
    }

    internal sealed class DivLongLong : DispatchBinaryFun
    {
        internal DivLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.GetLong() / right.GetLong()));
        }
    }

    internal sealed class DivLongSingle : DispatchBinaryFun
    {
        internal DivLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() / right.DirectGetReal());
        }
    }

    internal sealed class DivLongDouble : DispatchBinaryFun
    {
        internal DivLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetLong() / right.GetDouble()));
        }
    }

    internal sealed class DivSingleSingle : DispatchBinaryFun
    {
        internal DivSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() / right.DirectGetReal());
        }
    }

    internal sealed class DivSingleInt : DispatchBinaryFun
    {
        internal DivSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.DirectGetReal() / right.I4);
        }
    }

    internal sealed class DivSingleLong : DispatchBinaryFun
    {
        internal DivSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.DirectGetReal() / right.GetLong());
        }
    }

    internal sealed class DivSingleDouble : DispatchBinaryFun
    {
        internal DivSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.DirectGetReal() / right.GetDouble()));
        }
    }

    internal sealed class DivDoubleDouble : DispatchBinaryFun
    {
        internal DivDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() / right.GetDouble()));
        }
    }

    internal sealed class DivDoubleInt : DispatchBinaryFun
    {
        internal DivDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaDouble(left.GetDouble() / right.I4));
        }
    }

    internal sealed class DivDoubleLong : DispatchBinaryFun
    {
        internal DivDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaDouble(left.GetDouble() / right.GetLong()));
        }
    }

    internal sealed class DivDoubleSingle : DispatchBinaryFun
    {
        internal DivDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() / right.DirectGetReal()));
        }
    }
    #endregion


    #region Rem
    internal sealed class RemIntInt : DispatchBinaryFun
    {
        internal RemIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.I4 % right.I4);
        }
    }

    internal sealed class RemIntLong : DispatchBinaryFun
    {
        internal RemIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.I4 % right.GetLong()));
        }
    }

    internal sealed class RemIntSingle : DispatchBinaryFun
    {
        internal RemIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 % right.DirectGetReal());
        }
    }

    internal sealed class RemIntDouble : DispatchBinaryFun
    {
        internal RemIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.I4 % right.GetDouble()));
        }
    }

    internal sealed class RemLongInt : DispatchBinaryFun
    {
        internal RemLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.GetLong() % right.I4));
        }
    }

    internal sealed class RemLongLong : DispatchBinaryFun
    {
        internal RemLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaLong(left.GetLong() % right.GetLong()));
        }
    }

    internal sealed class RemLongSingle : DispatchBinaryFun
    {
        internal RemLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() % right.DirectGetReal());
        }
    }

    internal sealed class RemLongDouble : DispatchBinaryFun
    {
        internal RemLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetLong() % right.GetDouble()));
        }
    }

    internal sealed class RemSingleSingle : DispatchBinaryFun
    {
        internal RemSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() % right.DirectGetReal());
        }
    }

    internal sealed class RemSingleInt : DispatchBinaryFun
    {
        internal RemSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.DirectGetReal() % right.I4);
        }
    }

    internal sealed class RemSingleLong : DispatchBinaryFun
    {
        internal RemSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(left.DirectGetReal() % right.GetLong());
        }
    }

    internal sealed class RemSingleDouble : DispatchBinaryFun
    {
        internal RemSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.DirectGetReal() % right.GetDouble()));
        }
    }

    internal sealed class RemDoubleDouble : DispatchBinaryFun
    {
        internal RemDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() % right.GetDouble()));
        }
    }

    internal sealed class RemDoubleInt : DispatchBinaryFun
    {
        internal RemDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.I4 == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaDouble(left.GetDouble() % right.I4));
        }
    }

    internal sealed class RemDoubleLong : DispatchBinaryFun
    {
        internal RemDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            if (right.GetLong() == 0)
            {
                ctx.DivideByZero(left);
                return ElaObject.GetDefault();
            }

            return new ElaValue(new ElaDouble(left.GetDouble() % right.GetLong()));
        }
    }

    internal sealed class RemDoubleSingle : DispatchBinaryFun
    {
        internal RemDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaDouble(left.GetDouble() % right.DirectGetReal()));
        }
    }
    #endregion


    #region Pow
    internal sealed class PowIntInt : DispatchBinaryFun
    {
        internal PowIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.I4, right.I4));
        }
    }

    internal sealed class PowIntLong : DispatchBinaryFun
    {
        internal PowIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.I4, right.GetLong()));
        }
    }

    internal sealed class PowIntSingle : DispatchBinaryFun
    {
        internal PowIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.I4, right.DirectGetReal()));
        }
    }

    internal sealed class PowIntDouble : DispatchBinaryFun
    {
        internal PowIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.I4, right.GetDouble()));
        }
    }

    internal sealed class PowLongInt : DispatchBinaryFun
    {
        internal PowLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetLong(), right.I4));
        }
    }

    internal sealed class PowLongLong : DispatchBinaryFun
    {
        internal PowLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetLong(), right.GetLong()));
        }
    }

    internal sealed class PowLongSingle : DispatchBinaryFun
    {
        internal PowLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetLong(), right.DirectGetReal()));
        }
    }

    internal sealed class PowLongDouble : DispatchBinaryFun
    {
        internal PowLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
           return new ElaValue(Math.Pow(left.GetLong(), right.GetDouble()));
        }
    }

    internal sealed class PowSingleSingle : DispatchBinaryFun
    {
        internal PowSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.DirectGetReal(), right.DirectGetReal()));
        }
    }

    internal sealed class PowSingleInt : DispatchBinaryFun
    {
        internal PowSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.DirectGetReal(), right.I4));
        }
    }

    internal sealed class PowSingleLong : DispatchBinaryFun
    {
        internal PowSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.DirectGetReal(), right.GetLong()));
        }
    }

    internal sealed class PowSingleDouble : DispatchBinaryFun
    {
        internal PowSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.DirectGetReal(), right.GetDouble()));
        }
    }

    internal sealed class PowDoubleDouble : DispatchBinaryFun
    {
        internal PowDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetDouble(), right.GetDouble()));
        }
    }

    internal sealed class PowDoubleInt : DispatchBinaryFun
    {
        internal PowDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetDouble(), right.I4));
        }
    }

    internal sealed class PowDoubleLong : DispatchBinaryFun
    {
        internal PowDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetDouble(), right.GetLong()));
        }
    }

    internal sealed class PowDoubleSingle : DispatchBinaryFun
    {
        internal PowDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(Math.Pow(left.GetDouble(), right.DirectGetReal()));
        }
    }
    #endregion


    #region And
    internal sealed class AndIntInt : DispatchBinaryFun
    {
        internal AndIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 & right.I4);
        }
    }

    internal sealed class AndIntLong : DispatchBinaryFun
    {
        internal AndIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 & right.GetLong()));
        }
    }

    internal sealed class AndLongInt : DispatchBinaryFun
    {
        internal AndLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() & right.I4));
        }
    }

    internal sealed class AndLongLong : DispatchBinaryFun
    {
        internal AndLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() & right.GetLong()));
        }
    }
    #endregion


    #region Bor
    internal sealed class BorIntInt : DispatchBinaryFun
    {
        internal BorIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 | right.I4);
        }
    }

    internal sealed class BorIntLong : DispatchBinaryFun
    {
        internal BorIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var l = (long)left.I4;
            return new ElaValue(new ElaLong(l | right.GetLong()));
        }
    }

    internal sealed class BorLongInt : DispatchBinaryFun
    {
        internal BorLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var l = (long)right.I4;
            return new ElaValue(new ElaLong(left.GetLong() | l));
        }
    }

    internal sealed class BorLongLong : DispatchBinaryFun
    {
        internal BorLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() | right.GetLong()));
        }
    }
    #endregion


    #region Xor
    internal sealed class XorIntInt : DispatchBinaryFun
    {
        internal XorIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 ^ right.I4);
        }
    }

    internal sealed class XorIntLong : DispatchBinaryFun
    {
        internal XorIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var l = (long)left.I4;
            return new ElaValue(new ElaLong(l ^ right.GetLong()));
        }
    }

    internal sealed class XorLongInt : DispatchBinaryFun
    {
        internal XorLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var l = (long)right.I4;
            return new ElaValue(new ElaLong(left.GetLong() ^ l));
        }
    }

    internal sealed class XorLongLong : DispatchBinaryFun
    {
        internal XorLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() ^ right.GetLong()));
        }
    }
    #endregion


    #region Shl
    internal sealed class ShlIntInt : DispatchBinaryFun
    {
        internal ShlIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 << right.I4);
        }
    }

    internal sealed class ShlIntLong : DispatchBinaryFun
    {
        internal ShlIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 << (int)right.GetLong()));
        }
    }

    internal sealed class ShlLongInt : DispatchBinaryFun
    {
        internal ShlLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() << right.I4));
        }
    }

    internal sealed class ShlLongLong : DispatchBinaryFun
    {
        internal ShlLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() << (int)right.GetLong()));
        }
    }
    #endregion


    #region Shr
    internal sealed class ShrIntInt : DispatchBinaryFun
    {
        internal ShrIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >> right.I4);
        }
    }

    internal sealed class ShrIntLong : DispatchBinaryFun
    {
        internal ShrIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.I4 >> (int)right.GetLong()));
        }
    }

    internal sealed class ShrLongInt : DispatchBinaryFun
    {
        internal ShrLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() >> right.I4));
        }
    }

    internal sealed class ShrLongLong : DispatchBinaryFun
    {
        internal ShrLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(new ElaLong(left.GetLong() >> (int)right.GetLong()));
        }
    }
    #endregion


    #region Gtr
    internal sealed class GtrIntInt : DispatchBinaryFun
    {
        internal GtrIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 > right.I4);
        }
    }

    internal sealed class GtrIntLong : DispatchBinaryFun
    {
        internal GtrIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 > right.GetLong());
        }
    }

    internal sealed class GtrIntSingle : DispatchBinaryFun
    {
        internal GtrIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 > right.DirectGetReal());
        }
    }

    internal sealed class GtrIntDouble : DispatchBinaryFun
    {
        internal GtrIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 > right.GetDouble());
        }
    }

    internal sealed class GtrLongInt : DispatchBinaryFun
    {
        internal GtrLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() > right.I4);
        }
    }

    internal sealed class GtrLongLong : DispatchBinaryFun
    {
        internal GtrLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() > right.GetLong());
        }
    }

    internal sealed class GtrLongSingle : DispatchBinaryFun
    {
        internal GtrLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() > right.DirectGetReal());
        }
    }

    internal sealed class GtrLongDouble : DispatchBinaryFun
    {
        internal GtrLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() > right.GetDouble());
        }
    }

    internal sealed class GtrSingleSingle : DispatchBinaryFun
    {
        internal GtrSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() > right.DirectGetReal());
        }
    }

    internal sealed class GtrSingleInt : DispatchBinaryFun
    {
        internal GtrSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() > right.I4);
        }
    }

    internal sealed class GtrSingleLong : DispatchBinaryFun
    {
        internal GtrSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() > right.GetLong());
        }
    }

    internal sealed class GtrSingleDouble : DispatchBinaryFun
    {
        internal GtrSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() > right.GetDouble());
        }
    }

    internal sealed class GtrDoubleDouble : DispatchBinaryFun
    {
        internal GtrDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() > right.GetDouble());
        }
    }

    internal sealed class GtrDoubleInt : DispatchBinaryFun
    {
        internal GtrDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() > right.I4);
        }
    }

    internal sealed class GtrDoubleLong : DispatchBinaryFun
    {
        internal GtrDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() > right.GetLong());
        }
    }

    internal sealed class GtrDoubleSingle : DispatchBinaryFun
    {
        internal GtrDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() > right.DirectGetReal());
        }
    }

    internal sealed class GtrCharChar : DispatchBinaryFun
    {
        internal GtrCharChar(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 > right.I4);
        }
    }

    internal sealed class GtrStringString : DispatchBinaryFun
    {
        internal GtrStringString(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetString().CompareTo(right.DirectGetString()) > 0);
        }
    }
    #endregion


    #region Lsr
    internal sealed class LsrIntInt : DispatchBinaryFun
    {
        internal LsrIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 < right.I4);
        }
    }

    internal sealed class LsrIntLong : DispatchBinaryFun
    {
        internal LsrIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 < right.GetLong());
        }
    }

    internal sealed class LsrIntSingle : DispatchBinaryFun
    {
        internal LsrIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 < right.DirectGetReal());
        }
    }

    internal sealed class LsrIntDouble : DispatchBinaryFun
    {
        internal LsrIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 < right.GetDouble());
        }
    }

    internal sealed class LsrLongInt : DispatchBinaryFun
    {
        internal LsrLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() < right.I4);
        }
    }

    internal sealed class LsrLongLong : DispatchBinaryFun
    {
        internal LsrLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() < right.GetLong());
        }
    }

    internal sealed class LsrLongSingle : DispatchBinaryFun
    {
        internal LsrLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() < right.DirectGetReal());
        }
    }

    internal sealed class LsrLongDouble : DispatchBinaryFun
    {
        internal LsrLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() < right.GetDouble());
        }
    }

    internal sealed class LsrSingleSingle : DispatchBinaryFun
    {
        internal LsrSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() < right.DirectGetReal());
        }
    }

    internal sealed class LsrSingleInt : DispatchBinaryFun
    {
        internal LsrSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() < right.I4);
        }
    }

    internal sealed class LsrSingleLong : DispatchBinaryFun
    {
        internal LsrSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() < right.GetLong());
        }
    }

    internal sealed class LsrSingleDouble : DispatchBinaryFun
    {
        internal LsrSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() < right.GetDouble());
        }
    }

    internal sealed class LsrDoubleDouble : DispatchBinaryFun
    {
        internal LsrDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() < right.GetDouble());
        }
    }

    internal sealed class LsrDoubleInt : DispatchBinaryFun
    {
        internal LsrDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() < right.I4);
        }
    }

    internal sealed class LsrDoubleLong : DispatchBinaryFun
    {
        internal LsrDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() < right.GetLong());
        }
    }

    internal sealed class LsrDoubleSingle : DispatchBinaryFun
    {
        internal LsrDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() < right.DirectGetReal());
        }
    }

    internal sealed class LsrCharChar : DispatchBinaryFun
    {
        internal LsrCharChar(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 < right.I4);
        }
    }

    internal sealed class LsrStringString : DispatchBinaryFun
    {
        internal LsrStringString(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetString().CompareTo(right.DirectGetString()) < 0);
        }
    }
    #endregion


    #region Gte
    internal sealed class GteIntInt : DispatchBinaryFun
    {
        internal GteIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >= right.I4);
        }
    }

    internal sealed class GteIntLong : DispatchBinaryFun
    {
        internal GteIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >= right.GetLong());
        }
    }

    internal sealed class GteIntSingle : DispatchBinaryFun
    {
        internal GteIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >= right.DirectGetReal());
        }
    }

    internal sealed class GteIntDouble : DispatchBinaryFun
    {
        internal GteIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >= right.GetDouble());
        }
    }

    internal sealed class GteLongInt : DispatchBinaryFun
    {
        internal GteLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() >= right.I4);
        }
    }

    internal sealed class GteLongLong : DispatchBinaryFun
    {
        internal GteLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() >= right.GetLong());
        }
    }

    internal sealed class GteLongSingle : DispatchBinaryFun
    {
        internal GteLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() >= right.DirectGetReal());
        }
    }

    internal sealed class GteLongDouble : DispatchBinaryFun
    {
        internal GteLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() >= right.GetDouble());
        }
    }

    internal sealed class GteSingleSingle : DispatchBinaryFun
    {
        internal GteSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() >= right.DirectGetReal());
        }
    }

    internal sealed class GteSingleInt : DispatchBinaryFun
    {
        internal GteSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() >= right.I4);
        }
    }

    internal sealed class GteSingleLong : DispatchBinaryFun
    {
        internal GteSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() >= right.GetLong());
        }
    }

    internal sealed class GteSingleDouble : DispatchBinaryFun
    {
        internal GteSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() >= right.GetDouble());
        }
    }

    internal sealed class GteDoubleDouble : DispatchBinaryFun
    {
        internal GteDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() >= right.GetDouble());
        }
    }

    internal sealed class GteDoubleInt : DispatchBinaryFun
    {
        internal GteDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() >= right.I4);
        }
    }

    internal sealed class GteDoubleLong : DispatchBinaryFun
    {
        internal GteDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() >= right.GetLong());
        }
    }

    internal sealed class GteDoubleSingle : DispatchBinaryFun
    {
        internal GteDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() >= right.DirectGetReal());
        }
    }

    internal sealed class GteCharChar : DispatchBinaryFun
    {
        internal GteCharChar(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 >= right.I4);
        }
    }

    internal sealed class GteStringString : DispatchBinaryFun
    {
        internal GteStringString(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetString().CompareTo(right.DirectGetString()) >= 0);
        }
    }
    #endregion


    #region Lse
    internal sealed class LseIntInt : DispatchBinaryFun
    {
        internal LseIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 <= right.I4);
        }
    }

    internal sealed class LseIntLong : DispatchBinaryFun
    {
        internal LseIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 <= right.GetLong());
        }
    }

    internal sealed class LseIntSingle : DispatchBinaryFun
    {
        internal LseIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 <= right.DirectGetReal());
        }
    }

    internal sealed class LseIntDouble : DispatchBinaryFun
    {
        internal LseIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 <= right.GetDouble());
        }
    }

    internal sealed class LseLongInt : DispatchBinaryFun
    {
        internal LseLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() <= right.I4);
        }
    }

    internal sealed class LseLongLong : DispatchBinaryFun
    {
        internal LseLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() <= right.GetLong());
        }
    }

    internal sealed class LseLongSingle : DispatchBinaryFun
    {
        internal LseLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() <= right.DirectGetReal());
        }
    }

    internal sealed class LseLongDouble : DispatchBinaryFun
    {
        internal LseLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() <= right.GetDouble());
        }
    }

    internal sealed class LseSingleSingle : DispatchBinaryFun
    {
        internal LseSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() <= right.DirectGetReal());
        }
    }

    internal sealed class LseSingleInt : DispatchBinaryFun
    {
        internal LseSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() <= right.I4);
        }
    }

    internal sealed class LseSingleLong : DispatchBinaryFun
    {
        internal LseSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() <= right.GetLong());
        }
    }

    internal sealed class LseSingleDouble : DispatchBinaryFun
    {
        internal LseSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() <= right.GetDouble());
        }
    }

    internal sealed class LseDoubleDouble : DispatchBinaryFun
    {
        internal LseDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() <= right.GetDouble());
        }
    }

    internal sealed class LseDoubleInt : DispatchBinaryFun
    {
        internal LseDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() <= right.I4);
        }
    }

    internal sealed class LseDoubleLong : DispatchBinaryFun
    {
        internal LseDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() <= right.GetLong());
        }
    }

    internal sealed class LseDoubleSingle : DispatchBinaryFun
    {
        internal LseDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() <= right.DirectGetReal());
        }
    }

    internal sealed class LseCharChar : DispatchBinaryFun
    {
        internal LseCharChar(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 <= right.I4);
        }
    }

    internal sealed class LseStringString : DispatchBinaryFun
    {
        internal LseStringString(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetString().CompareTo(right.DirectGetString()) <= 0);
        }
    }
    #endregion


    #region Eql
    internal sealed class EqlIntInt : DispatchBinaryFun
    {
        internal EqlIntInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 == right.I4);
        }
    }

    internal sealed class EqlIntLong : DispatchBinaryFun
    {
        internal EqlIntLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 == right.GetLong());
        }
    }

    internal sealed class EqlIntSingle : DispatchBinaryFun
    {
        internal EqlIntSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 == right.DirectGetReal());
        }
    }

    internal sealed class EqlIntDouble : DispatchBinaryFun
    {
        internal EqlIntDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 == right.GetDouble());
        }
    }

    internal sealed class EqlLongInt : DispatchBinaryFun
    {
        internal EqlLongInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() == right.I4);
        }
    }

    internal sealed class EqlLongLong : DispatchBinaryFun
    {
        internal EqlLongLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() == right.GetLong());
        }
    }

    internal sealed class EqlLongSingle : DispatchBinaryFun
    {
        internal EqlLongSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() == right.DirectGetReal());
        }
    }

    internal sealed class EqlLongDouble : DispatchBinaryFun
    {
        internal EqlLongDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetLong() == right.GetDouble());
        }
    }

    internal sealed class EqlSingleSingle : DispatchBinaryFun
    {
        internal EqlSingleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() == right.DirectGetReal());
        }
    }

    internal sealed class EqlSingleInt : DispatchBinaryFun
    {
        internal EqlSingleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() == right.I4);
        }
    }

    internal sealed class EqlSingleLong : DispatchBinaryFun
    {
        internal EqlSingleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() == right.GetLong());
        }
    }

    internal sealed class EqlSingleDouble : DispatchBinaryFun
    {
        internal EqlSingleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetReal() == right.GetDouble());
        }
    }

    internal sealed class EqlDoubleDouble : DispatchBinaryFun
    {
        internal EqlDoubleDouble(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() == right.GetDouble());
        }
    }

    internal sealed class EqlDoubleInt : DispatchBinaryFun
    {
        internal EqlDoubleInt(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() == right.I4);
        }
    }

    internal sealed class EqlDoubleLong : DispatchBinaryFun
    {
        internal EqlDoubleLong(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() == right.GetLong());
        }
    }

    internal sealed class EqlDoubleSingle : DispatchBinaryFun
    {
        internal EqlDoubleSingle(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.GetDouble() == right.DirectGetReal());
        }
    }

    internal sealed class EqlCharChar : DispatchBinaryFun
    {
        internal EqlCharChar(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.I4 == right.I4);
        }
    }

    internal sealed class EqlStringString : DispatchBinaryFun
    {
        internal EqlStringString(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(left.DirectGetString() == right.DirectGetString());
        }
    }

    internal sealed class EqlModMod : DispatchBinaryFun
    {
        internal EqlModMod(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return new ElaValue(((ElaModule)left.Ref).Handle == ((ElaModule)right.Ref).Handle);
        }
    }

    internal sealed class EqlFunFun : DispatchBinaryFun
    {
        internal EqlFunFun(DispatchBinaryFun[][] funs) : base(funs) { }
        protected internal override ElaValue Call(ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            var leftFun = (ElaFunction)left.Ref;
            var rightFun = (ElaFunction)right.Ref;

            var ret = Object.ReferenceEquals(leftFun, rightFun) ||
                leftFun.Handle == rightFun.Handle &&
                leftFun.ModuleHandle == rightFun.ModuleHandle &&
                leftFun.AppliedParameters == rightFun.AppliedParameters &&
                leftFun.Flip == rightFun.Flip &&
                EqHelper.ListEquals<ElaValue>(leftFun.Parameters, rightFun.Parameters) &&
                (Object.ReferenceEquals(leftFun.LastParameter.Ref, rightFun.LastParameter.Ref) ||
                    leftFun.LastParameter.Equals(rightFun.LastParameter));
            return new ElaValue(ret);
        }
    }
    #endregion
}
