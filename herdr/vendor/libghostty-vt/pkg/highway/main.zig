extern "c" fn hwy_supported_targets() i64;

pub const Targets = packed struct(i64) {
    // x86_64
    _reserved: u4 = 0,
    avx3_spr: bool = false,
    _reserved_5: u1 = 0,
    avx3_zen4: bool = false,
    avx3_dl: bool = false,
    avx3: bool = false,
    avx2: bool = false,
    _reserved_10: u1 = 0,
    sse4: bool = false,
    ssse3: bool = false,
    _reserved_13: u1 = 0, // SSE3 reserved
    sse2: bool = false,
    _reserved_15_23: u9 = 0,

    // aarch64
    sve2_128: bool = false,
    sve_256: bool = false,
    sve2: bool = false,
    sve: bool = false,
    neon: bool = false,
    neon_without_aes: bool = false,
    _reserved_30_36: u6 = 0,

    // risc-v
    rvv: bool = false,
    _reserved_38_46: u9 = 0,

    // IBM Power
    ppc10: bool = false,
    ppc9: bool = false,
    ppc8: bool = false,
    z15: bool = false,
    z14: bool = false,
    _reserved_52_57: u6 = 0,

    // WebAssembly
    wasm_emu256: bool = false,
    wasm: bool = false,
    _reserved_60_61: u2 = 0,

    // Emulation
    emu128: bool = false,
    scalar: bool = false,
    _reserved_63: u1 = 0,
};

pub fn supported_targets() Targets {
    return @bitCast(hwy_supported_targets());
}

test {
    _ = supported_targets();
}
