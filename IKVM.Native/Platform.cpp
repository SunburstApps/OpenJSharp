/*
  Copyright (C) 2004, 2005 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
	 claim that you wrote the original software. If you use this software
	 in a product, an acknowledgment in the product documentation would be
	 appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
	 misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net

*/

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <stdio.h>
#include <string.h>
#include "jni.h"

INT_PTR WINAPI DllMain(HINSTANCE hInstance, DWORD reason, LPVOID /* reserved */) {
	if (reason == DLL_PROCESS_ATTACH) {
		DisableThreadLibraryCalls(hInstance);
	}

	return (INT_PTR)TRUE;
}

JNIEXPORT void * JNICALL ikvm_LoadLibrary(char *libName) {
	return ::LoadLibraryA(libName);
}

JNIEXPORT void JNICALL ikvm_FreeLibrary(HMODULE handle) {
	::FreeLibrary(handle);
}

JNIEXPORT void * JNICALL ikvm_GetProcAddress(HMODULE handle, char *procName, jint argc) {
#ifdef _WIN64
	return ::GetProcAddress(handle, procName);
#else
	char buf[512];
	if (strlen(procName) > sizeof(buf) - 11) return nullptr;
	_snprintf(buf, 512, "_%s@%d", procName, argc);
	auto pFunc = ::GetProcAddress(handle, buf);
	if (pFunc != nullptr) return pFunc;

	// If the mangled name was not found, try the unmangled name. This can
	// occur if you have an explicit EXPORT statement in a *.def file.
	return ::GetProcAddress(handle, procName);
#endif
}
