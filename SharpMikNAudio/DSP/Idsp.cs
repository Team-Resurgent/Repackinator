using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMik.DSP
{
	public abstract class Idsp
	{
		public abstract void PushData(sbyte[] data, uint count);
	}
}
