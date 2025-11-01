# Google Sheets Backup Tool

This tool creates backups of your Google Sheets data in multiple formats for safekeeping and data recovery purposes.

## Features

✅ **Multiple Formats**: Creates backups in CSV, JSON, and Text formats  
✅ **Automatic Timestamping**: Each backup has a unique timestamp  
✅ **Complete Data Capture**: Downloads all data from the specified sheet  
✅ **Metadata Preservation**: JSON format includes spreadsheet metadata  
✅ **Easy Recovery**: CSV format can be imported back to Google Sheets

## Usage

### Create a Backup

```bash
# Create a backup with automatic filename
dotnet run backup

# The backup will be saved in the bin/Debug/net8.0/backups/ directory
```

### List Available Backups

```bash
dotnet run list-backups
```

### Test Connection

```bash
dotnet run test-connection
```

## Backup Formats

### 1. CSV Format (`.csv`)

- Standard comma-separated values
- Can be imported directly into Google Sheets or Excel
- Best for data recovery and migration

### 2. JSON Format (`.json`)

- Structured data with complete metadata
- Includes spreadsheet ID, sheet name, timestamp
- Each cell has column letter and index information
- Best for programmatic access and detailed analysis

### 3. Text Format (`.txt`)

- Human-readable formatted output
- Shows cell references (A1, B2, etc.)
- Best for manual review and debugging

## Backup Location

Backups are automatically saved to:

```
bin/Debug/net8.0/backups/
```

Filename format:

```
sheet-backup_{SheetName}_{YYYY-MM-DD_HH-mm-ss}.{extension}
```

Example:

```
sheet-backup_2025_2025-10-01_20-04-36.csv
sheet-backup_2025_2025-10-01_20-04-36.json
sheet-backup_2025_2025-10-01_20-04-36.txt
```

## Configuration

Make sure your `appsettings.json` is properly configured:

```json
{
	"GoogleSheets": {
		"SpreadsheetId": "your-actual-spreadsheet-id-here",
		"DefaultSheetName": "2025",
		"CredentialsPath": "config/google-credentials.json"
	}
}
```

## Requirements

1. **Google Service Account**: Properly configured in `config/google-credentials.json`
2. **Sheet Access**: Service account must have read access to your Google Sheet
3. **Internet Connection**: Required to download data from Google Sheets

## Troubleshooting

If backup fails, check:

1. ✉️ **Sheet Sharing**: Make sure your Google Sheet is shared with:  
   `sa-banktransactionimporter@banktransactionsimporter.iam.gserviceaccount.com`

2. 🔑 **Credentials File**: Verify `config/google-credentials.json` exists

3. 📋 **Spreadsheet ID**: Check the ID in `appsettings.json` is correct

4. 📄 **Sheet Name**: Ensure the sheet name (e.g., "2025") exists in your spreadsheet

5. 🌐 **Network**: Verify internet connectivity

## Security

- Backup files contain your financial data - keep them secure
- The `config/google-credentials.json` file is automatically added to `.gitignore`
- Consider encrypting backup files if storing them long-term
- Regularly rotate your Google service account keys

## Best Practices

- **Regular Backups**: Create backups before making major changes
- **Version Control**: The timestamp in filenames helps track versions
- **Storage**: Consider copying important backups to a secure location
- **Cleanup**: Periodically remove old backups to save disk space

## Example Workflow

```bash
# 1. Test your connection first
dotnet run test-connection

# 2. Create a backup before processing transactions
dotnet run backup

# 3. Process your transactions (if satisfied with backup)
dotnet run

# 4. Create another backup after processing (optional)
dotnet run backup
```
