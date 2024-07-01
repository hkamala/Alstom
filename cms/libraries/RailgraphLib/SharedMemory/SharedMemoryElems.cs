using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.SharedMemory
{
	using EBISYSID = UInt32;
	using EBIOBJTYPE = UInt16;
	using StateBitVar = UInt64;

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IVType
	{
		public EBIOBJTYPE objType;
		public byte VarTyp;
		public byte BasTyp;
		public byte Props;

		public static int SizeOf() => sizeof(EBIOBJTYPE) + 3 * sizeof(byte);
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SReals      // Real variables
	{
		public StateBitVar lDynBits;   // status of variable
		public float CurVal;     // current real value

		public static int SizeOf() => sizeof(StateBitVar) + sizeof(float);
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SBinars     // structure for status points and CTC states
	{
		public StateBitVar lDynBits;   // dynamic bits of variable
		public StateBitVar lStaBits;   // status of variable

		public static int SizeOf() => 2 * sizeof(StateBitVar);
	};

	[StructLayout(LayoutKind.Explicit)]
	public struct SIVDef
	{
		[FieldOffset(0)] public SReals re;       // status and value of measurement
		[FieldOffset(0)] public SBinars bi;       // status and value of indication

		public static int SizeOf() => Math.Max(SReals.SizeOf(), SBinars.SizeOf());
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class SharedMemoryItem
	{
		public EBISYSID m_SysId;        // Data Base Id
		public IVType m_TypeInfo = new IVType();     // Type description
		public SIVDef m_CurVal = new SIVDef();       // Current value of variable
		public SReals[] m_Properties = new SReals[3]; // Other properties of variable

		public static int SizeOf() => sizeof(EBISYSID) + IVType.SizeOf() + SIVDef.SizeOf() + 3 * SReals.SizeOf();
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SharedMemoryHeader
	{
		public int nMaxElems;
		public int nNextFree;
		public int nHashTableSize;
		public StateBitVar mSTAInvalid;

		public static int SizeOf() => 3 * sizeof(int) + sizeof(StateBitVar);
	}
}
