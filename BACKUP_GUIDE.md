# 🗂️ Google Sheets Backup Guide

## Overview

The Bank Transaction Importer now includes powerful backup functionality to protect your Google Sheets data. You can create backups in multiple formats and store them anywhere you want.

## Quick Start

### Default Backup (Recommended)

```bash
dotnet run backup
```

Creates backup in `./bin/Debug/net8.0/backups/`

### Custom Folder Backup

```bash
dotnet run backup "C:\MyBackups"
dotnet run backup "%USERPROFILE%\Documents\BankBackups"
```

### Custom Filename Backup

```bash
dotnet run backup "monthly-backup-october"
```

## 📋 Command Reference

| Command                        | Description             | Example                             |
| ------------------------------ | ----------------------- | ----------------------------------- |
| `dotnet run backup`            | Default backup location | Creates in `./backups/`             |
| `dotnet run backup [folder]`   | Custom folder           | `dotnet run backup "C:\MyBackups"`  |
| `dotnet run backup [filename]` | Custom filename         | `dotnet run backup "my-backup"`     |
| `dotnet run list-backups`      | List all backups        | Shows backups from default location |
| `dotnet run test-connection`   | Test Google Sheets      | Verify connection works             |

## 🎯 Advanced Features

### Environment Variable Support

Set a default backup location that persists across sessions:

**Windows (PowerShell):**

```powershell
$env:BACKUP_PATH = "C:\MyBackups"
dotnet run backup  # Uses C:\MyBackups
```

**Windows (Command Prompt):**

```cmd
set BACKUP_PATH=C:\MyBackups
dotnet run backup
```

### Path Expansion

The backup tool automatically expands environment variables:

```bash
# These all work:
dotnet run backup "%USERPROFILE%\Documents"
dotnet run backup "$env:USERPROFILE\Documents"  # PowerShell
dotnet run backup "~\Documents"                 # Some systems
```

## 📁 Backup Formats

Each backup creates **3 files** in different formats:

### 1. CSV Format (.csv)

- **Best for**: Importing back to Excel/Google Sheets
- **Contains**: Raw spreadsheet data in comma-separated format
- **Use when**: You need to restore data or analyze in spreadsheet software

### 2. JSON Format (.json)

- **Best for**: Programmatic access and detailed metadata
- **Contains**: Structured data with cell references, timestamps, and metadata
- **Use when**: Building tools or need precise cell-by-cell data

### 3. Text Format (.txt)

- **Best for**: Human reading and debugging
- **Contains**: Formatted text with cell references (A1, B2, etc.)
- **Use when**: You want to quickly view backup contents

## 🔧 Backup Locations

### Default Location

```
./bin/Debug/net8.0/backups/
├── sheet-backup_2025_2025-10-01_20-04-36.csv
├── sheet-backup_2025_2025-10-01_20-04-36.json
└── sheet-backup_2025_2025-10-01_20-04-36.txt
```

### Custom Locations

You can store backups anywhere:

- **Documents folder**: `%USERPROFILE%\Documents\BankBackups`
- **Network drive**: `\\server\backups\BankData`
- **Cloud sync folder**: `C:\Users\YourName\OneDrive\Backups`
- **External drive**: `D:\Backups\GoogleSheets`

## 📝 Filename Convention

Backup files follow this pattern:

```
sheet-backup_{SheetName}_{YYYY-MM-DD_HH-mm-ss}.{extension}
```

Examples:

- `sheet-backup_2025_2025-10-01_20-14-05.csv`
- `sheet-backup_Budget_2025-10-01_15-30-22.json`

## 🚨 Best Practices

### 1. Regular Backups

Create backups before:

- Processing new bank transactions
- Making manual changes to your sheet
- Running the import tool for the first time

### 2. Backup Storage

- **Local**: Fast access, risk of hardware failure
- **Cloud**: Safe from hardware issues, requires internet
- **Both**: Recommended - local for speed, cloud for safety

### 3. Retention Strategy

```bash
# Keep daily backups for current month
dotnet run backup "Backups\Daily\$(Get-Date -Format 'yyyy-MM-dd')"

# Keep monthly backups for archives
dotnet run backup "Backups\Monthly\$(Get-Date -Format 'yyyy-MM')"
```

## 🛠️ Convenience Scripts

### Windows Batch File

Double-click `backup.bat` in the project folder for GUI backup creation.

### PowerShell Script

Run `.\backup.ps1` for enhanced PowerShell backup experience.

### Scheduled Backups

Create Windows Task Scheduler entry:

```
Program: dotnet
Arguments: run backup "C:\AutoBackups\$(Get-Date -Format 'yyyy-MM-dd')"
Start in: C:\path\to\BankTransactionImporter\src\BankTransactionImporter
```

## 🔍 Troubleshooting

### "No backups directory found"

- The default backup folder doesn't exist yet
- Run `dotnet run backup` to create your first backup

### "Permission denied" when writing to folder

- Make sure you have write permissions to the target folder
- Try running as administrator
- Choose a different backup location (like Documents folder)

### "Google Sheets connection failed"

- Run `dotnet run test-connection` first
- Check your internet connection
- Verify the service account still has access to your sheet

### Environment variable not working

```bash
# Check if variable is set
echo $env:BACKUP_PATH  # PowerShell
echo %BACKUP_PATH%     # Command Prompt
```

## 📊 Backup Contents

The backup captures:

- ✅ All cell values and formulas
- ✅ Sheet structure and layout
- ✅ Multiple sheets (if specified)
- ✅ Timestamps and metadata
- ❌ Cell formatting (colors, fonts)
- ❌ Charts and images
- ❌ Sheet protection settings

## 🔄 Restoring from Backup

### To Google Sheets:

1. Open Google Sheets
2. Create new spreadsheet
3. Import the `.csv` file
4. Adjust formatting as needed

### To Excel:

1. Open Excel
2. File → Open → Select `.csv` file
3. Use Text Import Wizard if needed

### Programmatic Restore:

Use the `.json` file for precise cell-by-cell restoration with your own scripts.

## 📞 Support

If you encounter issues:

1. Check this guide first
2. Run `dotnet run test-connection`
3. Check the application logs
4. Verify file permissions
5. Try a different backup location

---

_Happy backing up! Your data is now safe! 🛡️_
