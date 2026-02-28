---
name: system-info
description: Get system information — OS version, disk usage, memory, running processes, network status. Use when users ask about their computer status or system health.
allowed-tools:
  - Bash
---

# System Info Skill

Gather system information using standard command-line tools.

## Commands

```bash
# OS version
sw_vers                              # macOS
uname -a                             # Any Unix

# Disk usage
df -h                                # All volumes
du -sh ~/Desktop ~/Documents ~/Downloads  # Common folders

# Memory
vm_stat                              # macOS memory stats
top -l 1 -s 0 | head -12            # Quick system overview

# CPU
sysctl -n machdep.cpu.brand_string   # CPU model
sysctl -n hw.ncpu                    # CPU count

# Running processes (top CPU/memory)
ps aux --sort=-%cpu | head -10       # Top CPU
ps aux --sort=-%mem | head -10       # Top memory

# Network
ifconfig | grep "inet "              # IP addresses
networksetup -getairportnetwork en0  # WiFi network (macOS)
ping -c 1 8.8.8.8                    # Connectivity check

# Battery (MacBook)
pmset -g batt                        # Battery status

# Uptime
uptime
```

## Tips
- Use `sw_vers` for macOS version details
- Use `system_profiler SPHardwareDataType` for full hardware info
- Use `lsof -i :PORT` to check what's running on a port
