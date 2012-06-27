﻿using System;
using System.Collections.Generic;
using System.Text;
using Ela.Parsing;

namespace Ela.CodeModel
{
	public sealed class ElaModuleInclude : ElaExpression
	{
		internal ElaModuleInclude(Token tok) : base(tok, ElaNodeType.ModuleInclude)
		{
			Path = new List<String>();
		}
        
		public ElaModuleInclude() : this(null)
		{
			
		}
		
        internal override bool Safe()
        {
            return true;
        }

		internal override void ToString(StringBuilder sb, Fmt fmt)
		{
			sb.Append("open ");

			for (var i = 0; i < Path.Count; i++)
			{
				if (i > 0)
					sb.Append('.');

				sb.Append(Path[i]);
			}

			sb.Append(Name);

			if (!String.IsNullOrEmpty(Alias) && Alias != Name)
			{
				sb.Append('@');
				sb.Append(Alias);
			}

			if (!String.IsNullOrEmpty(DllName))
			{
				sb.Append('#');
				sb.Append(DllName);
			}
		}

		public string Name { get; set; }

		public string Alias { get; set; }

		public string DllName { get; set; }

        public bool RequireQuailified { get; set; }

		public List<String> Path { get; private set; }

		public bool HasImportList
		{
			get { return _importList != null; }
		}

		private List<ElaImportedVariable> _importList;
		public List<ElaImportedVariable> ImportList
		{
			get
			{
				if (_importList == null)
					_importList = new List<ElaImportedVariable>();

				return _importList;
			}
		}

        public ElaModuleInclude And { get; set; }
	}
}