# Independent processor reference fixtures

The committed JSON fixtures are generated independently of the C# interpreter. Ordinary `dotnet test` runs them offline. Changes to a fixture require reviewing its generator and the architectural expectation; the interpreter does not generate its own expected values.

- `generate-cpu-vectors.py`: 3,200 assembled integer, DSP and special-register cases executed by QEMU's Cortex-M4 on `mps2-an386`.
- `generate-float-vectors.py`: 26,480 numerical cases from Berkeley SoftFloat 3e using the ARM-VFPv2 specialization and tininess before rounding. Covers all four ARM rounding directions, special values, conversions and fused arithmetic.
- `generate-fpu-instruction-vectors.py`: 50,688 real FPv4-SP-D16 instruction cases executed by QEMU, including FPSCR rounding, default-NaN, flush-to-zero, alternative half precision, fixed-point conversions and negated operations.

Reference environment used for this revision: QEMU 11.1.3, Clang's `arm-none-eabi` assembler, Python 3 and a C99 compiler. The self-contained ELF harness needs no ARM linker or firmware toolchain. QEMU and SoftFloat are testing tools; neither is shipped with the browser or desktop application.

```sh
python3 scripts/reference/generate-cpu-vectors.py --check
python3 scripts/reference/generate-float-vectors.py --check
python3 scripts/reference/generate-fpu-instruction-vectors.py --check
```

Omit `--check` only when intentionally regenerating a fixture. The SoftFloat script downloads the author's immutable 3e archive, checks SHA-256 `21130ce885d35c1fe73fc1e1bf2244178167e05c6747cad5f450cc991714c746`, and builds the unmodified source. `--softfloat <directory>` permits an existing 3e checkout. Downloaded sources and executables remain in ignored `artifacts/reference`.

SoftFloat 3e is copyright the Regents of the University of California and is distributed under its three-clause Berkeley license. Preserve the archive's `COPYING.txt` and per-file notices when redistributing reference sources or binaries. This repository contains only an original test adapter and generated numeric results. See [SoftFloat documentation and license](https://www.jhauser.us/arithmetic/SoftFloat-3/doc/SoftFloat-source.html), [its numerical interface](https://www.jhauser.us/arithmetic/SoftFloat-3/doc/SoftFloat.html), and [QEMU's Cortex-M4 board documentation](https://www.qemu.org/docs/master/system/arm/mps2.html).

These are differential regression vectors, not an exhaustive hardware conformance proof. System traps/waits and the educational peripheral model have separate expected-state tests because they intentionally do not run firmware or exception handlers.
