# Bank Transaction Importer

A .NET console application that imports Swedish bank transaction CSV files and maps them to Google Sheets budget categories.

## Features

- ✅ Parse Swedish bank transaction CSV files with encoding fallback support
- ✅ Map transactions to budget categories using configurable rules
- ✅ Support for Swedish date and number formats
- ✅ Automatic year-based sheet mapping (transactions → "2025" sheet)
- ✅ Detailed logging and transaction mapping results
- ✅ Dry-run mode for preview before updating
- ✅ Google Sheets integration with real API support
- ✅ Backup and restore functionality
- ✅ Batch cell updates for improved performance

## Setup

### 1. Build the Application

```bash
cd src/BankTransactionImporter
dotnet build
```

### 2. Configuration

Configure the application by editing the files in the `config/` folder:

- **`appsettings.json`**: Main application settings
- **`mapping-rules.json`**: Transaction-to-category mapping rules

### 3. Google Sheets Setup

The application supports full Google Sheets integration:

1. Create a Google Cloud project
2. Enable the Google Sheets API
3. Create a Service Account and download credentials JSON
4. Place credentials at `src/BankTransactionImporter/config/google-credentials.json`
5. Update `SpreadsheetId` in `appsettings.json` with your sheet ID

## Usage

### Transaction Processing

```bash
# Process a transaction file (dry-run mode)
dotnet run -- --file "../../data/Transaktioner_2025-10-01_17-03-34.csv" --dry-run

# Process and update Google Sheets
dotnet run -- --file "../../data/Transaktioner_2025-10-01_17-03-34.csv"

# Specify a different sheet name (overrides automatic year detection)
dotnet run -- --file "../../data/transactions.csv" --sheet "2025"
```

### Backup Commands

```bash
# Create backup (default location)
dotnet run backup

# Create backup to custom folder
dotnet run backup C:\MyBackups

# List available backups
dotnet run list-backups
```

### Utility Commands

```bash
# Test Google Sheets connection
dotnet run test-connection

# Show help
dotnet run help
```

### Command Line Options

**Transaction Processing:**
- `--file, -f <path>`: Path to the CSV transaction file (required)
- `--sheet, -s <name>`: Name of the Google Sheets tab (optional, overrides auto year detection)
- `--dry-run, -d`: Preview changes without updating (optional)
- `--help, -h`: Show help message

**Special Commands:**
- `backup [path]`: Create backup of Google Sheets data
- `list-backups`: List all available backups
- `test-connection`: Test Google Sheets API connection
- `help`: Show detailed help message

## CSV File Format

The application expects Swedish bank transaction files with the following columns:

- Radnummer (Row number)
- Clearingnummer (Clearing number)
- Kontonummer (Account number)
- Produkt (Product)
- Valuta (Currency)
- Bokföringsdag (Booking date) - supports encoding variants like "Bokf�ringsdag"
- Transaktionsdag (Transaction date)
- Valutadag (Currency date)
- Referens (Reference)
- Beskrivning (Description)
- Belopp (Amount)
- Bokfört saldo (Booked balance) - supports encoding variants like "Bokf�rt saldo"

**Note:** The application automatically handles Swedish character encoding issues common in bank export files.

## Mapping Rules

Transaction mapping is based on the `Reference` or `Description` fields. Configure mappings in `config/mapping-rules.json`:

```json
{
	"mappingRules": {
		"TELE2": "Tele2",
		"MAT": "Mat",
		"HYRA": "Hyra"
	}
}
```

## Project Structure

```
BankTransactionImporter/
├── src/BankTransactionImporter/          # Main application
│   ├── Models/                           # Data models
│   ├── Services/                         # Business logic services
│   ├── Configuration/                    # Configuration classes
│   ├── Application.cs                    # Main application logic
│   └── Program.cs                        # Entry point
├── config/                               # Configuration files
│   ├── appsettings.json                 # App settings
│   └── mapping-rules.json               # Transaction mapping rules
├── data/                                # Sample data files
└── BankTransactionImporter.sln          # Solution file
```

## Example Output

```
info: BankTransactionImporter.Application[0]
      Starting Bank Transaction Importer
info: BankTransactionImporter.Application[0]
      Loaded 22 transactions from ../../data/Transaktioner_2025-10-01_17-03-34.csv
info: BankTransactionImporter.Application[0]
      Found transactions spanning 1 year(s): 2025
info: BankTransactionImporter.Application[0]
      Processing 22 transactions for year 2025 → sheet '2025'
info: BankTransactionImporter.Application[0]
      === RESULTS FOR SHEET '2025' ===
info: BankTransactionImporter.Application[0]
      === TRANSACTION MAPPING RESULTS ===
info: BankTransactionImporter.Application[0]
        Tele2 (09): ¤579.00 (1 transactions)
info: BankTransactionImporter.Application[0]
        A-kassa (09): ¤160.00 (1 transactions)
info: BankTransactionImporter.Application[0]
        Mat (09): ¤4,000.00 (1 transactions)
info: BankTransactionImporter.Application[0]
        Hyra (09): ¤9,657.00 (1 transactions)
info: BankTransactionImporter.Application[0]
        Inkomst (09): ¤32,505.00 (1 transactions)
info: BankTransactionImporter.Application[0]
      Dry run mode enabled for sheet '2025'. No changes will be made.
```

## How It Works

1. **Automatic Year Detection**: Transactions are automatically grouped by year and mapped to corresponding sheets (e.g., 2025 transactions → "2025" sheet)
2. **Month Column Mapping**: Each transaction is placed in the correct month column (01-12) based on the booking date
3. **Category Mapping**: Transactions are mapped to budget categories using configurable rules based on Reference or Description fields
4. **Batch Updates**: Multiple cell updates are batched together for optimal Google Sheets API performance
5. **Encoding Support**: Handles Swedish character encoding issues automatically

## Troubleshooting

### Common Issues

**Date parsing fails (year shows as 1):**
- Usually caused by CSV encoding issues with Swedish characters
- The app automatically handles `Bokf�ringsdag` variants
- Ensure your CSV uses standard Swedish bank export format

**Transactions not mapping to categories:**
- Check the mapping rules in `config/mapping-rules.json`
- Review the debug logs to see what reference/description values are being processed
- Add new mapping rules for unmapped transactions

**Google Sheets connection issues:**
- Verify `google-credentials.json` is in the correct location
- Check that the SpreadsheetId in `appsettings.json` is correct
- Use `dotnet run test-connection` to verify API access

## Future Enhancements

1. **Enhanced Mapping**: Add fuzzy matching and machine learning for better categorization
2. **Multi-file Processing**: Support batch processing of multiple CSV files  
3. **GUI Interface**: Consider creating a desktop or web interface
4. **Advanced Analytics**: Add spending analysis and reporting features

## Dependencies

- .NET 8.0
- CsvHelper (CSV parsing)
- Google.Apis.Sheets.v4 (Google Sheets API)
- Microsoft.Extensions.\* (Configuration, DI, Logging)
