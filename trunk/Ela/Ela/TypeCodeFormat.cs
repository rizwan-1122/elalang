﻿using System;

namespace Ela
{
	public static class TypeCodeFormat
	{
		private const string ERR = "INVALID";
		private const string CHAR = "char";
		private const string INT = "int";
		private const string LONG = "long";
		private const string SINGLE = "single";
		private const string DOUBLE = "double";
		private const string STRING = "string";
		private const string BOOL = "bool";
		private const string RECORD = "record";
		private const string TUPLE = "tuple";
		private const string LIST = "list";
		private const string FUN = "fun";
		private const string UNIT = "unit";
		private const string MOD = "module";
		private const string OBJ = "object";
		private const string LAZ = "thunk";
		private const string VAR = "variant";
        private const string TYP = "typeinfo";
		
        public static ElaTypeCode GetTypeCode(string type)
        {
            switch (type)
            {
                case CHAR: return ElaTypeCode.Char;
                case INT: return ElaTypeCode.Integer;
                case LONG: return ElaTypeCode.Long;
                case SINGLE: return ElaTypeCode.Single;
                case DOUBLE: return ElaTypeCode.Double;
                case BOOL: return ElaTypeCode.Boolean;
                case STRING: return ElaTypeCode.String;
                case LIST: return ElaTypeCode.List;
                case TUPLE: return ElaTypeCode.Tuple;
                case RECORD: return ElaTypeCode.Record;
                case FUN: return ElaTypeCode.Function;
                case UNIT: return ElaTypeCode.Unit;
                case MOD: return ElaTypeCode.Module;
                case OBJ: return ElaTypeCode.Object;
                case LAZ: return ElaTypeCode.Lazy;
                case VAR: return ElaTypeCode.Variant;
                case TYP: return ElaTypeCode.TypeInfo;
                default: return ElaTypeCode.None;
            }
        }

		public static string GetShortForm(ElaTypeCode @this)
		{
			switch (@this)
			{
				case ElaTypeCode.Char: return CHAR;
				case ElaTypeCode.Integer: return INT;
				case ElaTypeCode.Long: return LONG;
				case ElaTypeCode.Single: return SINGLE;
				case ElaTypeCode.Double: return DOUBLE;
				case ElaTypeCode.Boolean: return BOOL;
				case ElaTypeCode.String: return STRING;
				case ElaTypeCode.List: return LIST;
				case ElaTypeCode.Tuple: return TUPLE;
				case ElaTypeCode.Record: return RECORD;
				case ElaTypeCode.Function: return FUN;
				case ElaTypeCode.Unit: return UNIT;
				case ElaTypeCode.Module: return MOD;
				case ElaTypeCode.Object: return OBJ;
				case ElaTypeCode.Lazy: return LAZ;
				case ElaTypeCode.Variant: return VAR;
                case ElaTypeCode.TypeInfo: return TYP;
				default: return ERR;
			}
		}
	}
}