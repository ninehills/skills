use std::env;
use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;

fn zig_target(target: &str) -> &str {
    match target {
        "x86_64-unknown-linux-gnu" => "x86_64-linux-gnu",
        "aarch64-unknown-linux-gnu" => "aarch64-linux-gnu",
        "x86_64-unknown-linux-musl" => "x86_64-linux-musl",
        "aarch64-unknown-linux-musl" => "aarch64-linux-musl",
        "x86_64-apple-darwin" => "x86_64-macos",
        "aarch64-apple-darwin" => "aarch64-macos",
        other => panic!("unsupported target for libghostty-vt build: {other}"),
    }
}

fn env_bool(name: &str) -> Option<bool> {
    match env::var(name) {
        Ok(value) => match value.to_ascii_lowercase().as_str() {
            "1" | "true" | "yes" | "on" => Some(true),
            "0" | "false" | "no" | "off" => Some(false),
            other => panic!("invalid boolean value for {name}: {other}"),
        },
        Err(env::VarError::NotPresent) => None,
        Err(err) => panic!("failed to read {name}: {err}"),
    }
}

/// Extract the final archive path for a named Zig runtime archive from
/// verbose compiler/linker output.
fn find_archive_path(output: &str, archive_name: &str) -> Option<PathBuf> {
    output
        .split_whitespace()
        .map(|token| token.trim_matches(|c| matches!(c, '"' | '\'')))
        .filter(|token| token.ends_with(archive_name) && token.contains('/'))
        .map(PathBuf::from)
        .last()
}

/// Ask Zig which musl-compatible C++ runtime archives it would use for a
/// trivial target build.
///
/// This is necessary because `cargo:rustc-link-lib=stdc++` on a musl target
/// resolves through the host toolchain and produces a mixed musl/glibc binary,
/// while Zig's musl C++ driver resolves to its bundled libc++/libc++abi
/// archives.
fn zig_musl_cpp_runtime_archives(out_dir: &Path, zig_target: &str) -> (PathBuf, PathBuf) {
    let probe_dir = out_dir.join("zig-cpp-runtime-probe");
    fs::create_dir_all(&probe_dir).expect("failed to create Zig C++ runtime probe dir");

    let source = probe_dir.join("probe.cpp");
    fs::write(&source, "int main() { return 0; }\n")
        .expect("failed to write Zig C++ runtime probe source");

    let binary = probe_dir.join("probe");
    let local_cache = probe_dir.join("local-cache");
    let global_cache = probe_dir.join("global-cache");

    let output = Command::new("zig")
        .arg("c++")
        .arg("-target")
        .arg(zig_target)
        .arg("-v")
        .arg(&source)
        .arg("-o")
        .arg(&binary)
        .env("ZIG_LOCAL_CACHE_DIR", &local_cache)
        .env("ZIG_GLOBAL_CACHE_DIR", &global_cache)
        .output()
        .expect("failed to execute Zig C++ runtime probe");

    let stdout = String::from_utf8_lossy(&output.stdout);
    let stderr = String::from_utf8_lossy(&output.stderr);
    let combined = format!("{stdout}\n{stderr}");

    assert!(
        output.status.success(),
        "Zig C++ runtime probe failed: {}\nstdout:\n{}\nstderr:\n{}",
        output.status,
        stdout,
        stderr
    );

    let libcxxabi = find_archive_path(&combined, "libc++abi.a").unwrap_or_else(|| {
        panic!(
            "failed to locate Zig musl libc++abi archive path for target {zig_target}; zig output was:\n{combined}"
        )
    });
    let libcxx = find_archive_path(&combined, "libc++.a").unwrap_or_else(|| {
        panic!(
            "failed to locate Zig musl libc++ archive path for target {zig_target}; zig output was:\n{combined}"
        )
    });

    (libcxxabi, libcxx)
}

fn main() {
    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed=vendor/libghostty-vt.vendor.json");
    println!("cargo:rerun-if-changed=vendor/libghostty-vt/build.zig");
    println!("cargo:rerun-if-changed=vendor/libghostty-vt/build.zig.zon");
    println!("cargo:rerun-if-changed=vendor/libghostty-vt/VERSION");
    println!("cargo:rerun-if-env-changed=LIBGHOSTTY_VT_OPTIMIZE");
    println!("cargo:rerun-if-env-changed=LIBGHOSTTY_VT_SIMD");

    let manifest_dir = PathBuf::from(env::var("CARGO_MANIFEST_DIR").expect("CARGO_MANIFEST_DIR"));
    let out_dir = PathBuf::from(env::var("OUT_DIR").expect("OUT_DIR"));
    let vendored_dir = manifest_dir.join("vendor/libghostty-vt");
    let optimize = env::var("LIBGHOSTTY_VT_OPTIMIZE").unwrap_or_else(|_| "ReleaseFast".into());
    let simd = env_bool("LIBGHOSTTY_VT_SIMD").unwrap_or(true);
    let target = env::var("TARGET").expect("TARGET");
    let zig_target = zig_target(&target);
    let version_string = fs::read_to_string(vendored_dir.join("VERSION"))
        .expect("failed to read vendored libghostty-vt VERSION")
        .trim()
        .to_string();

    let status = Command::new("zig")
        .arg("build")
        .arg("-Demit-lib-vt")
        .arg(format!("-Doptimize={optimize}"))
        .arg(format!("-Dsimd={simd}"))
        .arg(format!("-Dtarget={zig_target}"))
        .arg(format!("-Dversion-string={version_string}"))
        .current_dir(&vendored_dir)
        .status()
        .expect("failed to execute zig build for vendored libghostty-vt");
    assert!(
        status.success(),
        "zig build for vendored libghostty-vt failed: {status}"
    );

    let lib_dir = vendored_dir.join("zig-out/lib");
    println!("cargo:rustc-link-search=native={}", lib_dir.display());
    if target.contains("apple-darwin") {
        let static_lib = lib_dir.join("libghostty-vt.a");
        println!("cargo:rustc-link-arg={}", static_lib.display());
        if simd {
            println!("cargo:rustc-link-lib=dylib=c++");
        }
    } else {
        println!("cargo:rustc-link-lib=static=ghostty-vt");
        if target.contains("linux") && simd {
            if target.contains("musl") {
                // Keep musl SIMD builds entirely in Zig's musl/libc++ runtime
                // world. Asking Cargo to link `stdc++` here resolves through the
                // host toolchain and reintroduces the crashing mixed-runtime
                // artifact shape.
                let (libcxxabi, libcxx) = zig_musl_cpp_runtime_archives(&out_dir, zig_target);
                let libcxxabi_dir = libcxxabi
                    .parent()
                    .expect("libc++abi archive should have a parent dir");
                let libcxx_dir = libcxx
                    .parent()
                    .expect("libc++ archive should have a parent dir");

                println!("cargo:rustc-link-search=native={}", libcxxabi_dir.display());
                if libcxx_dir != libcxxabi_dir {
                    println!("cargo:rustc-link-search=native={}", libcxx_dir.display());
                }
                println!("cargo:rustc-link-lib=static=c++abi");
                println!("cargo:rustc-link-lib=static=c++");
            } else {
                println!("cargo:rustc-link-lib=dylib=stdc++");
            }
        }
    }
}
