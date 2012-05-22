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
			-3, //Popelem
			1, //Pushstr_0
			-3, //Reccons
			0, //Genfin
			-1, //Cons
			-1, //Gen
			0, //Tail
			0, //Head
			0, //Ret
			-1, //Add
			-1, //Mul
			-1, //Div
			-1, //Sub
			1, //Dup
			0, //Swap
			1, //Newlazy
			1, //Newlist
			1, //Newmod
			-1, //Newtup_2
			0, //Typeid
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
			0, //TypeId
			0, //Isnil
			0, //Tagval
			0, //Skiphtag
			0, //Nil
            0, //Gettag
            
            1, //Pushext
			0, //Callf1
            -1,//Callf2
			0, //Skiptag
			0, //Newvar
			1, //Newrec
			1, //Newtup
			-1, //Skiptn
			-1, //Skiptl
			0, //Pat
			-1, //Tupex
			0, //Failwith
			0, //Start
			1, //Pushstr
			1, //PushCh
			1, //PushI4
			1, //PushR4
			1, //Pushvar
			0, //Pushfld
			-1, //Popvar
			-2, //Popfld
			0, //Runmod
			0, //Has
			0, //Br
			-1, //Brtrue
			-1, //Brfalse
			-2, //Br_lt
			-2, //Br_gt
			-2, //Br_eq
			-2, //Br_neq
			-1, //Brnil
			0, //Newfun			
		};
	}
}