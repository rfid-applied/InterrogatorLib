
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InterrogatorLib;

[TestClass()]
public class DataTest
{
	private struct taginfo
	{
		public string STID_URI;
		public byte[] UTID;
		public string PureIdentityURI;
	}

	private MockReaderLowLevel _Reader;
	private int _NumTags;

	private taginfo[] _TagInfos;
	[TestInitialize()]
	public void Setup()
	{
		var assembly = typeof(DataTest).GetTypeInfo().Assembly;
		string AssemblyName = assembly.GetName().Name;

		string Tags = null;
		using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(AssemblyName + ".TagPopulation.csv")))
		{
			Tags = sr.ReadToEnd();
		}

		List<string> LinesLst = new List<string>();
		foreach (string Ln in MockReaderLowLevel.SplitToLines(Tags))
		{
			LinesLst.Add(Ln);
		}

		// Remove the first line (header line)
		string[] Lines = LinesLst.Skip(1).ToArray();
		// Record how many tags we have
		_NumTags = Lines.Length;
		_TagInfos = new taginfo[_NumTags + 1];
		int I = 0;
		foreach (string line in Lines)
		{
			if (string.IsNullOrEmpty(line))
			{
				break;
			}

			taginfo info = new taginfo();
			string[] cells = line.Split(';');
			if (cells.Length < 5)
			{
				break;
			}

			if (System.Text.RegularExpressions.Regex.IsMatch(cells[3], "^([0-9a-fA-F]-?)+$"))
			{
				string hex = cells[3];
				info.UTID = Utility.HexStringToByteArray(hex);
			}
			else if (!string.IsNullOrEmpty(cells[3]))
			{
				info.STID_URI = cells[3];
			}
			info.PureIdentityURI = cells[4];

			_TagInfos[I] = info;
			I = I + 1;
		}

		// pass down to the mock reader
		_Reader = new MockReaderLowLevel(Lines);
	}

	[TestCleanup()]
	public void TearDown()
	{
		_Reader.Dispose();
	}

	[TestMethod()]
	public void TestEPCReading()
	{
		for (int index = 0; index < _NumTags; index++)
		{
			object tag = _Reader.SingulateTagAssisted(index);
			TID tid = default(TID);
			TagEPCURI_SGTIN epc = default(TagEPCURI_SGTIN);
			taginfo info = _TagInfos[index];

			Console.WriteLine("Testing tag at position {0}", index);

			DecodeError res = default(DecodeError);
			res = _Reader.ReadTID(tag, out tid);
			Assert.AreEqual(res, DecodeError.None);

			if (!string.IsNullOrEmpty(info.STID_URI))
			{
				Assert.AreEqual(tid.STID_URI, info.STID_URI);
			}
			if ((info.UTID != null) && info.UTID.Length > 0)
			{
				CollectionAssert.AreEqual(tid.Serial, info.UTID);
			}

			DecodeError res0 = default(DecodeError);
			res0 = _Reader.ReadEPC_SGTIN(tag, out epc);
			Assert.AreEqual(res0, DecodeError.None);
			Assert.AreEqual(epc.Identity.URI, info.PureIdentityURI);

			Console.WriteLine("Trying to round-trip");

			// Round-tripping should produce exactly the same data
			bool res1 = _Reader.WriteEPC(tag, epc);
			Assert.IsTrue(res1);

			TagEPCURI_SGTIN epc1 = default(TagEPCURI_SGTIN);
			res0 = _Reader.ReadEPC_SGTIN(tag, out epc1);
			Assert.AreEqual(res0, DecodeError.None);
			Assert.AreEqual(epc, epc1);
		}

	}

	[TestMethod()]
	public void TestMCS()
	{
		Console.WriteLine("Testing MCS");

		for (int index = 0; index < _NumTags; index++)
		{
			object tag = _Reader.SingulateTagAssisted(index);
			TID tid = default(TID);
			TagEPCURI_SGTIN epc = default(TagEPCURI_SGTIN);
			taginfo info = _TagInfos[index];

			Console.WriteLine("Testing tag at position {0}", index);

			DecodeError res = default(DecodeError);
			res = _Reader.ReadTID(tag, out tid);
			Assert.AreEqual(res, DecodeError.None);

			DecodeError res0 = _Reader.ReadEPC_SGTIN(tag, out epc);
			Assert.AreEqual(res0, DecodeError.None);

			Console.WriteLine("Generating a new EPC");

			if ((tid.Serial != null) && tid.Serial.Length > 0)
			{
				TagEPCURI_SGTIN epc1 = default(TagEPCURI_SGTIN);
				var res1 = MCS.GenerateEPC(ref tid, ref epc, out epc1);
				Assert.IsTrue(res1);

				Console.WriteLine("New EPC SGTIN Pure Identity Tag URI {0}", epc1.Identity.URI);

				// Round-tripping should produce exactly the data that we want to see
				//res = _Reader.WriteEPC(tag, epc1)
				//Assert.IsTrue(res)

				//Dim epc1 As TagEPCURI_SGTIN
				//res = _Reader.ReadEPC(tag, epc1)
				//Assert.IsTrue(res)
				//Assert.AreEqual(epc, epc1)
			}
			else
			{
				Console.WriteLine("Skipping, because the TID does not provide a serial number");
			}
		}

	}

}
