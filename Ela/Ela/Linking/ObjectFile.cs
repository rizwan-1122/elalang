﻿using System;
using System.IO;

namespace Ela.Linking
{
	public abstract class ObjectFile
	{
		private const int VERSION = 23;

		protected ObjectFile(FileInfo file)
		{
			File = file;
		}

		public FileInfo File { get; private set; }

		public int Version { get { return VERSION; } }
	}
}