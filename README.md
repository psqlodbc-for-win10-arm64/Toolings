# Toolings

## Tools

These are the build helper tools to build ARM64X binary.

Tool | Purpose
---|---
Arm64XDualObjCL | The wrapped CL.exe, it can treat pseudo-ARM64X COFF files.
Arm64XDualObjLIB | The wrapped LIB.exe, it can treat pseudo-ARM64X COFF files.
Arm64XDualObjLINK | The wrapped LINK.exe, it can treat pseudo-ARM64X COFF files.
Arm64XSingleLibCL | The wrapped CL.exe, it can use ARM64X lib files as COFF.
Arm64XSingleLibLIB | The wrapped LIB.exe, it can use ARM64X lib files as COFF.
Arm64XSingleLibLINK | The wrapped LINK.exe, it can use ARM64X lib files as COFF.

## pseudo-ARM64X COFF

A COFF file with magic 0x014C and 2 sections having names `AA64.obj` (ARM64 COFF) and `A641.obj` (ARM64EC COFF).
