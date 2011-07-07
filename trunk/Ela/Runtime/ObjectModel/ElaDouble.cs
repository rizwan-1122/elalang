﻿using System;

namespace Ela.Runtime.ObjectModel
{
	public sealed class ElaDouble : ElaObject
	{
		#region Construction
		public ElaDouble(double value) : base(ElaTypeCode.Double)
		{
			Value = value;
		}
		#endregion


        #region Methods
        internal override ElaValue Convert(ElaValue @this, ElaTypeCode type)
        {
            switch (type)
            {
                case ElaTypeCode.Integer: return new ElaValue((Int32)Value);
                case ElaTypeCode.Single: return new ElaValue((float)Value);
                case ElaTypeCode.Double: return @this;
                case ElaTypeCode.Long: return new ElaValue((Int64)Value);
                case ElaTypeCode.Char: return new ElaValue((Char)Value);
                default: return base.Convert(@this, type);
            }
        }


        internal override string GetTag()
        {
            return "Double#";
        }


        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }


        internal protected override int Compare(ElaValue @this, ElaValue other)
		{
			return other.TypeCode == ElaTypeCode.Single ? Value.CompareTo(other.DirectGetReal()) :
				other.TypeCode == ElaTypeCode.Integer ? Value.CompareTo((Double)other.I4) :
                other.TypeCode == ElaTypeCode.Long ? Value.CompareTo((Double)((ElaLong)other.Ref).Value) :
				other.TypeCode == ElaTypeCode.Double ? Value.CompareTo(((ElaDouble)other.Ref).Value) :
				-1;
		}


        protected internal override string Show(ElaValue @this, ShowInfo info, ExecutionContext ctx)
		{
			try
			{
				return !String.IsNullOrEmpty(info.Format) ? Value.ToString(info.Format, Culture.NumberFormat) :
					Value.ToString(Culture.NumberFormat);
			}
			catch (FormatException)
			{
				if (ctx == ElaObject.DummyContext)
					throw;

				ctx.InvalidFormat(info.Format, new ElaValue(this));
				return String.Empty;
			}
		}
		#endregion


		#region Properties
		internal double Value { get; private set; }
		#endregion
	}
}