﻿using System;

namespace Ela.Compilation
{
    public enum Op
    {
        Nop,
        Len,
        Pushunit,
        Pushelem,
        PushI4_0,
        PushI1_0,
        PushI1_1,
        Pop,
        Pushstr_0,
        Genfin,
        Cons,
        Gen,
        Tail,
        Head,
        Ret,
        Concat,
        Add,
        Mul,
        Div,
        Quot,
        Rem,
        Mod,
        Pow,
        Sub,
        Shr,
        Shl,
        Ceq,
        Cneq,
        Clt,
        Cgt,
        Cgteq,
        Clteq,
        AndBw,
        OrBw,
        Xor,
        Not,
        Neg,
        NotBw,
        Dup,
        Swap,
        Newlazy,
        Newlist,
        Newtup_2,
        Stop,
        NewI8,
        NewR8,
        Leave,
        Flip,
        LazyCall,
        Call,
        Callt,
        Throw,
        Rethrow,
        Force,
        Isnil,
        Show,
        Gettag,
        Recfld,
        Addmbr,                
        Traitch,
        Skiptag,
        Newtype,
        Ctype,        
        Ctypei,
        
        Untag,
        Reccons,
        Tupcons,
        Ctorid,
        Typeid,
        Classid,
        Newfunc,
        Newmod,
        Pushext,
        Newrec,
        Newtup,
        Failwith,
        Start,
        Pushstr,
        PushCh,
        PushI4,
        PushR4,
        Pushloc,
        Pushvar,
        Pushfld,
        Poploc,
        Popvar,
        Runmod,
        Br,
        Brtrue,
        Brfalse,
        Newfun
    }
}