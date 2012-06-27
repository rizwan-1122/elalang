﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
    public sealed class ElaNewtype : ElaExpression
    {
        internal ElaNewtype(Token tok): base(tok, ElaNodeType.Newtype)
        {

        }
        
        public ElaNewtype() : base(ElaNodeType.Newtype)
        {

        }
        
        internal override bool Safe()
        {
            return true;
        }

        internal override void ToString(StringBuilder sb, Fmt fmt)
        {
            sb.Append("type ");
            sb.Append(Name);
            sb.AppendLine();
        }

        public bool HasBody { get; set; }

        public string Name { get; set; }

        public ElaNewtype And { get; set; }
    }
}