
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
public class ConvertibilityTest
{

	private struct taginfo
	{
		public string PureIdentityURI;
		public int GS1CompanyPrefixLength;
		public string TagURI;
		public byte[] EPCbank;
	}

	private MockReaderLowLevel _Reader;
	private int _NumTags;

	private List<taginfo> _TagInfos;
	[TestInitialize()]
	public void Setup()
	{
		var assembly = typeof(ConvertibilityTest).GetTypeInfo().Assembly;
		string AssemblyName = assembly.GetName().Name;

		string Tags = null;
		using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(AssemblyName + ".TagPopulationConvertibility.csv")))
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
			if (cells.Length != 4)
			{
				break;
			}

			info.PureIdentityURI = cells[0];
			info.GS1CompanyPrefixLength = Convert.ToInt32(cells[1]);
			info.TagURI = cells[2];
			// add the missing memory chunk
			info.EPCbank = InterrogatorLib.Utility.HexStringToByteArray("C41E3400" + cells[3]);

			_TagInfos.Add(info);
			I = I + 1;
		}

		// pass down to the mock reader
		_Reader = new MockReaderLowLevel(_TagInfos.Select(ti => ti.EPCbank));
	}

	[TestCleanup()]
	public void TearDown()
	{
		_Reader.Dispose();
	}

	[TestMethod()]
	public void TestEPCConvertibility()
	{
		for (int index = 0; index < _NumTags; index++)
		{
			object tag = _Reader.SingulateTagAssisted(index);
			TagEPCURI_SGTIN epc = default(TagEPCURI_SGTIN);
			taginfo info = _TagInfos[index];

			Console.WriteLine("Testing tag at position {0}", index);

			DecodeError res0 = default(DecodeError);
			res0 = _Reader.ReadEPC_SGTIN(tag, out epc);
			Assert.AreEqual(res0, DecodeError.None);
			Assert.AreEqual(epc.Identity.URI, info.PureIdentityURI);
			Assert.AreEqual(epc.Identity.GS1CompanyPrefixLength, info.GS1CompanyPrefixLength);

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

}
