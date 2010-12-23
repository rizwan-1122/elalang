﻿using System;
using Ela.Runtime.ObjectModel;
using System.Runtime.InteropServices;

namespace Ela.Runtime
{
	public struct ElaValue
	{
		#region Construction
		public ElaValue(ElaObject val)
		{
			I4 = 0;
			Ref = val;
		}


		public ElaValue(int val)
		{
			I4 = val;
			Ref = ElaInteger.Instance;
		}


		public ElaValue(char val)
		{
			I4 = (Int32)val;
			Ref = ElaChar.Instance;
		}


		public ElaValue(int val, ElaObject obj)
		{
			I4 = val;
			Ref = obj;
		}


		public ElaValue(float val)
		{
			var conv = new Conv();
			conv.R4 = val;
			I4 = conv.I4_1;
			Ref = ElaSingle.Instance;
		}


		public ElaValue(bool val)
		{
			I4 = val ? 1 : 0;
			Ref = ElaBoolean.Instance;
		}


		public ElaValue(string value)
		{
			I4 = 0;
			Ref = new ElaString(value);
		}


		public ElaValue(long val)
		{
			I4 = 0;
			Ref = new ElaLong(val);
		}


		public ElaValue(double val)
		{
			I4 = 0;
			Ref = new ElaDouble(val);
		}
		#endregion


		#region Methods
		public override string ToString()
		{
			return Ref.Show(this, ElaObject.DummyContext, new ShowInfo(0, 40));
		}


		internal ElaValue Id(ExecutionContext ctx)
		{
			return Ref.TypeId == ElaMachine.LAZ ? Ref.Force(ctx) : this;
		}


		internal float GetReal()
		{
			if (DataType == ObjectType.Integer)
				return (Single)I4;
			else if (DataType == ObjectType.Long)
				return (Single)((ElaLong)Ref).InternalValue;

			return DirectGetReal();
		}


		internal float DirectGetReal()
		{
			var conv = new Conv();
			conv.I4_1 = I4;
			return conv.R4;
		}


		internal double GetDouble()
		{
			return DataType == ObjectType.Double ? ((ElaDouble)Ref).InternalValue :
				DataType == ObjectType.Single ? DirectGetReal() :
				DataType == ObjectType.Long ? ((ElaLong)Ref).InternalValue :
				(Double)I4;
		}


		internal long GetLong()
		{
			return DataType == ObjectType.Long ? ((ElaLong)Ref).InternalValue : I4;				
		}


		public static ElaValue FromObject(object value)
		{
			if (value is ElaObject)
				return new ElaValue((ElaObject)value);
			else if (value is Int32)
				return new ElaValue((Int32)value);
			else if (value is Int64)
				return new ElaValue((Int64)value);
			else if (value is Single)
				return new ElaValue((Single)value);
			else if (value is Double)
				return new ElaValue((Double)value);
			else if (value is Boolean)
				return new ElaValue((Boolean)value);
			else if (value is Char)
				return new ElaValue((Char)value);
			else if (value is String)
				return new ElaValue((String)value);
			else if (value is ElaValue)
				return (ElaValue)value;
			else
				throw new NotSupportedException();
		}
		#endregion


		#region Cast Methods
		public object AsObject()
		{
			switch (DataType)
			{
				case ObjectType.Array: return AsArray();
				case ObjectType.Boolean: return AsBoolean();
				case ObjectType.Char: return AsChar();
				case ObjectType.Double: return AsDouble();
				case ObjectType.Function: return AsFunction();
				case ObjectType.Integer: return AsInteger();
				case ObjectType.List: return AsList();
				case ObjectType.Long: return AsLong();
				case ObjectType.Record: return AsRecord();
				case ObjectType.Single: return AsSingle();
				case ObjectType.String: return AsString();
				case ObjectType.Tuple: return AsTuple();
				case ObjectType.Unit: return null;
				case ObjectType.Lazy: return ((ElaLazy)Ref).AsObject();
				default:
					if (Ref == null)
						throw new NotSupportedException();
					else
						return Ref;
			}
		}


		public string AsString()
		{
			if (DataType == ObjectType.String)
				return ((ElaString)Ref).GetValue();
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsString();
			else
				throw new ElaCastException(ObjectType.String, DataType);
		}


		public bool AsBoolean()
		{
			if (DataType == ObjectType.Boolean)
				return I4 > 0;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsBoolean();
			else
				throw new ElaCastException(ObjectType.Boolean, DataType);
		}


		public char AsChar()
		{
			if (DataType == ObjectType.Char)
				return (Char)I4;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsChar();
			else
				throw new ElaCastException(ObjectType.Char, DataType);
		}


		public double AsDouble()
		{
			if (DataType == ObjectType.Double)
				return ((ElaDouble)Ref).InternalValue;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsDouble();
			else
				throw new ElaCastException(ObjectType.Double, DataType);
		}


		public float AsSingle()
		{
			if (DataType == ObjectType.Single)
				return GetReal();
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsSingle();
			else
				throw new ElaCastException(ObjectType.Single, DataType);
		}


		public int AsInteger()
		{
			if (DataType == ObjectType.Integer)
				return I4;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsInteger();
			else
				throw new ElaCastException(ObjectType.Integer, DataType);
		}


		public long AsLong()
		{
			if (DataType == ObjectType.Long)
				return ((ElaLong)Ref).InternalValue;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsLong();
			else
				throw new ElaCastException(ObjectType.Long, DataType);
		}


		public ElaArray AsArray()
		{
			if (DataType == ObjectType.Array)
				return (ElaArray)Ref;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsArray();
			else
				throw new ElaCastException(ObjectType.Array, DataType);
		}


		public ElaList AsList()
		{
			if (DataType == ObjectType.List)
				return (ElaList)Ref;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsList();
			else
				throw new ElaCastException(ObjectType.List, DataType);
		}


		public ElaTuple AsTuple()
		{
			if (DataType == ObjectType.Tuple)
				return (ElaTuple)Ref;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsTuple();
			else
				throw new ElaCastException(ObjectType.Tuple, DataType);
		}


		public ElaRecord AsRecord()
		{
			if (DataType == ObjectType.Record)
				return (ElaRecord)Ref;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsRecord();
			else
				throw new ElaCastException(ObjectType.Record, DataType);
		}


		public ElaFunction AsFunction()
		{
			if (DataType == ObjectType.Function)
				return (ElaFunction)Ref;
			else if (DataType == ObjectType.Lazy)
				return ((ElaLazy)Ref).AsFunction();
			else
				throw new ElaCastException(ObjectType.Function, DataType);
		}
		#endregion


		#region Fields
		internal int I4;

		internal ElaObject Ref;
		#endregion


		#region Properties
		public ObjectType DataType
		{
			get { return Ref != null ? (ObjectType)Type : ObjectType.None; }
		}


		internal int Type
		{
			get { return Ref.TypeId; }
		}
		#endregion
	}
}