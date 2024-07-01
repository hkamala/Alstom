using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RailgraphLib.armd
{
	public class Armd
	{
		public static bool alreadyInitiated() => m_bAlreadyInitialized;

		public static void init(SolidDB.CSolidEntryPoint solidEntry)
		{
			if (m_bAlreadyInitialized)
				return;

			m_bAlreadyInitialized = true;

			var armdCommands = solidEntry.InitArmd(strArmdCommand);

			foreach (var cmd in armdCommands)
			{
				int selector = (int)cmd[SolidDB.CSolidEntryPoint.SELECTOR];
				bool useStaticBits = selector == -1 ? true : selector == 0 ? false : true;

				UInt64 pretMask = (UInt64)(long)cmd[SolidDB.CSolidEntryPoint.PRETMASK];
				UInt64 isequal = (UInt64)(long)cmd[SolidDB.CSolidEntryPoint.ISEQUAL];
				string strDescr = (string)cmd[SolidDB.CSolidEntryPoint.DESCR];

				SArmdObj sArmdObj = new SArmdObj(pretMask, isequal, useStaticBits);
				setArmdStruct(strDescr, ref sArmdObj);
			}
		}


		public static UInt64 getArmdObj(string str)
		{
			UInt64 mask = 0;
			if (!getArmdObjExact(str, ref mask))
				getArmdObjExact("ARMDInitErrorValue", ref mask);

			return mask;
		}
		public static UInt64 getArmdObj(string str, ref bool bUseStaticBits)
		{
			SArmdObj armdObj = getArmdStruct(str);
			bUseStaticBits = armdObj.m_bUseStaticBits;
			return armdObj.m_lBits;
		}
		public static bool getArmdObjExact(string str, ref UInt64 mask)
		{
			if (m_armdObjMap.ContainsKey(str))
			{
				mask = m_armdObjMap[str].m_lBits;
				return true;
			}

			return false;
		}

		public static SArmdObj getArmdStruct(string str)
		{
			bool bUseStaticBitsAsDefault = true;
			SArmdObj sArmdObj = new SArmdObj(0,0, bUseStaticBitsAsDefault);
			if (!getArmdStructExact(str, ref sArmdObj))
				getArmdStructExact("ARMDInitErrorValue", ref sArmdObj);

			return sArmdObj;
		}
		public static bool getArmdStructExact(string str, ref SArmdObj sArmdObj)
		{
			if (m_armdObjMap.ContainsKey(str))
			{
				sArmdObj = m_armdObjMap[str];
				return true;
			}

			return false;
		}
		public static bool setArmdStruct(string str, ref SArmdObj sArmdObj) // Returns false, if value was already set			
		{
			if (!m_armdObjMap.ContainsKey(str))
			{
				m_armdObjMap[str] = sArmdObj;
				return true;
			}

			return false;
		}
			
		private static bool m_bAlreadyInitialized = false;
		private static SortedDictionary<string, SArmdObj> m_armdObjMap = new SortedDictionary<string, SArmdObj>();
		private static readonly string strArmdCommand = "ARMD";
		private static readonly bool PRETEST_SELECTOR_USE_DYNAMICBITS = false;
	}
}
