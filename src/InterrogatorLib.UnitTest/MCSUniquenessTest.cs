
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using InterrogatorLib;

[TestClass()]
public class MCSUniquenessTest
{

	private struct taginfo
	{
		public byte[] TID;
		public byte[] EPC;
	}

	private MockReaderLowLevel _Reader;
	private int _NumTags;

	private List<taginfo> _TagInfos;
	[TestInitialize()]
	public void Setup()
	{
		var assembly = typeof(MCSUniquenessTest).GetTypeInfo().Assembly;
		string AssemblyName = assembly.GetName().Name;

		string Tags = null;
		using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(AssemblyName + ".TagPopulationMCSUniqueness.csv")))
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
		_TagInfos = new List<taginfo>();
		int I = 0;
		foreach (string line in Lines)
		{
			if (string.IsNullOrEmpty(line))
			{
				break;
			}

			taginfo info = new taginfo();
			string[] cells = line.Split(';');
			if (cells.Length < 2)
			{
				break;
			}

			info.TID = InterrogatorLib.Utility.HexStringToByteArray(cells[0]);
			info.EPC = InterrogatorLib.Utility.HexStringToByteArray(cells[1]);

			_TagInfos.Add(info);
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
	public void TestMCSUniqueness()
	{
		Dictionary<TagEPCURI_SGTIN, int> TIDmap = new Dictionary<TagEPCURI_SGTIN, int>();

		for (int index = 0; index < _NumTags - 1; index++)
		{
			object tag = _Reader.SingulateTagAssisted(index);
			TagEPCURI_SGTIN epc = default(TagEPCURI_SGTIN);
			taginfo info = _TagInfos[index];
			TID tid = default(TID);

			Console.WriteLine("Testing tag at position {0}", index);

			DecodeError res0 = default(DecodeError);
			res0 = _Reader.ReadTID(tag, out tid);
			Assert.AreEqual(res0, DecodeError.None);

			res0 = _Reader.ReadEPC_SGTIN(tag, out epc);
			Assert.AreEqual(res0, DecodeError.None);

			Console.WriteLine("Generating serial via MCS");

			bool resB = false;
			TagEPCURI_SGTIN epc1 = default(TagEPCURI_SGTIN);
			resB = MCS.GenerateEPC(ref tid, ref epc, out epc1);
			Assert.AreEqual(resB, true);

			// there should not be any equal values
			Assert.AreEqual(false, TIDmap.ContainsKey(epc1));
			TIDmap.Add(epc1, index);
		}

	}

}
