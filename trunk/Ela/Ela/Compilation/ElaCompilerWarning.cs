﻿using System;

namespace Ela.Compilation
{
	public enum ElaCompilerWarning
	{
		None = 0,

		
		// = 400,

		// = 401,

		MatchEntryAlwaysFail = 402,

		MatchEntryNotReachable = 403, 
		
		ValueNotUsed = 404,
		
		FunctionInvalidType = 405,

		// = 406,

        BottomValue = 407,
	}
}