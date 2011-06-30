﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Ela.CodeModel;
using Ela.Compilation;
using Ela.Debug;
using Ela.Linking;
using Ela.Runtime.ObjectModel;
using System.Runtime.InteropServices;

namespace Ela.Runtime
{
    using FunMap = Dictionary<String,ElaOverloadedFunction>;
   
    public sealed class ElaMachine : IDisposable
	{
		#region Construction
		internal ElaValue[][] modules;
		private readonly CodeAssembly asm;
        private readonly IntrinsicFrame argMod;
        private readonly int argModHandle;
        internal readonly FunMap overloads;
        private static readonly ElaFunction dummyFun = new ElaFunction(0, 0, 0, null, null);

        Dictionary<String,ElaFunction> AddToMap(string fun, string t1)
        {
            var funObj = default(ElaOverloadedFunction);

            if (!overloads.TryGetValue(fun, out funObj))
            {
                funObj = new ElaOverloadedFunction(fun, new Dictionary<String,ElaFunction>(), FastList<ElaValue[]>.Empty, this);
                overloads.Add(fun, funObj);
            }

            var of = new ElaOverloadedFunction(fun, new Dictionary<String, ElaFunction>(), FastList<ElaValue[]>.Empty, this);
            funObj.overloads.Add(t1, of);
            return of.overloads;
        }

		public ElaMachine(CodeAssembly asm)
		{
			this.asm = asm;
            overloads = new FunMap();
			var frame = asm.GetRootModule();
			MainThread = new WorkerThread(asm);
			var lays = frame.Layouts[0];
			modules = new ElaValue[asm.ModuleCount][];
			var mem = new ElaValue[lays.Size];
			modules[0] = mem;

            //var d = AddToMap("$add", "Int");
            //d.Add("Int", dummyFun);
            //d.Add("Long", dummyFun);

            argModHandle = asm.TryGetModuleHandle("$Args");

            if (argModHandle != -1)
            {
                argMod = (IntrinsicFrame)asm.GetModule(argModHandle);
                modules[argModHandle] = argMod.Memory;
                ReadPervasives(MainThread, argMod, argModHandle);
            }

			MainThread.CallStack.Push(new CallPoint(0, new EvalStack(lays.StackSize), mem, FastList<ElaValue[]>.Empty));
            InitializeTables();
		}
		#endregion


        #region Types
        internal const int UNI = (Int32)ElaTypeCode.Unit;
		internal const int INT = (Int32)ElaTypeCode.Integer;
		internal const int REA = (Int32)ElaTypeCode.Single;
		internal const int BYT = (Int32)ElaTypeCode.Boolean;
		internal const int CHR = (Int32)ElaTypeCode.Char;
		internal const int LNG = (Int32)ElaTypeCode.Long;
		internal const int DBL = (Int32)ElaTypeCode.Double;
		internal const int STR = (Int32)ElaTypeCode.String;
		internal const int LST = (Int32)ElaTypeCode.List;
		internal const int TUP = (Int32)ElaTypeCode.Tuple;
		internal const int REC = (Int32)ElaTypeCode.Record;
		internal const int FUN = (Int32)ElaTypeCode.Function;
		internal const int OBJ = (Int32)ElaTypeCode.Object;
		internal const int MOD = (Int32)ElaTypeCode.Module;
		internal const int LAZ = (Int32)ElaTypeCode.Lazy;
		internal const int VAR = (Int32)ElaTypeCode.Variant;

        private readonly static DispatchBinaryFun ___ = new NoneBinary("addition");
        
        private sealed class __table
        {
            internal readonly DispatchBinaryFun[][] table = 
            {
                //                        NON  INT  LNG  SNG  DBL  BYT  CHR  STR  UNI  LST  ___  TUP  REC  FUN  OBJ  MOD  LAZ  VAR
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //INT
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //NON
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //LNG
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //SNG
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //DBL
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //BYT
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //CHR
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //STR
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //UNI
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //LST
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //___
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //TUP
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //REC
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //FUN
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //OBJ
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //MOD
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //LAZ
                new DispatchBinaryFun[] { ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___ }, //VAR
            };
        }

