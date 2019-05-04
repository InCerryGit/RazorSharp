using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleSharp.Diagnostics;
using SimpleSharp.Extensions;
using SimpleSharp.Utilities;
using RazorSharp.CoreClr;
using RazorSharp.Native.Win32;

namespace RazorSharp.Native.Symbols
{
	internal static class SymbolAccess
	{
		private const string MASK_STR_DEFAULT = "*!*";
		
		internal static FileInfo DownloadSymbolFile(DirectoryInfo dest, FileInfo dll)
		{
			return DownloadSymbolFile(dest, dll, out _);
		}

		internal static Task<FileInfo> DownloadSymbolFileAsync(DirectoryInfo dest, FileInfo dll)
		{
			return new Task<FileInfo>(() => DownloadSymbolFile(dest, dll));
		}

		internal static FileInfo DownloadSymbolFile(DirectoryInfo dest, FileInfo dll, out DirectoryInfo super)
		{
			// symchk
			string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			var    symChk    = new FileInfo(String.Format(@"{0}\Windows Kits\10\Debuggers\x64\symchk.exe", progFiles));
			Conditions.Require(symChk.Exists);

			string cmd = String.Format("\"{0}\" \"{1}\" /s SRV*{2}*http://msdl.microsoft.com/download/symbols",
			                           symChk.FullName, dll.FullName, dest.FullName);


			using (var cmdProc = Common.Shell("\"" + cmd + "\"")) {
				

				cmdProc.ErrorDataReceived += (sender, args) =>
				{
					Global.Log.Error("Process error: {Error}", args.Data);
				};

				cmdProc.Start();

				var stdOut = cmdProc.StandardOutput;
				while (!stdOut.EndOfStream) {
					string ln = stdOut.ReadLine();
					Conditions.NotNull(ln, nameof(ln));
					if (ln.Contains("SYMCHK: PASSED + IGNORED files = 1")) {
						break;
					}

					
				}
			}

			Global.Log.Debug("Done downloading symbols");

			string   pdbStr = Path.Combine(dest.FullName, Clr.CLR_PDB_SHORT);
			FileInfo pdb;

			if (Directory.Exists(pdbStr)) {
				// directory will be named <symbol>.pdb
				super = new DirectoryInfo(pdbStr);

				// sole child directory will be something like 9FF14BF5D36043909E88FF823F35EE3B2
				DirectoryInfo[] children = super.GetDirectories();
				Conditions.Assert(children.Length == 1);
				var child = children[0];

				// (possibly sole) file will be the symbol file
				FileInfo[] files = child.GetFiles();
				pdb = files.First(f => f.Name.Contains(Clr.CLR_PDB_SHORT));
			}
			else if (File.Exists(pdbStr)) {
				super = null;
				pdb   = new FileInfo(pdbStr);
			}
			else {
				throw new Exception(String.Format("Error downloading symbols. File: {0}", pdbStr));
			}

			Conditions.Ensure(pdb.Exists);
			return pdb;
		}

		public static string Undname(string sz)
		{
			
			using (var cmd = Common.Shell($"undname \"{sz}\"", true)) {
				
				var stdOut = cmd.StandardOutput.ReadToEnd();
				stdOut = Encoding.ASCII.GetString(Encoding.Unicode.GetBytes(stdOut));
				var value  = stdOut.SubstringBetween("is :- \"","\"");
				return value;
			}
		}

		internal static unsafe bool GetFileSize(string pFileName, ref ulong fileSize)
		{
			var hFile = Kernel32.CreateFile(pFileName,
			                                FileAccess.Read,
			                                FileShare.Read,
			                                IntPtr.Zero,
			                                FileMode.Open,
			                                0,
			                                IntPtr.Zero);


			if (hFile == Kernel32.INVALID_HANDLE_VALUE) {
				return false;
			}

			fileSize = Kernel32.GetFileSize(hFile, null);

			Conditions.Ensure(Kernel32.CloseHandle(hFile));

			if (fileSize == Kernel32.INVALID_FILE_SIZE) {
				return false;
			}


			return fileSize != Kernel32.INVALID_FILE_SIZE;
		}

		internal static bool GetFileParams(string pFileName, ref ulong baseAddr, ref ulong fileSize)
		{
			// Is it .PDB file ?

			if (pFileName.Contains("pdb")) {
				// Yes, it is a .PDB file 

				// Determine its size, and use a dummy base address 

				baseAddr = 0x10000000; // it can be any non-zero value, but if we load symbols 
				// from more than one file, memory regions specified
				// for different files should not overlap
				// (region is "base address + file size")

				if (!SymbolAccess.GetFileSize(pFileName, ref fileSize)) {
					return false;
				}
			}
			else {
				// It is not a .PDB file 

				// Base address and file size can be 0 

				baseAddr = 0;
				fileSize = 0;

				throw new NotImplementedException();
			}

			return true;
		}
	}
}