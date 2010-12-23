﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ela.Runtime.ObjectModel
{
	public sealed class ElaUnit : ElaObject
	{
		#region Construction
		private const string STR = "()";
		public static readonly ElaUnit Instance = new ElaUnit();

		private ElaUnit() : base(ObjectType.Unit, ElaTraits.Eq | ElaTraits.Show)
		{

		}
		#endregion


		#region Traits
		protected internal override ElaValue Equals(ElaValue left, ElaValue right, ExecutionContext ctx)
		{
			return new ElaValue(left.Ref == right.Ref);
		}


		protected internal override ElaValue NotEquals(ElaValue left, ElaValue right, ExecutionContext ctx)
		{
			return new ElaValue(left.Ref != right.Ref);
		}


		protected internal override string Show(ExecutionContext ctx, ShowInfo info)
		{
			return STR;
		}
		#endregion
	}
}