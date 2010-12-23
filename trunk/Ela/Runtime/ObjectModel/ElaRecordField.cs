﻿using System;

namespace Ela.Runtime.ObjectModel
{
	public struct ElaRecordField
	{
		public ElaRecordField(string field, ElaValue value)
		{
			Field = field;
			Value = value;
		}

		public ElaRecordField(string field, object value)
		{
			Field = field;
			Value = ElaValue.FromObject(value);
		}

		public readonly string Field;
		public readonly ElaValue Value;
	}
}