        private readonly DispatchBinaryFun[][] add_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] sub_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] mul_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] div_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] rem_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] pow_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] and_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] bor_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] xor_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] shl_ovl = new __table().table;
        private readonly DispatchBinaryFun[][] shr_ovl = new __table().table;
        
        private DispatchBinaryFun addIntInt;
        private DispatchBinaryFun addIntLng;
        private DispatchBinaryFun addIntSng;
        private DispatchBinaryFun addIntDbl;
        private DispatchBinaryFun addLngInt;
        private DispatchBinaryFun addLngLng;
        private DispatchBinaryFun addLngSng;
        private DispatchBinaryFun addLngDbl;
        private DispatchBinaryFun addSngInt;
        private DispatchBinaryFun addSngLng;
        private DispatchBinaryFun addSngSng;
        private DispatchBinaryFun addSngDbl;
        private DispatchBinaryFun addDblInt;
        private DispatchBinaryFun addDblLng;
        private DispatchBinaryFun addDblSng;
        private DispatchBinaryFun addDblDbl;
        private DispatchBinaryFun addTupTup;
        private DispatchBinaryFun addLazLaz;

        private DispatchBinaryFun subIntInt;
        private DispatchBinaryFun subIntLng;
        private DispatchBinaryFun subIntSng;
        private DispatchBinaryFun subIntDbl;
        private DispatchBinaryFun subLngInt;
        private DispatchBinaryFun subLngLng;
        private DispatchBinaryFun subLngSng;
        private DispatchBinaryFun subLngDbl;
        private DispatchBinaryFun subSngInt;
        private DispatchBinaryFun subSngLng;
        private DispatchBinaryFun subSngSng;
        private DispatchBinaryFun subSngDbl;
        private DispatchBinaryFun subDblInt;
        private DispatchBinaryFun subDblLng;
        private DispatchBinaryFun subDblSng;
        private DispatchBinaryFun subDblDbl;
        private DispatchBinaryFun subTupTup;
        private DispatchBinaryFun subLazLaz;

        private DispatchBinaryFun mulIntInt;
        private DispatchBinaryFun mulIntLng;
        private DispatchBinaryFun mulIntSng;
        private DispatchBinaryFun mulIntDbl;
        private DispatchBinaryFun mulLngInt;
        private DispatchBinaryFun mulLngLng;
        private DispatchBinaryFun mulLngSng;
        private DispatchBinaryFun mulLngDbl;
        private DispatchBinaryFun mulSngInt;
        private DispatchBinaryFun mulSngLng;
        private DispatchBinaryFun mulSngSng;
        private DispatchBinaryFun mulSngDbl;
        private DispatchBinaryFun mulDblInt;
        private DispatchBinaryFun mulDblLng;
        private DispatchBinaryFun mulDblSng;
        private DispatchBinaryFun mulDblDbl;
        private DispatchBinaryFun mulTupTup;
        private DispatchBinaryFun mulLazLaz;

        private DispatchBinaryFun divIntInt;
        private DispatchBinaryFun divIntLng;
        private DispatchBinaryFun divIntSng;
        private DispatchBinaryFun divIntDbl;
        private DispatchBinaryFun divLngInt;
        private DispatchBinaryFun divLngLng;
        private DispatchBinaryFun divLngSng;
        private DispatchBinaryFun divLngDbl;
        private DispatchBinaryFun divSngInt;
        private DispatchBinaryFun divSngLng;
        private DispatchBinaryFun divSngSng;
        private DispatchBinaryFun divSngDbl;
        private DispatchBinaryFun divDblInt;
        private DispatchBinaryFun divDblLng;
        private DispatchBinaryFun divDblSng;
        private DispatchBinaryFun divDblDbl;
        private DispatchBinaryFun divTupTup;
        private DispatchBinaryFun divLazLaz;

        private DispatchBinaryFun remIntInt;
        private DispatchBinaryFun remIntLng;
        private DispatchBinaryFun remIntSng;
        private DispatchBinaryFun remIntDbl;
        private DispatchBinaryFun remLngInt;
        private DispatchBinaryFun remLngLng;
        private DispatchBinaryFun remLngSng;
        private DispatchBinaryFun remLngDbl;
        private DispatchBinaryFun remSngInt;
        private DispatchBinaryFun remSngLng;
        private DispatchBinaryFun remSngSng;
        private DispatchBinaryFun remSngDbl;
        private DispatchBinaryFun remDblInt;
        private DispatchBinaryFun remDblLng;
        private DispatchBinaryFun remDblSng;
        private DispatchBinaryFun remDblDbl;
        private DispatchBinaryFun remTupTup;
        private DispatchBinaryFun remLazLaz;

        private DispatchBinaryFun powIntInt;
        private DispatchBinaryFun powIntLng;
        private DispatchBinaryFun powIntSng;
        private DispatchBinaryFun powIntDbl;
        private DispatchBinaryFun powLngInt;
        private DispatchBinaryFun powLngLng;
        private DispatchBinaryFun powLngSng;
        private DispatchBinaryFun powLngDbl;
        private DispatchBinaryFun powSngInt;
        private DispatchBinaryFun powSngLng;
        private DispatchBinaryFun powSngSng;
        private DispatchBinaryFun powSngDbl;
        private DispatchBinaryFun powDblInt;
        private DispatchBinaryFun powDblLng;
        private DispatchBinaryFun powDblSng;
        private DispatchBinaryFun powDblDbl;
        private DispatchBinaryFun powTupTup;
        private DispatchBinaryFun powLazLaz;

        private DispatchBinaryFun andIntInt;
        private DispatchBinaryFun andIntLng;
        private DispatchBinaryFun andLngInt;
        private DispatchBinaryFun andLngLng;
        private DispatchBinaryFun andTupTup;
        private DispatchBinaryFun andLazLaz;

        private DispatchBinaryFun borIntInt;
        private DispatchBinaryFun borIntLng;
        private DispatchBinaryFun borLngInt;
        private DispatchBinaryFun borLngLng;
        private DispatchBinaryFun borTupTup;
        private DispatchBinaryFun borLazLaz;

        private DispatchBinaryFun xorIntInt;
        private DispatchBinaryFun xorIntLng;
        private DispatchBinaryFun xorLngInt;
        private DispatchBinaryFun xorLngLng;
        private DispatchBinaryFun xorTupTup;
        private DispatchBinaryFun xorLazLaz;

        private DispatchBinaryFun shlIntInt;
        private DispatchBinaryFun shlIntLng;
        private DispatchBinaryFun shlLngInt;
        private DispatchBinaryFun shlLngLng;
        private DispatchBinaryFun shlTupTup;
        private DispatchBinaryFun shlLazLaz;

        private DispatchBinaryFun shrIntInt;
        private DispatchBinaryFun shrIntLng;
        private DispatchBinaryFun shrLngInt;
        private DispatchBinaryFun shrLngLng;
        private DispatchBinaryFun shrTupTup;
        private DispatchBinaryFun shrLazLaz;


        private void InitializeTables()
        {
            addIntInt = new AddIntInt(add_ovl);
            addIntLng = new AddIntLong(add_ovl);
            addIntSng = new AddIntSingle(add_ovl);
            addIntDbl = new AddIntDouble(add_ovl);
            addLngInt = new AddLongInt(add_ovl);
            addLngLng = new AddLongLong(add_ovl);
            addLngSng = new AddLongSingle(add_ovl);
            addLngDbl = new AddLongDouble(add_ovl);
            addSngInt = new AddSingleInt(add_ovl);
            addSngLng = new AddSingleLong(add_ovl);
            addSngSng = new AddSingleSingle(add_ovl);
            addSngDbl = new AddSingleDouble(add_ovl);
            addDblInt = new AddDoubleInt(add_ovl);
            addDblLng = new AddDoubleLong(add_ovl);
            addDblSng = new AddDoubleSingle(add_ovl);
            addDblDbl = new AddDoubleDouble(add_ovl);
            addTupTup = new TupleBinary(add_ovl);
            addLazLaz = new ThunkBinary(add_ovl);

            subIntInt = new SubIntInt(sub_ovl);
            subIntLng = new SubIntLong(sub_ovl);
            subIntSng = new SubIntSingle(sub_ovl);
            subIntDbl = new SubIntDouble(sub_ovl);
            subLngInt = new SubLongInt(sub_ovl);
            subLngLng = new SubLongLong(sub_ovl);
            subLngSng = new SubLongSingle(sub_ovl);
            subLngDbl = new SubLongDouble(sub_ovl);
            subSngInt = new SubSingleInt(sub_ovl);
            subSngLng = new SubSingleLong(sub_ovl);
            subSngSng = new SubSingleSingle(sub_ovl);
            subSngDbl = new SubSingleDouble(sub_ovl);
            subDblInt = new SubDoubleInt(sub_ovl);
            subDblLng = new SubDoubleLong(sub_ovl);
            subDblSng = new SubDoubleSingle(sub_ovl);
            subDblDbl = new SubDoubleDouble(sub_ovl);
            subTupTup = new TupleBinary(sub_ovl);
            subLazLaz = new ThunkBinary(sub_ovl);

            mulIntInt = new MulIntInt(mul_ovl);
            mulIntLng = new MulIntLong(mul_ovl);
            mulIntSng = new MulIntSingle(mul_ovl);
            mulIntDbl = new MulIntDouble(mul_ovl);
            mulLngInt = new MulLongInt(mul_ovl);
            mulLngLng = new MulLongLong(mul_ovl);
            mulLngSng = new MulLongSingle(mul_ovl);
            mulLngDbl = new MulLongDouble(mul_ovl);
            mulSngInt = new MulSingleInt(mul_ovl);
            mulSngLng = new MulSingleLong(mul_ovl);
            mulSngSng = new MulSingleSingle(mul_ovl);
            mulSngDbl = new MulSingleDouble(mul_ovl);
            mulDblInt = new MulDoubleInt(mul_ovl);
            mulDblLng = new MulDoubleLong(mul_ovl);
            mulDblSng = new MulDoubleSingle(mul_ovl);
            mulDblDbl = new MulDoubleDouble(mul_ovl);
            mulTupTup = new TupleBinary(mul_ovl);
            mulLazLaz = new ThunkBinary(mul_ovl);

            divIntInt = new DivIntInt(div_ovl);
            divIntLng = new DivIntLong(div_ovl);
            divIntSng = new DivIntSingle(div_ovl);
            divIntDbl = new DivIntDouble(div_ovl);
            divLngInt = new DivLongInt(div_ovl);
            divLngLng = new DivLongLong(div_ovl);
            divLngSng = new DivLongSingle(div_ovl);
            divLngDbl = new DivLongDouble(div_ovl);
            divSngInt = new DivSingleInt(div_ovl);
            divSngLng = new DivSingleLong(div_ovl);
            divSngSng = new DivSingleSingle(div_ovl);
            divSngDbl = new DivSingleDouble(div_ovl);
            divDblInt = new DivDoubleInt(div_ovl);
            divDblLng = new DivDoubleLong(div_ovl);
            divDblSng = new DivDoubleSingle(div_ovl);
            divDblDbl = new DivDoubleDouble(div_ovl);
            divTupTup = new TupleBinary(div_ovl);
            divLazLaz = new ThunkBinary(div_ovl);

            remIntInt = new RemIntInt(rem_ovl);
            remIntLng = new RemIntLong(rem_ovl);
            remIntSng = new RemIntSingle(rem_ovl);
            remIntDbl = new RemIntDouble(rem_ovl);
            remLngInt = new RemLongInt(rem_ovl);
            remLngLng = new RemLongLong(rem_ovl);
            remLngSng = new RemLongSingle(rem_ovl);
            remLngDbl = new RemLongDouble(rem_ovl);
            remSngInt = new RemSingleInt(rem_ovl);
            remSngLng = new RemSingleLong(rem_ovl);
            remSngSng = new RemSingleSingle(rem_ovl);
            remSngDbl = new RemSingleDouble(rem_ovl);
            remDblInt = new RemDoubleInt(rem_ovl);
            remDblLng = new RemDoubleLong(rem_ovl);
            remDblSng = new RemDoubleSingle(rem_ovl);
            remDblDbl = new RemDoubleDouble(rem_ovl);
            remTupTup = new TupleBinary(rem_ovl);
            remLazLaz = new ThunkBinary(rem_ovl);

            powIntInt = new PowIntInt(pow_ovl);
            powIntLng = new PowIntLong(pow_ovl);
            powIntSng = new PowIntSingle(pow_ovl);
            powIntDbl = new PowIntDouble(pow_ovl);
            powLngInt = new PowLongInt(pow_ovl);
            powLngLng = new PowLongLong(pow_ovl);
            powLngSng = new PowLongSingle(pow_ovl);
            powLngDbl = new PowLongDouble(pow_ovl);
            powSngInt = new PowSingleInt(pow_ovl);
            powSngLng = new PowSingleLong(pow_ovl);
            powSngSng = new PowSingleSingle(pow_ovl);
            powSngDbl = new PowSingleDouble(pow_ovl);
            powDblInt = new PowDoubleInt(pow_ovl);
            powDblLng = new PowDoubleLong(pow_ovl);
            powDblSng = new PowDoubleSingle(pow_ovl);
            powDblDbl = new PowDoubleDouble(pow_ovl);
            powTupTup = new TupleBinary(pow_ovl);
            powLazLaz = new ThunkBinary(pow_ovl);

            andIntInt = new AndIntInt(and_ovl);
            andIntLng = new AndIntLong(and_ovl);
            andLngInt = new AndLongInt(and_ovl);
            andLngLng = new AndLongLong(and_ovl);
            andTupTup = new TupleBinary(and_ovl);
            andLazLaz = new ThunkBinary(and_ovl);

            borIntInt = new BorIntInt(bor_ovl);
            borIntLng = new BorIntLong(bor_ovl);
            borLngInt = new BorLongInt(bor_ovl);
            borLngLng = new BorLongLong(bor_ovl);
            borTupTup = new TupleBinary(bor_ovl);
            borLazLaz = new ThunkBinary(bor_ovl);

            xorIntInt = new XorIntInt(xor_ovl);
            xorIntLng = new XorIntLong(xor_ovl);
            xorLngInt = new XorLongInt(xor_ovl);
            xorLngLng = new XorLongLong(xor_ovl);
            xorTupTup = new TupleBinary(xor_ovl);
            xorLazLaz = new ThunkBinary(xor_ovl);

            shlIntInt = new ShlIntInt(shl_ovl);
            shlIntLng = new ShlIntLong(shl_ovl);
            shlLngInt = new ShlLongInt(shl_ovl);
            shlLngLng = new ShlLongLong(shl_ovl);
            shlTupTup = new TupleBinary(shl_ovl);
            shlLazLaz = new ThunkBinary(shl_ovl);

            shrIntInt = new ShrIntInt(shr_ovl);
            shrIntLng = new ShrIntLong(shr_ovl);
            shrLngInt = new ShrLongInt(shr_ovl);
            shrLngLng = new ShrLongLong(shr_ovl);
            shrTupTup = new TupleBinary(shr_ovl);
            shrLazLaz = new ThunkBinary(shr_ovl);

            add_ovl[INT][INT] = addIntInt;
            add_ovl[INT][LNG] = addIntLng;
            add_ovl[INT][REA] = addIntSng;
            add_ovl[INT][DBL] = addIntDbl;
            add_ovl[LNG][INT] = addLngInt;
            add_ovl[LNG][LNG] = addLngLng;
            add_ovl[LNG][REA] = addLngSng;
            add_ovl[LNG][DBL] = addLngDbl;
            add_ovl[REA][INT] = addSngInt;
            add_ovl[REA][LNG] = addSngLng;
            add_ovl[REA][REA] = addSngSng;
            add_ovl[REA][DBL] = addSngDbl;
            add_ovl[DBL][INT] = addDblInt;
            add_ovl[DBL][LNG] = addDblLng;
            add_ovl[DBL][REA] = addDblSng;
            add_ovl[DBL][DBL] = addDblDbl;
            add_ovl[TUP][INT] = addTupTup;
            add_ovl[TUP][LNG] = addTupTup;
            add_ovl[TUP][REA] = addTupTup;
            add_ovl[TUP][DBL] = addTupTup;
            add_ovl[TUP][TUP] = addTupTup;
            add_ovl[LAZ][INT] = addLazLaz;
            add_ovl[LAZ][LNG] = addLazLaz;
            add_ovl[LAZ][REA] = addLazLaz;
            add_ovl[LAZ][DBL] = addLazLaz;
            add_ovl[LAZ][TUP] = addLazLaz;
            add_ovl[LAZ][LAZ] = addLazLaz;

            sub_ovl[INT][INT] = subIntInt;
            sub_ovl[INT][LNG] = subIntLng;
            sub_ovl[INT][REA] = subIntSng;
            sub_ovl[INT][DBL] = subIntDbl;
            sub_ovl[LNG][INT] = subLngInt;
            sub_ovl[LNG][LNG] = subLngLng;
            sub_ovl[LNG][REA] = subLngSng;
            sub_ovl[LNG][DBL] = subLngDbl;
            sub_ovl[REA][INT] = subSngInt;
            sub_ovl[REA][LNG] = subSngLng;
            sub_ovl[REA][REA] = subSngSng;
            sub_ovl[REA][DBL] = subSngDbl;
            sub_ovl[DBL][INT] = subDblInt;
            sub_ovl[DBL][LNG] = subDblLng;
            sub_ovl[DBL][REA] = subDblSng;
            sub_ovl[DBL][DBL] = subDblDbl;
            sub_ovl[TUP][INT] = subTupTup;
            sub_ovl[TUP][LNG] = subTupTup;
            sub_ovl[TUP][REA] = subTupTup;
            sub_ovl[TUP][DBL] = subTupTup;
            sub_ovl[TUP][TUP] = subTupTup;
            sub_ovl[LAZ][INT] = subLazLaz;
            sub_ovl[LAZ][LNG] = subLazLaz;
            sub_ovl[LAZ][REA] = subLazLaz;
            sub_ovl[LAZ][DBL] = subLazLaz;
            sub_ovl[LAZ][TUP] = subLazLaz;
            sub_ovl[LAZ][LAZ] = subLazLaz;

            mul_ovl[INT][INT] = mulIntInt;
            mul_ovl[INT][LNG] = mulIntLng;
            mul_ovl[INT][REA] = mulIntSng;
            mul_ovl[INT][DBL] = mulIntDbl;
            mul_ovl[LNG][INT] = mulLngInt;
            mul_ovl[LNG][LNG] = mulLngLng;
            mul_ovl[LNG][REA] = mulLngSng;
            mul_ovl[LNG][DBL] = mulLngDbl;
            mul_ovl[REA][INT] = mulSngInt;
            mul_ovl[REA][LNG] = mulSngLng;
            mul_ovl[REA][REA] = mulSngSng;
            mul_ovl[REA][DBL] = mulSngDbl;
            mul_ovl[DBL][INT] = mulDblInt;
            mul_ovl[DBL][LNG] = mulDblLng;
            mul_ovl[DBL][REA] = mulDblSng;
            mul_ovl[DBL][DBL] = mulDblDbl;
            mul_ovl[TUP][INT] = mulTupTup;
            mul_ovl[TUP][LNG] = mulTupTup;
            mul_ovl[TUP][REA] = mulTupTup;
            mul_ovl[TUP][DBL] = mulTupTup;
            mul_ovl[TUP][TUP] = mulTupTup;
            mul_ovl[LAZ][INT] = mulLazLaz;
            mul_ovl[LAZ][LNG] = mulLazLaz;
            mul_ovl[LAZ][REA] = mulLazLaz;
            mul_ovl[LAZ][DBL] = mulLazLaz;
            mul_ovl[LAZ][TUP] = mulLazLaz;
            mul_ovl[LAZ][LAZ] = mulLazLaz;

            div_ovl[INT][INT] = divIntInt;
            div_ovl[INT][LNG] = divIntLng;
            div_ovl[INT][REA] = divIntSng;
            div_ovl[INT][DBL] = divIntDbl;
            div_ovl[LNG][INT] = divLngInt;
            div_ovl[LNG][LNG] = divLngLng;
            div_ovl[LNG][REA] = divLngSng;
            div_ovl[LNG][DBL] = divLngDbl;
            div_ovl[REA][INT] = divSngInt;
            div_ovl[REA][LNG] = divSngLng;
            div_ovl[REA][REA] = divSngSng;
            div_ovl[REA][DBL] = divSngDbl;
            div_ovl[DBL][INT] = divDblInt;
            div_ovl[DBL][LNG] = divDblLng;
            div_ovl[DBL][REA] = divDblSng;
            div_ovl[DBL][DBL] = divDblDbl;
            div_ovl[TUP][INT] = divTupTup;
            div_ovl[TUP][LNG] = divTupTup;
            div_ovl[TUP][REA] = divTupTup;
            div_ovl[TUP][DBL] = divTupTup;
            div_ovl[TUP][TUP] = divTupTup;
            div_ovl[LAZ][INT] = divLazLaz;
            div_ovl[LAZ][LNG] = divLazLaz;
            div_ovl[LAZ][REA] = divLazLaz;
            div_ovl[LAZ][DBL] = divLazLaz;
            div_ovl[LAZ][TUP] = divLazLaz;
            div_ovl[LAZ][LAZ] = divLazLaz;

            rem_ovl[INT][INT] = remIntInt;
            rem_ovl[INT][LNG] = remIntLng;
            rem_ovl[INT][REA] = remIntSng;
            rem_ovl[INT][DBL] = remIntDbl;
            rem_ovl[LNG][INT] = remLngInt;
            rem_ovl[LNG][LNG] = remLngLng;
            rem_ovl[LNG][REA] = remLngSng;
            rem_ovl[LNG][DBL] = remLngDbl;
            rem_ovl[REA][INT] = remSngInt;
            rem_ovl[REA][LNG] = remSngLng;
            rem_ovl[REA][REA] = remSngSng;
            rem_ovl[REA][DBL] = remSngDbl;
            rem_ovl[DBL][INT] = remDblInt;
            rem_ovl[DBL][LNG] = remDblLng;
            rem_ovl[DBL][REA] = remDblSng;
            rem_ovl[DBL][DBL] = remDblDbl;
            rem_ovl[TUP][INT] = remTupTup;
            rem_ovl[TUP][LNG] = remTupTup;
            rem_ovl[TUP][REA] = remTupTup;
            rem_ovl[TUP][DBL] = remTupTup;
            rem_ovl[TUP][TUP] = remTupTup;
            rem_ovl[LAZ][INT] = remLazLaz;
            rem_ovl[LAZ][LNG] = remLazLaz;
            rem_ovl[LAZ][REA] = remLazLaz;
            rem_ovl[LAZ][DBL] = remLazLaz;
            rem_ovl[LAZ][TUP] = remLazLaz;
            rem_ovl[LAZ][LAZ] = remLazLaz;

            pow_ovl[INT][INT] = powIntInt;
            pow_ovl[INT][LNG] = powIntLng;
            pow_ovl[INT][REA] = powIntSng;
            pow_ovl[INT][DBL] = powIntDbl;
            pow_ovl[LNG][INT] = powLngInt;
            pow_ovl[LNG][LNG] = powLngLng;
            pow_ovl[LNG][REA] = powLngSng;
            pow_ovl[LNG][DBL] = powLngDbl;
            pow_ovl[REA][INT] = powSngInt;
            pow_ovl[REA][LNG] = powSngLng;
            pow_ovl[REA][REA] = powSngSng;
            pow_ovl[REA][DBL] = powSngDbl;
            pow_ovl[DBL][INT] = powDblInt;
            pow_ovl[DBL][LNG] = powDblLng;
            pow_ovl[DBL][REA] = powDblSng;
            pow_ovl[DBL][DBL] = powDblDbl;
            pow_ovl[TUP][INT] = powTupTup;
            pow_ovl[TUP][LNG] = powTupTup;
            pow_ovl[TUP][REA] = powTupTup;
            pow_ovl[TUP][DBL] = powTupTup;
            pow_ovl[TUP][TUP] = powTupTup;
            pow_ovl[LAZ][INT] = powLazLaz;
            pow_ovl[LAZ][LNG] = powLazLaz;
            pow_ovl[LAZ][REA] = powLazLaz;
            pow_ovl[LAZ][DBL] = powLazLaz;
            pow_ovl[LAZ][TUP] = powLazLaz;
            pow_ovl[LAZ][LAZ] = powLazLaz;

            and_ovl[INT][INT] = andIntInt;
            and_ovl[INT][LNG] = andIntLng;
            and_ovl[LNG][INT] = andLngInt;
            and_ovl[LNG][LNG] = andLngLng;
            and_ovl[TUP][INT] = andTupTup;
            and_ovl[TUP][LNG] = andTupTup;
            and_ovl[TUP][TUP] = andTupTup;
            and_ovl[LAZ][INT] = andLazLaz;
            and_ovl[LAZ][LNG] = andLazLaz;
            and_ovl[LAZ][TUP] = andLazLaz;
            and_ovl[LAZ][LAZ] = andLazLaz;

            bor_ovl[INT][INT] = borIntInt;
            bor_ovl[INT][LNG] = borIntLng;
            bor_ovl[LNG][INT] = borLngInt;
            bor_ovl[LNG][LNG] = borLngLng;
            bor_ovl[TUP][INT] = borTupTup;
            bor_ovl[TUP][LNG] = borTupTup;
            bor_ovl[TUP][TUP] = borTupTup;
            bor_ovl[LAZ][INT] = borLazLaz;
            bor_ovl[LAZ][LNG] = borLazLaz;
            bor_ovl[LAZ][TUP] = borLazLaz;
            bor_ovl[LAZ][LAZ] = borLazLaz;

            xor_ovl[INT][INT] = xorIntInt;
            xor_ovl[INT][LNG] = xorIntLng;
            xor_ovl[LNG][INT] = xorLngInt;
            xor_ovl[LNG][LNG] = xorLngLng;
            xor_ovl[TUP][INT] = xorTupTup;
            xor_ovl[TUP][LNG] = xorTupTup;
            xor_ovl[TUP][TUP] = xorTupTup;
            xor_ovl[LAZ][INT] = xorLazLaz;
            xor_ovl[LAZ][LNG] = xorLazLaz;
            xor_ovl[LAZ][TUP] = xorLazLaz;
            xor_ovl[LAZ][LAZ] = xorLazLaz;

            shl_ovl[INT][INT] = shlIntInt;
            shl_ovl[INT][LNG] = shlIntLng;
            shl_ovl[LNG][INT] = shlLngInt;
            shl_ovl[LNG][LNG] = shlLngLng;
            shl_ovl[TUP][INT] = shlTupTup;
            shl_ovl[TUP][LNG] = shlTupTup;
            shl_ovl[TUP][TUP] = shlTupTup;
            shl_ovl[LAZ][INT] = shlLazLaz;
            shl_ovl[LAZ][LNG] = shlLazLaz;
            shl_ovl[LAZ][TUP] = shlLazLaz;
            shl_ovl[LAZ][LAZ] = shlLazLaz;

            shr_ovl[INT][INT] = shrIntInt;
            shr_ovl[INT][LNG] = shrIntLng;
            shr_ovl[LNG][INT] = shrLngInt;
            shr_ovl[LNG][LNG] = shrLngLng;
            shr_ovl[TUP][INT] = shrTupTup;
            shr_ovl[TUP][LNG] = shrTupTup;
            shr_ovl[TUP][TUP] = shrTupTup;
            shr_ovl[LAZ][INT] = shrLazLaz;
            shr_ovl[LAZ][LNG] = shrLazLaz;
            shr_ovl[LAZ][TUP] = shrLazLaz;
            shr_ovl[LAZ][LAZ] = shrLazLaz;
        }



        internal readonly DispatchBinaryFun[] neg_ovl = null;


        internal static ElaValue BinaryOp(DispatchBinaryFun[][] funs, ElaValue left, ElaValue right, ExecutionContext ctx)
        {
            return funs[left.TypeId][right.TypeId].Call(left, right, ctx);
        }


        internal static ElaValue UnaryOp(DispatchBinaryFun[] funs, ElaValue left, ExecutionContext ctx)
        {
            //return funs[left.TypeId].Call(left, ctx));
            return default(ElaValue);
        }
		#endregion


		#region Public Methods
		public void Dispose()
		{
			var ex = default(Exception);

			foreach (var m in asm.EnumerateForeignModules())
			{
				try
				{
					m.Close();
				}
				catch (Exception e)
				{
					ex = e;
				}
			}

			if (ex != null)
				throw ex;
		}


		public ExecutionResult Run()
		{
			MainThread.Offset = MainThread.Offset == 0 ? 0 : MainThread.Offset;
			var ret = default(ElaValue);

			try
			{
				ret = Execute(MainThread);
			}
			catch (ElaException)
			{
				throw;
			}
            //catch (Exception ex)
            //{
            //    var op = MainThread.Module != null && MainThread.Offset > 0 &&
            //        MainThread.Offset - 1 < MainThread.Module.Ops.Count ?
            //        MainThread.Module.Ops[MainThread.Offset - 1].ToString() : String.Empty;
            //    throw Exception("CriticalError", ex, MainThread.Offset - 1, op);
            //}
			
			var evalStack = MainThread.CallStack[0].Stack;

			if (evalStack.Count > 0)
				throw Exception("StackCorrupted");

			return new ExecutionResult(ret);
		}


		public ExecutionResult Run(int offset)
		{
			MainThread.Offset = offset;
			return Run();
		}


		public void Recover()
		{
			if (modules.Length > 0)
			{
				var m = modules[0];

				for (var i = 0; i < m.Length; i++)
				{
					var v = m[i];

					if (v.Ref == null)
						m[i] = new ElaValue(ElaUnit.Instance);
				}
			}
		}


		public void RefreshState()
		{
			if (modules.Length > 0)
			{
				if (modules.Length != asm.ModuleCount)
				{
					var mods = new ElaValue[asm.ModuleCount][];

					for (var i = 0; i < modules.Length; i++)
						mods[i] = modules[i];

					modules = mods;
				}

				var mem = modules[0];
				var frame = asm.GetRootModule();
				var arr = new ElaValue[frame.Layouts[0].Size];
				Array.Copy(mem, 0, arr, 0, mem.Length);
				modules[0] = arr;
				MainThread.SwitchModule(0);
				MainThread.CallStack.Clear();
				var cp = new CallPoint(0, new EvalStack(frame.Layouts[0].StackSize), arr, FastList<ElaValue[]>.Empty);
				MainThread.CallStack.Push(cp);

				for (var i = 0; i < asm.ModuleCount; i++)
					ReadPervasives(MainThread, asm.GetModule(i), i);
			}
		}


		public ElaValue GetVariableByHandle(int moduleHandle, int varHandle)
		{
			var mod = default(ElaValue[]);

			try
			{
				mod = modules[moduleHandle];
			}
			catch (IndexOutOfRangeException)
			{
				throw Exception("InvalidModuleHandle");
			}

			try
			{
				return mod[varHandle];
			}
			catch (IndexOutOfRangeException)
			{
				throw Exception("InvalidVariableAddress");
			}
		}
		#endregion


		class Call2 : ElaObject
		{
			public Call2() : base((ElaTypeCode)(-1)) { }

			internal override bool Call(EvalStack stack, ExecutionContext ctx)
			{
				stack.Replace(stack.Pop().I4 + stack.Peek().I4);
				return false;
			}
		}


		#region Execute
		private ElaValue Execute(WorkerThread thread)
		{
			var callStack = thread.CallStack;
			var evalStack = callStack.Peek().Stack;
			var frame = thread.Module;
			var ops = thread.Module.Ops.GetRawArray();
			var opData = thread.Module.OpData.GetRawArray();
			var locals = callStack.Peek().Locals;
			var captures = callStack.Peek().Captures;
			
			var ctx = thread.Context;
			var left = default(ElaValue);
			var right = default(ElaValue);
			var res = default(ElaValue);
			var i4 = 0;

		CYCLE:
			{
				#region Body
				var op = ops[thread.Offset];
				var opd = opData[thread.Offset];
				thread.Offset++;

				switch (op)
				{

					case Op.Pushadd:
						evalStack.Push(new ElaValue(new Call2()));
						break;
					case Op.Call2:
						{
							var obj = evalStack.PopFast().Ref;

							if (obj.Call(evalStack, ctx))
								goto SWITCH_MEM;

							if (ctx.Failed)
							{
								evalStack.Push(new ElaValue(obj));
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}
						}
						break;


					#region Stack Operations
					case Op.Tupex:
						{
							var tup = evalStack.Pop().Ref as ElaTuple;

							if (tup == null || tup.Length != opd)
							{
								ExecuteFail(ElaRuntimeError.MatchFailed, thread, evalStack);
								goto SWITCH_MEM;
							}
							else
							{
								for (var i = 0; i < tup.Length; i++)
									locals[i] = tup.FastGet(i);
							}
						}
						break;
					case Op.Pushvar:
						i4 = opd & Byte.MaxValue;

						if (i4 == 0)
							evalStack.Push(locals[opd >> 8]);
						else
							evalStack.Push(captures[captures.Count - i4][opd >> 8]);
						break;
					case Op.Pushstr:
						evalStack.Push(new ElaValue(frame.Strings[opd]));
						break;
					case Op.Pushstr_0:
						evalStack.Push(new ElaValue(String.Empty));
						break;
					case Op.PushI4:
						evalStack.Push(opd);
						break;
					case Op.PushI4_0:
						evalStack.Push(0);
						break;
					case Op.PushI1_0:
						evalStack.Push(new ElaValue(false));
						break;
					case Op.PushI1_1:
						evalStack.Push(new ElaValue(true));
						break;
					case Op.PushR4:
						evalStack.Push(new ElaValue(opd, ElaSingle.Instance));
						break;
					case Op.PushCh:
						evalStack.Push(new ElaValue((Char)opd));
						break;
					case Op.Pushunit:
						evalStack.Push(new ElaValue(ElaUnit.Instance));
						break;
					case Op.Pushelem:
						right = evalStack.Pop();
						left = evalStack.Peek();
						evalStack.Replace(left.Ref.GetValue(right.Id(ctx), ctx)); //use of ElaValue.Id

						if (ctx.Failed)
						{
							evalStack.Replace(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Pushfld:
						{
							var fld = frame.Strings[opd];
							right = evalStack.PopFast();

							if (right.TypeId == MOD)
							{
								i4 = ((ElaModule)right.Ref).Handle;
								var fr = asm.GetModule(i4);
								ScopeVar sc;

								if (!fr.GlobalScope.Locals.TryGetValue(fld, out sc))
								{
									ExecuteFail(new ElaError(ElaRuntimeError.UndefinedVariable, fld, asm.GetModuleName(i4)), thread, evalStack);
									goto SWITCH_MEM;
								}

								if ((sc.Flags & ElaVariableFlags.Private) == ElaVariableFlags.Private)
								{
									ExecuteFail(new ElaError(ElaRuntimeError.PrivateVariable, fld), thread, evalStack);
									goto SWITCH_MEM;
								}

								evalStack.Push(modules[i4][sc.Address]);
								break;
							}

							evalStack.Push(right.Ref.GetField(fld, ctx));

							if (ctx.Failed)
							{
								evalStack.Replace(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}

							break;
						}
					case Op.Pop:
						evalStack.PopVoid();
						break;
					case Op.Popvar:
						right = evalStack.Pop();
						i4 = opd & Byte.MaxValue;

						if (i4 == 0)
							locals[opd >> 8] = right;
						else
							captures[captures.Count - i4][opd >> 8] = right;
						break;
					case Op.Popelem:
						{
							right = evalStack.Pop();
							left = evalStack.Pop();
							var val = evalStack.Pop();
							left.Ref.SetValue(right.Id(ctx), val, ctx); //Use of ElaValue.Id

							if (ctx.Failed)
							{
								evalStack.Push(val);
								evalStack.Push(left);
								evalStack.Push(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}
						}
						break;
					case Op.Popfld:
						right = evalStack.Pop();
						left = evalStack.Pop();
						right.Ref.SetField(frame.Strings[opd], left, ctx);

						if (ctx.Failed)
						{
							evalStack.Push(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}

						break;
					case Op.Dup:
						evalStack.Push(evalStack.Peek());
						break;
					case Op.Swap:
						right = evalStack.Pop();
						left = evalStack.Peek();
						evalStack.Replace(right);
						evalStack.Push(left);
						break;
					#endregion

					#region Binary Operations
					case Op.AndBw:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 & right.I4);
							break;
						}

						evalStack.Replace(left.Ref.BitwiseAnd(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.OrBw:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 | right.I4);
							break;
						}

						evalStack.Replace(left.Ref.BitwiseOr(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Xor:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 ^ right.I4);
							break;
						}

						evalStack.Replace(left.Ref.BitwiseXor(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Shl:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 << right.I4);
							break;
						}

						evalStack.Replace(left.Ref.ShiftLeft(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Shr:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 >> right.I4);
							break;
						}

						evalStack.Replace(left.Ref.ShiftRight(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Concat:
						left = evalStack.Pop();
						right = evalStack.Peek();
						evalStack.Replace(left.Concatenate(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
                    case Op.Add:
                        left = evalStack.Pop();
                        right = evalStack.Peek();

                        //evalStack.Replace(left.Ref.Add(left, right, ctx));
                        evalStack.Replace(add_ovl[left.TypeId][right.TypeId].Call(left, right, ctx));

                        if (ctx.Failed)
                        {
                            evalStack.Replace(right);
                            evalStack.Push(left);
                            ExecuteThrow(thread, evalStack);
                            goto SWITCH_MEM;
                        }



                        //left = evalStack.Pop();
                        //right = evalStack.Peek();

                        ////if (left.TypeId == INT && right.TypeId == INT)
                        ////{
                        ////    evalStack.Replace(left.I4 + right.I4);
                        ////    break;
                        ////}

                        ////if (add_ovl[left.TypeId][right.TypeId])
                        ////{
                        ////    InvokeOverride("$add", left, right, evalStack, thread, ctx);
                        ////    goto SWITCH_MEM;
                        ////}

                        //evalStack.Replace(left.Ref.Add(left, right, ctx));

                        //if (ctx.Failed)
                        //{
                        //    evalStack.Replace(right);
                        //    evalStack.Push(left);
                        //    ExecuteThrow(thread, evalStack);
                        //    goto SWITCH_MEM;
                        //}
                        break;
					case Op.Sub:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 - right.I4);
							break;
						}

						evalStack.Replace(left.Ref.Subtract(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Div:
						left = evalStack.Pop();
						right = evalStack.Peek();
						evalStack.Replace(left.Divide(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Mul:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 * right.I4);
							break;
						}

						evalStack.Replace(left.Ref.Multiply(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Pow:
						left = evalStack.Pop();
						right = evalStack.Peek();
						evalStack.Replace(left.Ref.Power(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Rem:
						left = evalStack.Pop();
						right = evalStack.Peek();
						evalStack.Replace(left.Ref.Remainder(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					#endregion

					#region Comparison Operations
                    case Op.Pat:
						right = evalStack.Peek();
						evalStack.Replace(new ElaValue(((Int32)right.Id(ctx).Ref.GetSupportedPatterns() & opd) == opd));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Cgt:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 > right.I4);
							break;
						}

						evalStack.Replace(left.Ref.Greater(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Clt:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 < right.I4);
							break;
						}

						evalStack.Replace(left.Ref.Lesser(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Ceq:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 == right.I4);
							break;
						}

						evalStack.Replace(left.Ref.Equal(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Cneq:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 != right.I4);
							break;
						}

						evalStack.Replace(left.Ref.NotEqual(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Cgteq:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 >= right.I4);
							break;
						}

						evalStack.Replace(left.Ref.GreaterEqual(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Clteq:
						left = evalStack.Pop();
						right = evalStack.Peek();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							evalStack.Replace(left.I4 <= right.I4);
							break;
						}

						evalStack.Replace(left.Ref.LesserEqual(left, right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					#endregion

					#region Object Operations
					case Op.Clone:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.Clone(ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Elem:
                        right = evalStack.Peek();
                        res = right.Ref.GetLength(ctx);

                        if (!res.Equal(res, new ElaValue(opd & Byte.MaxValue), ctx).Bool(ctx))
                        {
                            ExecuteFail(ElaRuntimeError.MatchFailed, thread, evalStack);
                            goto SWITCH_MEM;
                        }

                        evalStack.Replace(right.Ref.GetValue(new ElaValue(opd >> 8), ctx));

                        if (ctx.Failed)
                        {
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
                            goto SWITCH_MEM;
                        }
                        break;
					case Op.Reccons:
						{
							right = evalStack.Pop();
							left = evalStack.Pop();
							var mut = evalStack.Pop();
							var rec = evalStack.Peek();

							if (rec.TypeId == REC)
								((ElaRecord)rec.Ref).AddField(right.DirectGetString(), mut.I4 == 1, left);
							else
							{
								InvalidType(left, thread, evalStack, TypeCodeFormat.GetShortForm(ElaTypeCode.Record));
								goto SWITCH_MEM;
							}
						}
						break;
					case Op.Cons:
						left = evalStack.Pop();
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.Cons(right.Ref, left, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Gen:
						right = evalStack.Pop();
						left = evalStack.Peek();
						evalStack.Replace(left.Ref.Generate(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							evalStack.Push(left);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Genfin:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.GenerateFinalize(ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Tail:
						evalStack.Replace((right = evalStack.Peek()).Ref.Tail(ctx));
						
						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Head:
						evalStack.Replace((right = evalStack.Peek()).Ref.Head(ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Nil:
						evalStack.Replace((right = evalStack.Peek()).Nil(ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Isnil:
						evalStack.Replace(new ElaValue((right = evalStack.Peek()).Ref.IsNil(ctx)));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Len:
						evalStack.Replace((right = evalStack.Peek()).Ref.GetLength(ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}

						break;
					case Op.Hasfld:
						evalStack.Replace((right = evalStack.Peek()).Ref.HasField(frame.Strings[opd], ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}

						break;
					case Op.Force:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.InternalForce(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Untag:
						evalStack.Replace((right = evalStack.Peek()).Ref.Untag(ctx));

                        if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
                    case Op.Gettag:
                        evalStack.Replace(new ElaValue((right = evalStack.Peek()).Ref.GetTag(ctx)));

                        if (ctx.Failed)
                        {
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
                            goto SWITCH_MEM;
                        }
                        break;
					#endregion

					#region Unary Operations
					case Op.Not:
						right = evalStack.Peek();

						if (right.TypeId == BYT)
						{
							evalStack.Replace(right.I4 != 1);
							break;
						}

						evalStack.Replace(!right.Ref.Bool(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Neg:
						right = evalStack.Peek();

						if (right.TypeId == INT)
						{
							evalStack.Replace(new ElaValue(-right.I4));
							break;
						}

						evalStack.Replace(right.Ref.Negate(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.NotBw:
						right = evalStack.Peek();

						if (right.TypeId == INT)
						{
							evalStack.Replace(new ElaValue(~right.I4));
							break;
						}

						evalStack.Replace(right.Ref.BitwiseNot(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Conv:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.Convert(right, (ElaTypeCode)opd, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					#endregion

					#region Goto Operations
					case Op.Skiptl:
						right = evalStack.PopFast();

						if (right.Ref.IsNil(ctx))
						{
							if (ctx.Failed)
							{
								evalStack.Push(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}

							break;
						}

						locals[opd] = right.Ref.Head(ctx);
						locals[opd + 1] = right.Ref.Tail(ctx);
						thread.Offset++;

						if (ctx.Failed)
						{
							evalStack.Push(right);
							thread.Offset--;
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Skiptn:
						{
							right = evalStack.PopFast();

							if (right.Ref.IsNil(ctx))
							{
								if (ctx.Failed)
								{
									evalStack.Push(right);
									ExecuteThrow(thread, evalStack);
									goto SWITCH_MEM;
								}

								break;
							}

							var obj = right.Ref.Tail(ctx);

							if (!obj.Ref.IsNil(ctx))
							{
								if (ctx.Failed)
								{
									ExecuteThrow(thread, evalStack);
									goto SWITCH_MEM;
								}

								break;
							}

							locals[opd] = right.Ref.Head(ctx);
							thread.Offset++;

							if (ctx.Failed)
							{
								evalStack.Push(right);
								thread.Offset--;
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}
							break;
						}
					case Op.Skiphtag:
						right = evalStack.Pop();

						if (!String.IsNullOrEmpty(right.Ref.GetTag(ctx)))
						{
							if (ctx.Failed)
							{
								evalStack.Push(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}

							thread.Offset++;
							break;
						}

						break;
					case Op.Skiptag:
						right = evalStack.Pop();

						if (frame.Strings[opd] == right.Ref.GetTag(ctx))
						{
							if (ctx.Failed)
							{
								evalStack.Push(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}

							thread.Offset++;
							break;
						}

						break;
					case Op.Brtrue:
						right = evalStack.Pop();

						if (right.TypeId == BYT)
						{
							if (right.I4 == 1)
								thread.Offset = opd;

							break;
						}

						if (right.Ref.Bool(right, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Brfalse:
						right = evalStack.Pop();

						if (right.TypeId == BYT)
						{
							if (right.I4 != 1)
								thread.Offset = opd;

							break;
						}

						if (!right.Ref.Bool(right, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Br:
						thread.Offset = opd;
						break;
					case Op.Br_eq:
						right = evalStack.Pop();
						left = evalStack.Pop();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							if (left.I4 == right.I4)
								thread.Offset = opd;

							break;
						}

						res = left.Ref.Equal(left, right, ctx);

						if (res.Ref.Bool(res, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Br_neq:
						right = evalStack.Pop();
						left = evalStack.Pop();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							if (left.I4 != right.I4)
								thread.Offset = opd;

							break;
						}

						res = left.Ref.NotEqual(left, right, ctx);

						if (res.Ref.Bool(res, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Br_lt:
						right = evalStack.Pop();
						left = evalStack.Pop();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							if (left.I4 < right.I4)
								thread.Offset = opd;

							break;
						}

						res = left.Ref.Lesser(res, right, ctx);

						if (res.Ref.Bool(res, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Br_gt:
						right = evalStack.Pop();
						left = evalStack.Pop();

						if (left.TypeId == INT && right.TypeId == INT)
						{
							if (left.I4 > right.I4)
								thread.Offset = opd;

							break;
						}

						res = left.Ref.Greater(left, right, ctx);

						if (res.Ref.Bool(res, ctx) && !ctx.Failed)
						{
							thread.Offset = opd;
							break;
						}

						if (ctx.Failed)
						{
							evalStack.Push(left);
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Brnil:
						right = evalStack.PopFast();

						if (right.IsNil(ctx))
							thread.Offset = opd;
						
						if (ctx.Failed)
						{
							evalStack.Push(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					#endregion

					#region CreateNew Operations
                    case Op.Newvar:
						right = evalStack.Peek();
						evalStack.Replace(new ElaValue(new ElaVariant(frame.Strings[opd], right)));
						break;
					case Op.Newlazy:
						evalStack.Push(new ElaValue(new ElaLazy((ElaFunction)evalStack.Pop().Ref, thread.Module)));
						break;
					case Op.Newfun:
						{
							var lst = new FastList<ElaValue[]>(captures);
							lst.Add(locals);
							var fun = new ElaFunction(opd, thread.ModuleHandle, evalStack.Pop().I4, lst, this);
							evalStack.Push(new ElaValue(fun));
						}
						break;
                    case Op.Newlist:
						evalStack.Push(new ElaValue(ElaList.Empty));
						break;
					case Op.Newrec:
						evalStack.Push(new ElaValue(new ElaRecord(opd)));
						break;
					case Op.Newtup:
						evalStack.Push(new ElaValue(new ElaTuple(opd)));
						break;
					case Op.Newtup_2:
						{
							right = evalStack.Pop();
							left = evalStack.Peek();
							var tup = default(ElaTuple);

							if (right.TypeId == TUP || left.TypeId == TUP)
							{
								tup = new ElaTuple(2);
								tup.InternalSetValue(left);
								tup.InternalSetValue(right);
							}
							else
							{
								tup = new ElaTuple(2);
								tup.InternalSetValue(0, left);
								tup.InternalSetValue(1, right);
							}

							evalStack.Replace(new ElaValue(tup));
						}
						break;
					case Op.NewI8:
						{
							right = evalStack.Pop();
							left = evalStack.Peek();
							var conv = new Conv();
							conv.I4_1 = left.I4;
							conv.I4_2 = right.I4;
							evalStack.Replace(new ElaValue(conv.I8));
						}
						break;
					case Op.NewR8:
						{
							right = evalStack.Pop();
							left = evalStack.Peek();
							var conv = new Conv();
							conv.I4_1 = left.I4;
							conv.I4_2 = right.I4;
							evalStack.Replace(new ElaValue(conv.R8));
						}
						break;
					case Op.Newmod:
						{
							var str = frame.Strings[opd];
							i4 = asm.GetModuleHandle(str);
							evalStack.Push(new ElaValue(new ElaModule(i4, this)));
						}
						break;
					#endregion

					#region Thunk Operations
					case Op.Flip:
						{
							right = evalStack.Peek().Id(ctx);

							if (right.TypeId != FUN)
							{
								evalStack.PopVoid();
								ExecuteFail(new ElaError(ElaRuntimeError.ExpectedFunction, TypeCodeFormat.GetShortForm(right.TypeCode)), thread, evalStack);
								goto SWITCH_MEM;
							}

							var fun = (ElaFunction)right.Ref;
							fun = fun.Captures != null ? fun.CloneFast() : fun.Clone();
							fun.Flip = !fun.Flip;
							evalStack.Replace(new ElaValue(fun));

							if (ctx.Failed)
							{
								evalStack.Push(right);
								ExecuteThrow(thread, evalStack);
								goto SWITCH_MEM;
							}
						}
						break;
                    case Op.Ovr:
                        if (OverloadFunction(evalStack, captures, locals, thread))
                        {
                            ExecuteThrow(thread, evalStack);
                            goto SWITCH_MEM;
                        }
                        break;
					case Op.Call:
						if (Call(evalStack.Pop().Ref, thread, evalStack, CallFlag.None))
						    goto SWITCH_MEM;
						break;
					case Op.LazyCall:
						{
							var fun = ((ElaFunction)evalStack.Pop().Ref).CloneFast();
							fun.LastParameter = evalStack.Pop();
							evalStack.Push(new  ElaValue(new ElaLazy(fun, thread.Module)));
						}
						break;
					case Op.Callt:
						{
							if (callStack.Peek().Thunk != null || evalStack.Peek().TypeId == LAZ)
							{
                                if (Call(evalStack.Pop().Ref, thread, evalStack, CallFlag.None))
									goto SWITCH_MEM;
								break;
							}

                            var cp = callStack.Pop();

                            if (Call(evalStack.Pop().Ref, thread, evalStack, CallFlag.NoReturn))
                                goto SWITCH_MEM;
                            else
                                callStack.Push(cp);
						}
						break;
					case Op.Ret:
						{
							var cc = callStack.Pop();

							if (cc.Thunk != null)
							{
								cc.Thunk.Value = evalStack.Pop();
								thread.Offset = callStack.Peek().BreakAddress;
								goto SWITCH_MEM;
							}

							if (callStack.Count > 0)
							{
								var om = callStack.Peek();
								om.Stack.Push(evalStack.Pop());

								if (om.BreakAddress == 0)
									return default(ElaValue);
								else
								{
									thread.Offset = om.BreakAddress;
									goto SWITCH_MEM;
								}
							}
							else
								return evalStack.PopFast();
						}
					#endregion

					#region Builtins
                    case Op.Succ:
						right = evalStack.Peek();

						if (right.TypeId == INT)
						{
							evalStack.Replace(right.I4 + 1);
							break;
						}

						evalStack.Replace(right.Ref.Successor(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Pred:
						right = evalStack.Peek();

						if (right.TypeId == INT)
						{
							evalStack.Replace(right.I4 - 1);
							break;
						}

						evalStack.Replace(right.Ref.Predecessor(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}
						break;
					case Op.Max:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.GetMax(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}

						break;
					case Op.Min:
						right = evalStack.Peek();
						evalStack.Replace(right.Ref.GetMin(right, ctx));

						if (ctx.Failed)
						{
							evalStack.Replace(right);
							ExecuteThrow(thread, evalStack);
							goto SWITCH_MEM;
						}

						break;
					#endregion

					#region Misc
					case Op.Nop:
						break;
					case Op.Show:
						{
							left = evalStack.Pop();
							right = evalStack.Peek();

							if (left.TypeId != STR)
							{
                                InvalidType(left, thread, evalStack, TypeCodeFormat.GetShortForm(ElaTypeCode.String));
								goto SWITCH_MEM;
							}

                            evalStack.Replace(new ElaValue(right.Ref.Show(right, new ShowInfo(0, 0, left.DirectGetString()), ctx)));

							if (ctx.Failed)
							{
								evalStack.Replace(right);
								evalStack.Push(left);
								ExecuteFail(ctx.Error, thread, evalStack);
								goto SWITCH_MEM;
							}
						}
						break;
					case Op.Type:
						evalStack.Replace(new ElaValue(evalStack.Peek().Ref.GetTypeInfo()));
						break;
					case Op.Typeid:
						right = evalStack.Peek();

						if (right.TypeId == LAZ)
						{
							var la = (ElaLazy)right.Ref;

							if (la.Value.Ref != null)
							{
								evalStack.Replace(new ElaValue(la.Value.TypeId));
								break;
							}
						}

						evalStack.Replace(new ElaValue(right.TypeId));
						break;
					case Op.Throw:
						{
							left = evalStack.Pop();
							right = evalStack.Pop();
							var code = left.Show(ShowInfo.Default, ctx);
							var msg = right.Show(ShowInfo.Default, ctx);
						
							if (ctx.Failed)
							{
								evalStack.Push(right);
								evalStack.Push(left);
								ExecuteThrow(thread, evalStack);
							}
							else
								ExecuteFail(new ElaError(code, msg), thread, evalStack);
							
							goto SWITCH_MEM;
						}
					case Op.Rethrow:
						{
							var err = (ElaError)evalStack.Pop().Ref;
							ExecuteFail(err, thread, evalStack);
							goto SWITCH_MEM;
						}
					case Op.Failwith:
						{
							ExecuteFail((ElaRuntimeError)opd, thread, evalStack);
							goto SWITCH_MEM;
						}
					case Op.Stop:
						if (callStack.Count > 1)
						{
							callStack.Pop();
							var modMem = callStack.Peek();
							thread.Offset = modMem.BreakAddress;
							var om = thread.Module;
							var omh = thread.ModuleHandle;
							thread.SwitchModule(modMem.ModuleHandle);

                            if (!asm.RequireQuailified(omh))
							    ReadPervasives(thread, om, omh);
							
                            right = evalStack.Pop();

							if (modMem.BreakAddress == 0)
								return right;
							else
								goto SWITCH_MEM;
						}
						return evalStack.Count > 0 ? evalStack.Pop() : new ElaValue(ElaUnit.Instance);
					case Op.Runmod:
						{
							var mn = frame.Strings[opd];
							var hdl = asm.GetModuleHandle(mn);

							if (modules[hdl] == null)
							{
								var frm = asm.GetModule(hdl);

								if (frm is IntrinsicFrame)
								{
									modules[hdl] = ((IntrinsicFrame)frm).Memory;

                                    if (!asm.RequireQuailified(hdl))
									    ReadPervasives(thread, frm, hdl);
								}
								else
								{
									i4 = frm.Layouts[0].Size;
									var loc = new ElaValue[i4];
									modules[hdl] = loc;
									callStack.Peek().BreakAddress = thread.Offset;
									callStack.Push(new CallPoint(hdl, new EvalStack(frm.Layouts[0].StackSize), loc, FastList<ElaValue[]>.Empty));
									thread.SwitchModule(hdl);
									thread.Offset = 0;
                                    
                                    if (argMod != null)
                                        ReadPervasives(thread, argMod, argModHandle);

									goto SWITCH_MEM;
								}
							}
							else
							{
								var frm = asm.GetModule(hdl);

                                if (!asm.RequireQuailified(hdl))
                                    ReadPervasives(thread, frm, hdl);
							}
						}
						break;
					case Op.Start:
						callStack.Peek().CatchMark = opd;
						callStack.Peek().StackOffset = evalStack.Count;
						break;
					case Op.Leave:
						callStack.Peek().CatchMark = null;
						break;
					#endregion

                    #region Jump Tags
                    case Op._Add:
                        evalStack.Replace(left.I4 + right.I4);
                        break;
                    #endregion
                }
				#endregion
			}
			goto CYCLE;

		SWITCH_MEM:
			{
				var mem = callStack.Peek();
				thread.SwitchModule(mem.ModuleHandle);
				locals = mem.Locals;
				captures = mem.Captures;
				ops = thread.Module.Ops.GetRawArray();
				opData = thread.Module.OpData.GetRawArray();
				frame = thread.Module;
				evalStack = mem.Stack;
			}
			goto CYCLE;

		}
		#endregion


		#region Operations
        //This function is a bit over complicated at the moment. VM assumes that Ovr's are called in chain and op code
        //has a different behavior depending on the fact whether it is a final Ovr or not (stack behavior however is fixed
        //only internal logic is different)
        //TODO: consider splitting it into two or even three op codes.
        private bool OverloadFunction(EvalStack evalStack, FastList<ElaValue[]> captures, ElaValue[] locals, WorkerThread thread)
        {
            var tag = evalStack.Pop().DirectGetString();
            var fn = evalStack.Pop().DirectGetString();
            var newObj = evalStack.PopFast().Ref;
            var oldObj = evalStack.Pop().Ref;

            var lst = new FastList<ElaValue[]>(captures);
            lst.Add(locals);

            var funObj = oldObj as ElaFunction;
            var nf = newObj as ElaFunction;

            if (funObj == null) //No previous overloads
            {
                var of = new ElaOverloadedFunction(fn, new Dictionary<String, ElaFunction>(), lst, this);
                var funToPush = nf;

                if (nf != null) //An overload by first param only
                    of.overloads.Add(tag, nf);
                else            //An overload by multiple params, other Ovr's to come
                {
                    funToPush = new ElaOverloadedFunction(fn, new Dictionary<String, ElaFunction>(), lst, this);
                    of.overloads.Add(tag, funToPush);
                }

                overloads.Add(fn, of);
                evalStack.Push(new ElaValue(funToPush));
            }
            else //We have previous overloads
            {
                if (!funObj.Overloaded) //A previous overload is a regular fun
                {
                    var funToAdd = default(ElaOverloadedFunction);

                    if (!overloads.TryGetValue(fn, out funToAdd)) //No overloads are registered for the fun name, patch it
                    {
                        funToAdd = new ElaOverloadedFunction(fn, new Dictionary<String, ElaFunction>(), lst, this);
                        overloads.Add(fn, funToAdd);
                    }

                    //Register an existing fun as "works for any" overload 
                    funToAdd.overloads.Add("$Any", funObj);
                    funObj = funToAdd;
                }

                var map = ((ElaOverloadedFunction)funObj).overloads;
                var cfun = default(ElaFunction);

                //A function for such parameter already exists. This is perfectly OK if we will have a longer singature,
                //otherwise we will fail here.
                if (map.TryGetValue(tag, out cfun))
                {
                    if (nf != null || cfun == dummyFun)
                    {
                        thread.Context.Fail(ElaRuntimeError.OverloadDuplicate, fn.Replace("$", String.Empty));
                        return true;
                    }

                    var ovrCfun = cfun as ElaOverloadedFunction;

                    if (ovrCfun == null)
                    {
                        thread.Context.Fail(ElaRuntimeError.OverloadInvalid, fn.Replace("$", String.Empty));
                        return true;
                    }

                    evalStack.Push(new ElaValue(cfun));
                }
                else
                {
                   var funToPush = nf;

                    if (nf == null) //We have an overload for multiple params, other Ovr's to come
                        funToPush = new ElaOverloadedFunction(fn, new Dictionary<String, ElaFunction>(), lst, this);
                    else            //No other overloads, just push the "top" function
                        funToPush = overloads[fn];

                    map.Add(tag, nf ?? funToPush);
                    evalStack.Push(new ElaValue(funToPush));
                }
            }

            return false;
        }


        private void InvokeOverride(string fun, ElaValue left, ElaValue right, EvalStack evalStack, WorkerThread thread, ExecutionContext ctx)
        {
            evalStack.Pop();
            var f = overloads[fun].Resolve(left, ctx).Resolve(right, ctx);
            f.Parameters[0] = left;
            f.AppliedParameters = 1;
            f.LastParameter = right;
            Call(f, thread, evalStack, CallFlag.AllParams);
        }


		private void ReadPervasives(WorkerThread thread, CodeFrame frame, int handle)
		{
			var mod = thread.Module;
			var locals = modules[thread.ModuleHandle];
			var externs = frame.GlobalScope.Locals;
			var extMem = modules[handle];
			ScopeVar sv;

			foreach (var s in mod.LateBounds)
			{
				if (externs.TryGetValue(s.Name, out sv))
					locals[s.Address] = extMem[sv.Address];
			}
		}


		internal ElaValue CallPartial(ElaFunction fun, ElaValue arg)
		{
			if (fun.AppliedParameters < fun.Parameters.Length)
			{
				fun = fun.Clone();
				fun.Parameters[fun.AppliedParameters++] = arg;
				return new ElaValue(fun);
			}

			var t = MainThread.Clone();

			if (fun.ModuleHandle != t.ModuleHandle)
				t.SwitchModule(fun.ModuleHandle);

			var layout = t.Module.Layouts[fun.Handle];
			var stack = new EvalStack(layout.StackSize);

			stack.Push(arg);

			if (fun.AppliedParameters > 0)
				for (var i = 0; i < fun.AppliedParameters; i++)
					stack.Push(fun.Parameters[fun.AppliedParameters - i - 1]);

			var cp = new CallPoint(fun.ModuleHandle, stack, new ElaValue[layout.Size], fun.Captures);
			t.CallStack.Push(cp);
			t.Offset = layout.Address;
			return Execute(t);
		}


		internal ElaValue Call(ElaFunction fun, ElaValue[] args)
		{
			var t = MainThread.Clone();

			if (fun.ModuleHandle != t.ModuleHandle)
				t.SwitchModule(fun.ModuleHandle);

			var layout = t.Module.Layouts[fun.Handle];
			var stack = new EvalStack(layout.StackSize);
			var len = args.Length;

			if (fun.AppliedParameters > 0)
			{
				for (var i = 0; i < fun.AppliedParameters; i++)
					stack.Push(fun.Parameters[fun.AppliedParameters - i - 1]);

				len += fun.AppliedParameters;
			}

			for (var i = 0; i < len; i++)
				stack.Push(args[len - i - 1]);

			if (len != fun.Parameters.Length + 1)
			{
				InvalidParameterNumber(fun.Parameters.Length + 1, len, t, stack);
				return default(ElaValue);
			}

			var cp = new CallPoint(fun.ModuleHandle, stack, new ElaValue[layout.Size], fun.Captures);
			t.CallStack.Push(cp);
			t.Offset = layout.Address;
			return Execute(t);

		}
		

		private bool Call(ElaObject fun, WorkerThread thread, EvalStack stack, CallFlag cf)
		{
			if (fun.TypeId != FUN)
			{
				var arg = stack.Peek();
				var res = fun.Call(arg, thread.Context);
				stack.Replace(res);

				if (thread.Context.Failed)
				{
					stack.Replace(arg);
					stack.Push(new ElaValue(fun));
					ExecuteThrow(thread, stack);
					return true;
				}

				return false;
			}

			var natFun = (ElaFunction)fun;

			if (natFun.Captures != null)
			{
                var p = cf != CallFlag.AllParams ? stack.PopFast() : natFun.LastParameter;

                if (natFun.Overloaded)
                {
                    natFun = natFun.Resolve(p, thread.Context);

                    if (natFun == null)
                    {
                        ExecuteThrow(thread, stack);
                        return true;
                    }
                }
                
                if (natFun.AppliedParameters < natFun.Parameters.Length)
				{
					var newFun = natFun.CloneFast();
					newFun.Parameters[natFun.AppliedParameters] = p;
					newFun.AppliedParameters++;
					stack.Push(new ElaValue(newFun));
					return false;
				}
				
				if (natFun.AppliedParameters == natFun.Parameters.Length)
				{
					if (natFun.ModuleHandle != thread.ModuleHandle)
						thread.SwitchModule(natFun.ModuleHandle);
                        
					var mod = thread.Module;
					var layout = mod.Layouts[natFun.Handle];

					var newLoc = new ElaValue[layout.Size];
					var newStack = new EvalStack(layout.StackSize);
					var newMem = new CallPoint(natFun.ModuleHandle, newStack, newLoc, natFun.Captures);

					if (cf != CallFlag.NoReturn)
						thread.CallStack.Peek().BreakAddress = thread.Offset;

                    newStack.Push(p);

					for (var i = 0; i < natFun.Parameters.Length; i++)
						newStack.Push(natFun.Parameters[natFun.Parameters.Length - i - 1]);
					
					if (natFun.Flip)
					{
						var right = newStack.Pop();
						var left = newStack.Peek();
						newStack.Replace(right);
						newStack.Push(left);
					}

					thread.CallStack.Push(newMem);
					thread.Offset = layout.Address;
					return true;
				}
				
				InvalidParameterNumber(natFun.Parameters.Length + 1, natFun.Parameters.Length + 2, thread, stack);
				return true;
			}

			if (natFun.AppliedParameters == natFun.Parameters.Length)
			{
				CallExternal(thread, stack, natFun);
				return false;
			}
			else if (natFun.AppliedParameters < natFun.Parameters.Length)
			{
				var newFun = natFun.Clone();
				newFun.Parameters[newFun.AppliedParameters] = stack.Peek();
				newFun.AppliedParameters++;
				stack.Replace(new ElaValue(newFun));
				return true;
			}
			
			InvalidParameterNumber(natFun.Parameters.Length + 1, natFun.Parameters.Length + 2, thread, stack);		
			return true;
		}


		private void CallExternal(WorkerThread thread, EvalStack stack, ElaFunction funObj)
		{
			try
			{
				var arr = new ElaValue[funObj.Parameters.Length + 1];

				if (funObj.AppliedParameters > 0)
					Array.Copy(funObj.Parameters, arr, funObj.Parameters.Length);

				arr[funObj.Parameters.Length] = stack.Pop();

				if (funObj.Flip)
				{
					var x = arr[0];
					arr[0] = arr[1];
					arr[1] = x;
				}

				stack.Push(funObj.Call(arr));
			}
			catch (ElaRuntimeException ex)
			{
				ExecuteFail(new ElaError(ex.Category, ex.Message), thread, stack);
			}
			catch (Exception ex)
			{
				ExecuteFail(new ElaError(ElaRuntimeError.CallFailed, ex.Message), thread, stack);
			}
		}
		#endregion


		#region Exceptions
		private void ExecuteFail(ElaRuntimeError err, WorkerThread thread, EvalStack stack)
		{
			thread.Context.Fail(err);
			ExecuteThrow(thread, stack);
		}


		private void ExecuteFail(ElaError err, WorkerThread thread, EvalStack stack)
		{
			thread.Context.Fail(err);
			ExecuteThrow(thread, stack);
		}


        private void RunOverloadedFunction(WorkerThread thread, EvalStack stack)
        {
            thread.Context.Failed = false;
            var ofun = default(ElaOverloadedFunction);

            if (!overloads.TryGetValue(thread.Context.OverloadFunction, out ofun))
            {
                thread.Context.NoOverload(thread.Context.Tag, thread.Context.OverloadFunction);
                thread.Context.Tag = thread.Context.OverloadFunction = null;
                ExecuteThrow(thread, stack);
                return;
            }

            var fun = default(ElaFunction);

            if (ofun == null || !ofun.overloads.TryGetValue(thread.Context.Tag, out fun))
            {
                thread.Context.NoOverload(thread.Context.Tag, thread.Context.OverloadFunction);
                thread.Context.Tag = thread.Context.OverloadFunction = null;
                ExecuteThrow(thread, stack);
                return;
            }

            var f = fun.Clone();
            thread.Context.Tag = thread.Context.OverloadFunction = null;

            if (f.Parameters.Length > 0)
            {
                f.Parameters[f.AppliedParameters] = stack.Pop();
                f.AppliedParameters++;
            }

            Call(f, thread, stack, CallFlag.None);
        }


		private void ExecuteThrow(WorkerThread thread, EvalStack stack)
		{
			if (thread.Context.Thunk != null)
			{
				thread.Context.Failed = false;
				var t = thread.Context.Thunk;
				thread.Context.Thunk = null;
				thread.Offset--;
                Call(t.Function, thread, stack, CallFlag.AllParams);
				thread.CallStack.Peek().Thunk = t;
				return;
			}
            else if (thread.Context.Tag != null)
            {
                RunOverloadedFunction(thread, stack);
                return;
            }

			var err = thread.Context.Error;
			thread.Context.Reset();
			var callStack = thread.CallStack;
			var cm = default(int?);
			var i = 0;

			if (callStack.Count > 0)
			{
				do
					cm = callStack[callStack.Count - (++i)].CatchMark;
				while (cm == null && i < callStack.Count);
			}

			if (cm == null)
				throw CreateException(Dump(err, thread));
			else
			{
				var c = 1;

				while (c++ < i)
					callStack.Pop();

				var curStack = callStack.Peek().Stack;
				curStack.Clear(callStack.Peek().StackOffset);
				curStack.Push(new ElaValue(Dump(err, thread)));
				thread.Offset = cm.Value;
			}
		}


		private ElaError Dump(ElaError err, WorkerThread thread)
		{
			if (err.Stack != null)
				return err;

			err.Module = thread.ModuleHandle;
			err.CodeOffset = thread.Offset;
			var st = new Stack<StackPoint>();

			for (var i = 0; i < thread.CallStack.Count; i++)
			{
				var cm = thread.CallStack[i];
				st.Push(new StackPoint(cm.BreakAddress, cm.ModuleHandle));
			}

			err.Stack = st;
			return err;
		}


		private ElaCodeException CreateException(ElaError err)
		{
			var deb = new ElaDebugger(asm);
			var mod = asm.GetModule(err.Module);
			var cs = deb.BuildCallStack(err.CodeOffset, mod, mod.File, err.Stack);
			return new ElaCodeException(err.FullMessage.Replace("\0",""), err.Code, cs.File, cs.Line, cs.Column, cs, err);
		}


		private ElaMachineException Exception(string message, Exception ex, params object[] args)
		{
			return new ElaMachineException(Strings.GetMessage(message, args), ex);
		}


		private ElaMachineException Exception(string message)
		{
			return Exception(message, null);
		}


		private void NoOperation(string op, ElaValue value, WorkerThread thread, EvalStack evalStack)
		{
			var str = value.Ref.Show(value, new ShowInfo(10, 10), thread.Context);

			if (str.Length > 40)
				str = str.Substring(0, 40) + "...";

			ExecuteFail(new ElaError(ElaRuntimeError.InvalidOp, str, value.GetTypeName(), op), thread, evalStack);
		}


		private void InvalidType(ElaValue val, WorkerThread thread, EvalStack evalStack, string type)
		{
			ExecuteFail(new ElaError(ElaRuntimeError.InvalidType, type, val.GetTypeName()), thread, evalStack);
		}


		private void InvalidParameterNumber(int pars, int passed, WorkerThread thread, EvalStack evalStack)
		{
			if (passed == 0)
				ExecuteFail(new ElaError(ElaRuntimeError.CallWithNoParams, pars), thread, evalStack);
			else if (passed > pars)
				ExecuteFail(new ElaError(ElaRuntimeError.TooManyParams), thread, evalStack);
			else if (passed < pars)
				ExecuteFail(new ElaError(ElaRuntimeError.TooFewParams), thread, evalStack);
		}


		private void ConversionFailed(ElaValue val, int target, string err, WorkerThread thread, EvalStack evalStack)
		{
			ExecuteFail(new ElaError(ElaRuntimeError.ConversionFailed, val.GetTypeName(),
				TypeCodeFormat.GetShortForm((ElaTypeCode)target), err), thread, evalStack);
		}
		#endregion


		#region Properties
		public CodeAssembly Assembly { get { return asm; } }

		internal WorkerThread MainThread { get; private set; }
		#endregion
	}
}