# EditPE Usage

## Export raw DVRT

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe export-dvrt C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe dvrt.bin
Info: DVRT rva from 0000E350 to 0000E5CB (636 bytes)
```

## Convert raw DVRT into human editable XML

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe dvrt-to-xml dvrt.bin dvrt.xml
```

## Convert edited XML back to raw DVRT

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe xml-to-dvrt dvrt.edit.xml dvrt.edit.bin
```

## Import edited raw DVRT

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe import-dvrt C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe dvrt.edit.bin C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe
Name     | VirtualAddress      | AtFile   | Size     | VirtSize | Character
---------|---------------------|----------|----------|----------|----------
.text    | 00001000 - 000041FF | 00000400 | 00003200 | 00003046 | 60000020
.hexpthk | 00005000 - 000051FF | 00003600 | 00000200 | 00000010 | 60000020
.rdata   | 00006000 - 000091FF | 00003800 | 00003200 | 00003198 | 40000040
.data    | 0000A000 - 0000A1FF | 00006A00 | 00000200 | 00000C88 | C0000040
.pdata   | 0000B000 - 0000B3FF | 00006C00 | 00000400 | 00000248 | 40000040
.a64xrm  | 0000C000 - 0000C1FF | 00007000 | 00000200 | 00000010 | 40000040
.rsrc    | 0000D000 - 0000D1FF | 00007200 | 00000200 | 000001E0 | 40000040
.reloc   | 0000E000 - 0000E3FF | 00007400 | 00000400 | 00000350 | 42000040

Info: Current DVRT is ranging rva from 0000E110 to 0000E357 (688 bytes)
Info: Current DVRT is located at section ".reloc"
Info: Glow the existing section ".reloc", and then write to the appended space.

Name     | VirtualAddress      | AtFile   | Size     | VirtSize | Character
---------|---------------------|----------|----------|----------|----------
.text    | 00001000 - 000041FF | 00000400 | 00003200 | 00003046 | 60000020
.hexpthk | 00005000 - 000051FF | 00003600 | 00000200 | 00000010 | 60000020
.rdata   | 00006000 - 000091FF | 00003800 | 00003200 | 00003198 | 40000040
.data    | 0000A000 - 0000A1FF | 00006A00 | 00000200 | 00000C88 | C0000040
.pdata   | 0000B000 - 0000B3FF | 00006C00 | 00000400 | 00000248 | 40000040
.a64xrm  | 0000C000 - 0000C1FF | 00007000 | 00000200 | 00000010 | 40000040
.rsrc    | 0000D000 - 0000D1FF | 00007200 | 00000200 | 000001E0 | 40000040
.reloc   | 0000E000 - 0000E5FF | 00007400 | 00000600 | 00000600 | 42000040
```

## Test run

### Run Arm64X exe as Arm64EC

Running an exe from x64 process will launch Arm64X exe as Arm64EC.

```
C:\Windows\System32>C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe
Running as: Arm64EC

RVA 000000000000A120 may be patched by DVRT:
0000: 44 44 00 00 88 88 00 00 BC BB FF FF 78 77 FF FF
0010: 12 34 00 00 12 34 56 78 11 22 33 44 55 66 77 88
0020: 00 00 FF FF 00 00 00 00 FF FF FF FF FF FF FF FF
0030: 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF

RVA 000000000000A0E0 Original data:
0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0010: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0020: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
0030: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
```

### Run Arm64X exe as Arm64

```
C:\Proj\RustAndArm64X1>C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe
Running as: Arm64

RVA 000000000000A1A0 may be patched by DVRT:
0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0010: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0020: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
0030: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF

RVA 000000000000A160 Original data:
0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0010: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0020: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
0030: FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF FF
```

## DvrtPatchTest1.cpp

```cpp
#include <Windows.h>
#include <stdio.h>

#define INSERT_DATA \
 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, \
 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, \
 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, \
 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, \

char buf1[] = {
INSERT_DATA
};
char buf2[] = {
INSERT_DATA
};

void dump(const char* buf, int len)
{
	for (int y = 0; y < len; y += 16)
	{
		printf("%04X: ", y);
		int x = 0;
		for (; x < 16 && y + x < len; x++) {
			printf("%02X ", (BYTE)buf[y + x]);
		}
		for (; x < 16; x++) {
			printf("   ");
		}
		puts("");
	}
}

int main()
{
	printf(
		"Running as: "
#if defined(_M_ARM64EC)
		"Arm64EC\n"
#elif defined(_M_ARM64)
		"Arm64\n"
#else
		"?\n"
#endif
	);
	printf("\n");
	printf("RVA %p may be patched by DVRT:\n", (const void*)(buf1 - (const char*)GetModuleHandle(NULL)));
	dump(buf1, sizeof(buf1));
	printf("\n");
	printf("RVA %p Original data:\n", (const void*)(buf2 - (const char*)GetModuleHandle(NULL)));
	dump(buf2, sizeof(buf2));
	return 0;
}
```

## Apply DVRT data

Make the Arm64X PE binary into a simple Arm64EC PE binary. It will get us to inspect the Arm64EC side of Arm64X PE binary easily.

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe apply-dvrt C:\Proj\RustAndArm64X1\ARM64EC\Release\DvrtPatchTest1.exe apply.exe
```

## Nullify DVRT data

Ensure zero fill the pointer to the DVRT data in PE binary.

```
C:\Proj\psqlodbc-for-win10-arm64\Toolings> EditPE\bin\Debug\net8.0\EditPE.exe nullify-dvrt apply.exe apply.exe          
```
