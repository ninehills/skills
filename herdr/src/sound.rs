//! Sound notifications for agent state changes.
//!
//! Embeds mp3 files in the binary and plays them via system audio tools.
//! Uses afplay (macOS) or paplay/aplay (Linux) — no Rust audio dependencies.

use std::io::Write;
use std::path::Path;
use std::process::{Command, Output};

use tracing::warn;

static SOUND_DONE: &[u8] = include_bytes!("../assets/sounds/done.mp3");
static SOUND_REQUEST: &[u8] = include_bytes!("../assets/sounds/request.mp3");

/// Which notification sound to play.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Sound {
    /// Agent finished work (transitioned to Idle).
    Done,
    /// Agent needs input (transitioned to Blocked).
    Request,
}

/// Play a notification sound in a background thread.
/// Silently does nothing if no audio player is available.
pub fn play(sound: Sound, config: &crate::config::SoundConfig) {
    let custom_path = config.path_for(sound);
    std::thread::spawn(move || {
        if let Some(path) = custom_path {
            match play_file(&path) {
                Ok(()) => return,
                Err(err) => {
                    warn!(path = %path.display(), sound = ?sound, err = %err, "custom sound playback failed, falling back to built-in sound")
                }
            }
        }

        let data = match sound {
            Sound::Done => SOUND_DONE,
            Sound::Request => SOUND_REQUEST,
        };

        if let Err(err) = play_bytes(data) {
            warn!(sound = ?sound, err = %err, "sound playback failed");
        }
    });
}

fn play_file(path: &Path) -> Result<(), String> {
    match run_player(path) {
        Ok(output) if output.status.success() => Ok(()),
        Ok(output) => Err(format!("player exited with {}", output.status)),
        Err(err) => Err(err),
    }
}

fn play_bytes(data: &[u8]) -> Result<(), String> {
    // Write to a temp file (audio players need a file path)
    let tmp = std::env::temp_dir().join(format!("herdr-sound-{}.mp3", std::process::id()));
    let mut file = std::fs::File::create(&tmp).map_err(|e| e.to_string())?;
    file.write_all(data).map_err(|e| e.to_string())?;
    drop(file);

    let result = run_player(&tmp);

    let _ = std::fs::remove_file(&tmp);

    match result {
        Ok(output) if output.status.success() => Ok(()),
        Ok(output) => Err(format!("player exited with {}", output.status)),
        Err(e) => Err(e),
    }
}

fn run_player(path: &Path) -> Result<Output, String> {
    if cfg!(target_os = "macos") {
        Command::new("afplay")
            .arg(path)
            .output()
            .map_err(|e| format!("no audio player available: {e}"))
    } else {
        // Try paplay (PulseAudio) first, fall back to aplay (ALSA)
        Command::new("paplay")
            .arg(path)
            .output()
            .or_else(|_| Command::new("aplay").arg(path).output())
            .map_err(|e| format!("no audio player available: {e}"))
    }
}
