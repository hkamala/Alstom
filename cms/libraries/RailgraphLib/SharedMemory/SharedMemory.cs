using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace RailgraphLib.SharedMemory
{
	using EBISYSID = UInt32;
	using EBIOBJTYPE = UInt16;
	using StateBitVar = UInt64;

	public class SharedMemory
	{
		private static string IVTABLE_SHMEM_NAME = "IVTBL_SHARED";
		private readonly int HEADERSIZE;
		private readonly int ITEMSIZE;
		
		private SharedMemoryHeader m_header;
		protected MemoryMappedFile m_MappedFile;
		protected MemoryMappedViewAccessor m_MemoryAccessor;
		private SortedDictionary<EBISYSID, EBISYSID> ByteIndexTable = new SortedDictionary<EBISYSID, EBISYSID>();
		
		private static SharedMemory instValTable = new SharedMemory();

		public SharedMemory() 
		{
			HEADERSIZE = SharedMemoryHeader.SizeOf();
			ITEMSIZE = SharedMemoryItem.SizeOf();
		}

		public bool InitTable()
		{
			m_MappedFile = MemoryMappedFile.OpenExisting(IVTABLE_SHMEM_NAME, MemoryMappedFileRights.Read);
			m_MemoryAccessor = m_MappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			m_header = GetHeader();
			if (CreateSysIdMap() > 0)
				return true;

			return false;
		}

		public bool GetDynBits(EBISYSID sysid, ref StateBitVar value, ref bool bWasReadButIsInvalid) // if false is returned one can check reason from bWasReadButIsInvalid
		{
			bool bReturnValue = false;

			if (GetDynBitsAsNumber(sysid, ref value))
			{
				if ((value & m_header.mSTAInvalid) != 0)
					bWasReadButIsInvalid = true;
				else
					bReturnValue = true;
			}
			return bReturnValue;
		}
		
		public bool GetDynBitsAsNumber(EBISYSID sysid, ref StateBitVar value) // does not care about invalid bit...
		{
			IntPtr ptr = GetRawPtr(sysid);
			if (ptr == IntPtr.Zero)
				return false;

			IntPtr tmp = ptr + sizeof(EBISYSID) + IVType.SizeOf(); //item.m_CurVal.bi.lDynBits;
			value = (UInt64)Marshal.ReadInt64(tmp);
			Marshal.FreeHGlobal(ptr);
			return true;
		}
		
		public bool GetStaBits(EBISYSID sysid, ref StateBitVar value)
		{
			IntPtr ptr = GetRawPtr(sysid);
			if (ptr == IntPtr.Zero)
				return false;

			IntPtr tmp = ptr + sizeof(EBISYSID) + IVType.SizeOf() + sizeof(StateBitVar);
			value = (UInt64)Marshal.ReadInt64(tmp); //item.m_CurVal.bi.lStaBits;
			Marshal.FreeHGlobal(ptr);
			return true;
		}

		public bool GetMeasurement(EBISYSID sysid, ref float real, ref StateBitVar value)
		{
			IntPtr ptr = GetRawPtr(sysid);
			if (ptr == IntPtr.Zero)
				return false;

			IntPtr tmp = ptr + sizeof(EBISYSID) + IVType.SizeOf();
			value = (UInt64)Marshal.ReadInt64(tmp); //item.m_CurVal.re.lDynBits;
			tmp += sizeof(UInt64);
			float[] floatArr = new float[1]; //item.m_CurVal.re.CurVal;
			Marshal.Copy(tmp, floatArr, 0, 1);
			real = floatArr[0];

			Marshal.FreeHGlobal(ptr);
			return true;
		}
		
		public bool GetIVTypes(EBISYSID sysid, ref EBIOBJTYPE objTyp, ref UInt16 basTyp, ref UInt16 varTyp, ref UInt16 props) 
		{
			IntPtr ptr = GetRawPtr(sysid);
			if (ptr == IntPtr.Zero)
				return false;

			IntPtr tmp = ptr +sizeof(EBISYSID);
			objTyp = (EBIOBJTYPE)Marshal.ReadInt16(tmp); //item.m_TypeInfo.objType;
			tmp += sizeof(EBIOBJTYPE);

			varTyp = (byte)Marshal.ReadByte(tmp); //item.m_TypeInfo.VarTyp;
			tmp += sizeof(byte);

			basTyp = (byte)Marshal.ReadByte(tmp); //item.m_TypeInfo.BasTyp;
			tmp += sizeof(byte);

			props = (byte)Marshal.ReadByte(tmp); //item.m_TypeInfo.Props;

			Marshal.FreeHGlobal(ptr);
			return true;
		}
		
		public bool GetRealProperties(EBISYSID sysid, ref float fProp0, ref float fProp1, ref float fProp2)
		{
			IntPtr ptr = GetRawPtr(sysid);
			if (ptr == IntPtr.Zero)
				return false;

			IntPtr tmp = ptr + sizeof(EBISYSID) + IVType.SizeOf() + SIVDef.SizeOf() + sizeof(StateBitVar);

			float[] floatArr = new float[1];
			Marshal.Copy(tmp, floatArr, 0, 1);
			fProp0 = floatArr[0]; //item.m_Properties[0].CurVal;

			tmp += sizeof(float) + sizeof(StateBitVar);
			Marshal.Copy(tmp, floatArr, 0, 1);
			fProp1 = floatArr[0]; //item.m_Properties[1].CurVal;

			tmp += sizeof(float) + sizeof(StateBitVar);
			Marshal.Copy(tmp, floatArr, 0, 1);
			fProp2 = floatArr[0]; //item.m_Properties[2].CurVal;

			Marshal.FreeHGlobal(ptr);
			return true;
		}

		protected int CreateSysIdMap()
		{
			byte[] buffer = new byte[ITEMSIZE * m_header.nNextFree];
			m_MemoryAccessor.ReadArray(HEADERSIZE, buffer, 0, m_header.nNextFree * ITEMSIZE);

			for (int i = 0; i < m_header.nNextFree; i++)
			{
				IntPtr ptr = Marshal.AllocHGlobal(ITEMSIZE);
				Marshal.Copy(buffer, i * ITEMSIZE, ptr, ITEMSIZE);

				SharedMemoryItem? item = GetSharedMemoryItem(ptr);
				if (item != null && !ByteIndexTable.ContainsKey(item.m_SysId))
					ByteIndexTable.Add(item.m_SysId, (uint)i);

				Marshal.FreeHGlobal(ptr);
			}

			return ByteIndexTable.Count;
		}

		public static SharedMemory Inst() { return instValTable; }

		private SharedMemoryHeader GetHeader()
		{
			byte[] buffer = new byte[HEADERSIZE];
			m_MemoryAccessor.ReadArray(0, buffer, 0, HEADERSIZE);
			return (SharedMemoryHeader)Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), typeof(SharedMemoryHeader));
		}

		private SharedMemoryItem GetSharedMemoryItem(IntPtr ptr)
		{
			IntPtr tmp = ptr;

			SharedMemoryItem item = new SharedMemoryItem();
			item.m_SysId = (uint)Marshal.ReadInt32(tmp);
			tmp += sizeof(uint);

			item.m_TypeInfo.objType = (UInt16)Marshal.ReadInt16(tmp);
			tmp += sizeof(UInt16);
			item.m_TypeInfo.VarTyp = Marshal.ReadByte(tmp);
			tmp += sizeof(byte);
			item.m_TypeInfo.BasTyp = Marshal.ReadByte(tmp);
			tmp += sizeof(byte);
			item.m_TypeInfo.Props = Marshal.ReadByte(tmp);
			tmp += sizeof(byte);

			item.m_CurVal.bi = new SBinars();
			item.m_CurVal.bi.lDynBits = (UInt64)Marshal.ReadInt64(tmp);
			tmp += sizeof(UInt64);
			item.m_CurVal.bi.lStaBits = (UInt64)Marshal.ReadInt64(tmp);
			tmp += sizeof(UInt64);

			for (int i = 0; i < item.m_Properties.Length; i++)
			{
				item.m_Properties[i] = new SReals();
				item.m_Properties[i].lDynBits = (UInt64)Marshal.ReadInt64(tmp);
				tmp += sizeof(UInt64);
				float[] floatArr = new float[1];
				Marshal.Copy(tmp, floatArr, 0, 1);
				item.m_Properties[i].CurVal = floatArr[0];
				tmp += sizeof(float);
			}

			return item;
		}

		public SharedMemoryItem GetItemBySysID(EBISYSID sysID)
		{
			IntPtr ptr = GetRawPtr(sysID);
			if (ptr == IntPtr.Zero)
				return null;

			SharedMemoryItem item = GetSharedMemoryItem(ptr);
			Marshal.FreeHGlobal(ptr);
			return item;
		}

		private IntPtr GetRawPtr(uint sysID)
		{
			if (ByteIndexTable.ContainsKey(sysID))
			{
				byte[] buffer = new byte[ITEMSIZE];
				m_MemoryAccessor.ReadArray(HEADERSIZE + ByteIndexTable[sysID] * ITEMSIZE, buffer, 0, ITEMSIZE);
				IntPtr ptr = Marshal.AllocHGlobal(ITEMSIZE);
				Marshal.Copy(buffer, 0, ptr, ITEMSIZE);
				return ptr;
			}

			return IntPtr.Zero;
		}
	}
}
