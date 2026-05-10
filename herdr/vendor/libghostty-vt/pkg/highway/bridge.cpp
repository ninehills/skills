#include <hwy/targets.h>
#include <stdint.h>

extern "C" {

int64_t hwy_supported_targets() {
  return HWY_SUPPORTED_TARGETS;
}
}
