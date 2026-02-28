---
name: file-manager
description: Find, organize, and manage files on the user's computer. Search by name, type, size, or date. Move, rename, compress, and clean up files.
allowed-tools:
  - Bash
  - Read
  - Write
  - Glob
  - Grep
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

```bash
# Show large files in Downloads
du -sh ~/Downloads/* | sort -rh | head -20

# Empty trash (macOS)
rm -rf ~/.Trash/*

# Clear old downloads (older than 30 days)
find ~/Downloads -mtime +30 -type f
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
