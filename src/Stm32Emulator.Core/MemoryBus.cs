namespace Stm32Emulator.Core;

/// <summary>64 KiB educational SRAM window and explicitly supported peripheral registers.</summary>
public sealed class MemoryBus
{
    public const uint RamBase = 0x20000000;
    public const uint RamSize = 64 * 1024;
    public const uint RccAhb2Enr = 0x4002104C;
    public const uint RccApb1Enr1 = 0x40021058;
    public const uint RccApb2Enr = 0x40021060;
    public const uint GpioA = 0x48000000;
    public const uint GpioB = 0x48000400;
    public const uint Tim1 = 0x40012C00;
    public const uint Tim7 = 0x40001400;
    private readonly Dictionary<uint, byte> ram = [];
    private readonly Dictionary<uint, byte> flash = [];
    private (uint Address, int Width)? reservation;
    private readonly Dictionary<uint, uint> registers = [];
    private uint inputA, inputB;
    private uint prescaler1, prescaler7;
    private static readonly uint[] GpioOffsets = [0, 4, 8, 12, 16, 20, 24, 32, 36];
    private static readonly uint[] TimerOffsets = [0, 12, 16, 20, 24, 32, 36, 40, 44, 48, 52, 68];

    public int AllocatedRamBytes => ram.Count;
    public ulong Ticks { get; private set; }

    public MemoryBus() => Reset();

    public void Reset()
    {
        ram.Clear(); flash.Clear(); reservation = null; registers.Clear(); inputA = inputB = prescaler1 = prescaler7 = 0; Ticks = 0;
        foreach (var address in new[] { RccAhb2Enr, RccApb1Enr1, RccApb2Enr }) registers[address] = 0;
        foreach (var port in new[] { GpioA, GpioB })
            foreach (var offset in GpioOffsets) registers[port + offset] = offset == 0 ? uint.MaxValue : 0;
        foreach (var timer in new[] { Tim1, Tim7 })
            foreach (var offset in TimerOffsets.Where(offset => timer == Tim1 || offset is 0 or 12 or 16 or 20 or 36 or 40 or 44))
                registers[timer + offset] = offset == 44 ? 65535u : 0u;
    }

