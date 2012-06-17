﻿using System;
using System.Collections.Generic;

namespace Ela.Runtime.ObjectModel
{
	public abstract class ElaObject : IFormattable
	{
        internal static readonly ExecutionContext DummyContext = new ExecutionContext();
        internal const string INVALID = "<INVALID>";

        internal sealed class ElaInvalidObject : ElaObject
        {
            internal static readonly ElaInvalidObject Instance = new ElaInvalidObject();

            internal ElaInvalidObject()
            {

            }

            public override string ToString(string format, IFormatProvider formatProvider)
            {
                return INVALID;
            }
        }

		protected ElaObject() : this(ElaTypeCode.Object)
		{
			
		}
		
		internal ElaObject(ElaTypeCode type)
		{
			TypeId = (Int32)type;
		} 
        
        internal virtual long AsLong()
        {
            return default(Int64);
        }

        internal virtual double AsDouble()
        {
            return default(Double);
        }

		public override string ToString()
		{
            return ToString(String.Empty, Culture.NumberFormat);
		}

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            return "[" + GetTypeName() + ":" + TypeId + "]";
        }
        
		protected ElaValue Default()
		{
			return new ElaValue(ElaInvalidObject.Instance);
		}
        
		protected internal virtual string GetTypeName()
		{
			return TypeCodeFormat.GetShortForm((ElaTypeCode)TypeId);
		}
    	
		protected internal virtual ElaValue Tail(ExecutionContext ctx)
		{
			ctx.NoOperator(new ElaValue(this), "tail");
			return Default();
		}

		protected internal virtual ElaValue Generate(ElaValue value, ExecutionContext ctx)
		{
			ctx.NoOperator(new ElaValue(this), "gen");
			return Default();
		}
        
		protected internal virtual ElaValue GenerateFinalize(ExecutionContext ctx)
		{
			ctx.NoOperator(new ElaValue(this), "genfin");
			return Default();
		}
        
        internal virtual ElaValue Force(ElaValue @this, ExecutionContext ctx)
		{
			return @this;
		}
        
		internal virtual string GetTag(ExecutionContext ctx)
		{
			return String.Empty;
		}
        
		internal virtual ElaValue Untag(ExecutionContext ctx)
        {
			ctx.NoOperator(new ElaValue(this), "untag");
			return Default();
        }

        internal int TypeId { get; private set; }
    }
}