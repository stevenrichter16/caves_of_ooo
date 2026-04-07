using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class testZip : MonoBehaviour
{
	private int zres;

	private string myFile;

	private string log;

	private string ppath;

	private bool compressionStarted;

	private bool pass;

	private bool downloadDone;

	private byte[] reusableBuffer;

	private byte[] reusableBuffer2;

	private byte[] reusableBuffer3;

	private byte[] fixedInBuffer = new byte[262144];

	private byte[] fixedOutBuffer = new byte[786432];

	private byte[] fixedBuffer = new byte[1048576];

	private int[] progress = new int[1];

	private ulong[] progress2 = new ulong[1];

	private ulong[] byteProgress = new ulong[1];

	private void plog(string t = "")
	{
		log = log + t + "\n";
	}

	private void Start()
	{
		ppath = Application.persistentDataPath;
		ppath = ".";
		Debug.Log("persistentDataPath: " + ppath);
		reusableBuffer = new byte[4096];
		reusableBuffer2 = new byte[0];
		reusableBuffer3 = new byte[0];
		Screen.sleepTimeout = -1;
		StartCoroutine(DownloadZipFile());
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}

	private void OnGUI()
	{
		if (GUI.Button(new Rect(Screen.width - 100, 90f, 80f, 40f), "Cancel"))
		{
			lzip.setCancel();
		}
		if (downloadDone)
		{
			GUI.Label(new Rect(10f, 0f, 250f, 30f), "package downloaded, ready to extract");
			GUI.Label(new Rect(10f, 50f, 650f, 100f), ppath);
		}
		if (compressionStarted)
		{
			GUI.TextArea(new Rect(10f, 160f, Screen.width - 20, Screen.height - 170), log);
			GUI.Label(new Rect(Screen.width - 30, 0f, 80f, 40f), progress[0].ToString());
			GUI.Label(new Rect(Screen.width - 140, 0f, 80f, 40f), progress2[0].ToString());
		}
		if (downloadDone)
		{
			if (GUI.Button(new Rect(10f, 90f, 110f, 50f), "Zip test"))
			{
				log = "";
				compressionStarted = true;
				DoDecompression();
			}
			if (GUI.Button(new Rect(130f, 90f, 110f, 50f), "FileBuffer test"))
			{
				log = "";
				compressionStarted = true;
				DoDecompression_FileBuffer();
			}
			if (GUI.Button(new Rect(250f, 90f, 110f, 50f), "InMemory Test"))
			{
				log = "";
				compressionStarted = true;
				DoInMemoryTest();
			}
			if (GUI.Button(new Rect(370f, 90f, 110f, 50f), "Merged zip Test"))
			{
				log = "";
				compressionStarted = true;
				DoDecompression_Merged();
			}
			if (GUI.Button(new Rect(490f, 90f, 110f, 50f), "Gzip/Bz2 Test"))
			{
				log = "";
				compressionStarted = true;
				DoGzipBz2Tests();
			}
			if (GUI.Button(new Rect(610f, 90f, 110f, 50f), "Tar Test"))
			{
				log = "";
				compressionStarted = true;
				DoTarTests();
			}
		}
	}

	private void DoDecompression()
	{
		plog("Validate: " + lzip.validateFile(ppath + "/testZip.zip"));
		zres = lzip.decompress_File(ppath + "/testZip.zip", ppath + "/", progress, null, progress2);
		plog("decompress: " + zres);
		plog();
		plog("true total files: " + lzip.getTotalFiles(ppath + "/testZip.zip"));
		plog("true total entries: " + lzip.getTotalEntries(ppath + "/testZip.zip"));
		plog("entry exists: " + lzip.entryExists(ppath + "/testZip.zip", "dir1/dir2/test2.bmp"));
		plog();
		plog("DateTime: " + lzip.entryDateTime(ppath + "/testZip.zip", "dir1/dir2/test2.bmp"));
		zres = lzip.extract_entry(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ppath + "/test22P.bmp", null, progress2);
		plog("extract entry: " + zres);
		plog();
		zres = lzip.compress_File(9, ppath + "/test2Zip.zip", ppath + "/dir1/dir2/test2.bmp", append: false, "dir1/dir2/test2.bmp", null, null, useBz2: false, 0, byteProgress);
		plog("compress: " + zres);
		zres = lzip.compress_File(0, ppath + "/test2Zip.zip", ppath + "/dir1/dir2/dir3/Unity_1.jpg", append: true, "dir1/dir2/dir3/Unity_1.jpg", "ccc", null, useBz2: false, 0, byteProgress);
		plog("append: " + zres + "\nTotal bytes processed: " + byteProgress[0]);
		byteProgress[0] = 0uL;
		lzip.getFileInfo(ppath + "/test2Zip.zip");
		int entryIndex = lzip.getEntryIndex("dir1/dir2/dir3/Unity_1.jpg");
		if (entryIndex != -1)
		{
			int num = (int)lzip.uinfo[entryIndex];
			int num2 = (int)lzip.localOffset[entryIndex] + 30 + lzip.ninfo[entryIndex].Length;
			plog("Real Offset: " + num2);
			byte[] array = new byte[num];
			using (BinaryReader binaryReader = new BinaryReader(new FileStream(ppath + "/test2Zip.zip", FileMode.Open)))
			{
				binaryReader.BaseStream.Seek(num2, SeekOrigin.Begin);
				binaryReader.Read(array, 0, num);
			}
			File.WriteAllBytes(ppath + "/Offset.jpg", array);
			array = null;
		}
		plog();
		progress2[0] = 0uL;
		zres = lzip.compress_File(9, ppath + "/test2ZipSPAN.zip", ppath + "/dir1/dir2/test2.bmp", append: false, "dir1/dir2/test2.bmp", null, null, useBz2: false, 20000, progress2);
		plog("compress SPAN: " + zres + "  progress: " + progress2[0]);
		zres = lzip.compress_File(9, ppath + "/test2ZipSPAN.zip", ppath + "/dir1/dir2/dir3/Unity_1.jpg", append: true, "dir1/dir2/dir3/Unity_1.jpg", null, null, useBz2: false, 20000, progress2);
		plog("compress SPAN 2: " + zres + "  progress: " + progress2[0]);
		progress2[0] = 0uL;
		zres = lzip.decompress_File(ppath + "/test2ZipSPAN.zip", ppath + "/SPANNED/", progress, null, progress2);
		plog("decompress SPAN: " + zres + "  progress: " + progress2[0]);
		plog();
		bool flag = true;
		flag = false;
		List<string> list = new List<string>();
		list.Add(ppath + "/test22P.bmp");
		list.Add(ppath + "/dir1/dir2/test2.bmp");
		List<string> list2 = new List<string>();
		list2.Add("NEW_test22P.bmp");
		list2.Add("dir13/dir23/New_test2.bmp");
		zres = lzip.compress_File_List(9, ppath + "/fileList.zip", list.ToArray(), progress, append: false, list2.ToArray(), "password", flag);
		plog("MultiFile Compress password: " + zres);
		list.Clear();
		list = null;
		list2.Clear();
		list2 = null;
		zres = lzip.decompress_File(ppath + "/fileList.zip", ppath + "/", progress, null, progress2, "password");
		plog("decompress password: " + zres);
		plog();
		plog("Buffer2File: " + lzip.buffer2File(9, ppath + "/test3Zip.zip", "buffer.bin", reusableBuffer));
		plog("Buffer2File append: " + lzip.buffer2File(9, ppath + "/test3Zip.zip", "dir4/buffer.bin", reusableBuffer, flag));
		plog();
		plog("get entry size: " + lzip.getEntrySize(ppath + "/testZip.zip", "dir1/dir2/test2.bmp"));
		plog();
		plog("entry2Buffer1: " + lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref reusableBuffer2));
		plog();
		plog("entry2FixedBuffer: " + lzip.entry2FixedBuffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref fixedBuffer));
		plog();
		byte[] array2 = lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp");
		zres = 0;
		if (array2 != null)
		{
			zres = 1;
		}
		plog("entry2Buffer2: " + zres);
		plog();
		int num3 = lzip.compressBufferFixed(array2, ref fixedInBuffer, 9);
		plog(" # Compress Fixed size Buffer: " + num3);
		if (num3 > 0)
		{
			int num4 = lzip.decompressBufferFixed(fixedInBuffer, ref fixedOutBuffer);
			if (num4 > 0)
			{
				plog(" # Decompress Fixed size Buffer: " + num4);
			}
		}
		plog();
		pass = lzip.compressBuffer(reusableBuffer2, ref reusableBuffer3, 9);
		plog("compressBuffer1: " + pass);
		if (pass)
		{
			File.WriteAllBytes(ppath + "/out.bin", reusableBuffer3);
		}
		array2 = lzip.compressBuffer(reusableBuffer2, 9);
		zres = 0;
		if (array2 != null)
		{
			zres = 1;
		}
		plog("compressBuffer2: " + zres);
		plog();
		pass = lzip.decompressBuffer(reusableBuffer3, ref reusableBuffer2);
		plog("decompressBuffer1: " + pass);
		if (pass)
		{
			File.WriteAllBytes(ppath + "/out.bmp", reusableBuffer2);
		}
		zres = 0;
		if (array2 != null)
		{
			zres = 1;
		}
		array2 = lzip.decompressBuffer(reusableBuffer3);
		if (array2 != null)
		{
			plog("decompressBuffer2: " + array2.Length);
		}
		else
		{
			plog("decompressBuffer2: Failed");
		}
		if (array2 != null)
		{
			File.WriteAllBytes(ppath + "/out2.bmp", array2);
		}
		plog();
		plog("total bytes: " + lzip.getFileInfo(ppath + "/testZip.zip"));
		if (lzip.ninfo != null)
		{
			for (int i = 0; i < lzip.ninfo.Count; i++)
			{
				log = log + lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
			}
		}
		plog();
		int[] array3 = new int[1];
		lzip.compressDir(ppath + "/dir1", 9, ppath + "/recursive.zip", includeRoot: false, array3);
		plog("recursive - no. of files: " + array3[0]);
		zres = lzip.decompress_File(ppath + "/recursive.zip", ppath + "/recursive/", progress, null, progress2);
		plog("decompress recursive: " + zres);
		new Thread(decompressFunc).Start();
		if (File.Exists(ppath + "/test-Zip.zip"))
		{
			File.Delete(ppath + "/test-Zip.zip");
		}
		if (File.Exists(ppath + "/testZip.zip"))
		{
			File.Copy(ppath + "/testZip.zip", ppath + "/test-Zip.zip");
		}
		byte[] newFileBuffer = lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp");
		plog("replace entry: " + lzip.replace_entry(ppath + "/test-Zip.zip", "dir1/dir2/test2.bmp", newFileBuffer));
		plog("replace entry 2: " + lzip.replace_entry(ppath + "/test-Zip.zip", "dir1/dir2/test2.bmp", ppath + "/dir1/dir2/test2.bmp"));
		plog("delete entry: " + lzip.delete_entry(ppath + "/test-Zip.zip", "dir1/dir2/test2.bmp"));
	}

	private void decompressFunc()
	{
		int num = lzip.decompress_File(ppath + "/recursive.zip", ppath + "/recursive/", progress, null, progress2);
		if (num == 1)
		{
			plog("multithreaded ok");
		}
		else
		{
			plog("multithreaded error: " + num);
		}
	}

	private void DoDecompression_FileBuffer()
	{
		byte[] fileBuffer = File.ReadAllBytes(ppath + "/testZip.zip");
		plog("Validate: " + lzip.validateFile(null, fileBuffer));
		zres = lzip.decompress_File(null, ppath + "/", progress, fileBuffer, progress2);
		plog("decompress: " + zres + "  progress: " + progress2[0]);
		plog("true total files: " + lzip.getTotalFiles(null, fileBuffer));
		plog("total entries: " + lzip.getTotalEntries(null, fileBuffer));
		plog("entry exists: " + lzip.entryExists(null, "dir1/dir2/test2.bmp", fileBuffer));
		zres = lzip.extract_entry(null, "dir1/dir2/test2.bmp", ppath + "/test22B.bmp", fileBuffer, progress2);
		plog("extract entry: " + zres + "  progress: " + progress2[0]);
		plog();
		plog("get entry size: " + lzip.getEntrySize(null, "dir1/dir2/test2.bmp", fileBuffer));
		plog();
		plog("entry2Buffer1: " + lzip.entry2Buffer(null, "dir1/dir2/test2.bmp", ref reusableBuffer2, fileBuffer));
		byte[] array = lzip.entry2Buffer(null, "dir1/dir2/test2.bmp", fileBuffer);
		zres = 0;
		if (array != null)
		{
			zres = 1;
		}
		plog("entry2Buffer2: " + zres);
		plog();
		plog("total bytes: " + lzip.getFileInfo(null, fileBuffer));
		if (lzip.ninfo != null)
		{
			for (int i = 0; i < lzip.ninfo.Count; i++)
			{
				log = log + lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
			}
		}
		plog();
	}

	private void DoInMemoryTest()
	{
		if (!File.Exists(ppath + "/dir1/dir2/test2.bmp"))
		{
			lzip.decompress_File(ppath + "/testZip.zip");
			lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref reusableBuffer2);
		}
		lzip.inMemory inMemory = new lzip.inMemory();
		byte[] buffer = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");
		lzip.compress_Buf2Mem(inMemory, 9, buffer, "inmem/Unity_1.jpg", null, "1234", useBz2: true);
		buffer = null;
		plog("inMemory zip size: " + inMemory.size());
		lzip.compress_Buf2Mem(inMemory, 9, reusableBuffer2, "inmem/test.bmp", null, "1234", useBz2: true);
		plog("inMemory zip size: " + inMemory.info[0]);
		byte[] zipBuffer = inMemory.getZipBuffer();
		File.WriteAllBytes(ppath + "/MEM.zip", zipBuffer);
		zipBuffer = null;
		progress2[0] = 0uL;
		plog("decompress_Mem2File: " + lzip.decompress_Mem2File(inMemory, ppath + "/", null, progress2, "1234") + "  progress: " + progress2[0]);
		lzip.getFileInfoMem(inMemory);
		plog();
		if (lzip.ninfo != null)
		{
			for (int i = 0; i < lzip.ninfo.Count; i++)
			{
				log = log + lzip.ninfo[i] + " - " + lzip.uinfo[i] + " / " + lzip.cinfo[i] + "\n";
			}
		}
		plog();
		byte[] buffer2 = null;
		plog("entry2BufferMem: " + lzip.entry2BufferMem(inMemory, "inmem/test.bmp", ref buffer2, "1234"));
		plog("entry2FixedBufferMem: " + lzip.entry2FixedBufferMem(inMemory, "inmem/test.bmp", ref buffer2, "1234"));
		byte[] array = lzip.entry2BufferMem(inMemory, "inmem/test.bmp", "1234");
		plog("entry2BufferMem new buffer: " + array.Length);
		lzip.free_inmemory(inMemory);
		plog();
		lzip.inMemory inMemory2 = new lzip.inMemory();
		lzip.inMemoryZipStart(inMemory2);
		buffer = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");
		lzip.inMemoryZipAdd(inMemory2, 9, buffer, "test.jpg");
		lzip.inMemoryZipAdd(inMemory2, 9, reusableBuffer2, "directory/test.bmp");
		lzip.inMemoryZipClose(inMemory2);
		lzip.inMemoryZipStart(inMemory2);
		lzip.inMemoryZipAdd(inMemory2, 9, buffer, "newDir/test2.jpg");
		lzip.inMemoryZipAdd(inMemory2, 9, reusableBuffer2, "directory2/test2.bmp");
		lzip.inMemoryZipClose(inMemory2);
		plog("Size of Low Level inMemory zip: " + inMemory2.size());
		File.WriteAllBytes(ppath + "/MEM2.zip", inMemory2.getZipBuffer());
		buffer = null;
		lzip.free_inmemory(inMemory2);
		plog();
	}

	private void DoGzipBz2Tests()
	{
		if (reusableBuffer2.Length < 1)
		{
			lzip.entry2Buffer(ppath + "/testZip.zip", "dir1/dir2/test2.bmp", ref reusableBuffer2);
		}
		byte[] array = new byte[reusableBuffer2.Length + 18];
		int num = lzip.gzip(reusableBuffer2, array, 10);
		plog("gzip compressed size: " + num);
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, num);
		File.WriteAllBytes(ppath + "/test2.bmp.gz", array2);
		byte[] array3 = new byte[lzip.gzipUncompressedSize(array2)];
		int num2 = lzip.unGzip(array2, array3);
		if (num2 > 0)
		{
			File.WriteAllBytes(ppath + "/test2GZIP.bmp", array3);
			plog("gzip decompression: success " + num2);
		}
		else
		{
			plog("gzip decompression error: " + num2);
		}
		array = null;
		array2 = null;
		array3 = null;
		plog();
		ulong[] array4 = new ulong[1];
		plog("Gzip file creation: " + lzip.gzipFile(ppath + "/test2GZIP.bmp", ppath + "/Ftest2GZIP.bmp.gz", 10, array4) + "  progress: " + array4[0]);
		plog("Gzip file decompression: " + lzip.ungzipFile(ppath + "/Ftest2GZIP.bmp.gz", ppath + "/Ftest2GZIP.bmp", array4) + "  progress: " + array4[0]);
		plog();
		plog("bz2 creation: " + lzip.bz2Create(ppath + "/Ftest2GZIP.bmp", ppath + "/Ftest2GZIP.bmp.bz2", 9, array4) + "  progress: " + array4[0]);
		plog("bz2 extract: " + lzip.bz2Decompress(ppath + "/Ftest2GZIP.bmp.bz2", ppath + "/Ftest2GZIP-Bz2.bmp", array4) + "  progress: " + array4[0]);
		plog();
	}

	private void DoTarTests()
	{
		if (!Directory.Exists(ppath + "/mergedTests"))
		{
			DoDecompression_Merged();
		}
		log = "";
		ulong[] array = new ulong[1] { 0uL };
		plog("Create Tar: " + lzip.tarDir(ppath + "/mergedTests", ppath + "/out.tar", includeRoot: true, null, array));
		plog("processed: " + array[0]);
		plog();
		array[0] = 0uL;
		plog("Extract Tar: " + lzip.tarExtract(ppath + "/out.tar", ppath + "/tarOut", null, array));
		plog("processed: " + array[0]);
		plog();
		plog("Extract Tar entry: " + lzip.tarExtractEntry(ppath + "/out.tar", "mergedTests/dir1/dir2/test2.bmp", ppath + "/tarOut2", fullPaths: true, array));
		plog("Extract Tar entry absolute Path: " + lzip.tarExtractEntry(ppath + "/out.tar", "mergedTests/overriden.jpg", ppath + "/outTarAbsolute.jpeg", fullPaths: false, array));
		plog();
		plog("tar.gz creation: " + lzip.gzipFile(ppath + "/out.tar", ppath + "/out.tar.gz", 10));
		plog("tar.bz2 creation: " + lzip.bz2Create(ppath + "/out.tar", ppath + "/out.tar.bz2"));
		plog();
		lzip.getTarInfo(ppath + "/out.tar");
		if (lzip.ninfo != null && lzip.ninfo.Count > 0)
		{
			for (int i = 0; i < lzip.ninfo.Count; i++)
			{
				plog("Entry no: " + (i + 1) + "   " + lzip.ninfo[i] + "  size: " + lzip.uinfo[i]);
			}
		}
	}

	private void DoDecompression_Merged()
	{
		if (!File.Exists(ppath + "/merged.jpg"))
		{
			if (!File.Exists(ppath + "/dir1/dir2/dir3/Unity_1.jpg"))
			{
				lzip.decompress_File(ppath + "/testZip.zip", ppath + "/", progress, null, progress2);
			}
			byte[] array = File.ReadAllBytes(ppath + "/dir1/dir2/dir3/Unity_1.jpg");
			byte[] array2 = File.ReadAllBytes(ppath + "/testZip.zip");
			byte[] array3 = new byte[array.Length + array2.Length];
			Array.Copy(array, 0, array3, 0, array.Length);
			Array.Copy(array2, 0, array3, array.Length, array2.Length);
			File.WriteAllBytes(ppath + "/merged.jpg", array3);
			array = null;
			array2 = null;
			array3 = null;
		}
		plog("Get Info of merged zip: " + lzip.getZipInfo(ppath + "/merged.jpg"));
		if (lzip.zinfo != null && lzip.zinfo.Count > 0)
		{
			for (int i = 0; i < lzip.zinfo.Count; i++)
			{
				plog("Entry no: " + (i + 1) + "   " + lzip.zinfo[i].filename + "  uncompressed: " + lzip.zinfo[i].UncompressedSize + "  compressed: " + lzip.zinfo[i].CompressedSize);
			}
		}
		plog();
		int[] array4 = new int[1];
		ulong[] array5 = new ulong[1];
		plog("Decompress to disk from merged file: " + lzip.decompressZipMerged(ppath + "/merged.jpg", ppath + "/mergedTests/", array4, array5) + " progress: " + array5[0]);
		plog("Extract entry to disk from merged file: " + lzip.entry2FileMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg", ppath + "/mergedTests", "overriden.jpg"));
		plog();
		byte[] buffer = File.ReadAllBytes(ppath + "/merged.jpg");
		plog("Get Info of merged zip in Buffer: " + lzip.getZipInfoMerged(buffer));
		if (lzip.zinfo != null && lzip.zinfo.Count > 0)
		{
			for (int j = 0; j < lzip.zinfo.Count; j++)
			{
				plog("Entry no: " + (j + 1) + "   " + lzip.zinfo[j].filename + "  uncompressed: " + lzip.zinfo[j].UncompressedSize + "  compressed: " + lzip.zinfo[j].CompressedSize);
			}
		}
		plog();
		plog("Decompress to disk from merged buffer: " + lzip.decompressZipMerged(buffer, ppath + "/mergedTests/", array4));
		plog("Entry2File from merged buffer: " + lzip.entry2FileMerged(buffer, "dir1/dir2/dir3/Unity_1.jpg", ppath + "/mergedTests"));
		plog();
		byte[] array6 = lzip.entry2BufferMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg");
		plog("Size of entry in new buffer 1: " + array6.Length);
		array6 = null;
		byte[] array7 = new byte[11264];
		plog("Size of entry in fixed buffer 1: " + lzip.entry2FixedBufferMerged(ppath + "/merged.jpg", "dir1/dir2/dir3/Unity_1.jpg", ref array7));
		plog();
		byte[] array8 = lzip.entry2BufferMerged(buffer, "dir1/dir2/dir3/Unity_1.jpg");
		plog("Size of entry in new buffer 2: " + array8.Length);
		plog("Size of entry in fixed buffer 2: " + lzip.entry2FixedBufferMerged(buffer, "dir1/dir2/dir3/Unity_1.jpg", ref array7));
		array7 = null;
		buffer = null;
	}

	private IEnumerator DownloadZipFile()
	{
		myFile = "testZip.zip";
		if (File.Exists(ppath + "/" + myFile))
		{
			downloadDone = true;
			yield break;
		}
		Debug.Log("starting download");
		using UnityWebRequest www = UnityWebRequest.Get("https://dl.dropboxusercontent.com/s/xve34ldz3pqvmh1/" + myFile);
		yield return www.SendWebRequest();
		if (www.error != null)
		{
			Debug.Log(www.error);
			yield break;
		}
		downloadDone = true;
		File.WriteAllBytes(ppath + "/" + myFile, www.downloadHandler.data);
		Debug.Log("download done");
	}
}
