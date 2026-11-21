# STM32L432KC educational emulator

An educational microcontroller lab originally built to help University of Warwick engineering students learn without physical boards. The repaired Windows application and the interactive browser demo share one C# execution core.

**[Open the browser lab](https://jideoyelayo.com/demos/stm32/)** · [Model, instruction reference and limitations](docs/model.md)

The browser lab includes editable assembly, Run/Pause/Step/Reset controls, a 16-register view, N/Z/C/V flags, a memory inspector, virtual GPIO inputs and LEDs, and TIM1 PWM/TIM7 counter examples. Programs execute locally in the browser without a backend or account.

## Run and build

Use the **.NET 10 SDK**. `global.json` permits current .NET 10 feature releases. The original project targeted .NET 6; the old README's .NET Framework 4.8 claim was incorrect.

```sh
dotnet test tests/Stm32Emulator.Core.Tests
dotnet publish src/Stm32Emulator.Web -c Release -o artifacts/browser
python3 scripts/package-browser.py
```

Serve `artifacts/browser/wwwroot` at `/demos/stm32/`, matching its published `<base>` path. For example, create a local server root with that directory mounted or symlinked as `demos/stm32`, then run `python3 -m http.server --directory <server-root>`. Open `/demos/stm32/` rather than `/`.

On **Windows**, run the retained desktop application:

```sh
dotnet run --project test1/test1.csproj
```

The desktop starts with the LED program. Load text or a local assembly file, step or run it, toggle GPIOB inputs, and open More Options for timer/memory inspection. Reset preserves editor text and clears the loaded machine state. The desktop source is compiled on Windows CI; macOS/Linux can run the core and browser projects.

## Architecture and repairs

- `src/Stm32Emulator.Core`: parser, registers/flags, sparse byte-addressed memory bus, selected GPIO/RCC/timers and cancellable execution controller.
- `src/Stm32Emulator.Web`: standalone Blazor WebAssembly workbench and the three teaching examples.
- `test1`: Windows Forms adapter using the same core.
- `tests/Stm32Emulator.Core.Tests`: expected-state regression vectors and execution lifecycle tests.

Repairs replace multi-gigabyte memory/address arrays, substring/register decoding, incorrect flags and byte accesses, broken branch/BX destinations, PB0's stuck toggle, and endless timer tasks. The browser examples cover LED writes, button-controlled output, and TIM1 PWM with TIM7 counting.

## Scope

This is an **assembly-text interpreter with selected peripheral simulation**, not a complete or cycle-accurate Cortex-M4 emulator. It does not load firmware binaries, dispatch interrupts, execute floating-point arithmetic, or support every Thumb instruction/peripheral. Unsupported input produces an explicit source-line error. See [the model](docs/model.md) for precise syntax, register behavior, known simplifications and historical sample migration.

Source is limited to 64 KiB / 2,000 instructions. Run pauses after 100,000 source instructions and can be resumed. All task state stays local to the browser; keep a copy of edited source before leaving the page.

## Release and portfolio integration

CI tests the core, builds the browser output, and compiles the Windows adapter. A `demo-v*` tag publishes `stm32-browser.tar.gz` only after both jobs pass. The portfolio pins that GitHub release archive and its SHA-256, then installs it into `build/demos/stm32/`. Updating the demo requires a new release and an explicit portfolio manifest update; visitors do not fetch a changing branch at runtime.

The original README and original sample programs remain available for historical context in [docs/original-readme.md](docs/original-readme.md), `testingLEDs.txt`, and `testingPWMandTimers.txt`. Historical claims of full instruction coverage or a packaged release are not current guarantees. No license terms are changed by this update.