    public void LoadData(IReadOnlyDictionary<uint, byte> image)
    {
        foreach (var (address, value) in image)
            if (InRam(address)) { if (value != 0) ram[address] = value; }
            else if (address >= AssemblyProgram.CodeBase && address - AssemblyProgram.CodeBase < AssemblyProgram.CodeSize) flash[address] = value;
            else throw new InvalidOperationException("Initial data outside mapped memory.");
    }
    public void ClearExclusive() => reservation = null;
    public void ReserveExclusive(uint address, int width)
    { ValidateAccess(address, width, false, true); if (!InRam(address)) throw new InvalidOperationException("Exclusive accesses require SRAM."); reservation = (address, width); }
    public bool ConsumeExclusive(uint address, int width)
    { var success = reservation == (address, width); reservation = null; return success; }
    private void Invalidate(uint address, int size)
    { if (reservation is { } r && (ulong)address < (ulong)r.Address + (uint)r.Width && (ulong)r.Address < (ulong)address + (uint)size) reservation = null; }
    public void ValidateAccess(uint address, int width, bool write, bool aligned = false)
    {
        if (width is not (1 or 2 or 4 or 8) || (ulong)address + (uint)width > 0x100000000UL) throw new InvalidOperationException("Invalid memory access extent.");
        if (aligned && (address & (uint)(Math.Min(width, 4) - 1)) != 0) throw new InvalidOperationException("This access requires natural alignment.");
        if (InRam(address)) { if ((ulong)address + (uint)width > RamBase + RamSize) throw new InvalidOperationException("Access crosses the SRAM boundary."); return; }
        if (address >= AssemblyProgram.CodeBase && address - AssemblyProgram.CodeBase < AssemblyProgram.CodeSize)
        {
            if (write) throw new InvalidOperationException("Code/literal memory is read-only.");
            for (var i = 0; i < width; i++) if (!flash.ContainsKey(address + (uint)i)) throw new InvalidOperationException("Instruction slots have no binary encoding; only declared data/literals can be read.");
            return;
        }
        if ((address & (uint)(Math.Min(width, 4) - 1)) != 0) throw new InvalidOperationException("Peripheral access must be naturally aligned.");
        for (var i = 0; i < width; i++)
        {
            var word = (address + (uint)i) & ~3u;
            if (!registers.ContainsKey(word)) throw new InvalidOperationException($"Unmapped address 0x{address:X8}.");
            if (write && Clocked(word) && (word == GpioA + 16 || word == GpioB + 16)) throw new InvalidOperationException("GPIO IDR is read-only.");
        }
    }
    public ushort ReadHalf(uint address)
    { ValidateAccess(address, 2, false); return (ushort)(ReadByte(address) | ReadByte(address + 1) << 8); }
    public void WriteHalf(uint address, ushort value)
    {
        ValidateAccess(address, 2, true); if (InRam(address)) { WriteByte(address, (byte)value); WriteByte(address + 1, (byte)(value >> 8)); }
        else { var aligned = address & ~3u; var shift = (int)(address & 3) * 8; WriteWord(aligned, ReadWord(aligned) & ~(65535u << shift) | (uint)value << shift); }
    }
    private static bool InRam(uint address) => address >= RamBase && address - RamBase < RamSize;
    private static void Aligned(uint address)
    { if ((address & 3) != 0) throw new InvalidOperationException($"Word address 0x{address:X8} must be four-byte aligned. Use LDRB/STRB for bytes."); }
    private bool Clocked(uint address) => address switch
    {
        >= GpioA and < GpioA + 0x400 => (registers[RccAhb2Enr] & 1) != 0,
        >= GpioB and < GpioB + 0x400 => (registers[RccAhb2Enr] & 2) != 0,
        >= Tim1 and < Tim1 + 0x100 => (registers[RccApb2Enr] & (1u << 11)) != 0,
        >= Tim7 and < Tim7 + 0x100 => (registers[RccApb1Enr1] & (1u << 5)) != 0,
        _ => true
    };

    public uint ReadWord(uint address)
    {
        ValidateAccess(address, 4, false);
        if (InRam(address) || flash.ContainsKey(address)) return (uint)(ReadByte(address) | ReadByte(address + 1) << 8 | ReadByte(address + 2) << 16 | ReadByte(address + 3) << 24);
        if (!registers.TryGetValue(address, out var value)) throw new InvalidOperationException($"Unmapped address 0x{address:X8}. Only SRAM and the documented GPIO/RCC/TIM registers are supported.");
        if (!Clocked(address)) return 0;
        if (address == GpioA + 16) return ReadPins(GpioA, inputA);
        if (address == GpioB + 16) return ReadPins(GpioB, inputB);
        if (address == GpioA + 24 || address == GpioB + 24 || address == Tim1 + 20 || address == Tim7 + 20) return 0;
        return value;
    }

    public byte ReadByte(uint address)
    {
        ValidateAccess(address, 1, false);
        if (InRam(address)) return ram.GetValueOrDefault(address);
        if (flash.TryGetValue(address, out var value)) return value;
        return (byte)(ReadWord(address & ~3u) >> (int)((address & 3) * 8));
    }

    public void WriteByte(uint address, byte value)
    {
        ValidateAccess(address, 1, true); Invalidate(address, 1);
        if (InRam(address)) { if (value == 0) ram.Remove(address); else ram[address] = value; return; }
        var aligned = address & ~3u;
        var shift = (int)((address & 3) * 8);
        WriteWord(aligned, (ReadWord(aligned) & ~(255u << shift)) | (uint)value << shift);
    }

