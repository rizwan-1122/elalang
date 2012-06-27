﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
    public sealed class ElaAs : ElaExpression
    {
        internal ElaAs(Token t) : base(t, ElaNodeType.As)
        {

        }

        public ElaAs() : base(ElaNodeType.As)
        {

        }

        internal override bool Safe()
        {
            return Expression.Safe();
        }

        internal override void ToString(StringBuilder sb, Fmt fmt)
        {
            Format.PutInBraces(Expression, sb, fmt);
            sb.Append("@");
            sb.Append(Name);
        }

        public string Name { get; set; }

        public ElaExpression Expression { get; set; }
    }
}