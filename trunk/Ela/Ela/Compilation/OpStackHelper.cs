﻿using System;

namespace Ela.Compilation
{
    internal static class OpStackHelper
    {
        internal static int[] Op = 
		{
			0, //Nop
			0, //Len
			1, //Pushunit
			-1, //Pushelem
			1, //PushI4_0
			1, //PushI1_0
			1, //PushI1_1
			-1, //Pop
			1, //Pushstr_0
			0, //Genfin
			-1, //Cons
			-1, //Gen
			0, //Tail
			0, //Header
			0, //Ret
			-1, //Concat
			-1, //Add
			-1, //Mul
			-1, //Div
            -1, //Quot
			-1, //Rem
            -1, //Mod
			-1, //Pow
			-1, //Sub
			-1, //Shr
			-1, //Shl
			-1, //Ceq
			-1, //Cneq
			-1, //Clt
			-1, //Cgt
			-1, //Cgteq
			-1, //Clteq
			-1, //AndBw
			-1, //OrBw
			-1, //Xor
			0, //Not
			0, //Neg
			0, //NotBw
			1, //Dup
			0, //Swap
			1, //Newlazy
			1, //Newlist
			-1, //Newtup_2
			0, //Stop
			-1, //NewI8
			-1, //NewR8
			0, //Leave
			1, //Flip,
			-1, //LazyCall
			-1, //Call
			-1, //Callt
			-2, //Throw
			-1, //Rethrow
			0, //Force
			0, //Type
			0, //Isnil
			0, //Succ
			0, //Pred
			-1, //Show
			0, //Untag
			0, //Skiphtag
			0, //Gettag
            -1, //Recfld
            -3, //Addmbr            
            -1, //Traitch

            0, //Newtype
            -2, //Reccons
			-1, //Tupcons
            1, //Typeid
            1, //Classid
            -1, //Newfunc
            1, //Newmod
			1, //Pushext
            0, //Skiptag
			0, //Newvar
			1, //Newrec
			1, //Newtup
			0, //Failwith
			0, //Start
			1, //Pushstr
			1, //PushCh
			1, //PushI4
			1, //PushR4
            1, //Pushloc
			1, //Pushvar
			0, //Pushfld
            1, //Poploc
			-1, //Popvar
			0, //Runmod
			0, //Br
			-1, //Brtrue
			-1, //Brfalse
			0, //Newfun			
		};
    }
}