    public void WriteWord(uint address, uint value)
    {
        ValidateAccess(address, 4, true); Invalidate(address, 4);
        if (InRam(address))
        {
            for (var i = 0; i < 4; i++) WriteByte(address + (uint)i, (byte)(value >> (8 * i)));
            return;
        }
        if (!registers.ContainsKey(address)) throw new InvalidOperationException($"Unmapped address 0x{address:X8}.");
        if (!Clocked(address)) return;
        if (address == GpioA + 16 || address == GpioB + 16) throw new InvalidOperationException("GPIO IDR is read-only. Change inputs with the board buttons.");
        foreach (var port in new[] { GpioA, GpioB })
        {
            if (address == port + 24)
            { registers[port + 20] = ((registers[port + 20] & ~(value >> 16)) | (value & 0xFFFF)) & 0xFFFF; return; }
            if (address == port + 20) { registers[address] = value & 0xFFFF; return; }
        }
        foreach (var timer in new[] { Tim1, Tim7 })
        {
            if (address == timer && (value & ~0x81u) != 0) throw new InvalidOperationException("Only timer up-counting with CEN/ARPE is supported.");
            if (address == timer + 16) { registers[address] &= value & 1; return; } // UIF is cleared by writing zero
            if (address == timer + 20)
            {
                if ((value & 1) != 0)
                {
                    registers[timer + 36] = 0; registers[timer + 16] |= 1;
                    if (timer == Tim1) prescaler1 = 0; else prescaler7 = 0;
                }
                return;
            }
            if (address >= timer + 36 && address <= timer + 52) value &= 0xFFFF;
        }
        if (address == Tim1 + 48 && value != 0) throw new InvalidOperationException("Timer repetition counting is unsupported.");
        if (address == Tim1 + 24 && value != 0 && value != 0x60) throw new InvalidOperationException("Only TIM1 channel 1 PWM mode 1 (CCMR1 = 0x60) is supported.");
        registers[address] = value;
    }

    public void SetInputPin(char port, int pin, bool high)
    {
        if (pin is < 0 or > 15 || port is not ('A' or 'B')) throw new ArgumentOutOfRangeException(nameof(pin));
        var mask = 1u << pin;
        if (port == 'A') inputA = high ? inputA | mask : inputA & ~mask;
        else inputB = high ? inputB | mask : inputB & ~mask;
    }

    private uint ReadPins(uint port, uint external)
    {
        if (!Clocked(port)) return 0;
        uint result = 0;
        for (var pin = 0; pin < 16; pin++)
        {
            var mode = (registers[port] >> (pin * 2)) & 3;
            var high = mode == 1 ? (registers[port + 20] & (1u << pin)) != 0 : mode == 0 && (external & (1u << pin)) != 0;
            if (port == GpioA && pin == 8 && mode == 2 && (registers[port + 36] & 15) == 1) high = PwmHigh;
            if (high) result |= 1u << pin;
        }
        return result;
    }

    public bool PinHigh(char port, int pin)
    {
        if (pin is < 0 or > 15 || port is not ('A' or 'B')) throw new ArgumentOutOfRangeException(nameof(pin));
        return (ReadWord((port == 'A' ? GpioA : GpioB) + 16) & (1u << pin)) != 0;
    }

    public bool PwmEnabled => Clocked(Tim1) && (registers[Tim1] & 1) != 0 &&
        (registers[Tim1 + 32] & 1) != 0 && (registers[Tim1 + 68] & 0x8000) != 0 && ((registers[Tim1 + 24] >> 4) & 7) == 6;
    public double PwmDuty => PwmEnabled ? Math.Min(1, (double)registers[Tim1 + 52] / (registers[Tim1 + 44] + 1)) : 0;
    public bool PwmHigh => PwmEnabled && registers[Tim1 + 36] < registers[Tim1 + 52];

    public void AdvanceTicks(int count = 1)
    {
        if (count is < 0 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(count));
        for (var i = 0; i < count; i++)
        {
            Tick(Tim1, ref prescaler1); Tick(Tim7, ref prescaler7); Ticks++;
        }
    }

    private void Tick(uint timer, ref uint prescaler)
    {
        if (!Clocked(timer) || (registers[timer] & 1) == 0) return;
        if (++prescaler <= registers[timer + 40]) return;
        prescaler = 0;
        if (registers[timer + 36] >= registers[timer + 44])
        { registers[timer + 36] = 0; registers[timer + 16] |= 1; }
        else registers[timer + 36]++;
    }
}
