# Toolings

## Tools

These are the build helper tools to build an ARM64X binary.

Tool | Purpose
---|---
Arm64XDualObjCL | The wrapped CL.exe can treat pseudo-ARM64X COFF files.
Arm64XDualObjLIB | The wrapped LIB.exe can treat pseudo-ARM64X COFF files.
Arm64XDualObjLINK | The wrapped LINK.exe can treat pseudo-ARM64X COFF files.
Arm64XSingleLibCL | The wrapped CL.exe can use ARM64X lib files as COFF.
Arm64XSingleLibLIB | The wrapped LIB.exe can use ARM64X lib files as COFF.
Arm64XSingleLibLINK | The wrapped LINK.exe can use ARM64X lib files as COFF.
IsArm64x | Determine the binary type of a file.
PkgConfigAlternative | The alternative of pkg-config.
ReadPEExportTable | Print out the export table of a PE file.
ReadPEImportTable | Print out the import table of a PE file.

## pseudo-ARM64X COFF

A COFF file with magic 0x014C and 2 sections having names `AA64.obj` (ARM64 COFF) and `A641.obj` (ARM64EC COFF).
