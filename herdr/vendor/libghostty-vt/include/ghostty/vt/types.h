/**
 * @file types.h
 *
 * Common types, macros, and utilities for libghostty-vt.
 */

#ifndef GHOSTTY_VT_TYPES_H
#define GHOSTTY_VT_TYPES_H

#include <stddef.h>
#include <stdint.h>

// Symbol visibility for shared library builds. On Windows, functions
// are exported from the DLL when building and imported when consuming.
// On other platforms with GCC/Clang, functions are marked with default
// visibility so they remain accessible when the library is built with
// -fvisibility=hidden. For static library builds, define GHOSTTY_STATIC
// before including this header to make this a no-op.
#ifndef GHOSTTY_API
#if defined(GHOSTTY_STATIC)
  #define GHOSTTY_API
#elif defined(_WIN32) || defined(_WIN64)
  #ifdef GHOSTTY_BUILD_SHARED
    #define GHOSTTY_API __declspec(dllexport)
  #else
    #define GHOSTTY_API __declspec(dllimport)
  #endif
#elif defined(__GNUC__) && __GNUC__ >= 4
  #define GHOSTTY_API __attribute__((visibility("default")))
#else
  #define GHOSTTY_API
#endif
#endif

/**
 * Result codes for libghostty-vt operations.
 */
typedef enum {
    /** Operation completed successfully */
    GHOSTTY_SUCCESS = 0,
    /** Operation failed due to failed allocation */
    GHOSTTY_OUT_OF_MEMORY = -1,
    /** Operation failed due to invalid value */
    GHOSTTY_INVALID_VALUE = -2,
    /** Operation failed because the provided buffer was too small */
    GHOSTTY_OUT_OF_SPACE = -3,
    /** The requested value has no value */
    GHOSTTY_NO_VALUE = -4,
} GhosttyResult;

/**
 * A borrowed byte string (pointer + length).
 *
 * The memory is not owned by this struct. The pointer is only valid
 * for the lifetime documented by the API that produces or consumes it.
 */
typedef struct {
  /** Pointer to the string bytes. */
  const uint8_t* ptr;

  /** Length of the string in bytes. */
  size_t len;
} GhosttyString;

/**
 * Initialize a sized struct to zero and set its size field.
 *
 * Sized structs use a `size` field as the first member for ABI
 * compatibility. This macro zero-initializes the struct and sets the
 * size field to `sizeof(type)`, which allows the library to detect
 * which version of the struct the caller was compiled against.
 *
 * @param type The struct type to initialize
 * @return A zero-initialized struct with the size field set
 *
 * Example:
 * @code
 * GhosttyFormatterTerminalOptions opts = GHOSTTY_INIT_SIZED(GhosttyFormatterTerminalOptions);
 * opts.emit = GHOSTTY_FORMATTER_FORMAT_PLAIN;
 * opts.trim = true;
 * @endcode
 */
#define GHOSTTY_INIT_SIZED(type) \
  ((type){ .size = sizeof(type) })

/**
 * Return a pointer to a null-terminated JSON string describing the
 * layout of every C API struct for the current target.
 *
 * This is primarily useful for language bindings that can't easily
 * set C struct fields and need to do so via byte offsets. For example,
 * WebAssembly modules can't share struct definitions with the host.
 *
 * Example (abbreviated):
 * @code{.json}
 * {
 *   "GhosttyMouseEncoderSize": {
 *     "size": 40,
 *     "align": 8,
 *     "fields": {
 *       "size":           { "offset": 0,  "size": 8, "type": "u64" },
 *       "screen_width":   { "offset": 8,  "size": 4, "type": "u32" },
 *       "screen_height":  { "offset": 12, "size": 4, "type": "u32" },
 *       "cell_width":     { "offset": 16, "size": 4, "type": "u32" },
 *       "cell_height":    { "offset": 20, "size": 4, "type": "u32" },
 *       "padding_top":    { "offset": 24, "size": 4, "type": "u32" },
 *       "padding_bottom": { "offset": 28, "size": 4, "type": "u32" },
 *       "padding_right":  { "offset": 32, "size": 4, "type": "u32" },
 *       "padding_left":   { "offset": 36, "size": 4, "type": "u32" }
 *     }
 *   }
 * }
 * @endcode
 *
 * The returned pointer is valid for the lifetime of the process.
 *
 * @return Pointer to the null-terminated JSON string.
 */
GHOSTTY_API const char *ghostty_type_json(void);

#endif /* GHOSTTY_VT_TYPES_H */
