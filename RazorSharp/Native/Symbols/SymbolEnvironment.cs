using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RazorCommon;
using RazorCommon.Extensions;
using RazorSharp.Native.Win32;

namespace RazorSharp.Native.Symbols
{
	public unsafe class SymbolEnvironment : IDisposable
	{
		private string m_img;

		private IntPtr m_proc;
		private Symbol m_symBuf;

		private readonly ulong m_modBase;


		public SymbolEnvironment(string img)
		{
			m_img = img;

			m_proc = Kernel32.GetCurrentProcess();

			uint options = DbgHelp.SymGetOptions();

			// SYMOPT_DEBUG option asks DbgHelp to print additional troubleshooting
			// messages to debug output - use the debugger's Debug Output window
			// to view the messages


			options |= DbgHelp.SYMOPT_DEBUG;

			DbgHelp.SymSetOptions(options);

			// Initialize DbgHelp and load symbols for all modules of the current process 
			var symInit = DbgHelp.SymInitialize(m_proc, null, true);

			NativeHelp.Call(symInit, nameof(DbgHelp.SymInitialize));

			ulong baseAddr = 0;
			ulong fileSize = 0;

			bool getFile = SymbolReader.GetFileParams(img, ref baseAddr, ref fileSize);
			NativeHelp.Call(getFile);

			m_modBase = DbgHelp.SymLoadModule64(
				m_proc,         // Process handle of the current process
				IntPtr.Zero,    // Handle to the module's image file (not needed)
				img,            // Path/name of the file
				null,           // User-defined short name of the module (it can be NULL)
				baseAddr,       // Base address of the module (cannot be NULL if .PDB file is used, otherwise it can be NULL)
				(uint) fileSize // Size of the file (cannot be NULL if .PDB file is used, otherwise it can be NULL)
			);

			NativeHelp.Call(m_modBase != 0, nameof(DbgHelp.SymLoadModule64));
		}


		private bool FirstSymCallback(IntPtr sym, uint symSize, IntPtr userCtx)
		{
			var pSym = (SymbolInfo*) sym;


			var symName = NativeHelp.GetString(&pSym->Name, pSym->NameLen);
			var ctxStr  = Marshal.PtrToStringAuto(userCtx);


			if (symName == ctxStr) {
				m_symBuf = new Symbol(pSym);
				return false;
			}

			return true;
		}

		public long GetSymOffset(string name) => First(name).Offset;

		public Symbol First(string name, string mask = null)
		{
			IntPtr nameNative = Marshal.StringToHGlobalAuto(name);

			var symEnumSuccess = DbgHelp.SymEnumSymbols(
				m_proc,           // Process handle of the current process
				m_modBase,        // Base address of the module
				mask,             // Mask (NULL -> all symbols)
				FirstSymCallback, // The callback function
				nameNative        // A used-defined context can be passed here, if necessary
			);

			NativeHelp.Call(symEnumSuccess, nameof(DbgHelp.SymEnumSymbols));
			Marshal.FreeHGlobal(nameNative);


			var cpy = m_symBuf;
			m_symBuf = null;
			return cpy;
		}

		public void Dispose()
		{
			NativeHelp.Call(DbgHelp.SymUnloadModule64(m_proc, m_modBase), nameof(DbgHelp.SymUnloadModule64));

			NativeHelp.Call(DbgHelp.SymCleanup(m_proc), nameof(DbgHelp.SymCleanup));

			m_proc   = IntPtr.Zero;
			m_symBuf = null;
		}
	}
}