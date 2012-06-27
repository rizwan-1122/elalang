﻿using System;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public abstract class ElaExpression
	{
		protected ElaExpression(ElaNodeType type) : this(null, type)
		{

		}
        
		internal ElaExpression(Token tok, ElaNodeType type)
		{
			Type = type;

			if (tok != null)
			{
				Line = tok.line;
				Column = tok.col;
			}
		}
		
        public void SetLinePragma(int line, int column)
		{
			Line = line;
			Column = column;
		}

        internal virtual string GetName()
		{
			return null;
		}
        
        internal virtual bool Safe()
        {
            return false;
        }
        
		public override string ToString()
		{
			var sb = new StringBuilder();
			ToString(sb, new Fmt());
			return sb.ToString();
		}
        
		internal abstract void ToString(StringBuilder sb, Fmt fmt);
		
        public int Line { get; private set; }

		public int Column { get; private set; }
		
		public ElaNodeType Type { get; protected set; }

        public bool Parens { get; set; }

        internal string Code
        {
            get { return ToString(); }
        }
	}
}