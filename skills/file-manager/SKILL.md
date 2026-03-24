---
name: file-manager
description: "Use when the user asks to find files, organize folders, clean up disk space, locate duplicates, or manage their file system. Search by name, type, size, or date. Move, rename, compress, and clean up files and directories."
allowed-tools: "Bash, Read, Write, Glob, Grep"
---

# File Manager Skill

Help users find and organize files on their computer.

## Find Files

```bash
# By name (case-insensitive)
find ~/Desktop ~/Documents ~/Downloads -iname "*report*" -type f 2>/dev/null

# By extension
find ~/Downloads -name "*.pdf" -type f

# By size (larger than 100MB)
find ~ -size +100M -type f 2>/dev/null | head -20

# Recently modified (last 7 days)
find ~/Documents -mtime -7 -type f | head -20

# Duplicates by size (potential dupes)
find ~/Downloads -type f -exec ls -la {} + | sort -k5 -n | uniq -d -f4
```

## Organize

```bash
# Move all PDFs from Downloads to Documents
mv ~/Downloads/*.pdf ~/Documents/

# Create dated folder and move files
mkdir -p ~/Documents/$(date +%Y-%m-%d)

# Rename files (pattern)
for f in *.jpeg; do mv "$f" "${f%.jpeg}.jpg"; done
```

## Cleanup

Always preview before deleting:

```bash
# 1. Preview: show large files in Downloads
du -sh ~/Downloads/* | sort -rh | head -20

# 2. Preview: list old downloads (older than 30 days) — do NOT delete yet
find ~/Downloads -mtime +30 -type f

# 3. Only after user confirms, delete specific files
# rm ~/Downloads/specific-file.zip

# Empty trash (macOS)
rm -rf ~/.Trash/*
```

## Compress/Extract

```bash
# Create zip
zip -r archive.zip folder/

# Create tar.gz
tar czf archive.tar.gz folder/

# Extract
unzip archive.zip
tar xzf archive.tar.gz
```

## Tips
- Always use `trash` over `rm` when available (recoverable)
- Preview file lists before bulk operations
- Ask before deleting — show what would be affected first
