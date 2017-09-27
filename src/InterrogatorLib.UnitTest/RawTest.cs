
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InterrogatorLib;
using System.Linq;
using System.Text;

[TestClass()]
public class RawTest
{
	private struct taginfo
	{
		public byte[] Bytes;
		public string RawURI;
	}

	private MockReaderLowLevel _Reader;
	private int _NumTags;

	private taginfo[] _TagInfos;
	[TestInitialize()]
	public void Setup()
	{
		var assembly = typeof(ConvertibilityTest).GetTypeInfo().Assembly;
		string AssemblyName = assembly.GetName().Name;

		string Tags = null;
		using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(AssemblyName + ".TagPopulationRaw.csv")))
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
			if (cells.Length < 2)
			{
				break;
			}

			info.Bytes = InterrogatorLib.Utility.HexStringToByteArray(cells[0]);
			info.RawURI = cells[1];

			_TagInfos[I] = info;
			I = I + 1;
		}

		// pass down to the mock reader
		_Reader = new MockReaderLowLevel(_TagInfos.Select(i => i.Bytes));
	}

	[TestCleanup()]
	public void TearDown()
	{
		_Reader.Dispose();
	}

	[TestMethod()]
	public void TestRawTagURIReading()
	{
		StringBuilder sb = new StringBuilder();

		for (int index = 0; index < _NumTags; index++)
		{
			object tag = _Reader.SingulateTagAssisted(index);
			TagRaw raw = default(TagRaw);
			taginfo info = _TagInfos[index];

			Console.WriteLine("Testing tag at position {0}", index);

			DecodeError res = default(DecodeError);
			res = _Reader.ReadEPC_Raw(tag, out raw);
			Assert.AreEqual(res, DecodeError.None);

			sb.Length = 0;
			raw.GetURI(sb);
			string uri = sb.ToString();
			Assert.AreEqual(uri, info.RawURI);

			// TODO: write the encoding code, and test that it works
		}

	}

}
