using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class lzip
{
	public class inMemory
	{
		public IntPtr pointer = IntPtr.Zero;

		public IntPtr zf = IntPtr.Zero;

		public IntPtr memStruct = IntPtr.Zero;

		public IntPtr fileStruct = IntPtr.Zero;

		public int[] info = new int[3];

		public int lastResult;

		public bool isClosed = true;

		public int size()
		{
			return info[0];
		}

		public byte[] getZipBuffer()
		{
			if (pointer != IntPtr.Zero && info[0] > 0)
			{
				byte[] array = new byte[info[0]];
				Marshal.Copy(pointer, array, 0, info[0]);
				return array;
			}
			return null;
		}
	}

	public struct zipInfo
	{
		public short VersionMadeBy;

		public short MinimumVersionToExtract;

		public short BitFlag;

		public short CompressionMethod;

		public short FileLastModificationTime;

		public short FileLastModificationDate;

		public int CRC;

		public int CompressedSize;

		public int UncompressedSize;

		public short DiskNumberWhereFileStarts;

		public short InternalFileAttributes;

		public int ExternalFileAttributes;

		public int RelativeOffsetOfLocalFileHeader;

		public int AbsoluteOffsetOfLocalFileHeaderStore;

		public string filename;

		public string extraField;

		public string fileComment;
	}

	private const string libname = "libzipw";

	public static List<string> ninfo = new List<string>();

	public static List<ulong> uinfo = new List<ulong>();

	public static List<ulong> cinfo = new List<ulong>();

	public static List<ulong> localOffset = new List<ulong>();

	public static int zipFiles;

	public static int zipFolders;

	public static ulong totalCompressedSize;

	public static ulong totalUncompressedSize;

	public static List<zipInfo> zinfo;

	[DllImport("libzipw")]
	internal static extern int zsetPermissions(string filePath, string _user, string _group, string _other);

	[DllImport("libzipw")]
	internal static extern bool zipValidateFile(string zipArchive, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern int zipGetTotalFiles(string zipArchive, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern int zipGetTotalEntries(string zipArchive, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern int zipGetInfoA(string zipArchive, IntPtr total, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern IntPtr zipGetInfo(string zipArchive, int size, IntPtr unc, IntPtr comp, IntPtr offs, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern void releaseBuffer(IntPtr buffer);

	[DllImport("libzipw")]
	internal static extern ulong zipGetEntrySize(string zipArchive, string entry, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern bool zipEntryExists(string zipArchive, string entry, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern int zipCD(int levelOfCompression, string zipArchive, string inFilePath, string fileName, string comment, [MarshalAs(UnmanagedType.LPStr)] string password, bool useBz2, int diskSize, IntPtr bprog);

	[DllImport("libzipw")]
	internal static extern int zipCDList(int levelOfCompression, string zipArchive, IntPtr filename, int arrayLength, IntPtr prog, IntPtr filenameForced, [MarshalAs(UnmanagedType.LPStr)] string password, bool useBz2, int diskSize, IntPtr bprog);

	[DllImport("libzipw")]
	internal static extern bool zipBuf2File(int levelOfCompression, string zipArchive, string arc_filename, IntPtr buffer, int bufferSize, string comment, [MarshalAs(UnmanagedType.LPStr)] string password, bool useBz2);

	[DllImport("libzipw")]
	internal static extern int zipDeleteFile(string zipArchive, string arc_filename, string tempArchive);

	[DllImport("libzipw")]
	internal static extern int zipEntry2Buffer(string zipArchive, string entry, IntPtr buffer, int bufferSize, IntPtr FileBuffer, int fileBufferLength, [MarshalAs(UnmanagedType.LPStr)] string password);

	[DllImport("libzipw")]
	internal static extern IntPtr zipCompressBuffer(IntPtr source, int sourceLen, int levelOfCompression, ref int v);

	[DllImport("libzipw")]
	internal static extern IntPtr zipDecompressBuffer(IntPtr source, int sourceLen, ref int v);

	[DllImport("libzipw")]
	internal static extern int zipEX(string zipArchive, string outPath, IntPtr progress, IntPtr FileBuffer, int fileBufferLength, IntPtr proc, [MarshalAs(UnmanagedType.LPStr)] string password);

	[DllImport("libzipw")]
	internal static extern int zipEntry(string zipArchive, string arc_filename, string outpath, IntPtr FileBuffer, int fileBufferLength, IntPtr proc, [MarshalAs(UnmanagedType.LPStr)] string password);

	[DllImport("libzipw")]
	internal static extern uint getEntryDateTime(string zipArchive, string arc_filename, IntPtr FileBuffer, int fileBufferLength);

	[DllImport("libzipw")]
	internal static extern int freeMemStruct(IntPtr buffer);

	[DllImport("libzipw")]
	internal static extern IntPtr zipCDMem(IntPtr info, IntPtr pnt, int levelOfCompression, IntPtr source, int sourceLen, string fileName, string comment, [MarshalAs(UnmanagedType.LPStr)] string password, bool useBz2);

	[DllImport("libzipw")]
	internal static extern IntPtr initMemStruct();

	[DllImport("libzipw")]
	internal static extern IntPtr initFileStruct();

	[DllImport("libzipw")]
	internal static extern int freeMemZ(IntPtr pointer);

	[DllImport("libzipw")]
	internal static extern int freeFileZ(IntPtr pointer);

	[DllImport("libzipw")]
	internal static extern IntPtr zipCDMemStart(IntPtr info, IntPtr pnt, IntPtr fileStruct, IntPtr memStruct);

	[DllImport("libzipw")]
	internal static extern int zipCDMemAdd(IntPtr zf, int levelOfCompression, IntPtr source, int sourceLen, string fileName, string comment, [MarshalAs(UnmanagedType.LPStr)] string password, bool useBz2);

	[DllImport("libzipw")]
	internal static extern IntPtr zipCDMemClose(IntPtr zf, IntPtr memStruct, IntPtr info, int err);

	[DllImport("libzipw")]
	internal static extern int zipGzip(IntPtr source, int sourceLen, IntPtr outBuffer, int levelOfCompression, bool addHeader, bool addFooter);

	[DllImport("libzipw")]
	internal static extern int zipUnGzip(IntPtr source, int sourceLen, IntPtr outBuffer, int outLen, bool hasHeader, bool hasFooter);

	[DllImport("libzipw")]
	internal static extern int zipUnGzip2(IntPtr source, int sourceLen, IntPtr outBuffer, int outLen);

	[DllImport("libzipw")]
	internal static extern int gzip_File(string inFile, string outFile, int level, IntPtr progress, bool addHeader);

	[DllImport("libzipw")]
	internal static extern int ungzip_File(string inFile, string outFile, IntPtr progress);

	[DllImport("libzipw")]
	public static extern void setCancel();

	[DllImport("libzipw")]
	internal static extern int readTarA(string zipArchive, IntPtr total);

	[DllImport("libzipw")]
	internal static extern IntPtr readTar(string zipArchive, int size, IntPtr unc);

	[DllImport("libzipw")]
	internal static extern int createTar(string outFile, IntPtr filePath, IntPtr filename, int arrayLength, IntPtr prog, IntPtr bprog);

	[DllImport("libzipw")]
	internal static extern int extractTar(string inFile, string outDir, string entry, IntPtr prog, IntPtr bprog, bool fullPaths);

	[DllImport("libzipw")]
	internal static extern int bz2(bool decompress, int level, string inFile, string outFile, IntPtr byteProgress);

	internal static GCHandle gcA(object o)
	{
		return GCHandle.Alloc(o, GCHandleType.Pinned);
	}

	public static int getTotalFiles(string zipArchive, byte[] FileBuffer = null)
	{
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			int result = zipGetTotalFiles(null, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
			return result;
		}
		return zipGetTotalFiles(zipArchive, IntPtr.Zero, 0);
	}

	public static int getTotalEntries(string zipArchive, byte[] FileBuffer = null)
	{
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			int result = zipGetTotalEntries(null, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
			return result;
		}
		return zipGetTotalEntries(zipArchive, IntPtr.Zero, 0);
	}

	public static int getEntryIndex(string entry)
	{
		if (ninfo == null || ninfo.Count == 0)
		{
			return -1;
		}
		int result = -1;
		for (int i = 0; i < ninfo.Count; i++)
		{
			if (entry == ninfo[i])
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static ulong getFileInfo(string zipArchive, byte[] FileBuffer = null)
	{
		ninfo.Clear();
		uinfo.Clear();
		cinfo.Clear();
		localOffset.Clear();
		zipFiles = 0;
		zipFolders = 0;
		totalCompressedSize = 0uL;
		totalUncompressedSize = 0uL;
		int num = 0;
		int[] array = new int[1];
		GCHandle gCHandle = gcA(array);
		if (FileBuffer != null)
		{
			GCHandle gCHandle2 = gcA(FileBuffer);
			num = zipGetInfoA(null, gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle2.Free();
		}
		else
		{
			num = zipGetInfoA(zipArchive, gCHandle.AddrOfPinnedObject(), IntPtr.Zero, 0);
		}
		gCHandle.Free();
		if (num <= 0)
		{
			return 0uL;
		}
		IntPtr zero = IntPtr.Zero;
		ulong[] array2 = new ulong[array[0]];
		ulong[] array3 = new ulong[array[0]];
		ulong[] array4 = new ulong[array[0]];
		GCHandle gCHandle3 = gcA(array2);
		GCHandle gCHandle4 = gcA(array3);
		GCHandle gCHandle5 = gcA(array4);
		if (FileBuffer != null)
		{
			GCHandle gCHandle6 = gcA(FileBuffer);
			zero = zipGetInfo(null, num, gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject(), gCHandle6.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle6.Free();
		}
		else
		{
			zero = zipGetInfo(zipArchive, num, gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), gCHandle5.AddrOfPinnedObject(), IntPtr.Zero, 0);
		}
		if (zero == IntPtr.Zero)
		{
			gCHandle3.Free();
			gCHandle4.Free();
			gCHandle5.Free();
			return 0uL;
		}
		StringReader stringReader = new StringReader(Marshal.PtrToStringAuto(zero));
		ulong num2 = 0uL;
		for (int i = 0; i < array[0]; i++)
		{
			string item;
			if ((item = stringReader.ReadLine()) != null)
			{
				ninfo.Add(item);
			}
			if (array2 != null)
			{
				uinfo.Add(array2[i]);
				num2 += array2[i];
				if (array2[i] != 0)
				{
					zipFiles++;
				}
				else
				{
					zipFolders++;
				}
			}
			if (array3 != null)
			{
				cinfo.Add(array3[i]);
				totalCompressedSize += array3[i];
			}
			if (array4 != null)
			{
				localOffset.Add(array4[i]);
			}
		}
		stringReader.Close();
		stringReader.Dispose();
		gCHandle3.Free();
		gCHandle4.Free();
		gCHandle5.Free();
		releaseBuffer(zero);
		array = null;
		array2 = null;
		array3 = null;
		array4 = null;
		totalUncompressedSize = num2;
		return num2;
	}

	public static ulong getEntrySize(string zipArchive, string entry, byte[] FileBuffer = null)
	{
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			ulong result = zipGetEntrySize(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
			return result;
		}
		return zipGetEntrySize(zipArchive, entry, IntPtr.Zero, 0);
	}

	public static bool entryExists(string zipArchive, string entry, byte[] FileBuffer = null)
	{
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			bool result = zipEntryExists(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
			return result;
		}
		return zipEntryExists(zipArchive, entry, IntPtr.Zero, 0);
	}

	public static bool buffer2File(int levelOfCompression, string zipArchive, string arc_filename, byte[] buffer, bool append = false, string comment = null, string password = null, bool useBz2 = false)
	{
		if (!append && File.Exists(zipArchive))
		{
			File.Delete(zipArchive);
		}
		GCHandle gCHandle = gcA(buffer);
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 9)
		{
			levelOfCompression = 9;
		}
		if (password == "")
		{
			password = null;
		}
		if (comment == "")
		{
			comment = null;
		}
		bool result = zipBuf2File(levelOfCompression, zipArchive, arc_filename, gCHandle.AddrOfPinnedObject(), buffer.Length, comment, password, useBz2);
		gCHandle.Free();
		return result;
	}

	public static int delete_entry(string zipArchive, string arc_filename)
	{
		string text = zipArchive + ".tmp";
		int num = zipDeleteFile(zipArchive, arc_filename, text);
		if (num > 0)
		{
			File.Delete(zipArchive);
			File.Move(text, zipArchive);
			return num;
		}
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		return num;
	}

	public static int replace_entry(string zipArchive, string arc_filename, string newFilePath, int level = 9, string comment = null, string password = null, bool useBz2 = false)
	{
		if (delete_entry(zipArchive, arc_filename) < 0)
		{
			return -3;
		}
		if (password == "")
		{
			password = null;
		}
		if (comment == "")
		{
			comment = null;
		}
		return zipCD(level, zipArchive, newFilePath, arc_filename, comment, password, useBz2, 0, IntPtr.Zero);
	}

	public static int replace_entry(string zipArchive, string arc_filename, byte[] newFileBuffer, int level = 9, string password = null, bool useBz2 = false)
	{
		if (delete_entry(zipArchive, arc_filename) < 0)
		{
			return -5;
		}
		if (buffer2File(level, zipArchive, arc_filename, newFileBuffer, append: true, null, password, useBz2))
		{
			return 1;
		}
		return -6;
	}

	public static int extract_entry(string zipArchive, string arc_filename, string outpath, byte[] FileBuffer = null, ulong[] proc = null, string password = null)
	{
		if (!Directory.Exists(Path.GetDirectoryName(outpath)))
		{
			return -1;
		}
		int num = -1;
		if (proc == null)
		{
			proc = new ulong[1];
		}
		GCHandle gCHandle = gcA(proc);
		if (FileBuffer != null)
		{
			GCHandle gCHandle2 = gcA(FileBuffer);
			num = ((proc == null) ? zipEntry(null, arc_filename, outpath, gCHandle2.AddrOfPinnedObject(), FileBuffer.Length, IntPtr.Zero, password) : zipEntry(null, arc_filename, outpath, gCHandle2.AddrOfPinnedObject(), FileBuffer.Length, gCHandle.AddrOfPinnedObject(), password));
			gCHandle2.Free();
			gCHandle.Free();
			return num;
		}
		num = ((proc == null) ? zipEntry(zipArchive, arc_filename, outpath, IntPtr.Zero, 0, IntPtr.Zero, password) : zipEntry(zipArchive, arc_filename, outpath, IntPtr.Zero, 0, gCHandle.AddrOfPinnedObject(), password));
		gCHandle.Free();
		return num;
	}

	public static int decompress_File(string zipArchive, string outPath = null, int[] progress = null, byte[] FileBuffer = null, ulong[] proc = null, string password = null)
	{
		if (outPath == null)
		{
			outPath = Path.GetDirectoryName(zipArchive);
		}
		if (outPath.Substring(outPath.Length - 1, 1) != "/")
		{
			outPath += "/";
		}
		GCHandle gCHandle = gcA(progress);
		if (proc == null)
		{
			proc = new ulong[1];
		}
		GCHandle gCHandle2 = gcA(proc);
		int result;
		if (FileBuffer != null)
		{
			GCHandle gCHandle3 = gcA(FileBuffer);
			result = ((proc == null) ? zipEX(null, outPath, gCHandle.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), FileBuffer.Length, IntPtr.Zero, password) : zipEX(null, outPath, gCHandle.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), FileBuffer.Length, gCHandle2.AddrOfPinnedObject(), password));
			gCHandle3.Free();
			gCHandle.Free();
			gCHandle2.Free();
			return result;
		}
		result = ((proc == null) ? zipEX(zipArchive, outPath, gCHandle.AddrOfPinnedObject(), IntPtr.Zero, 0, IntPtr.Zero, password) : zipEX(zipArchive, outPath, gCHandle.AddrOfPinnedObject(), IntPtr.Zero, 0, gCHandle2.AddrOfPinnedObject(), password));
		gCHandle.Free();
		gCHandle2.Free();
		return result;
	}

	public static int compress_File(int levelOfCompression, string zipArchive, string inFilePath, bool append = false, string fileName = "", string comment = null, string password = null, bool useBz2 = false, int diskSize = 0, ulong[] byteProgress = null)
	{
		if (!File.Exists(inFilePath))
		{
			return -10;
		}
		if (!append && File.Exists(zipArchive))
		{
			File.Delete(zipArchive);
		}
		if (fileName == null || fileName == "")
		{
			fileName = Path.GetFileName(inFilePath);
		}
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 9)
		{
			levelOfCompression = 9;
		}
		if (password == "")
		{
			password = null;
		}
		if (comment == "")
		{
			comment = null;
		}
		int num = 0;
		if (byteProgress == null)
		{
			num = zipCD(levelOfCompression, zipArchive, inFilePath, fileName, comment, password, useBz2, diskSize, IntPtr.Zero);
		}
		else
		{
			GCHandle gCHandle = gcA(byteProgress);
			num = zipCD(levelOfCompression, zipArchive, inFilePath, fileName, comment, password, useBz2, diskSize, gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
		}
		return num;
	}

	public static int compress_File_List(int levelOfCompression, string zipArchive, string[] inFilePath, int[] progress = null, bool append = false, string[] fileName = null, string password = null, bool useBz2 = false, int diskSize = 0, ulong[] byteProgress = null)
	{
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 9)
		{
			levelOfCompression = 9;
		}
		if (password == "")
		{
			password = null;
		}
		if (!append && File.Exists(zipArchive))
		{
			File.Delete(zipArchive);
		}
		if (inFilePath == null)
		{
			return -3;
		}
		if (fileName != null && fileName.Length != inFilePath.Length)
		{
			return -4;
		}
		for (int i = 0; i < inFilePath.Length; i++)
		{
			if (!File.Exists(inFilePath[i]))
			{
				return -10;
			}
		}
		IntPtr[] fp = new IntPtr[inFilePath.Length];
		IntPtr[] np = new IntPtr[inFilePath.Length];
		int num = 0;
		fillPointers(zipArchive, fileName, inFilePath, ref fp, ref np);
		if (byteProgress == null)
		{
			byteProgress = new ulong[1];
		}
		if (progress == null)
		{
			progress = new int[1];
		}
		GCHandle gCHandle = gcA(fp);
		GCHandle gCHandle2 = gcA(np);
		GCHandle gCHandle3 = gcA(progress);
		GCHandle gCHandle4 = gcA(byteProgress);
		num = zipCDList(levelOfCompression, zipArchive, gCHandle.AddrOfPinnedObject(), inFilePath.Length, gCHandle3.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), password, useBz2, diskSize, gCHandle4.AddrOfPinnedObject());
		for (int j = 0; j < inFilePath.Length; j++)
		{
			Marshal.FreeCoTaskMem(fp[j]);
			Marshal.FreeCoTaskMem(np[j]);
		}
		gCHandle.Free();
		fp = null;
		gCHandle2.Free();
		np = null;
		gCHandle3.Free();
		gCHandle4.Free();
		return num;
	}

	public static int compressDir(string sourceDir, int levelOfCompression, string zipArchive = null, bool includeRoot = false, int[] progress = null, string password = null, bool useBz2 = false, int diskSize = 0, bool append = false, ulong[] byteProgress = null)
	{
		if (!Directory.Exists(sourceDir))
		{
			return 0;
		}
		string text = sourceDir.Replace("\\", "/");
		if (sourceDir.Substring(sourceDir.Length - 1) != "/")
		{
			text += "/";
		}
		if (zipArchive == null)
		{
			zipArchive = sourceDir.Substring(0, sourceDir.Length - 1) + ".zip";
		}
		if (getAllFiles(text) == 0)
		{
			return 0;
		}
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 9)
		{
			levelOfCompression = 9;
		}
		int result = 0;
		if (Directory.Exists(text))
		{
			List<string> inFilePath = new List<string>();
			List<string> fileName = new List<string>();
			fillLists(text, includeRoot, ref inFilePath, ref fileName);
			result = compress_File_List(levelOfCompression, zipArchive, inFilePath.ToArray(), progress, append, fileName.ToArray(), password, useBz2, diskSize, byteProgress);
			inFilePath.Clear();
			inFilePath = null;
			fileName.Clear();
			fileName = null;
		}
		return result;
	}

	private static void fillPointers(string outFile, string[] fileName, string[] inFilePath, ref IntPtr[] fp, ref IntPtr[] np)
	{
		string[] array = null;
		string directoryName = Path.GetDirectoryName(outFile);
		if (fileName == null)
		{
			array = new string[inFilePath.Length];
			for (int i = 0; i < inFilePath.Length; i++)
			{
				array[i] = inFilePath[i].Replace(directoryName, "");
			}
		}
		else
		{
			array = fileName;
		}
		for (int j = 0; j < inFilePath.Length; j++)
		{
			if (array[j] == null)
			{
				array[j] = inFilePath[j].Replace(directoryName, "");
			}
		}
		for (int k = 0; k < inFilePath.Length; k++)
		{
			inFilePath[k] = inFilePath[k].Replace("\\", "/");
			array[k] = array[k].Replace("\\", "/");
			fp[k] = Marshal.StringToCoTaskMemAuto(inFilePath[k]);
			np[k] = Marshal.StringToCoTaskMemAuto(array[k]);
		}
		directoryName = null;
	}

	private static void fillLists(string fdir, bool includeRoot, ref List<string> inFilePath, ref List<string> fileName)
	{
		string[] array = fdir.Split('/');
		string text = array[^1];
		string text2 = text;
		if (array.Length > 1 && includeRoot)
		{
			text2 = (text = array[^2] + "/");
		}
		string[] files = Directory.GetFiles(fdir, "*", SearchOption.AllDirectories);
		foreach (string text3 in files)
		{
			string text4 = text3.Replace(fdir, text).Replace("\\", "/").Replace("//", "/");
			if (!includeRoot)
			{
				text4 = text4.Substring(text2.Length);
				if (text4.Substring(0, 1) == "/")
				{
					text4 = text4.Substring(1, text4.Length - 1);
				}
			}
			inFilePath.Add(text3);
			fileName.Add(text4);
		}
	}

	public static int getAllFiles(string dir)
	{
		return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length;
	}

	public static long getFileSize(string file)
	{
		FileInfo fileInfo = new FileInfo(file);
		if (fileInfo.Exists)
		{
			return fileInfo.Length;
		}
		return 0L;
	}

	public static ulong getDirSize(string dir)
	{
		string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
		ulong num = 0uL;
		for (int i = 0; i < files.Length; i++)
		{
			FileInfo fileInfo = new FileInfo(files[i]);
			if (fileInfo.Exists)
			{
				num += (ulong)fileInfo.Length;
			}
		}
		return num;
	}

	public static int tarExtract(string inFile, string outPath = null, int[] progress = null, ulong[] byteProgress = null)
	{
		if (outPath == null)
		{
			outPath = Path.GetDirectoryName(inFile);
		}
		if (outPath.Substring(outPath.Length - 1, 1) != "/")
		{
			outPath += "/";
		}
		GCHandle gCHandle = gcA(progress);
		GCHandle gCHandle2 = gcA(byteProgress);
		int result = extractTar(inFile, outPath, null, gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), fullPaths: true);
		gCHandle.Free();
		gCHandle2.Free();
		return result;
	}

	public static int tarExtractEntry(string inFile, string entry, string outPath = null, bool fullPaths = true, ulong[] byteProgress = null)
	{
		if (outPath == null)
		{
			outPath = Path.GetDirectoryName(inFile);
		}
		if (fullPaths && outPath.Substring(outPath.Length - 1, 1) != "/")
		{
			outPath += "/";
		}
		if (fullPaths && File.Exists(outPath))
		{
			Debug.Log("There is a file with the same name in the path!");
			return -7;
		}
		if (!fullPaths && Directory.Exists(outPath))
		{
			Debug.Log("There is a directory with the same name in the path!");
			return -8;
		}
		GCHandle gCHandle = gcA(byteProgress);
		int result = extractTar(inFile, outPath, entry, IntPtr.Zero, gCHandle.AddrOfPinnedObject(), fullPaths);
		gCHandle.Free();
		return result;
	}

	public static int tarDir(string sourceDir, string outFile = null, bool includeRoot = false, int[] progress = null, ulong[] byteProgress = null)
	{
		if (!Directory.Exists(sourceDir))
		{
			return 0;
		}
		string text = sourceDir.Replace("\\", "/");
		if (sourceDir.Substring(sourceDir.Length - 1) != "/")
		{
			text += "/";
		}
		if (outFile == null)
		{
			outFile = sourceDir.Substring(0, sourceDir.Length - 1) + ".tar";
		}
		if (getAllFiles(text) == 0)
		{
			return 0;
		}
		int result = 0;
		if (Directory.Exists(text))
		{
			List<string> inFilePath = new List<string>();
			List<string> fileName = new List<string>();
			fillLists(text, includeRoot, ref inFilePath, ref fileName);
			result = tarList(outFile, inFilePath.ToArray(), fileName.ToArray(), progress, byteProgress);
			inFilePath.Clear();
			inFilePath = null;
			fileName.Clear();
			fileName = null;
		}
		return result;
	}

	public static int tarList(string outFile, string[] inFilePath, string[] fileName = null, int[] progress = null, ulong[] byteProgress = null)
	{
		if (inFilePath == null)
		{
			return -3;
		}
		if (fileName != null && fileName.Length != inFilePath.Length)
		{
			return -4;
		}
		for (int i = 0; i < inFilePath.Length; i++)
		{
			if (!File.Exists(inFilePath[i]))
			{
				return -10;
			}
		}
		if (File.Exists(outFile))
		{
			File.Delete(outFile);
		}
		int num = 0;
		IntPtr[] fp = new IntPtr[inFilePath.Length];
		IntPtr[] np = new IntPtr[inFilePath.Length];
		fillPointers(outFile, fileName, inFilePath, ref fp, ref np);
		GCHandle gCHandle = gcA(fp);
		GCHandle gCHandle2 = gcA(np);
		GCHandle gCHandle3 = gcA(progress);
		GCHandle gCHandle4 = gcA(byteProgress);
		num = createTar(outFile, gCHandle.AddrOfPinnedObject(), gCHandle2.AddrOfPinnedObject(), inFilePath.Length, gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject());
		for (int j = 0; j < inFilePath.Length; j++)
		{
			Marshal.FreeCoTaskMem(fp[j]);
			Marshal.FreeCoTaskMem(np[j]);
		}
		gCHandle.Free();
		fp = null;
		gCHandle2.Free();
		np = null;
		gCHandle3.Free();
		gCHandle4.Free();
		return num;
	}

	public static ulong getTarInfo(string tarArchive)
	{
		ninfo.Clear();
		uinfo.Clear();
		cinfo.Clear();
		localOffset.Clear();
		zipFiles = 0;
		zipFolders = 0;
		totalCompressedSize = 0uL;
		totalUncompressedSize = 0uL;
		int num = 0;
		int[] array = new int[1];
		GCHandle gCHandle = gcA(array);
		num = readTarA(tarArchive, gCHandle.AddrOfPinnedObject());
		gCHandle.Free();
		if (num <= 0)
		{
			return 0uL;
		}
		IntPtr zero = IntPtr.Zero;
		ulong[] array2 = new ulong[array[0]];
		GCHandle gCHandle2 = gcA(array2);
		zero = readTar(tarArchive, num, gCHandle2.AddrOfPinnedObject());
		if (zero == IntPtr.Zero)
		{
			gCHandle2.Free();
			return 0uL;
		}
		StringReader stringReader = new StringReader(Marshal.PtrToStringAuto(zero));
		ulong num2 = 0uL;
		for (int i = 0; i < array[0]; i++)
		{
			string item;
			if ((item = stringReader.ReadLine()) != null)
			{
				ninfo.Add(item);
			}
			if (array2 != null)
			{
				uinfo.Add(array2[i]);
				num2 += array2[i];
				if (array2[i] != 0)
				{
					zipFiles++;
				}
				else
				{
					zipFolders++;
				}
			}
		}
		stringReader.Close();
		stringReader.Dispose();
		gCHandle2.Free();
		releaseBuffer(zero);
		array = null;
		array2 = null;
		totalUncompressedSize = num2;
		return num2;
	}

	public static DateTime entryDateTime(string zipArchive, string entry, byte[] FileBuffer = null)
	{
		uint num = 0u;
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			num = getEntryDateTime(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
		}
		else
		{
			num = getEntryDateTime(zipArchive, entry, IntPtr.Zero, 0);
		}
		uint num2 = (num & 0xFFFF0000u) >> 16;
		uint num3 = num & 0xFFFF;
		uint year = (num2 >> 9) + 1980;
		uint month = (num2 & 0x1E0) >> 5;
		uint day = num2 & 0x1F;
		uint hour = num3 >> 11;
		uint minute = (num3 & 0x7E0) >> 5;
		uint second = (num3 & 0x1F) * 2;
		if (num == 0 || num == 1 || num == 2)
		{
			Debug.Log("Error in getting DateTime: " + num);
			return DateTime.Now;
		}
		return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second);
	}

	public static void free_inmemory(inMemory t)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return;
		}
		if (freeMemStruct(t.pointer) != 1)
		{
			Debug.Log("In memory pointer was not freed");
		}
		t.info = null;
		if (t.memStruct != IntPtr.Zero && freeMemZ(t.memStruct) != 1)
		{
			Debug.Log("MemStruct was not freed");
		}
		if (t.fileStruct != IntPtr.Zero && freeFileZ(t.fileStruct) != 1)
		{
			Debug.Log("FileStruct was not freed");
		}
		t = null;
	}

	public static bool inMemoryZipStart(inMemory t)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return false;
		}
		if (t.fileStruct == IntPtr.Zero)
		{
			t.fileStruct = initFileStruct();
		}
		if (t.memStruct == IntPtr.Zero)
		{
			t.memStruct = initMemStruct();
		}
		if (!t.isClosed)
		{
			inMemoryZipClose(t);
		}
		GCHandle gCHandle = gcA(t.info);
		t.zf = zipCDMemStart(gCHandle.AddrOfPinnedObject(), t.pointer, t.fileStruct, t.memStruct);
		gCHandle.Free();
		t.isClosed = false;
		if (t.zf != IntPtr.Zero)
		{
			return true;
		}
		return false;
	}

	public static int inMemoryZipAdd(inMemory t, int levelOfCompression, byte[] buffer, string fileName, string comment = null, string password = null, bool useBz2 = false)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return -1;
		}
		if (t.isClosed)
		{
			Debug.Log("Can't add entry. inMemory zip is closed.");
			return -2;
		}
		if (password == "")
		{
			password = null;
		}
		if (comment == "")
		{
			comment = null;
		}
		if (fileName == null)
		{
			fileName = "";
		}
		GCHandle gCHandle = gcA(buffer);
		int num = zipCDMemAdd(t.zf, levelOfCompression, gCHandle.AddrOfPinnedObject(), buffer.Length, fileName, comment, password, useBz2);
		gCHandle.Free();
		t.lastResult = num;
		return num;
	}

	public static IntPtr inMemoryZipClose(inMemory t)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return IntPtr.Zero;
		}
		if (t.isClosed)
		{
			Debug.Log("Can't close zip. inMemory zip is closed.");
			return t.pointer;
		}
		GCHandle gCHandle = gcA(t.info);
		t.pointer = zipCDMemClose(t.zf, t.memStruct, gCHandle.AddrOfPinnedObject(), t.lastResult);
		gCHandle.Free();
		t.isClosed = true;
		return t.pointer;
	}

	public static IntPtr compress_Buf2Mem(inMemory t, int levelOfCompression, byte[] buffer, string fileName, string comment = null, string password = null, bool useBz2 = false)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return IntPtr.Zero;
		}
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 9)
		{
			levelOfCompression = 9;
		}
		if (password == "")
		{
			password = null;
		}
		if (comment == "")
		{
			comment = null;
		}
		if (fileName == null)
		{
			fileName = "";
		}
		if (buffer == null || buffer.Length == 0)
		{
			Debug.Log("Buffer was null or zero size !");
			return t.pointer;
		}
		GCHandle gCHandle = gcA(buffer);
		GCHandle gCHandle2 = gcA(t.info);
		t.pointer = zipCDMem(gCHandle2.AddrOfPinnedObject(), t.pointer, levelOfCompression, gCHandle.AddrOfPinnedObject(), buffer.Length, fileName, comment, password, useBz2);
		gCHandle.Free();
		gCHandle2.Free();
		return t.pointer;
	}

	public static int decompress_Mem2File(inMemory t, string outPath, int[] progress = null, ulong[] proc = null, string password = null)
	{
		if (t.info == null)
		{
			Debug.Log("inMemory object is null");
			return -1;
		}
		if (outPath.Substring(outPath.Length - 1, 1) != "/")
		{
			outPath += "/";
		}
		int num = 0;
		GCHandle gCHandle = gcA(progress);
		if (progress == null)
		{
			progress = new int[1];
		}
		if (proc == null)
		{
			proc = new ulong[1];
		}
		GCHandle gCHandle2 = gcA(proc);
		if (t != null)
		{
			num = ((proc == null) ? zipEX(null, outPath, gCHandle.AddrOfPinnedObject(), t.pointer, t.info[0], IntPtr.Zero, password) : zipEX(null, outPath, gCHandle.AddrOfPinnedObject(), t.pointer, t.info[0], gCHandle2.AddrOfPinnedObject(), password));
			gCHandle.Free();
			gCHandle2.Free();
			return num;
		}
		return 0;
	}

	public static int entry2BufferMem(inMemory t, string entry, ref byte[] buffer, string password = null)
	{
		if (t.info == null)
		{
			return -2;
		}
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (t != null)
		{
			num = (int)zipGetEntrySize(null, entry, t.pointer, t.info[0]);
		}
		if (num <= 0)
		{
			return -18;
		}
		if (buffer == null)
		{
			buffer = new byte[0];
		}
		Array.Resize(ref buffer, num);
		GCHandle gCHandle = gcA(buffer);
		int result = 0;
		if (t != null)
		{
			result = zipEntry2Buffer(null, entry, gCHandle.AddrOfPinnedObject(), num, t.pointer, t.info[0], password);
		}
		gCHandle.Free();
		return result;
	}

	public static byte[] entry2BufferMem(inMemory t, string entry, string password = null)
	{
		if (t.info == null)
		{
			return null;
		}
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (t != null)
		{
			num = (int)zipGetEntrySize(null, entry, t.pointer, t.info[0]);
		}
		if (num <= 0)
		{
			return null;
		}
		byte[] array = new byte[num];
		GCHandle gCHandle = gcA(array);
		int num2 = 0;
		if (t != null)
		{
			num2 = zipEntry2Buffer(null, entry, gCHandle.AddrOfPinnedObject(), num, t.pointer, t.info[0], password);
		}
		gCHandle.Free();
		if (num2 != 1)
		{
			return null;
		}
		return array;
	}

	public static int entry2FixedBufferMem(inMemory t, string entry, ref byte[] fixedBuffer, string password = null)
	{
		if (t.info == null)
		{
			return -2;
		}
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (t != null)
		{
			num = (int)zipGetEntrySize(null, entry, t.pointer, t.info[0]);
		}
		if (num <= 0)
		{
			return -18;
		}
		if (fixedBuffer.Length < num)
		{
			return -19;
		}
		GCHandle gCHandle = gcA(fixedBuffer);
		int num2 = 0;
		if (t != null)
		{
			num2 = zipEntry2Buffer(null, entry, gCHandle.AddrOfPinnedObject(), num, t.pointer, t.info[0], password);
		}
		gCHandle.Free();
		if (num2 != 1)
		{
			return num2;
		}
		return num;
	}

	public static ulong getFileInfoMem(inMemory t)
	{
		if (t.info == null)
		{
			return 0uL;
		}
		ninfo.Clear();
		uinfo.Clear();
		cinfo.Clear();
		localOffset.Clear();
		zipFiles = 0;
		zipFolders = 0;
		totalCompressedSize = 0uL;
		totalUncompressedSize = 0uL;
		int num = 0;
		int[] array = new int[1];
		GCHandle gCHandle = gcA(array);
		if (t != null)
		{
			num = zipGetInfoA(null, gCHandle.AddrOfPinnedObject(), t.pointer, t.info[0]);
		}
		gCHandle.Free();
		if (num <= 0)
		{
			return 0uL;
		}
		IntPtr intPtr = IntPtr.Zero;
		ulong[] array2 = new ulong[array[0]];
		ulong[] array3 = new ulong[array[0]];
		ulong[] array4 = new ulong[array[0]];
		GCHandle gCHandle2 = gcA(array2);
		GCHandle gCHandle3 = gcA(array3);
		GCHandle gCHandle4 = gcA(array4);
		if (t != null)
		{
			intPtr = zipGetInfo(null, num, gCHandle2.AddrOfPinnedObject(), gCHandle3.AddrOfPinnedObject(), gCHandle4.AddrOfPinnedObject(), t.pointer, t.info[0]);
		}
		if (intPtr == IntPtr.Zero)
		{
			gCHandle2.Free();
			gCHandle3.Free();
			gCHandle4.Free();
			return 0uL;
		}
		StringReader stringReader = new StringReader(Marshal.PtrToStringAuto(intPtr));
		ulong num2 = 0uL;
		for (int i = 0; i < array[0]; i++)
		{
			string item;
			if ((item = stringReader.ReadLine()) != null)
			{
				ninfo.Add(item);
			}
			if (array2 != null)
			{
				uinfo.Add(array2[i]);
				num2 += array2[i];
				if (array2[i] != 0)
				{
					zipFiles++;
				}
				else
				{
					zipFolders++;
				}
			}
			if (array3 != null)
			{
				cinfo.Add(array3[i]);
				totalCompressedSize += array3[i];
			}
			if (array4 != null)
			{
				localOffset.Add(array4[i]);
			}
		}
		stringReader.Close();
		stringReader.Dispose();
		gCHandle2.Free();
		gCHandle3.Free();
		gCHandle4.Free();
		releaseBuffer(intPtr);
		array = null;
		array2 = null;
		array3 = null;
		array4 = null;
		totalUncompressedSize = num2;
		return num2;
	}

	public static int entry2Buffer(string zipArchive, string entry, ref byte[] buffer, byte[] FileBuffer = null, string password = null)
	{
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			num = (int)zipGetEntrySize(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
		}
		else
		{
			num = (int)zipGetEntrySize(zipArchive, entry, IntPtr.Zero, 0);
		}
		if (num <= 0)
		{
			return -18;
		}
		Array.Resize(ref buffer, num);
		GCHandle gCHandle2 = gcA(buffer);
		int num2 = 0;
		if (FileBuffer != null)
		{
			GCHandle gCHandle3 = gcA(FileBuffer);
			num2 = zipEntry2Buffer(null, entry, gCHandle2.AddrOfPinnedObject(), num, gCHandle3.AddrOfPinnedObject(), FileBuffer.Length, password);
			gCHandle3.Free();
		}
		else
		{
			num2 = zipEntry2Buffer(zipArchive, entry, gCHandle2.AddrOfPinnedObject(), num, IntPtr.Zero, 0, password);
		}
		gCHandle2.Free();
		return num2;
	}

	public static int entry2FixedBuffer(string zipArchive, string entry, ref byte[] fixedBuffer, byte[] FileBuffer = null, string password = null)
	{
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			num = (int)zipGetEntrySize(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
		}
		else
		{
			num = (int)zipGetEntrySize(zipArchive, entry, IntPtr.Zero, 0);
		}
		if (num <= 0)
		{
			return -18;
		}
		if (fixedBuffer.Length < num)
		{
			return -19;
		}
		GCHandle gCHandle2 = gcA(fixedBuffer);
		int num2 = 0;
		if (FileBuffer != null)
		{
			GCHandle gCHandle3 = gcA(FileBuffer);
			num2 = zipEntry2Buffer(null, entry, gCHandle2.AddrOfPinnedObject(), num, gCHandle3.AddrOfPinnedObject(), FileBuffer.Length, password);
			gCHandle3.Free();
		}
		else
		{
			num2 = zipEntry2Buffer(zipArchive, entry, gCHandle2.AddrOfPinnedObject(), num, IntPtr.Zero, 0, password);
		}
		gCHandle2.Free();
		if (num2 != 1)
		{
			return num2;
		}
		return num;
	}

	public static byte[] entry2Buffer(string zipArchive, string entry, byte[] FileBuffer = null, string password = null)
	{
		int num = 0;
		if (password == "")
		{
			password = null;
		}
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			num = (int)zipGetEntrySize(null, entry, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
		}
		else
		{
			num = (int)zipGetEntrySize(zipArchive, entry, IntPtr.Zero, 0);
		}
		if (num <= 0)
		{
			return null;
		}
		byte[] array = new byte[num];
		GCHandle gCHandle2 = gcA(array);
		int num2 = 0;
		if (FileBuffer != null)
		{
			GCHandle gCHandle3 = gcA(FileBuffer);
			num2 = zipEntry2Buffer(null, entry, gCHandle2.AddrOfPinnedObject(), num, gCHandle3.AddrOfPinnedObject(), FileBuffer.Length, password);
			gCHandle3.Free();
		}
		else
		{
			num2 = zipEntry2Buffer(zipArchive, entry, gCHandle2.AddrOfPinnedObject(), num, IntPtr.Zero, 0, password);
		}
		gCHandle2.Free();
		if (num2 != 1)
		{
			return null;
		}
		return array;
	}

	public static bool validateFile(string zipArchive, byte[] FileBuffer = null)
	{
		if (FileBuffer != null)
		{
			GCHandle gCHandle = gcA(FileBuffer);
			bool result = zipValidateFile(null, gCHandle.AddrOfPinnedObject(), FileBuffer.Length);
			gCHandle.Free();
			return result;
		}
		return zipValidateFile(zipArchive, IntPtr.Zero, 0);
	}

	public static bool getZipInfo(string fileName)
	{
		if (!File.Exists(fileName))
		{
			Debug.Log("File not found: " + fileName);
			return false;
		}
		int pos = 0;
		int size = 0;
		using (FileStream input = File.OpenRead(fileName))
		{
			using BinaryReader reader = new BinaryReader(input);
			if (findPK(reader))
			{
				int num = findEnd(reader, ref pos, ref size);
				if (num > 0)
				{
					getCentralDir(reader, num);
					return true;
				}
				Debug.Log("No Entries in zip");
				return false;
			}
		}
		return false;
	}

	public static bool getZipInfoMerged(string fileName, ref int pos, ref int size, bool getCentralDirectory = false)
	{
		if (!File.Exists(fileName))
		{
			Debug.Log("File not found: " + fileName);
			return false;
		}
		using (FileStream input = File.OpenRead(fileName))
		{
			using BinaryReader reader = new BinaryReader(input);
			if (findPK(reader))
			{
				int num = findEnd(reader, ref pos, ref size);
				if (num > 0)
				{
					if (getCentralDirectory)
					{
						getCentralDir(reader, num);
					}
					return true;
				}
				Debug.Log("No Entries in zip");
				return false;
			}
		}
		return false;
	}

	public static bool getZipInfoMerged(byte[] buffer, ref int pos, ref int size, bool getCentralDirectory = false)
	{
		if (buffer == null)
		{
			Debug.Log("Buffer is null");
			return false;
		}
		using (MemoryStream input = new MemoryStream(buffer))
		{
			using BinaryReader reader = new BinaryReader(input);
			if (findPK(reader))
			{
				int num = findEnd(reader, ref pos, ref size);
				if (num > 0)
				{
					if (getCentralDirectory)
					{
						getCentralDir(reader, num);
					}
					return true;
				}
				Debug.Log("No Entries in zip");
				return false;
			}
		}
		return false;
	}

	public static bool getZipInfoMerged(byte[] buffer)
	{
		if (buffer == null)
		{
			Debug.Log("Buffer is null");
			return false;
		}
		int pos = 0;
		int size = 0;
		using (MemoryStream input = new MemoryStream(buffer))
		{
			using BinaryReader reader = new BinaryReader(input);
			if (findPK(reader))
			{
				int num = findEnd(reader, ref pos, ref size);
				if (num > 0)
				{
					getCentralDir(reader, num);
					return true;
				}
				Debug.Log("No Entries in zip");
				return false;
			}
		}
		return false;
	}

	private static bool findPK(BinaryReader reader)
	{
		byte b = reader.ReadByte();
		bool result = false;
		int num = 0;
		while (reader.BaseStream.Position < reader.BaseStream.Length - 3)
		{
			num++;
			if (b == 80)
			{
				if (reader.ReadByte() == 75 && reader.ReadByte() == 5 && reader.ReadByte() == 6)
				{
					reader.BaseStream.Seek(reader.BaseStream.Position - 4, SeekOrigin.Begin);
					result = true;
					break;
				}
				reader.BaseStream.Seek(num, SeekOrigin.Begin);
			}
			b = reader.ReadByte();
		}
		return result;
	}

	private static int findEnd(BinaryReader reader, ref int pos, ref int size)
	{
		_ = reader.BaseStream.Position;
		int num = 0;
		while (num == 0 && reader.BaseStream.Position < reader.BaseStream.Length)
		{
			byte b = reader.ReadByte();
			while (b != 80 && reader.BaseStream.Position < reader.BaseStream.Length)
			{
				b = reader.ReadByte();
			}
			if (reader.BaseStream.Position >= reader.BaseStream.Length)
			{
				break;
			}
			if (reader.ReadByte() == 75 && reader.ReadByte() == 5 && reader.ReadByte() == 6)
			{
				reader.ReadInt16();
				reader.ReadInt16();
				reader.ReadInt16();
				num = reader.ReadInt16();
				int num2 = reader.ReadInt32();
				int num3 = reader.ReadInt32();
				int count = reader.ReadInt16();
				reader.ReadBytes(count);
				pos = (int)reader.BaseStream.Position - (num3 + num2 + 22);
				size = (int)reader.BaseStream.Position - pos;
				reader.BaseStream.Seek(pos + num3, SeekOrigin.Begin);
				break;
			}
		}
		return num;
	}

	private static void getCentralDir(BinaryReader reader, int count)
	{
		if (zinfo != null && zinfo.Count > 0)
		{
			zinfo.Clear();
		}
		zinfo = new List<zipInfo>();
		for (int i = 0; i < count; i++)
		{
			if (reader.ReadInt32() == 33639248)
			{
				zipInfo item = default(zipInfo);
				item.VersionMadeBy = reader.ReadInt16();
				item.MinimumVersionToExtract = reader.ReadInt16();
				item.BitFlag = reader.ReadInt16();
				item.CompressionMethod = reader.ReadInt16();
				item.FileLastModificationTime = reader.ReadInt16();
				item.FileLastModificationDate = reader.ReadInt16();
				item.CRC = reader.ReadInt32();
				item.CompressedSize = reader.ReadInt32();
				item.UncompressedSize = reader.ReadInt32();
				short count2 = reader.ReadInt16();
				short count3 = reader.ReadInt16();
				short count4 = reader.ReadInt16();
				item.DiskNumberWhereFileStarts = reader.ReadInt16();
				item.InternalFileAttributes = reader.ReadInt16();
				item.ExternalFileAttributes = reader.ReadInt32();
				item.RelativeOffsetOfLocalFileHeader = reader.ReadInt32();
				item.filename = Encoding.UTF8.GetString(reader.ReadBytes(count2));
				item.AbsoluteOffsetOfLocalFileHeaderStore = item.RelativeOffsetOfLocalFileHeader + 30 + item.filename.Length;
				byte[] bytes = reader.ReadBytes(count3);
				item.extraField = Encoding.ASCII.GetString(bytes);
				item.fileComment = Encoding.UTF8.GetString(reader.ReadBytes(count4));
				zinfo.Add(item);
			}
		}
	}

	public static byte[] getMergedZip(string filePath, ref int position, ref int siz)
	{
		int pos = 0;
		int size = 0;
		if (!File.Exists(filePath))
		{
			return null;
		}
		getZipInfoMerged(filePath, ref pos, ref size);
		position = pos;
		siz = size;
		if (size == 0)
		{
			return null;
		}
		byte[] array = new byte[size];
		using FileStream input = File.OpenRead(filePath);
		using BinaryReader binaryReader = new BinaryReader(input);
		binaryReader.BaseStream.Seek(pos, SeekOrigin.Begin);
		binaryReader.Read(array, 0, size);
		return array;
	}

	public static byte[] getMergedZip(string filePath)
	{
		int pos = 0;
		int size = 0;
		if (!File.Exists(filePath))
		{
			return null;
		}
		getZipInfoMerged(filePath, ref pos, ref size);
		if (size == 0)
		{
			return null;
		}
		byte[] array = new byte[size];
		using FileStream input = File.OpenRead(filePath);
		using BinaryReader binaryReader = new BinaryReader(input);
		binaryReader.BaseStream.Seek(pos, SeekOrigin.Begin);
		binaryReader.Read(array, 0, size);
		return array;
	}

	public static byte[] getMergedZip(byte[] buffer, ref int position, ref int siz)
	{
		int pos = 0;
		int size = 0;
		if (buffer == null)
		{
			return null;
		}
		getZipInfoMerged(buffer, ref pos, ref size);
		position = pos;
		siz = size;
		if (size == 0)
		{
			return null;
		}
		byte[] array = new byte[size];
		using MemoryStream input = new MemoryStream(buffer);
		using BinaryReader binaryReader = new BinaryReader(input);
		binaryReader.BaseStream.Seek(pos, SeekOrigin.Begin);
		binaryReader.Read(array, 0, size);
		return array;
	}

	public static byte[] getMergedZip(byte[] buffer)
	{
		int pos = 0;
		int size = 0;
		if (buffer == null)
		{
			return null;
		}
		getZipInfoMerged(buffer, ref pos, ref size);
		if (size == 0)
		{
			return null;
		}
		byte[] array = new byte[size];
		using MemoryStream input = new MemoryStream(buffer);
		using BinaryReader binaryReader = new BinaryReader(input);
		binaryReader.BaseStream.Seek(pos, SeekOrigin.Begin);
		binaryReader.Read(array, 0, size);
		return array;
	}

	public static int decompressZipMerged(string file, string outPath, int[] progress = null, ulong[] proc = null, string password = null)
	{
		if (!File.Exists(file))
		{
			return 0;
		}
		outPath = outPath.Replace("//", "/");
		if (!Directory.Exists(outPath))
		{
			Directory.CreateDirectory(outPath);
		}
		int position = 0;
		int siz = 0;
		int result = 0;
		byte[] mergedZip = getMergedZip(file, ref position, ref siz);
		if (mergedZip != null)
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(mergedZip);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64());
			inMemory.info[0] = siz;
			result = decompress_Mem2File(inMemory, outPath, progress, proc, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			mergedZip = null;
		}
		return result;
	}

	public static int decompressZipMerged(byte[] buffer, string outPath, int[] progress = null, ulong[] proc = null, string password = null)
	{
		if (buffer == null)
		{
			return 0;
		}
		outPath = outPath.Replace("//", "/");
		if (!Directory.Exists(outPath))
		{
			Directory.CreateDirectory(outPath);
		}
		int pos = 0;
		int size = 0;
		int result = 0;
		if (getZipInfoMerged(buffer, ref pos, ref size))
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(buffer);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + pos);
			inMemory.info[0] = size;
			result = decompress_Mem2File(inMemory, outPath, progress, proc, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
		}
		return result;
	}

	private static void writeFile(byte[] tb, string entry, string outPath, string overrideEntryName, ref int res)
	{
		if (tb != null)
		{
			string text = ((overrideEntryName != null) ? overrideEntryName : ((!entry.Contains("/")) ? entry : entry.Split('/')[^1]));
			File.WriteAllBytes(outPath + "/" + text, tb);
			res = 1;
		}
		else
		{
			Debug.Log("Could not extract entry.");
		}
	}

	public static int entry2FileMerged(string file, string entry, string outPath, string overrideEntryName = null, string password = null)
	{
		if (!File.Exists(file))
		{
			return -10;
		}
		outPath = outPath.Replace("//", "/");
		int position = 0;
		int siz = 0;
		int res = 0;
		byte[] mergedZip = getMergedZip(file, ref position, ref siz);
		if (mergedZip != null)
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(mergedZip);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64());
			inMemory.info[0] = siz;
			byte[] tb = entry2BufferMem(inMemory, entry, password);
			gCHandle.Free();
			writeFile(tb, entry, outPath, overrideEntryName, ref res);
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			mergedZip = null;
		}
		return res;
	}

	public static int entry2FileMerged(byte[] buffer, string entry, string outPath, string overrideEntryName = null, string password = null)
	{
		if (buffer == null)
		{
			return -10;
		}
		outPath = outPath.Replace("//", "/");
		int pos = 0;
		int size = 0;
		int res = 0;
		if (getZipInfoMerged(buffer, ref pos, ref size))
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(buffer);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + pos);
			inMemory.info[0] = size;
			byte[] tb = entry2BufferMem(inMemory, entry, password);
			gCHandle.Free();
			writeFile(tb, entry, outPath, overrideEntryName, ref res);
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
		}
		return res;
	}

	public static byte[] entry2BufferMerged(string file, string entry, string password = null)
	{
		if (!File.Exists(file))
		{
			return null;
		}
		int position = 0;
		int siz = 0;
		byte[] mergedZip = getMergedZip(file, ref position, ref siz);
		if (mergedZip != null)
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(mergedZip);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64());
			inMemory.info[0] = siz;
			byte[] result = entry2BufferMem(inMemory, entry, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			mergedZip = null;
			return result;
		}
		return null;
	}

	public static int entry2BufferMerged(string file, string entry, ref byte[] refBuffer, string password = null)
	{
		if (!File.Exists(file))
		{
			return 0;
		}
		int position = 0;
		int siz = 0;
		byte[] mergedZip = getMergedZip(file, ref position, ref siz);
		if (mergedZip != null)
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(mergedZip);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64());
			inMemory.info[0] = siz;
			int result = entry2BufferMem(inMemory, entry, ref refBuffer, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			mergedZip = null;
			return result;
		}
		return 0;
	}

	public static int entry2FixedBufferMerged(string file, string entry, ref byte[] fixedBuffer, string password = null)
	{
		if (!File.Exists(file))
		{
			return 0;
		}
		int position = 0;
		int siz = 0;
		byte[] mergedZip = getMergedZip(file, ref position, ref siz);
		if (mergedZip != null)
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(mergedZip);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64());
			inMemory.info[0] = siz;
			int result = entry2FixedBufferMem(inMemory, entry, ref fixedBuffer, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			mergedZip = null;
			return result;
		}
		return 0;
	}

	public static byte[] entry2BufferMerged(byte[] buffer, string entry, string password = null)
	{
		if (buffer == null)
		{
			return null;
		}
		int pos = 0;
		int size = 0;
		if (getZipInfoMerged(buffer, ref pos, ref size))
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(buffer);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + pos);
			inMemory.info[0] = size;
			byte[] result = entry2BufferMem(inMemory, entry, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			return result;
		}
		return null;
	}

	public static int entry2BufferMerged(byte[] buffer, string entry, ref byte[] refBuffer, string password = null)
	{
		if (buffer == null)
		{
			return 0;
		}
		int pos = 0;
		int size = 0;
		if (getZipInfoMerged(buffer, ref pos, ref size))
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(buffer);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + pos);
			inMemory.info[0] = size;
			int result = entry2BufferMem(inMemory, entry, ref refBuffer, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			return result;
		}
		return 0;
	}

	public static int entry2FixedBufferMerged(byte[] buffer, string entry, ref byte[] fixedBuffer, string password = null)
	{
		if (buffer == null)
		{
			return 0;
		}
		int pos = 0;
		int size = 0;
		if (getZipInfoMerged(buffer, ref pos, ref size))
		{
			inMemory inMemory = new inMemory();
			GCHandle gCHandle = gcA(buffer);
			inMemory.pointer = new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + pos);
			inMemory.info[0] = size;
			int result = entry2FixedBufferMem(inMemory, entry, ref fixedBuffer, password);
			gCHandle.Free();
			inMemory.info = null;
			inMemory.pointer = IntPtr.Zero;
			return result;
		}
		return 0;
	}

	public static bool compressBuffer(byte[] source, ref byte[] outBuffer, int levelOfCompression)
	{
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 10)
		{
			levelOfCompression = 10;
		}
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipCompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, levelOfCompression, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return false;
		}
		Array.Resize(ref outBuffer, v);
		Marshal.Copy(intPtr, outBuffer, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return true;
	}

	public static int compressBufferFixed(byte[] source, ref byte[] outBuffer, int levelOfCompression, bool safe = true)
	{
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 10)
		{
			levelOfCompression = 10;
		}
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipCompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, levelOfCompression, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return 0;
		}
		if (v > outBuffer.Length)
		{
			if (safe)
			{
				gCHandle.Free();
				releaseBuffer(intPtr);
				return 0;
			}
			v = outBuffer.Length;
		}
		Marshal.Copy(intPtr, outBuffer, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return v;
	}

	public static byte[] compressBuffer(byte[] source, int levelOfCompression)
	{
		if (levelOfCompression < 0)
		{
			levelOfCompression = 0;
		}
		if (levelOfCompression > 10)
		{
			levelOfCompression = 10;
		}
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipCompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, levelOfCompression, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return null;
		}
		byte[] array = new byte[v];
		Marshal.Copy(intPtr, array, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return array;
	}

	public static bool decompressBuffer(byte[] source, ref byte[] outBuffer)
	{
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipDecompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return false;
		}
		Array.Resize(ref outBuffer, v);
		Marshal.Copy(intPtr, outBuffer, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return true;
	}

	public static int decompressBufferFixed(byte[] source, ref byte[] outBuffer, bool safe = true)
	{
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipDecompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return 0;
		}
		if (v > outBuffer.Length)
		{
			if (safe)
			{
				gCHandle.Free();
				releaseBuffer(intPtr);
				return 0;
			}
			v = outBuffer.Length;
		}
		Marshal.Copy(intPtr, outBuffer, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return v;
	}

	public static byte[] decompressBuffer(byte[] source)
	{
		GCHandle gCHandle = gcA(source);
		int v = 0;
		IntPtr intPtr = zipDecompressBuffer(gCHandle.AddrOfPinnedObject(), source.Length, ref v);
		if (v == 0 || intPtr == IntPtr.Zero)
		{
			gCHandle.Free();
			releaseBuffer(intPtr);
			return null;
		}
		byte[] array = new byte[v];
		Marshal.Copy(intPtr, array, 0, v);
		gCHandle.Free();
		releaseBuffer(intPtr);
		return array;
	}

	public static int gzip(byte[] source, byte[] outBuffer, int level, bool addHeader = true, bool addFooter = true, bool overrideDateTimeWithLength = false)
	{
		if (source == null || outBuffer == null)
		{
			return 0;
		}
		GCHandle gCHandle = gcA(source);
		GCHandle gCHandle2 = gcA(outBuffer);
		if (level < 0)
		{
			level = 0;
		}
		if (level > 10)
		{
			level = 10;
		}
		int num = zipGzip(gCHandle.AddrOfPinnedObject(), source.Length, gCHandle2.AddrOfPinnedObject(), level, addHeader, addFooter);
		gCHandle.Free();
		gCHandle2.Free();
		int num2 = 0;
		if (addHeader)
		{
			num2 += 10;
		}
		if (addFooter)
		{
			num2 += 8;
		}
		int num3 = num + num2;
		if (addHeader && overrideDateTimeWithLength)
		{
			outBuffer[4] = (byte)(num3 & 0xFF);
			outBuffer[5] = (byte)((num3 >>> 8) & 0xFF);
			outBuffer[6] = (byte)((num3 >>> 16) & 0xFF);
			outBuffer[7] = (byte)((num3 >>> 24) & 0xFF);
			outBuffer[9] = 254;
		}
		return num3;
	}

	public static int gzipUncompressedSize(byte[] source)
	{
		if (source == null)
		{
			return 0;
		}
		int num = source.Length;
		return (source[num - 4] & 0xFF) | ((source[num - 3] & 0xFF) << 8) | ((source[num - 2] & 0xFF) << 16) | ((source[num - 1] & 0xFF) << 24);
	}

	public static int gzipCompressedSize(byte[] source, int offset = 0)
	{
		if (source == null)
		{
			return 0;
		}
		if (source[offset + 9] != 254)
		{
			Debug.Log("Gzip has not been marked to have compressed size stored.");
			return 0;
		}
		int num = offset + 8;
		return (source[num - 4] & 0xFF) | ((source[num - 3] & 0xFF) << 8) | ((source[num - 2] & 0xFF) << 16) | ((source[num - 1] & 0xFF) << 24);
	}

	public static int findGzStart(byte[] buffer)
	{
		if (buffer == null)
		{
			return 0;
		}
		int result = 0;
		for (int i = 0; i < buffer.Length - 2; i++)
		{
			if (buffer[i] == 31 && buffer[i + 1] == 139 && buffer[i + 2] == 8)
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int unGzip(byte[] source, byte[] outBuffer, bool hasHeader = true, bool hasFooter = true)
	{
		if (source == null || outBuffer == null)
		{
			return 0;
		}
		GCHandle gCHandle = gcA(source);
		GCHandle gCHandle2 = gcA(outBuffer);
		int result = zipUnGzip(gCHandle.AddrOfPinnedObject(), source.Length, gCHandle2.AddrOfPinnedObject(), outBuffer.Length, hasHeader, hasFooter);
		gCHandle.Free();
		gCHandle2.Free();
		return result;
	}

	public static int unGzip2(byte[] source, byte[] outBuffer)
	{
		if (source == null || outBuffer == null)
		{
			return 0;
		}
		GCHandle gCHandle = gcA(source);
		GCHandle gCHandle2 = gcA(outBuffer);
		int result = zipUnGzip2(gCHandle.AddrOfPinnedObject(), source.Length, gCHandle2.AddrOfPinnedObject(), outBuffer.Length);
		gCHandle.Free();
		gCHandle2.Free();
		return result;
	}

	public static int unGzip2Merged(byte[] source, int offset, int bufferLength, byte[] outBuffer)
	{
		if (source == null || outBuffer == null)
		{
			return 0;
		}
		if (bufferLength == 0)
		{
			return 0;
		}
		GCHandle gCHandle = gcA(source);
		GCHandle gCHandle2 = gcA(outBuffer);
		int result = zipUnGzip2(new IntPtr(gCHandle.AddrOfPinnedObject().ToInt64() + offset), bufferLength, gCHandle2.AddrOfPinnedObject(), outBuffer.Length);
		gCHandle.Free();
		gCHandle2.Free();
		return result;
	}

	public static int gzipFile(string inFile, string outFile = null, int level = 9, ulong[] progress = null, bool addHeader = true)
	{
		int num = -1;
		if (level < 1)
		{
			level = 1;
		}
		if (level > 10)
		{
			level = 10;
		}
		if (outFile == null)
		{
			outFile = inFile + ".gz";
		}
		if (progress != null)
		{
			GCHandle gCHandle = gcA(progress);
			num = gzip_File(inFile.Replace("//", "/"), outFile.Replace("//", "/"), level, gCHandle.AddrOfPinnedObject(), addHeader);
			gCHandle.Free();
		}
		else
		{
			num = gzip_File(inFile.Replace("//", "/"), outFile.Replace("//", "/"), level, IntPtr.Zero, addHeader);
		}
		if (num == 0)
		{
			return 1;
		}
		return num;
	}

	public static int ungzipFile(string inFile, string outFile = null, ulong[] progress = null)
	{
		int num = -1;
		if (outFile == null)
		{
			if (inFile.Substring(inFile.Length - 3, 3).ToLower() != ".gz")
			{
				Debug.Log("Input file does not have a .gz extension");
				return -2;
			}
			outFile = inFile.Substring(0, inFile.Length - 3);
		}
		if (progress != null)
		{
			GCHandle gCHandle = gcA(progress);
			num = ungzip_File(inFile.Replace("//", "/"), outFile.Replace("//", "/"), gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
		}
		else
		{
			num = ungzip_File(inFile.Replace("//", "/"), outFile.Replace("//", "/"), IntPtr.Zero);
		}
		return num;
	}

	public static int bz2Create(string inFile, string outFile = null, int level = 9, ulong[] byteProgress = null)
	{
		int num = -10;
		if (outFile == null)
		{
			outFile = inFile + ".bz2";
		}
		if (byteProgress != null)
		{
			GCHandle gCHandle = gcA(byteProgress);
			num = bz2(decompress: false, level, inFile.Replace("//", "/"), outFile.Replace("//", "/"), gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
		}
		else
		{
			num = bz2(decompress: false, level, inFile.Replace("//", "/"), outFile.Replace("//", "/"), IntPtr.Zero);
		}
		return num;
	}

	public static int bz2Decompress(string inFile, string outFile = null, ulong[] byteProgress = null)
	{
		int num = -10;
		if (outFile == null)
		{
			if (inFile.Substring(inFile.Length - 4, 4).ToLower() != ".bz2")
			{
				Debug.Log("Input file does not have a .bz2 extension");
				return -2;
			}
			outFile = inFile.Substring(0, inFile.Length - 4);
		}
		if (byteProgress != null)
		{
			GCHandle gCHandle = gcA(byteProgress);
			num = bz2(decompress: true, 0, inFile.Replace("//", "/"), outFile.Replace("//", "/"), gCHandle.AddrOfPinnedObject());
			gCHandle.Free();
		}
		else
		{
			num = bz2(decompress: true, 0, inFile.Replace("//", "/"), outFile.Replace("//", "/"), IntPtr.Zero);
		}
		return num;
	}
}
